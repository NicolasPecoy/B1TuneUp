# Documentación Completa de B1TuneUp

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Instalación y Configuración](#instalación-y-configuración)
4. [Módulos Principales](#módulos-principales)
5. [Guía de Uso](#guía-de-uso)
6. [Referencia de Macros](#referencia-de-macros)
7. [Preguntas Frecuentes](#preguntas-frecuentes)
8. [Soporte Técnico](#soporte-técnico)

---

## Introducción

### ¿Qué es B1TuneUp?

**B1TuneUp** es un addon desarrollado para SAP Business One que replica y amplía la funcionalidad del **B1 Usability Package** de Boyum-IT. Está diseñado para mejorar significativamente la usabilidad, productividad y personalización del sistema SAP Business One.

### Objetivos Principales

- ✅ **Personalización de Interfaz**: Modificar formularios SAP sin programación
- ✅ **Automatización de Procesos**: Ejecutar tareas repetitivas mediante macros
- ✅ **Validaciones Avanzadas**: Implementar reglas de negocio personalizadas
- ✅ **Reportes Personalizados**: Crear y gestionar reportes Crystal Reports
- ✅ **Integración de Datos**: Conectar con sistemas externos vía REST/SOAP
- ✅ **Programación de Tareas**: Automatizar procesos periódicos

### Beneficios Clave

| Beneficio            | Descripción                                             |
| -------------------- | ------------------------------------------------------- |
| 🎯 **Productividad** | Reduce clicks y tiempo en operaciones diarias           |
| 🔧 **Flexibilidad**  | Adapta SAP B1 a tus procesos, no al revés               |
| 💰 **Ahorro**        | Evita desarrollos costosos con soluciones configurables |
| 📊 **Control**       | Auditoría completa de todas las acciones realizadas     |
| 🌐 **Integración**   | Conecta fácilmente con otros sistemas                   |

---

## Arquitectura del Sistema

### Componentes Principales

```
┌─────────────────────────────────────────────────────────┐
│                    B1TuneUp Add-on                       │
├─────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Core       │  │   Modules    │  │    Utils     │  │
│  │              │  │              │  │              │  │
│  │ - B1App      │  │ - MacroEngine│  │ - Logger     │  │
│  │ - EventDisp. │  │ - UICustomizer│ │ - Settings   │  │
│  │              │  │ - Validators │  │ - Localization│ │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
├─────────────────────────────────────────────────────────┤
│              SAP Business One SDK (DI & UI)             │
└─────────────────────────────────────────────────────────┘
```

### Flujo de Eventos

1. **Captura de Eventos**: El `EventDispatcher` intercepta todos los eventos de SAP B1
2. **Evaluación de Reglas**: Se verifican las reglas definidas en base de datos
3. **Ejecución de Acciones**: Se ejecutan macros o validaciones según corresponda
4. **Auditoría**: Todas las acciones quedan registradas en el log

### Tablas de Base de Datos

B1TuneUp utiliza las siguientes tablas personalizadas:

| Tabla         | Código             | Descripción                                   |
| ------------- | ------------------ | --------------------------------------------- |
| `@BTUN_RULES` | Reglas de negocio  | Macros, validaciones y personalizaciones      |
| `@BTUN_UI`    | Personalización UI | Cambios en formularios (mostrar, mover, etc.) |
| `@BTUN_LOG`   | Log de auditoría   | Historial de acciones realizadas              |
| `@BTUN_SCHED` | Programador        | Tareas programadas periódicamente             |
| `@BTUN_TBOX`  | Toolbox            | Configuraciones generales del sistema         |
| `@BTUN_EMAIL` | Plantillas Email   | Configuración de envíos de correo             |
| `@BTUN_RPT`   | Reportes           | Plantillas de reportes personalizados         |

---

## Instalación y Configuración

### Requisitos Previos

#### Hardware Recomendado

- **Servidor**: 4 GB RAM mínimo, 8 GB recomendado
- **Cliente**: 2 GB RAM, procesador dual-core o superior
- **Espacio en Disco**: 500 MB para la instalación

#### Software Requerido

- ✅ SAP Business One 9.0 o superior (PLATINO o Profesional)
- ✅ .NET Framework 4.8 o superior
- ✅ Windows 10/11 o Windows Server 2016+
- ✅ Acceso de administrador para crear tablas personalizadas

### Proceso de Instalación

#### Paso 1: Preparación del Entorno

1. Verificar que SAP Business One esté instalado y funcionando correctamente
2. Confirmar versión del SDK disponible
3. Asegurar permisos de administración en la base de datos

#### Paso 2: Instalación del Add-on

```powershell
# Ejecutar el instalador como administrador
.\installer\build-installer.ps1

# O usar el script de despliegue directo
.\installer\deploy.ps1 -TargetPath "C:\Program Files\SAP\SAP Business One\"
```

#### Paso 3: Creación de Tablas

El sistema creará automáticamente las tablas necesarias al iniciar por primera vez. Si hay errores:

```csharp
// Forzar creación de metadatos
MetadataManager.SetupMetadata();
```

#### Paso 4: Configuración Inicial

1. Abrir SAP Business One
2. Ir a **Módulos > B1TuneUp > Configuración**
3. Configurar idioma preferido (Español/Inglés)
4. Establecer parámetros SMTP si se usará envío de emails

### Configuración de Parámetros

#### Configuración de Idioma

El sistema soporta múltiples idiomas mediante archivos JSON:

- `Resources/lang/es.json` - Español
- `Resources/lang/en.json` - Inglés

Para cambiar el idioma:

```
Menú B1TuneUp > Language > Seleccionar idioma
```

#### Configuración SMTP (Email)

Configurar en `@BTUN_TBOX`:

| Código           | Valor               | Descripción             |
| ---------------- | ------------------- | ----------------------- |
| `SMTP_Server`    | smtp.gmail.com      | Servidor SMTP           |
| `SMTP_Port`      | 587                 | Puerto del servidor     |
| `SMTP_Username`  | usuario             | Cuenta de correo        |
| `SMTP_Password`  | contraseña          | Contraseña de la cuenta |
| `SMTP_FromEmail` | noreply@empresa.com | Email remitente         |
| `SMTP_EnableSSL` | true                | Usar conexión segura    |

---

## Módulos Principales

### 1. MacroEngine - Motor de Macros

**Propósito**: Ejecutar comandos automatizados en formularios SAP

#### Funcionalidades Clave

- Manipulación de items (clicks, valores, visibilidad)
- Navegación entre formularios
- Ejecución de consultas SQL
- Condicionales y bucles
- Integración con archivos y sistema operativo

#### Ejemplos de Uso

**Ejemplo 1: Mensaje Simple**

```
Msg('Hola Usuario')
```

**Ejemplo 2: Establecer Valor**

```
SetValue('CardCode', 'C00001')
```

**Ejemplo 3: Click en Botón**

```
Click('btnOK')
```

**Ejemplo 4: Condicional SQL**

```
IF(SELECT COUNT(*) FROM OCRD WHERE CardCode = '$[$CardCode.0.0]') THEN {
    Msg('Cliente existe')
} ELSE {
    Msg('Cliente no existe')
}
```

**Ejemplo 5: Loop en Matrix**

```
Loop('38', 'SetValue(38.U_MyField, $[$38.1.0])')
```

### 2. UICustomizer - Personalización de Interfaz

**Propósito**: Modificar la apariencia y comportamiento de formularios SAP

#### Acciones Disponibles

| Acción           | Descripción            | Ejemplo                     |
| ---------------- | ---------------------- | --------------------------- |
| `Hide`           | Ocultar un item        | Ocultar campo no utilizado  |
| `Move`           | Mover posición         | Reubicar botón              |
| `Resize`         | Cambiar tamaño         | Agrandar campo de texto     |
| `ChangeLabel`    | Modificar etiqueta     | Renombrar campo             |
| `Enable/Disable` | Habilitar/deshabilitar | Bloquear campo solo lectura |
| `AddButton`      | Agregar botón          | Botón personalizado         |
| `AddFolder`      | Agregar pestaña        | Nueva sección               |
| `AddEditText`    | Agregar campo          | Campo personalizado         |

#### Ejemplo de Configuración

Para agregar un botón en Pedidos de Venta (FormType 149):

```sql
INSERT INTO [@BTUN_UI] (Code, U_FormType, U_Action, U_ItemID, U_Label, U_Top, U_Left, U_Width, U_Height)
VALUES ('BTN001', '149', 'AddButton', 'btnCustom', 'Mi Acción', 100, 200, 80, 25)
```

### 3. ValidationManager - Validaciones Avanzadas

**Propósito**: Implementar reglas de negocio y validaciones personalizadas

#### Tipos de Validación

1. **Campos Obligatorios Dinámicos**

   - Define campos obligatorios según condiciones
   - Ejemplo: "Si Tipo Cliente = Mayorista, exigir Campo Ventas"

2. **Validaciones de Formato**

   - Patrones regex para emails, teléfonos, códigos
   - Ejemplo: "Email debe contener @"

3. **Validaciones Cruzadas**

   - Compara datos entre tablas
   - Ejemplo: "Fecha Entrega >= Fecha Pedido"

4. **Reglas Personalizadas SQL**
   - Cualquier consulta SQL como condición
   - Ejemplo: "Validar stock disponible antes de agregar"

#### Configuración de Validaciones

Ir a: **Módulos > B1TuneUp > Sistema de Validaciones**

1. Click en **Agregar**
2. Seleccionar Formulario (ej. 149 - Pedido)
3. Definir evento (Add, Update, Load)
4. Escribir condición SQL
5. Definir acción (mensaje, bloqueo, corrección)

### 4. SchedulerManager - Programador de Tareas

**Propósito**: Ejecutar macros automáticamente en intervalos regulares

#### Configuración de Tarea Programada

Tabla `@BTUN_SCHED`:

| Campo        | Tipo     | Descripción                  |
| ------------ | -------- | ---------------------------- |
| `U_Name`     | Texto    | Nombre descriptivo           |
| `U_Action`   | Texto    | Macro a ejecutar             |
| `U_Interval` | Numérico | Intervalo en minutos         |
| `U_LastRun`  | Fecha    | Última ejecución             |
| `U_Active`   | Char     | 'Y' = Activo, 'N' = Inactivo |

#### Ejemplo: Enviar Email Diario

```sql
INSERT INTO [@BTUN_SCHED] (U_Name, U_Action, U_Interval, U_Active)
VALUES ('Reporte Diario', 'Email(123)', 1440, 'Y')
```

Esto enviará un email cada 1440 minutos (24 horas).

### 5. AuditLogManager - Auditoría y Logs

**Propósito**: Registrar todas las acciones realizadas por el sistema

#### Información Registrada

- **Tipo de Acción**: RuleExecution, ValidationRule, UserLog, etc.
- **Detalles**: Descripción completa de la operación
- **Estado**: Success, Error, Warning
- **Usuario**: Quién realizó la acción
- **Fecha/Hora**: Timestamp exacto

#### Consultar Logs

```sql
-- Ver últimos 100 logs
SELECT TOP 100 * FROM [@BTUN_LOG] ORDER BY DocEntry DESC

-- Filtrar por tipo
SELECT * FROM [@BTUN_LOG] WHERE U_Type = 'ValidationRule'

-- Filtrar por usuario
SELECT * FROM [@BTUN_LOG] WHERE U_User = 'manager'
```

### 6. ReportManager - Gestión de Reportes

**Propósito**: Crear, modificar y visualizar reportes Crystal Reports

#### Funcionalidades

- ✅ Cargar plantillas Crystal Reports (.rpt)
- ✅ Personalizar parámetros de reporte
- ✅ Vista previa antes de imprimir
- ✅ Exportar a PDF, Excel, Word
- ✅ Asignar reportes a menús personalizados

#### Uso Básico

1. Ir a **Módulos > B1TuneUp > Reportes**
2. Click en **Administrar Templates**
3. Agregar nuevo reporte:
   - Nombre descriptivo
   - Archivo .rpt
   - Parámetros requeridos
4. Click en **Vista Previa** para probar

### 7. IntegrationManager - Integraciones

**Propósito**: Conectar con sistemas externos vía REST/SOAP

#### Llamadas REST

**Sintaxis:**

```
REST(url, method, body, headers)
```

**Ejemplo GET:**

```
REST('https://api.ejemplo.com/clientes', 'GET', null, 'Authorization: Bearer token123')
```

**Ejemplo POST:**

```
REST('https://api.ejemplo.com/pedidos', 'POST', '{"cardcode":"C001"}', 'Content-Type: application/json')
```

#### Llamadas SOAP

**Sintaxis:**

```
SOAP(url, action, body)
```

**Ejemplo:**

```
SOAP('http://servicio.ejemplo.com/service.asmx', 'ConsultarCliente', '<request><id>123</id></request>')
```

### 8. ToolboxManager - Herramientas Generales

**Propósito**: Funcionalidades transversales del sistema

#### Características Incluidas

- **Bloqueo de Períodos**: Evitar movimientos en períodos cerrados
- **Validación de NIF/CIF**: Algoritmos por país (ES, DE, FR, IT, GB, US, MX)
- **Formateo Automático**: Aplicar máscaras a campos
- **Configuraciones Globales**: Temas, notificaciones, idioma

#### Validación de NIF Español

El sistema valida automáticamente NIF español:

- Personas físicas: Dígito de control correcto
- Sociedades: Verificación de letra inicial

### 9. EmailManager - Envío de Emails

**Propósito**: Enviar correos electrónicos desde SAP B1

#### Configuración de Plantilla

Tabla `@BTUN_EMAIL`:

```sql
INSERT INTO [@BTUN_EMAIL] (U_To, U_Subject, U_Body, U_Attach)
VALUES (
    'cliente@ejemplo.com',
    'Confirmación de Pedido',
    'Estimado cliente, su pedido ha sido creado.',
    'C:\Pedidos\pedido_123.pdf'
)
```

#### Enviar Email desde Macro

```
Email('123')  // Donde 123 es el DocEntry del email configurado
```

### 10. DefaultValueManager - Valores por Defecto

**Propósito**: Asignar valores automáticos a campos

#### Tipos de Valores por Defecto

1. **OnLoad**: Al abrir el formulario
2. **OnChange**: Al modificar un campo específico

#### Ejemplos

**Fecha actual al cargar:**

```
FormType: 149
ItemID: DocDate
DefaultValue: @GETDATE()
```

**Copiar valor de otro campo:**

```
FormType: 149
TriggerItem: CardCode
TargetItem: Address
Action: SetValue('Address', $[$CardCode.0.0])
```

### 11. TemplateManager - Plantillas de Documento

**Propósito**: Guardar y cargar configuraciones de documentos

#### Funcionalidades

- **Crear Plantilla**: Guardar estado actual del formulario
- **Cargar Plantilla**: Restaurar configuración guardada
- **Administrar**: Gestionar plantillas disponibles

#### Uso

1. Configurar formulario a gusto
2. Click en **Crear Plantilla**
3. Asignar nombre descriptivo
4. Para reutilizar: **Cargar Plantilla**

### 12. DashboardManager - Dashboards Personalizados

**Propósito**: Mostrar métricas e indicadores clave

#### Características

- Gráficos en tiempo real
- KPIs configurables
- Filtros dinámicos
- Exportación de datos

#### Acceder al Dashboard

```
ShowDashboard()
```

O desde menú: **Módulos > B1TuneUp > Dashboard**

---

## Guía de Uso

### Escenario 1: Agregar Botón Personalizado

**Objetivo**: Crear botón que exporte líneas de pedido a Excel

#### Paso 1: Configurar UI

```sql
INSERT INTO [@BTUN_UI] (U_FormType, U_Action, U_ItemID, U_Label, U_Top, U_Left, U_Width, U_Height)
VALUES ('149', 'AddButton', 'btnExport', 'Exportar Líneas', 400, 10, 100, 30)
```

#### Paso 2: Crear Regla Macro

```sql
INSERT INTO [@BTUN_RULES] (U_FormType, U_Type, U_EventType, U_Before, U_Action)
VALUES ('149', 'Macro', 'et_ITEM_CLICK', 0,
        "ExportToExcel('LineasPedido.csv'); Status('Exportación completada')")
```

#### Paso 3: Probar

1. Abrir Pedido de Venta
2. Click en botón "Exportar Líneas"
3. Verificar archivo CSV generado

### Escenario 2: Validar Campo Obligatorio

**Objetivo**: Exigir completar "Dirección de Envío" si Cliente es Extranjero

#### Configuración

```sql
INSERT INTO [@BTUN_RULES] (U_FormType, U_Type, U_EventType, U_Before, U_Condition, U_Action)
VALUES ('149', 'Validation', 'et_FORM_DATA_ADD', 1,
        "SELECT CASE WHEN U_Extranjero = 'Y' AND ShipToAddr IS NULL THEN 'FAIL' ELSE 'OK' END",
        "Msg('Debe completar Dirección de Envío para clientes extranjeros'); Stop()")
```

### Escenario 3: Programar Tarea Diaria

**Objetivo**: Enviar reporte de ventas diarias a las 18:00

#### Configuración

1. Crear macro de envío:

```
Email('reporte_ventas_diario')
```

2. Programar tarea:

```sql
INSERT INTO [@BTUN_SCHED] (U_Name, U_Action, U_Interval, U_Active)
VALUES ('Reporte Ventas', 'Email(''reporte_ventas_diario'')', 1440, 'Y')
```

### Escenario 4: Integración con Sistema Externo

**Objetivo**: Consultar stock en sistema WMS externo

#### Macro de Integración

```
REST('https://wms.empresa.com/api/stock?item=$[$ItemCode.0.0]',
     'GET',
     null,
     'Authorization: Bearer abc123')
```

### Mejores Prácticas

#### 1. Naming Convention

Usar prefijos descriptivos:

- `btn_` para botones
- `txt_` para campos de texto
- `fld_` para carpetas/pestañas
- `grd_` para grillas

#### 2. Manejo de Errores

Siempre incluir validaciones:

```
IF(SELECT COUNT(*) FROM table) THEN {
    // Operación principal
} ELSE {
    Msg('Error: Datos no encontrados')
}
```

#### 3. Performance

- Evitar loops grandes en formularios
- Usar índices en consultas SQL frecuentes
- Liberar objetos COM explícitamente

#### 4. Seguridad

- No hardcodear contraseñas
- Usar variables de entorno para credenciales
- Validar permisos de usuario antes de ejecutar

#### 5. Mantenibilidad

- Comentar macros complejas
- Usar nombres descriptivos en reglas
- Documentar cambios en código

---

## Referencia de Macros

### Comandos Disponibles

#### Manipulación de UI

| Comando    | Sintaxis                      | Descripción                |
| ---------- | ----------------------------- | -------------------------- |
| `Msg`      | `Msg('texto')`                | Mostrar mensaje            |
| `Status`   | `Status('texto')`             | Mensaje en barra de estado |
| `Click`    | `Click('itemId')`             | Click en elemento          |
| `SetValue` | `SetValue('itemId', 'valor')` | Asignar valor              |
| `Focus`    | `Focus('itemId')`             | Poner foco                 |
| `Enable`   | `Enable('itemId')`            | Habilitar elemento         |
| `Disable`  | `Disable('itemId')`           | Deshabilitar elemento      |
| `OpenForm` | `OpenForm('formType')`        | Abrir formulario SAP       |

#### Navegación y Flujo

| Comando | Sintaxis                                 | Descripción              |
| ------- | ---------------------------------------- | ------------------------ |
| `IF`    | `IF(cond) THEN { macro } ELSE { macro }` | Condicional              |
| `Loop`  | `Loop('matrixId', 'macro')`              | Iterar matrix            |
| `Stop`  | `Stop()`                                 | Detener ejecución        |
| `Close` | `Close()`                                | Cerrar formulario activo |

#### Formularios

| Comando    | Sintaxis          | Descripción           |
| ---------- | ----------------- | --------------------- |
| `OpenForm` | `OpenForm('149')` | Abrir formulario      |
| `Refresh`  | `Refresh()`       | Actualizar formulario |
| `SaveForm` | `SaveForm()`      | Guardar formulario    |
| `Freeze`   | `Freeze('true')`  | Congelar UI           |

#### Estado de Ventana

| Comando    | Sintaxis     | Descripción       |
| ---------- | ------------ | ----------------- |
| `Maximize` | `Maximize()` | Maximizar ventana |
| `Minimize` | `Minimize()` | Minimizar ventana |
| `Restore`  | `Restore()`  | Restaurar tamaño  |

#### Archivos y Sistema

| Comando        | Sintaxis                 | Descripción                 |
| -------------- | ------------------------ | --------------------------- |
| `FileExists`   | `FileExists('ruta')`     | Verificar si existe archivo |
| `CreateFolder` | `CreateFolder('ruta')`   | Crear carpeta               |
| `DeleteFile`   | `DeleteFile('ruta')`     | Eliminar archivo            |
| `Launch`       | `Launch('programa.exe')` | Ejecutar programa           |

#### Base de Datos

| Comando      | Sintaxis                         | Descripción       |
| ------------ | -------------------------------- | ----------------- |
| `SQLExecute` | `SQLExecute('SELECT...')`        | Ejecutar consulta |
| `Transfer`   | `Transfer('from', 'to', 'form')` | Transferir datos  |

#### Integración

| Comando | Sintaxis                           | Descripción       |
| ------- | ---------------------------------- | ----------------- |
| `REST`  | `REST(url, method, body, headers)` | Llamada HTTP REST |
| `SOAP`  | `SOAP(url, action, body)`          | Llamada SOAP      |
| `Email` | `Email('docEntry')`                | Enviar email      |

#### Reportes

| Comando         | Sintaxis                     | Descripción        |
| --------------- | ---------------------------- | ------------------ |
| `Print`         | `Print('reporte')`           | Imprimir reporte   |
| `SendToPrinter` | `SendToPrinter('impresora')` | Enviar a impresora |
| `ManageReports` | `ManageReports()`            | Gestionar reportes |

#### Utilidades

| Comando  | Sintaxis         | Descripción             |
| -------- | ---------------- | ----------------------- |
| `Copy`   | `Copy('texto')`  | Copiar al portapapeles  |
| `Log`    | `Log('detalle')` | Registrar en auditoría  |
| `Search` | `Search()`       | Abrir buscador B1TuneUp |

### Variables Dinámicas

#### Sintaxis de Variables

```
$[$ItemId.ColType.Row]
```

#### Ejemplos

| Variable           | Descripción                                |
| ------------------ | ------------------------------------------ |
| `$[$CardCode.0.0]` | Valor del campo CardCode                   |
| `$[$38.1.0]`       | Columna 1, fila seleccionada del matrix 38 |
| `$[$DocDate.0.0]`  | Valor del campo DocDate                    |

#### En Consultas SQL

```sql
SELECT * FROM OCRD
WHERE CardCode = '$[$CardCode.0.0]'
```

### Funciones Especiales

#### Funciones de Texto

- `Len(cadena)` - Longitud de texto
- `Left(cadena, n)` - Primeros n caracteres
- `Right(cadena, n)` - Últimos n caracteres
- `Mid(cadena, inicio, n)` - Subcadena

#### Funciones Numéricas

- `Round(numero, decimales)` - Redondear
- `Abs(numero)` - Valor absoluto
- `Val(texto)` - Convertir a número

#### Funciones de Fecha

- `GetDate()` - Fecha actual
- `DateAdd(fecha, dias)` - Sumar días
- `DateDiff(fecha1, fecha2)` - Diferencia en días

---

## Preguntas Frecuentes

### Instalación

**¿El addon funciona en SAP HANA?**  
✅ Sí, B1TuneUp es compatible con bases de datos HANA y SQL Server.

**¿Requiere reiniciar SAP después de instalar?**  
⚠️ Recomendable pero no obligatorio. Algunos cambios requieren reinicio.

**¿Funciona en versión Web Client?**  
❌ No, actualmente solo compatible con SAP B1 Desktop Client.

### Funcionalidad

**¿Puedo usar B1TuneUp en múltiples compañías?**  
✅ Sí, las reglas se almacenan por compañía.

**¿Hay límite de reglas/macros?**  
❌ No hay límite técnico, pero se recomienda mantenerlo razonable (<1000 reglas).

**¿Las macros se ejecutan en segundo plano?**  
⚠️ Depende. Las macros simples son síncronas. Para procesos largos, usar el Programador.

### Desarrollo

**¿Puedo crear mis propias funciones?**  
✅ Sí, usando el módulo DynamicCodeEngine.

**¿Cómo depuro una macro que falla?**

1. Activar logs detallados
2. Revisar tabla `@BTUN_LOG`
3. Probar macro por partes

**¿Se puede integrar con Power BI?**  
✅ Sí, mediante exportación de datos o llamadas REST.

### Soporte

**¿Qué hago si encuentro un error?**

1. Revisar logs en `@BTUN_LOG`
2. Verificar versión del addon
3. Contactar soporte con detalles del error

**¿Hay actualizaciones automáticas?**  
❌ No, las actualizaciones son manuales.

**¿Dónde encuentro documentación adicional?**  
📖 En la carpeta `Documentacion` del proyecto.

---

## Soporte Técnico

### Canales de Soporte

#### Email

📧 soporte@b1tuneup.com

#### Teléfono

📞 +XX XXX XXX XXXX  
Horario: Lunes a Viernes 9:00 - 18:00

#### Portal de Tickets

🌐 https://soporte.b1tuneup.com

### Información Requerida

Al contactar soporte, incluir:

1. **Versión de B1TuneUp**: Menú > Ayuda > Acerca de
2. **Versión de SAP B1**: 9.x PLxx
3. **Base de Datos**: HANA / SQL Server
4. **Descripción del Problema**: Pasos para reproducir
5. **Logs Adjuntos**: Exportar desde `@BTUN_LOG`

### Actualizaciones

#### Verificar Versión

```sql
-- Versión actual (si está implementado)
SELECT U_Value FROM [@BTUN_TBOX] WHERE U_Code = 'VERSION'
```

#### Proceso de Actualización

1. Backup de base de datos
2. Descargar nueva versión
3. Ejecutar instalador
4. Verificar tablas actualizadas
5. Probar funcionalidades críticas

### Comunidad y Recursos

#### Foro de Usuarios

💬 https://comunidad.b1tuneup.com

#### Blog Oficial

📝 https://blog.b1tuneup.com

#### GitHub (Código Abierto)

🔗 https://github.com/b1tuneup/B1TuneUp

### Servicios Profesionales

Ofrecemos servicios adicionales:

- ✅ Implementación personalizada
- ✅ Capacitación in-company
- ✅ Desarrollo de módulos a medida
- ✅ Auditoría de configuración existente

Contactar: ventas@b1tuneup.com

---

## Apéndice A: Referencia Rápida

### Atajos de Teclado

| Tecla        | Función            |
| ------------ | ------------------ |
| `Ctrl + F12` | Abrir Action Pad   |
| `F5`         | Refresh formulario |
| `Ctrl + S`   | Guardar documento  |
| `Esc`        | Cancelar/Cerrar    |

### Códigos de Formulario Comunes

| Código | Formulario        |
| ------ | ----------------- |
| 139    | Pedido de Venta   |
| 140    | Entrega           |
| 141    | Factura A/R       |
| 169    | Pedido de Compra  |
| 170    | Entrada Mercancía |
| 171    | Factura A/P       |
| 2      | Socios de Negocio |
| 4      | Artículos         |

### Colores de Estado

| Color       | Significado     |
| ----------- | --------------- |
| 🟢 Verde    | Éxito/Activo    |
| 🟡 Amarillo | Advertencia     |
| 🔴 Rojo     | Error/Bloqueado |
| 🔵 Azul     | Informativo     |

---

## Apéndice B: Glosario de Términos

| Término             | Definición                                         |
| ------------------- | -------------------------------------------------- |
| **Addon**           | Complemento que extiende funcionalidad de SAP      |
| **Macro**           | Secuencia de comandos automatizados                |
| **UDT**             | Tabla definida por usuario (User Defined Table)    |
| **UDF**             | Campo definido por usuario (User Defined Field)    |
| **FormType**        | Código numérico que identifica formulario SAP      |
| **ItemUID**         | Identificador único de elemento en formulario      |
| **Matrix**          | Grilla/tabla dentro de formulario SAP              |
| **EventDispatcher** | Componente que gestiona eventos del sistema        |
| **Rule**            | Configuración que define comportamiento automático |

---

**Documento Version**: 1.0  
**Última Actualización**: Marzo 2026  
**Autor**: Equipo B1TuneUp

© 2026 B1TuneUp. Todos los derechos reservados.
