# Guía Técnica para Desarrolladores - B1TuneUp

## Estructura del Proyecto

```
B1TuneUp/
├── Core/                      # Núcleo del sistema
│   ├── B1App.cs              # Singleton de conexión SAP
│   └── EventDispatcher.cs    # Gestor central de eventos
│
├── Models/                    # Modelos de datos
│   └── B1Rule.cs             # Definición de reglas
│
├── Modules/                   # Módulos funcionales
│   ├── MacroEngine.cs        # Motor de macros (734 líneas)
│   ├── UICustomizer.cs       # Personalización UI
│   ├── ValidationManager.cs  # Validaciones avanzadas
│   ├── SchedulerManager.cs   # Programador de tareas
│   ├── AuditLogManager.cs   # Sistema de auditoría
│   ├── ReportManager.cs      # Gestión de reportes
│   ├── EmailManager.cs       # Envío de emails
│   ├── IntegrationManager.cs # Integraciones REST/SOAP
│   ├── ToolboxManager.cs     # Herramientas generales
│   ├── DefaultValueManager.cs# Valores por defecto
│   ├── TemplateManager.cs    # Plantillas de documentos
│   ├── DashboardManager.cs   # Dashboards personalizados
│   ├── RightClickMenuManager.cs # Menús contextuales
│   ├── MenuManager.cs        # Menús principales
│   ├── ItemActionManager.cs  # Acciones por item
│   ├── ItemEditorManager.cs  # Editor de items
│   ├── ItemPlacementManager.cs# Posicionamiento items
│   ├── UIEnhancementsManager.cs# Mejoras de UI
│   ├── MandatoryFieldManager.cs # Campos obligatorios
│   ├── MasterDataManager.cs  # Datos maestros
│   ├── DynamicCodeEngine.cs  # Código dinámico
│   ├── DynamicMapperManager.cs # Mapeo dinámico
│   ├── ExchangeRateManager.cs # Tipos de cambio
│   ├── LetterMergeManager.cs # Combinación de cartas
│   ├── PLDExtensionsManager.cs# Extensiones PLD
│   ├── PrintDeliveryManager.cs# Impresión y entrega
│   ├── QueryExportManager.cs # Exportación de consultas
│   ├── RecurringInvoiceManager.cs # Facturas recurrentes
│   └── B1SearchManager.cs    # Búsqueda avanzada
│
├── Utils/                     # Utilidades
│   ├── Logger.cs             # Sistema de logging
│   ├── LocalizationManager.cs# Internacionalización
│   ├── SettingsManager.cs    # Configuraciones
│   ├── MetadataManager.cs    # Metadatos SAP
│   ├── ComObjectManager.cs   # Gestión de objetos COM
│   └── ... (otros helpers)
│
├── Resources/                 # Recursos estáticos
│   └── lang/                 # Archivos de idioma
│       ├── en.json
│       └── es.json
│
├── Modules/Forms/            # Formularios personalizados
│   ├── ItemActionsManagerForm.cs
│   ├── DesignSurfaceForm.cs
│   ├── LayoutManagerForm.cs
│   └── ... (13 formularios)
│
├── installer/                # Scripts de instalación
│   ├── wix/                  # Configuración WiX Toolset
│   ├── build-installer.ps1
│   ├── deploy.ps1
│   └── uninstall.ps1
│
├── Documentacion/           # Documentación del proyecto
│   ├── DOCUMENTACION_COMPLETA_B1TUNEUP.md
│   ├── GUIA_RAPIDA_B1TUNEUP.md
│   └── GUIA_TECNICA_B1TUNEUP.md (este archivo)
│
├── B1TuneUp.csproj         # Proyecto principal
├── B1TuneUp.sln            # Solución Visual Studio
└── Program.cs              # Punto de entrada
```

---

## Arquitectura Técnica

### Patrón de Diseño Principal

**Singleton + Event Dispatcher**

