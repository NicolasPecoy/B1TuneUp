# Documentación Completa de B1TuneUp (versión abril 2026)

## 1. Introducción

**B1TuneUp** es el add-on que replica y amplía las capacidades del Boyum B1 Usability Package para SAP Business One, ahora con una suite completa de **estudios WPF** con estética SAP Business One (tema SQL/HANA automático). Cada estudio puede abrirse desde el menú de B1TuneUp y permite configurar procesos complejos sin abandonar el cliente SAP.

### 1.1 Objetivos actualizados

- **Centralizar configuración**: Todos los motores (Integración, Scheduler, MacroEngine, UI, Validaciones, Reportes, Email, ToolBox) cuentan con un estudio WPF coherente.
- **Automatizar sin código**: Macros, workflows y despliegues se configuran con asistentes visuales. Cuando se necesita lógica avanzada se expone `InvokeHandler` para invocar clases personalizadas por reflection.
- **Garantizar consistencia visual**: Cada estudio detecta si la compañía está en SQL Server u HANA y aplica la paleta/cromática correspondiente.
- **Mantener trazabilidad**: Todo cambio queda registrado en `@BTUN_LOG`, visible desde el nuevo Audit Log Studio.

---

## 2. Arquitectura del Sistema

```
+------------------------------+
¦         Clientes SAP         ¦
¦  (WPF hosteado + SAP UI API) ¦
+------------------------------+
               ¦
        +------?------+
        ¦  B1TuneUp   ¦  Núcleo .NET 4.8
        ¦  Core DLL   ¦  (B1App, EventDispatcher,
        +-------------+   MacroEngine, Localization)
               ¦
     +---------?---------+
     ¦  Módulos / Servicios¦  (IntegrationService, SchedulerService,
     ¦  (carpeta Modules) ¦   UiDesignerService, etc.)
     +-------------------+
               ¦
        +------?------+
        ¦ SAP SDK DI  ¦  (Company, Recordset, Documents)
        ¦ SAP SDK UI  ¦  (Application, Form, Items)
        +-------------+
```

### 2.1 Componentes clave

- **WPF Shell**: `ShellWindow` aplica tema SAP B1 con diccionario de recursos compartido (`WindowBackgroundBrush`, `TextPrimaryBrush`, etc.) y carga los ViewModels.
- **Servicios**: Cada estudio expone un `*Service` para CRUD sobre las tablas `@BTUN_*` y los catálogos auxiliares.
- **MenuManager**: Asegura que cada estudio tenga su entrada en el menú de SAP y dispara el launcher adecuado (por ejemplo `DashboardSearchMacroLauncher`).
- **InvokeHandler Runtime**: MacroEngine puede cargar clases externas usando reflection para extender comportamientos.

### 2.2 Flujo WPF ? SAP

1. MenuManager registra los menús en SAP (localizados vía `LocalizationManager` > `Menu.*`).
2. Al hacer clic en un menú se invoca el launcher correspondiente (`IntegrationConfiguratorLauncher`, `UiCustomizerLauncher`, etc.).
3. El launcher evalúa si ya existe una ventana y, si no, crea el `Window` WPF y asigna el ViewModel.
4. Los ViewModels trabajan con servicios específicos y con el `Application` de SAP para leer formulario activo cuando aplica (por ejemplo Item Placement).
5. Toda operación registra historial en `@BTUN_LOG` y puede disparar macros o invocar `InvokeHandler` según corresponda.

---

## 3. Instalación y Configuración Paso a Paso

1. **Requisitos**
   - SAP Business One 9.2 PLxx o superior (SQL u HANA).
   - .NET Framework 4.8.
   - Usuario con permisos para crear UDT/UDF.
2. **Preparar entorno**
   - Descargar el paquete desde el repositorio o build interno.
   - Validar que el cliente SAP esté cerrado antes de copiar DLLs.
3. **Instalar**
   ```powershell
   # Ejecutar como administrador
   .\installer\deploy.ps1 -Target "C:\Program Files\SAP\SAP Business One"
   ```
4. **Registrar add-on**
   - Desde el Add-On Administration de SAP, cargar el `.ard` generado y asignarlo a cada compañía.