```csharp
// Core/B1App.cs - Singleton
public class B1App {
    private static B1App _instance;
    public static B1App Instance => _instance ?? (_instance = new B1App());

    public SAPbouiCOM.Application Application { get; private set; }
    public SAPbobsCOM.Company Company { get; private set; }
}

// Core/EventDispatcher.cs - Dispatcher
public class EventDispatcher {
    private static EventDispatcher _instance;
    public static EventDispatcher Instance => _instance ?? (_instance = new EventDispatcher());

    public void Init() {
        B1App.Instance.Application.ItemEvent += OnItemEvent;
        B1App.Instance.Application.MenuEvent += OnMenuEvent;
        // ... más eventos
    }
}
```

### Flujo de Eventos

```
Evento SAP B1
    ↓
EventDispatcher.OnItemEvent()
    ↓
Filtrar reglas matching (FormType, EventType, BeforeAction)
    ↓
MacroEngine.CheckCondition(conditionSQL, form)
    ↓
Si condición = TRUE:
    - AuditLogManager.LogAction()
    - MacroEngine.ExecuteMacro(action, form)
    ↓
Ejecutar comandos de macro
    ↓
Retornar BubbleEvent (true/false)
```

---

## Componentes Críticos

### 1. MacroEngine (734 líneas)

**Responsabilidad**: Interpretar y ejecutar comandos de macro

#### Métodos Principales

```csharp
public static void ExecuteMacro(string macroCommand, Form activeForm = null, int rowOverride = -1)
private static void ProcessCommand(string command, Form activeForm, int rowOverride)
private static bool CheckCondition(string sql, Form activeForm)
private static string ProcessSqlVariables(string input, Form activeForm, int rowOverride)
```

#### Extensibilidad

Para agregar nuevos comandos:

```csharp
// En ProcessCommand(), agregar nuevo caso:
else if (command.StartsWith("NuevoComando(")) {
    string param = ExtractParameter(command, "NuevoComando");
    // Implementación personalizada
}
```

### 2. UICustomizer (240 líneas)

**Responsabilidad**: Modificar UI de formularios SAP

#### Métodos Clave

```csharp
public static void ApplyCustomization(SAPbouiCOM.Form oForm)
public static void AddButton(...)
public static void AddFolder(...)
public static void AddEditText(...)
public static void HideItem(...)
public static void MoveItem(...)
```

#### Base de Datos

Lee de `@BTUN_UI`:

- `U_FormType`: Tipo de formulario
- `U_Action`: Acción a realizar (Hide, Move, Add, etc.)
- `U_ItemID`: Item objetivo
- `U_Label`, `U_Top`, `U_Left`, etc.: Parámetros

### 3. ValidationManager (1142 líneas)

**Responsabilidad**: Implementar validaciones de negocio

#### Tipos de Validación

1. **FormDataEvent** (et_FORM_DATA_ADD/UPDATE)

   - Se ejecuta antes de guardar
   - Puede bloquear operación (BubbleEvent = false)

2. **ItemEvent** (et_VALIDATE, et_COMBO_SELECT)

   - Se ejecuta al cambiar campos
   - Validación en tiempo real

3. **Form Load** (et_FORM_LOAD)
   - Configura validaciones al cargar

### 4. EventDispatcher (308 líneas)

**Responsabilidad**: Centralizar y distribuir eventos

#### Eventos Interceptados

```csharp
B1App.Instance.Application.ItemEvent += OnItemEvent;
B1App.Instance.Application.MenuEvent += OnMenuEvent;
B1App.Instance.Application.AppEvent += OnAppEvent;
B1App.Instance.Application.FormDataEvent += OnFormDataEvent;
B1App.Instance.Application.RightClickEvent += OnRightClickEvent;
B1App.Instance.Application.LayoutKeyEvent += OnLayoutKeyBefore;
```

#### Handlers Locales

Sistema de registro de handlers por formulario/item:

```csharp
public void RegisterLocalItemChangeHandler(Form form, string itemId, Action<Form, string> handler)
public void UnregisterLocalItemChangeHandler(Form form, string itemId)
```

---

## Base de Datos - Tablas Personalizadas

### @BTUN_RULES (Reglas de Negocio)

```sql
CREATE TABLE [@BTUN_RULES] (
    DocEntry INT PRIMARY KEY,
    Code NVARCHAR(50),
    U_FormType NVARCHAR(10),      -- Ej: '139', '149'
    U_Type NVARCHAR(20),          -- 'Macro', 'Validation', 'UICustomization'
    U_EventType NVARCHAR(30),     -- 'et_ITEM_CLICK', 'et_FORM_LOAD'
    U_Before CHAR(1),             -- 'Y' o 'N'
    U_Condition NVARCHAR(MAX),    -- SQL condition
    U_Action NVARCHAR(MAX)        -- Macro commands
)
```

### @BTUN_UI (Personalización UI)

```sql
CREATE TABLE [@BTUN_UI] (
    DocEntry INT PRIMARY KEY,
    Code NVARCHAR(50),
    U_FormType NVARCHAR(10),
    U_Action NVARCHAR(20),        -- 'Hide', 'Move', 'AddButton', etc.
    U_ItemID NVARCHAR(50),
    U_Label NVARCHAR(100),
    U_Top INT,
    U_Left INT,
    U_Width INT,
    U_Height INT
)
```

### @BTUN_LOG (Auditoría)

```sql
CREATE TABLE [@BTUN_LOG] (
    DocEntry INT PRIMARY KEY,
    U_Date DATETIME,
    U_Type NVARCHAR(50),         -- 'RuleExecution', 'ValidationRule', etc.
    U_Details NVARCHAR(MAX),
    U_Status NVARCHAR(20),       -- 'Success', 'Error'
    U_User NVARCHAR(50)
)
```

### @BTUN_SCHED (Programador)

```sql
CREATE TABLE [@BTUN_SCHED] (
    DocEntry INT PRIMARY KEY,
    Code NVARCHAR(50),
    U_Name NVARCHAR(100),
    U_Action NVARCHAR(MAX),
    U_Interval INT,              -- Minutos
    U_LastRun DATETIME,
    U_Active CHAR(1)
)
```

### @BTUN_TBOX (Toolbox Settings)

```sql
CREATE TABLE [@BTUN_TBOX] (
    DocEntry INT PRIMARY KEY,
    Code NVARCHAR(50),
    U_Value NVARCHAR(MAX)
)
-- Índices: SMTP_Server, PERIOD_LOCK, GENERAL_VALIDATION, etc.
```

---

## Patrones de Desarrollo

### 1. Manejo de Objetos COM

**Patrón**: Usar `ComObjectManager.Release()`

```csharp
Recordset rs = null;
try {
    rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
    rs.DoQuery(sql);
    // Procesar datos
} finally {
    ComObjectManager.Release(rs);  // CRÍTICO: Evitar memory leaks
}
```

### 2. Soporte HANA/SQL Server

**Patrón**: Verificar `B1App.Instance.IsHana`

```csharp
string sql = B1App.Instance.IsHana
    ? "SELECT * FROM \"@BTUN_RULES\" WHERE \"U_FormType\" = '139'"
    : "SELECT * FROM [@BTUN_RULES] WHERE [U_FormType] = '139'";
```

### 3. Localización (i18n)

**Patrón**: Usar `LocalizationManager`

```csharp
// Resources/lang/es.json
{
  "B1TuneUp.Connected": "B1TuneUp: Conectado con éxito"
}

// Código
string msg = LocalizationManager.GetString("B1TuneUp.Connected");
```

### 4. Logging Defensivo

**Patrón**: Try-catch con logging silencioso

```csharp
try {
    // Operación potencialmente fallida
} catch (Exception ex) {
    Logger.Error("Descripción del error", ex);
    // No propagar, continuar ejecución
}
```