5. **Ejecutar inicialización**
   - Al primer arranque B1TuneUp crea las tablas `@BTUN_*`. Si alguna compañía falla, correr `MetadataManager.SetupMetadata();` desde el modo developer.
6. **Configurar idioma**
   - Abrir **Módulos > B1TuneUp > Configuración** y seleccionar Español por defecto (carga `Resources/lang/es.json`).
7. **Configurar SMTP y credenciales**
   - En el estudio **Toolbox / Settings** registrar SMTP, rutas compartidas y flags globales.
8. **Verificar menús**
   - Abrir SAP > Módulos > B1TuneUp y confirmar que aparecen los menús de los estudios (Integration Studio, Scheduler Studio, etc.).
9. **Checklist post instalación**
   - Ejecutar el Integration Studio en modo solo lectura para validar conexión.
   - Abrir Automation Dashboard y sincronizar menús.
   - Crear una macro simple y dispararla desde un botón agregado con UiCustomizer para comprobar `InvokeHandler`.

---

## 4. Estudios WPF y Paneles Especializados

### 4.1 Mapa general

| Estudio WPF | Propósito | Menú SAP |
|-------------|-----------|----------|
| Integration Studio | Conectar APIs REST/SOAP, definir escenarios, credenciales y pruebas en vivo. | `B1TuneUp > Integración > Configurador` |
| Scheduler / SchedulerManager | Programar macros, DLLs o integraciones con agendas por ambiente. | `B1TuneUp > Automatizaciones > Scheduler Studio` |
| MacroEngine & Rule Builder | Diseñar reglas condicionales, macros y eventos con vista árbol + tarjetas. | `B1TuneUp > Automatizaciones > MacroEngine Studio` |
| UiCustomizer & Item Placement | Editar layouts SAP (SRF) y aplicar mejoras visuales, drag & drop y helpers. | `B1TuneUp > UI > Item Placement & Enhancer` |
| Process Designer / Workflow | Modelar ProcessSteps, estados y responsables con canvas drag & drop. | `B1TuneUp > Procesos > Process Designer` |
| Automation Dashboard (MenuManager + MacroEngine) | Gobernar menús personalizados, macros activos y despliegues entre ambientes. | `B1TuneUp > Automatizaciones > Automation Dashboard` |
| Dashboard / Search / Macro Studio | Configurar dashboards KPI, búsquedas globales y macros reutilizables. | `B1TuneUp > Analítica > Dashboard/Search/Macro` |
| Validation & Mandatory Fields | Reglas de validación, campos obligatorios dinámicos y mensajes localizados. | `B1TuneUp > Calidad de Datos > Validation Studio` |
| Form Enhancements (FormSettings/DefaultValue/LockField) | Valores por defecto, bloqueo de campos y reglas de presentación avanzadas. | `B1TuneUp > UI > Form Enhancements Studio` |
| Template & Report Studio | Gestionar plantillas Crystal/PDF y despliegue por sucursal. | `B1TuneUp > Documentos > Template & Report Studio` |
| Email / Notification Designer | Diseñar notificaciones HTML con vista previa y pruebas SMTP. | `B1TuneUp > Comunicaciones > Email Designer` |
| Action Pad / Quick Copy / Item Actions | Crear paneles de acción, plantillas de copiado y botones personalizados. | `B1TuneUp > UX > Action Pad Studio` |
| Audit Log / Log Viewer | Filtrar y exportar los registros de `@BTUN_LOG`. | `B1TuneUp > Auditoría > Visor de Logs` |
| Toolbox / Settings | Ajustes globales del add-on, flags por compañía/entidad. | `B1TuneUp > Configuración > Toolbox Settings` |
| DashboardSearchMacro Launcher | (Nuevo) acceso directo con tres columnas para dashboards, búsquedas y macros globales. | `B1TuneUp > Analítica > Dashboard/Search/Macro Studio` |

Cada estudio comparte el mismo layout: **columna izquierda** (filtros/listado), **columna central** (edición), **columna derecha** (resumen/acciones rápidas) y un overlay de progreso.

### 4.2 Integration Studio (SAP SQL/HANA)

1. **Seleccionar ambiente** (SQL u HANA) en la columna izquierda para cargar conexiones y variables.
2. **Crear o clonar un escenario** desde la grilla central; definir origen/destino, método (REST/SOAP/DB) y credenciales.
3. **Editar el pipeline** usando las tarjetas de pasos: transformación, validación y confirmación.
4. **Probar** con el botón "Ejecutar prueba" que abre el panel lateral con request/response y logging.
5. **Agendar** el escenario asociándolo a Scheduler Studio o ejecutarlo via MacroEngine (comando `RunIntegration('Code')`).

### 4.3 Scheduler / SchedulerManager

- **Vista Jobs**: listado de tareas con estado, próxima ejecución y última corrida.
- **Paso a paso**:
  1. Click en "Nuevo Job".
  2. Elegir tipo (Macro, DLL externa, Integration Scenario).
  3. Definir trigger (minutos/cron, compañía y ambiente).
  4. Asignar notificación (email o log).
  5. Guardar y usar el botón "Probar ahora" para ejecutar sin esperar el cron.

### 4.4 MacroEngine & Rule Builder Studio

- **Panel izquierdo**: árbol por FormType/EventType.
- **Panel central**: editor con resaltado, plantillas y soporte para `InvokeHandler`.
- **Panel derecho**: historial de ejecuciones y resultados.
- **Ejemplo paso a paso**:
  1. Selecciona `Formulario 149 > et_ITEM_PRESSED`.
  2. Presiona **"Agregar Regla"** y define condición (por ejemplo cliente con deuda).
  3. Inserta macro base con snippet `InvokeHandler`.
  4. Guarda y usa "Probar" para ejecutar contra el formulario activo de SAP.

### 4.5 UiCustomizer & Item Placement / UI Enhancements

- **Columna izquierda**: biblioteca de layouts guardados en `@BTUN_LAYOUT` (filtros por FormType, autor, versión).
- **Columna central**: visor de estructura y herramientas (mover, redimensionar, añadir tabs, agregar botones).
- **Columna derecha**: helpers (Drag & Drop, Editor enriquecido, UI Enhancements, Quick Preview SQL).
- **Flujo recomendado**:
  1. Haz clic en "Capturar formulario activo" para traer la estructura del formulario abierto en SAP.
  2. Ajusta items usando la grilla (posiciones, tamaño, binding).
  3. Usa "Añadir botón" indicando `ItemUID` y macro asociada.
  4. Guarda el layout como versión nueva.
  5. Desde la tarjeta de acciones aplica el layout al formulario actual.

> **Referente B1UP / Beas**: El *Item Placement Tool* de B1UP y las extensiones de Beas permiten hacer clic derecho sobre el formulario vivo, mover/ocultar campos in-place, añadir pestañas, definir campos obligatorios y empaquetar los cambios por usuario o localización. citeturn2search5

> **Brecha actual en B1TuneUp**: Nuestro diseñador WPF tabulado (`UiCustomizerWindow.xaml:213`) sigue requiriendo ingresar FormType/ItemID manualmente, no tiene gancho directo “Editar con TuneUp” dentro del formulario de SAP ni herencia por usuario/rol (`UiCustomizerViewModel.cs:127`). Tampoco existe integración nativa con módulos como Function Buttons o Validation Studio para compartir reglas.

> **Plan de cierre**:
> - Inyectar un overlay dentro de SAP (botón contextual “Editar con TuneUp”) que capture coordenadas reales y permita drag & drop.
> - Guardar variantes por usuario/rol/localización con herencia y despliegue empaquetado (import/export con dependencias).
> - Sincronizar con Function Buttons, Validation y Action Pad para que los botones agregados desde UiCustomizer ya conozcan macros/validaciones relacionadas.

### 4.6 Process Designer / Workflow

- Permite modelar procesos completos con pasos, transiciones y responsables.
- **Pasos**:
  1. Arrastra un template (Inicio, Paso Manual, Paso Automático) al canvas.
  2. Configura propiedades (FormType, macro asociada, responsable, SLA).
  3. Conecta nodos con flechas para definir el flujo.
  4. Publica la versión y pruébala disparando un caso desde SAP.