### 5. Inicialización Perezosa (Lazy Loading)

**Patrón**: Singleton con inicialización diferida

```csharp
private static EventDispatcher _instance;
public static EventDispatcher Instance => _instance ?? (_instance = new EventDispatcher());
```

---

## Extensión del Sistema

### Agregar Nuevo Módulo

**Paso 1**: Crear clase en `Modules/`

```csharp
namespace B1TuneUp.Modules
{
    public static class NuevoModulo
    {
        public static void Initialize() {
            // Inicialización
        }

        public static void Procesar(string parametro) {
            // Lógica
        }
    }
}
```

**Paso 2**: Registrar en `EventDispatcher` constructor

```csharp
private EventDispatcher() {
    _rules = new List<B1Rule>();
    LoadRules();
    MenuManager.LoadCustomMenus();
    ToolboxManager.ApplyToolboxSettings();
    NuevoModulo.Initialize();  // <-- Agregar aquí
}
```

### Agregar Nuevo Comando de Macro

**Paso 1**: Editar `MacroEngine.cs`

```csharp
else if (command.StartsWith("MiComando(")) {
    string param = ExtractParameter(command, "MiComando");
    param = ProcessSqlVariables(param, activeForm, rowOverride);

    // Implementación
    B1App.Instance.Application.SetStatusBarMessage(
        $"Ejecutando MiComando: {param}",
        BoMessageTime.bmt_Short, false
    );
}
```

**Paso 2**: Documentar en referencia de macros

### Agregar Nueva Validación

**Paso 1**: Crear método en `ValidationManager.cs`

```csharp
public static bool ValidarNuevoCampo(Form form, string itemId) {
    try {
        // Lógica de validación
        return true;
    } catch (Exception ex) {
        Logger.Error("Error en validación", ex);
        return false;
    }
}
```

**Paso 2**: Llamar desde `EventDispatcher.OnItemEvent`

```csharp
if (pVal.EventType == BoEventTypes.et_VALIDATE && !pVal.BeforeAction) {
    ValidationManager.ValidarNuevoCampo(oForm, pVal.ItemUID);
}
```

---

## Debugging y Troubleshooting

### Logs Disponibles

1. **Logger Interno** (`Utils/Logger.cs`)

   ```csharp
   Logger.Info("Mensaje informativo")
   Logger.Warn("Advertencia")
   Logger.Error("Error crítico", exception)
   ```

2. **Tabla @BTUN_LOG**

   ```sql
   SELECT TOP 100 * FROM [@BTUN_LOG] ORDER BY DocEntry DESC
   ```

3. **StatusBar de SAP**
   ```csharp
   B1App.Instance.Application.SetStatusBarMessage("Mensaje", BoMessageTime.bmt_Short, true)
   ```

### Técnicas de Debug

**1. Breakpoints Condicionales**

```csharp
// En EventDispatcher.OnItemEvent
if (pVal.FormTypeEx == "139" && pVal.ItemUID == "btnOK") {
    System.Diagnostics.Debugger.Break();  // Breakpoint programático
}
```

**2. Logging Detallado Temporal**

```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// Operación costosa
stopwatch.Stop();
Logger.Info($"Operación tomó {stopwatch.ElapsedMilliseconds}ms");
```

**3. Variables Watch en Visual Studio**

Agregar watches:

- `B1App.Instance.Application.Forms.Count`
- `((SAPbouiCOM.EditText)oForm.Items.Item("CardCode").Specific).Value`
- `_rules.Count`

### Problemas Comunes

**Memory Leak de Objetos COM**

Síntoma: SAP se pone lento después de horas de uso

Solución:

```csharp
// MAL
var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
rs.DoQuery(sql);
// Objeto nunca liberado

// BIEN
Recordset rs = null;
try {
    rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
    rs.DoQuery(sql);
} finally {
    Marshal.ReleaseComObject(rs);
}
```