### 4.7 Automation Dashboard (MenuManager + MacroEngine)

- Columnas: **Entornos**, **Menús/Macros activos**, **Despliegue/Test**.
- **Uso**:
  1. Selecciona entorno (DEV, QA, PROD) para ver menús custom y macros asignadas.
  2. Marca qué elementos quieres promulgar.
  3. Ejecuta "Deploy" para replicar en otra compañía (usa los servicios de MenuManager y MacroEngine).
  4. Usa "Test" para abrir SAP y validar que el menú abre el estudio correcto.

### 4.8 Dashboard / Search / Macro Studio

- Diseña dashboards SQL, configuraciones de búsqueda global y macros compartidas.
- **Pasos**:
  1. Seleccionar pestaña (Dashboard, Search, Macro).
  2. Crear tarjeta (ej. KPI "Ventas del día").
  3. Definir query y parámetros.
  4. Guardar y probar en tiempo real dentro del panel derecho.

### 4.9 Validation & Mandatory Fields Studio

- Mantiene reglas de validación y campos obligatorios basada en condiciones.
- **Paso a paso**:
  1. Filtra por FormType.
  2. Agrega regla y define evento (Before Add, Before Update, etc.).
  3. Especifica condición SQL usando variables `$[$Item.Col.Row]`.
  4. Define acción (mensaje localizado, bloqueo, corrección automática).
  5. Guarda y usa "Test" contra formulario activo.

### 4.10 Form Enhancements Studio (FormSettingsManager + DefaultValueManager + LockFieldManager)

- Tres tarjetas:
  - **Form Settings**: layout y pestañas.
  - **Default Values**: expresiones OnLoad/OnChange.
  - **Lock Rules**: bloqueo dinámico.
- **Ejemplo**:
  1. Selecciona FormType 149.
  2. En Default Values crea regla "DocDate = Fecha actual" (selector de función).
  3. En Lock Rules bloquea `Price` si el usuario pertenece al rol Ventas Jr.
  4. Publica y sincroniza con UiCustomizer para que aparezca en SAP.

### 4.11 Template & Report Studio

1. Arrastra un archivo `.rpt` o `.pdf` en la tarjeta "Repositorio".
2. Define metadata (nombre, compañía, sucursal, parámetros).
3. Usa "Vista previa" con datos reales.
4. Asigna atajo de menú o botón.

### 4.12 Email / Notification Designer

- Editor HTML con vista previa y modo "Plantilla" + "Notificación".
- **Flujo**:
  1. Configura plantilla (destinatarios, asunto, cuerpo) con variables.
  2. Adjunta archivos o reportes generados.
  3. Usa "Enviar prueba" (toma configuración SMTP del Toolbox).
  4. Publica y vincula a macros o Scheduler.

### 4.13 Action Pad / Quick Copy / Item Actions

- **Action Pad**: Diseña paneles con botones y grupos.
- **Quick Copy**: Define qué campos copiar al duplicar documentos.
- **Item Actions**: Asigna botones a macros o `InvokeHandler`.
- **Ejemplo**:
  1. Desde la pestaña Action Pad crea un layout y arrastra botones.
  2. Asocia cada botón a una macro o `InvokeHandler`.
  3. En Quick Copy configura plantillas para copiar pedidos.
  4. En Item Actions vincula el botón agregado desde UiCustomizer.

### 4.14 Audit Log / Log Viewer

- Filtros por fecha, usuario, tipo y severidad.
- Botones de exportación CSV y "Abrir en Excel".
- Permite saltar al registro en SAP o copiar detalles para soporte.

### 4.15 Toolbox / Settings Studio

- Gestiona parámetros globales (`@BTUN_TBOX`).
- Incluye conmutadores para features (habilitar Automation Dashboard, forzar español por defecto, etc.).
- Paso a paso: selecciona sucursal o compañía, modifica valores y pulsa "Guardar" para replicar en todas.

### 4.16 DashboardSearchMacro Launcher (nuevo WPF)

- Tres columnas: **Dashboard**, **Search**, **Macro**.
- Permite editar widgets, búsquedas con filtros y macros globales desde un solo lugar.
- Accesible vía `DashboardSearchMacroLauncher` y con cadena localizada `Menu.DashboardSearchMacroStudio`.

---

## 5. InvokeHandler y extensibilidad avanzada

`InvokeHandler` permite delegar lógica a clases .NET externas sin recompilar B1TuneUp.

```csharp
// MacroEngine
InvokeHandler(
    'B1TuneUp.CustomLogic.LogisticsHandler',
    'Enviar',
    '$[$38.0.0]|NA|PRIORIDAD_ALTA',
    'B1TuneUp.CustomLogic.dll');
```

### Cómo funciona

1. MacroEngine resuelve el tipo indicado (cargando la DLL si el cuarto parámetro está presente).
2. Instancia la clase (o usa método estático) y busca el método indicado (por defecto `Execute`).
3. Inyecta parámetros automáticamente si el método declara:
   - `SAPbouiCOM.Application`
   - `SAPbouiCOM.Form`
   - `SAPbobsCOM.Company`
   - `string payload` o `string[] args`
   - `int row`
4. Cualquier excepción se registra en `@BTUN_LOG` y se muestra en la barra de estado.

### Ejemplo paso a paso para ligar un botón a InvokeHandler

1. **Crear botón** desde UiCustomizer (ItemID `btnLogistica`).
2. **Registrar acción** en Item Actions Studio apuntando a una macro que llame `InvokeHandler`.
3. **Desarrollar handler** en un proyecto separado:
   ```csharp
   public class LogisticsHandler
   {
       public void Enviar(SAPbouiCOM.Form form, string[] args, SAPbobsCOM.Company company)
       {
           var docEntry = int.Parse(args[0]);
           var orders = (SAPbobsCOM.Documents)company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
           if (orders.GetByKey(docEntry))
           {
               orders.UserFields.Fields.Item("U_LogisticsStatus").Value = "READY";
               orders.Update();
               B1App.Instance.Application.SetStatusBarMessage($"Pedido {docEntry} enviado", SAPbouiCOM.BoMessageTime.bmt_Short, false);
           }
       }
   }
   ```
4. **Copiar DLL** al directorio del add-on o registrarla en Toolbox > Paths.
5. **Probar** desde el Action Pad o el botón en SAP.

---

## 6. Guías paso a paso por escenario

### 6.1 Crear una integración REST y agendarla

1. Abra **Integration Studio** y cliquee "Nuevo".
2. Complete los datos de la API (URL, headers, autenticación, body). Use el probador integrado.
3. Guarde el escenario como `CRM_ClientSync`.
4. Desde la tarjeta "Programar" haga clic en "Enviar a Scheduler" y elija periodicidad diaria 08:00.
5. Verifique en **Scheduler Studio** que el job quedó activo y ejecute una prueba inmediata.

### 6.2 Rediseñar un formulario y vincular macros

1. Abra el formulario SAP (por ejemplo Pedido de Venta) y luego **Item Placement & UI Enhancer**.
2. Capture el formulario activo, reubique campos y agregue el botón "Autorizar".
3. Guarde layout versión 2.1.
4. Abra **Action Pad / Item Actions** para asociar el botón a una macro que valida crédito usando `InvokeHandler`.
5. Pruebe el botón directamente desde SAP.

### 6.3 Definir validaciones y campos obligatorios contextuales

1. Abra **Validation & Mandatory Fields Studio**.
2. Filtre `FormType 133 (Orden de Compra)`.
3. Cree regla: evento Before Add, condición "Proveedor es extranjero" y acción "Campo Incoterms obligatorio".
4. Active la vista previa y ejecute "Simular" con datos del formulario actual.

### 6.4 Publicar dashboards, búsquedas y macros globales

1. Abra **Dashboard/Search/Macro Studio**.
2. En la pestaña Dashboard cree KPI "Ventas hoy" con query SQL/HANA.
3. En la pestaña Search defina campos y filtros que se usarán en el buscador.
4. En Macro cree macro reutilizable "Abrir cliente".
5. Desde Automation Dashboard despliegue la configuración a QA y luego a PROD.

### 6.5 Auditar y documentar cambios