**Condición Siempre Falsa**

Causa: Variables mal formateadas

Debug:

```csharp
string processedSql = ProcessSqlVariables(sql, activeForm);
Logger.Info($"SQL procesado: {processedSql}");
```

**Macro No Se Ejecuta**

Verificar:

1. Regla existe en BD: `SELECT * FROM [@BTUN_RULES] WHERE ...`
2. FormType coincide exactamente
3. EventType correcto (et_ITEM_CLICK vs et_ITEM_PRESSED)
4. BeforeAction flag correcto

---

## Performance y Optimización

### Mejores Prácticas

**1. Cachear Consultas Frecuentes**

```csharp
// MAL: Consulta en cada evento
private void OnItemEvent(...) {
    var rules = GetRulesFromDB();  // Query a BD cada vez
}

// BIEN: Cachear en memoria
private List<B1Rule> _cachedRules;
private void LoadRules() {
    _cachedRules = LoadFromDatabase();
}
```

**2. Filtrar Temprano**

```csharp
// MAL: Traer todo y filtrar en código
var allRules = GetAllRules();
var filtered = allRules.Where(r => r.FormType == formType);

// BIEN: Filtrar en SQL
string sql = $"SELECT * FROM [@BTUN_RULES] WHERE U_FormType = '{formType}'";
```

**3. Evitar Loops Anidados**

```csharp
// MAL: O(n²)
foreach (var rule in rules) {
    foreach (var form in forms) {
        // Procesar
    }
}

// MEJOR: Usar Dictionary para lookup O(1)
var rulesByForm = rules.GroupBy(r => r.FormType)
                       .ToDictionary(g => g.Key, g => g.ToList());
```

**4. Liberar Recursos Explícitamente**

```csharp
// Implementar IDisposable en clases que usen muchos COM objects
public class MiClase : IDisposable {
    private Recordset rs;

    public void Dispose() {
        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            rs = null;
        }
    }
}
```

### Métricas de Performance

Objetivos:

- ItemEvent: < 50ms
- FormDataEvent: < 100ms
- Macro simple: < 10ms
- Loop matrix (100 filas): < 500ms

Tools:

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
// Código a medir
sw.Stop();
Logger.Info($"Tiempo: {sw.ElapsedMilliseconds}ms");
```

---

## Testing

### Estrategia de Tests

**Unit Testing (Desafiante por dependencias SAP)**

Mocking de objetos SAP:

```csharp
// Usar interfaces para permitir mocking
public interface ISBOApplication {
    SAPbouiCOM.Application GetApplication();
}

// Test unitario
[Test]
public void MacroEngine_Debe_Ejecutar_Msg() {
    var mockApp = new Mock<ISBOApplication>();
    var engine = new MacroEngine(mockApp.Object);
    engine.ExecuteMacro("Msg('test')");
    // Verificar
}
```

**Integration Testing**

Casos de prueba manuales:

1. **Crear regla básica**

   - Insertar en [@BTUN_RULES]
   - Abrir formulario
   - Verificar ejecución

2. **Validar condicional**

   - Configurar condición SQL
   - Probar con datos que cumplen/no cumplen
   - Verificar comportamiento

3. **Test de carga**
   - 100 reglas cargadas
   - Medir tiempo de inicio
   - Medir tiempo por evento

### Checklist de QA

Antes de release:

- [ ] Todos los módulos compilan sin warnings
- [ ] No hay memory leaks (usar profiler)
- [ ] Funciona en HANA y SQL Server
- [ ] Logs no muestran errores críticos
- [ ] Performance dentro de objetivos
- [ ] Documentación actualizada
- [ ] Version number incrementado

---

## Seguridad

### Credenciales

**NUNCA hardcodear:**

```csharp
// ❌ MAL
string password = "admin123";