1. Abra **Audit Log Viewer** y filtre por usuario y rango de fechas.
2. Exporte a CSV y adjunte al ticket de soporte.
3. Use el botón "Abrir en Studio" para saltar al estudio responsable del registro.

---

## 7. Referencia de Macros (extracto actualizado)

| Comando | Descripción | Ejemplo |
|---------|-------------|---------|
| `InvokeHandler(type, method?, payload?, assembly?)` | Invoca clase .NET usando reflection. | `InvokeHandler('B1TuneUp.CustomLogic.Handler','Execute','$[$DocEntry.0.0]')` |
| `RunIntegration(code)` | Ejecuta escenario del Integration Studio. | `RunIntegration('CRM_ClientSync')` |
| `ShowStudio(name)` | Abre un estudio WPF desde macro (`Integration`, `Scheduler`, `Validation`, etc.). | `ShowStudio('AutomationDashboard')` |
| `OpenDashboard(id)` | Abre un dashboard publicado. | `OpenDashboard('KPI_VentasDia')` |
| `EmailTemplate(code)` | Envía plantilla creada en Email Designer. | `EmailTemplate('ALTA_CLIENTE')` |

Variables, funciones de texto/fecha y comandos clásicos permanecen vigentes (ver anexo anterior). Todos los nuevos comandos aceptan parámetros provenientes del formulario actual mediante la sintaxis `$[$Item.Col.Row]`.

---

## 8. Preguntas frecuentes (actualizado)

- **¿Los estudios funcionan en SAP HANA y SQL?** Sí, detectan el origen y cambian paleta/consultas automáticamente.
- **¿Necesito permisos especiales?** Únicamente pertenecer al grupo que pueda ejecutar Add-Ons y modificar tablas UDT.
- **¿Dónde traduzco los menús?** `LocalizationManager` ? archivo `Resources/lang/es.json` contiene la cadena `Menu.DashboardSearchMacroStudio` y el resto de los captions.
- **¿Puedo extender MacroEngine?** Sí, con `InvokeHandler` o implementando nuevos comandos via `MacroScriptService`.
- **¿Cómo publico cambios en otro ambiente?** Utiliza Automation Dashboard para exportar/importar menús, macros y layouts.

---

## 9. Soporte y siguientes pasos

1. **Checklist semanal**
   - Revisar Automation Dashboard.
   - Exportar logs críticos.
   - Respaldar tablas `@BTUN_*`.
2. **Capacitación recomendada**
   - 1 hora sobre Integration Studio.
   - 1 hora sobre UiCustomizer + Item Placement.
   - 1 hora sobre Validation/MacroEngine con ejemplos.
3. **Roadmap Q2 2026**
   - Widgets adicionales para Dashboard Studio.
   - Plantillas preconstruidas para Automation Dashboard.
   - Librería pública de handlers para InvokeHandler.

---

**Última actualización**: 3 de abril de 2026. Responsable: Equipo B1TuneUp.
## Actualizacion Mayo 2026: paridad operativa tipo B1UP

La solucion incorpora una capa de madurez orientada a consultores:

- `ConsultantConfigurationService`: inspeccion de configuracion usada por formulario, creacion rapida de triggers/validaciones desde contexto, duplicado de Universal Functions, export/import de paquetes, preview de diferencias, backups y rollback.
- `SearchIndexService`: indice persistente de B1 Search en `@BTUN_TBOX` (`SEARCHIDX_`), watermarks (`SEARCHWM_`), ranking por relevancia, favoritos e historial.
- `UniversalFunctionService`: nuevos tipos `ContentCreator`, `SQLReport`, `FileExporter`, `FileImporter`, `CreateActivity`, `Dashboard`, `InternalMessage` y `DataLauncher`, mas `Test()` para pruebas auditables.
- `ValidationTraceService`: trazas por regla con formulario, item, columna, fila, evento, SQL procesado, resultado, bloqueo y usuario.
- `RightClickMenuManager`: opciones contextuales para ver configuracion usada, crear trigger inactivo y crear backup manual.

Ver la guia dedicada: [GUIA_CONSULTOR_B1UP_PARITY.md](./GUIA_CONSULTOR_B1UP_PARITY.md).