// ✅ BIEN
string password = SettingsManager.GetSetting("SMTP_Password", "");
```

### SQL Injection

**Usar parámetros cuando sea posible:**

```csharp
// ❌ Riesgoso si input no está sanitizado
string sql = $"SELECT * FROM OCRD WHERE CardCode = '{userInput}'";

// ✅ Mejor (aunque DI API no soporta parameters nativamente)
string sanitizedInput = userInput.Replace("'", "''");
string sql = $"SELECT * FROM OCRD WHERE CardCode = '{sanitizedInput}'";
```

### Permisos de Usuario

**Validar autorización:**

```csharp
public static bool UserHasPermission(string permission) {
    string userId = B1App.Instance.Company.UserName;
    // Verificar en tabla de permisos
    return true; // Simplificado
}

// Uso
if (!UserHasPermission("DELETE_CUSTOMER")) {
    Msg('No tiene permisos para eliminar clientes');
    Stop();
}
```

---

## Deploy y CI/CD

### Build Script

`build.ps1`:

```powershell
# Compilar en Release
msbuild B1TuneUp.sln /p:Configuration=Release /t:Rebuild

# Ejecutar tests
vstest.console.exe B1TuneUp.Tests.dll

# Crear installer
.\installer\build-installer.ps1
```

### Deployment Automatizado

`deploy.ps1`:

```powershell
param(
    [string]$TargetPath = "C:\Program Files\SAP\SAP Business One\"
)

# Detener addon si está corriendo
Stop-Process -Name "B1TuneUp" -Force -ErrorAction SilentlyContinue

# Copiar archivos
Copy-Item -Path ".\bin\Release\*.*" -Destination $TargetPath -Recurse -Force

# Registrar DLL (si aplica)
# regasm B1TuneUp.dll /codebase

Write-Host "Deploy completado exitosamente"
```

### Versionado

Semántico: `MAJOR.MINOR.PATCH`

- **MAJOR**: Cambios incompatibles
- **MINOR**: Nuevas features (compatibles)
- **PATCH**: Bug fixes

Convención de tags Git:

```bash
git tag -a v1.2.3 -m "Versión 1.2.3 - Mejoras en validaciones"
git push origin v1.2.3
```

---

## Recursos para Desarrolladores

### Documentación Oficial

- SAP B1 SDK: `Documentacion/SAP Business One SDK developer guide.pdf`
- Working with SAP B1 Studio Suite: PDF incluido
- Boyum IT B1UP Reference: PDFs en `Documentacion/`

### Herramientas Recomendadas

- **Visual Studio 2019/2022** con herramientas SAP
- **SQL Server Management Studio** o **HANA Studio**
- **WiX Toolset** para installers
- **JetBrains dotPeek** para decompilar y debuggear

### Comunidades

- SAP Community Network (SCN): https://answers.sap.com/tags/73554900100800001360
- Boyum IT Forum: https://forum.boyum-it.com/
- Stack Overflow: Tag `sap-business-one`

---

## Contribuciones

### Cómo Contribuir

1. Fork del repositorio
2. Crear branch feature: `git checkout -b feature/nueva-funcionalidad`
3. Commit cambios: `git commit -m 'Add: nueva funcionalidad'`
4. Push: `git push origin feature/nueva-funcionalidad`
5. Pull Request

### Estándares de Código

- Usar C# 8.0+
- Seguir convenciones de naming .NET
- Comentarios en inglés o español (consistente)
- Máximo 120 caracteres por línea
- Methods < 50 líneas
- Classes < 500 líneas

### Code Review Checklist

- [ ] Código sigue estándares del proyecto
- [ ] Tests agregados (si corresponde)
- [ ] Documentación actualizada
- [ ] No breaking changes
- [ ] Performance considerado
- [ ] Manejo de errores adecuado
- [ ] Logging implementado

---

**Última actualización**: Marzo 2026  
**Versión**: 1.0  
**Mantenimiento**: Equipo de Desarrollo B1TuneUp

© 2026 B1TuneUp - Licencia MIT para uso interno
