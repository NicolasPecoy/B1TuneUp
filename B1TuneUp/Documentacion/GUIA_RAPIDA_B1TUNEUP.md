# Guía Rápida de B1TuneUp

## Inicio Rápido ⚡

### 1. Primeros Pasos (5 minutos)

```
✅ Paso 1: Instalar addon con deploy.ps1
✅ Paso 2: Abrir SAP Business One
✅ Paso 3: Ir a Módulos > B1TuneUp > Configuración
✅ Paso 4: Seleccionar idioma y guardar
```

### 2. Tu Primera Macro (ejemplo real)

**Objetivo**: Mostrar mensaje al abrir Pedidos de Venta

```sql
-- Ejecutar en SQL Server/HANA:
INSERT INTO [@BTUN_RULES] (U_FormType, U_Type, U_EventType, U_Before, U_Action)
VALUES ('139', 'Macro', 'et_FORM_LOAD', 0, "Msg('Bienvenido a Ventas')")
```

¡Listo! La próxima vez que abras un pedido, verás el mensaje.

---

## Comandos Más Usados 🎯

### Manipulación Básica

```csharp
// Mostrar mensaje
Msg('Hola Usuario')

// Click en botón
Click('btnOK')

// Establecer valor
SetValue('CardCode', 'C00001')

// Mensaje en barra estado
Status('Proceso completado')
```

### Variables Dinámicas

```csharp
// Obtener valor de campo actual
$[$CardCode.0.0]

// Obtener valor de matrix (fila seleccionada)
$[$38.1.0]

// Ejemplo en SQL
IF(SELECT COUNT(*) FROM OCRD WHERE CardCode = '$[$CardCode.0.0]') THEN {
    Msg('Cliente existe')
}
```

### Condicionales

```csharp
// IF simple
IF(condicionSQL) THEN {
    Msg('Verdadero')
} ELSE {
    Msg('Falso')
}

// Ejemplo práctico
IF($[$DocTotal.0.0] > 1000) THEN {
    Msg('Pedido mayor a $1000')
}
```

### Loops (Matrices)

```csharp
// Recorrer líneas de pedido
Loop('38', 'SetValue(38.U_Campo, $[$38.1.0])')
```

---

## Personalizar Formularios 🎨

### Agregar Botón

```sql
INSERT INTO [@BTUN_UI] (
    U_FormType, U_Action, U_ItemID, U_Label,
    U_Top, U_Left, U_Width, U_Height
)
VALUES (
    '149', 'AddButton', 'btnMiAccion', 'Mi Acción',
    400, 10, 100, 30
)
```

### Ocultar Campo

```sql
UPDATE [@BTUN_UI]
SET U_Action = 'Hide'
WHERE U_FormType = '149' AND U_ItemID = 'miCampo'
```

### Cambiar Etiqueta

```sql
INSERT INTO [@BTUN_UI] (
    U_FormType, U_Action, U_ItemID, U_Label
)
VALUES (
    '139', 'ChangeLabel', 'lblDireccion', 'Dirección de Envío'
)
```

---

## Validaciones Comunes ✅

### Campo Obligatorio

```sql
-- Exigir campo antes de agregar/actualizar
INSERT INTO [@BTUN_RULES] (
    U_FormType, U_Type, U_EventType, U_Before,
    U_Condition, U_Action
)
VALUES (
    '149', 'Validation', 'et_FORM_DATA_ADD', 1,
    "SELECT CASE WHEN CardCode IS NULL THEN 'FAIL' ELSE 'OK' END",
    "Msg('Debe seleccionar un cliente'); Stop()"
)
```

### Validar Email

```sql
-- Verificar formato email
IF(SELECT COUNT(*) FROM CRD1 WHERE E_Mail LIKE '%@%.%' AND CardCode = '$[$CardCode.0.0]') THEN {
    Status('Email válido')
} ELSE {
    Msg('Email inválido')
}
```

### Validar Stock

```sql
-- Verificar stock antes de agregar línea
IF(SELECT ISNULL(OnHand, 0) FROM OITM WHERE ItemCode = '$[$38.1.0]' WHERE DocEntry = '$[$DocEntry.0.0]') >=
   (SELECT Quantity FROM RDR1 WHERE DocEntry = '$[$DocEntry.0.0]' AND LineNum = '$[$38.0.0]') THEN {
    Status('Stock suficiente')
} ELSE {
    Msg('Stock insuficiente'); Stop()
}
```

---

## Integración 🔌

### Llamada REST Simple

```csharp
// GET
REST('https://api.ejemplo.com/clientes/$[$CardCode.0.0]', 'GET', null, null)

// POST con JSON
REST('https://api.ejemplo.com/pedidos', 'POST',
     '{"cardcode":"$[$CardCode.0.0]","total":$[$DocTotal.0.0]}',
     'Content-Type: application/json')
```

### Enviar Email

```csharp
// Configurar primero en @BTUN_EMAIL
Email('123')  // 123 = DocEntry del email configurado
```

### Consultar Datos Externos

```csharp
// SOAP
SOAP('http://servicio.com/api.asmx', 'ConsultarPrecio',
     '<request><item>$[$ItemCode.0.0]</item></request>')
```

---

## Programar Tareas ⏰

### Tarea Diaria

```sql
-- Ejecutar cada 24 horas (1440 minutos)
INSERT INTO [@BTUN_SCHED] (U_Name, U_Action, U_Interval, U_Active)
VALUES (
    'Reporte Diario',
    'Email(''reporte_diario'')',
    1440,
    'Y'
)
```

### Tarea Cada Hora

```sql
-- Ejecutar cada 60 minutos
INSERT INTO [@BTUN_SCHED] (U_Name, U_Action, U_Interval, U_Active)
VALUES (
    'Sync Inventario',
    'REST(...)',
    60,
    'Y'
)
```

---

## Reportes 📊

### Imprimir Reporte

```csharp
// Imprimir con reporte predeterminado
Print('Factura')

// Enviar a impresora específica
SendToPrinter('HP LaserJet')
```

### Administrar Reportes

```csharp
// Abrir administrador de reportes
ManageReports()

// Vista previa
ShowReportPreview('MiReporte')
```

---

## Utilidades 🛠️

### Exportar a Excel

```csharp
ExportToExcel('C:\Export\pedido.csv')
```

### Archivos

```csharp
// Verificar si existe
FileExists('C:\archivo.txt')

// Crear carpeta
CreateFolder('C:\NuevaCarpeta')

// Eliminar archivo
DeleteFile('C:\archivo.tmp')
```

### Portapapeles

```csharp
// Copiar texto
Copy('$[$CardCode.0.0]')
```

### Log de Auditoría

```csharp
// Registrar acción
Log('Usuario realizó acción X')
```

---

## Dashboard y Métricas 📈

### Mostrar Dashboard

```csharp
ShowDashboard()
```

### Action Pad

```csharp
// Mostrar pad de acciones para formulario activo
ShowPad()
```

---

## Solución de Problemas 🔧

### El Add-on No Carga

```
1. Verificar .NET Framework 4.8+ instalado
2. Revisar logs de Windows Event Viewer
3. Confirmar versión de SAP B1 SDK
```

### Las Macros No Se Ejecutan

```sql
-- Verificar reglas cargadas
SELECT * FROM [@BTUN_RULES] WHERE U_FormType = '139'

-- Revisar logs
SELECT TOP 50 * FROM [@BTUN_LOG] ORDER BY DocEntry DESC
```

### Error en Validación

```
1. Revisar sintaxis SQL en U_Condition
2. Verificar que campos existen en formulario
3. Activar logs detallados
```

---

## Referencia de Formulario 📋

### Formatos Comunes

| Código | Descripción    | Uso          |
| ------ | -------------- | ------------ |
| 139    | Pedido Venta   | Ventas       |
| 140    | Entrega        | Logística    |
| 141    | Factura A/R    | Contabilidad |
| 169    | Pedido Compra  | Compras      |
| 170    | Entrada Merc.  | Almacén      |
| 171    | Factura A/P    | Proveedores  |
| 2      | Socios Negocio | Maestros     |
| 4      | Artículos      | Inventario   |

### Items Comunes en Formularios

| ItemUID   | Descripción     | Tipo       |
| --------- | --------------- | ---------- |
| CardCode  | Código Socio    | EditText   |
| CardName  | Nombre Socio    | EditText   |
| DocDate   | Fecha Documento | DatePicker |
| DocTotal  | Total Documento | EditText   |
| btnOK     | Botón OK        | Button     |
| btnCancel | Botón Cancelar  | Button     |

---

## Atajos Útiles ⌨️

### En Formularios

- `Tab` - Siguiente campo
- `Shift+Tab` - Campo anterior
- `Ctrl+Tab` - Siguiente pestaña
- `F4` - Buscar (cuadro con lupa)
- `Ctrl+F` - Buscar en grilla
- `Ctrl+S` - Guardar
- `Esc` - Cancelar

### En Grillas (Matrix)

- `Flechas` - Moverse entre celdas
- `Enter` - Editar celda
- `Espacio` - Seleccionar fila
- `Ctrl+A` - Seleccionar todo
- `Ctrl+C` - Copiar filas
- `Ctrl+V` - Pegar filas

---

## Ejemplos Prácticos 💡

### 1. Auto-completar Dirección

```csharp
// Cuando cambia CardCode, copiar dirección
SetValue('Address2', $[$2.0.0])
```

### 2. Calcular Descuento

```csharp
// Si total > 1000, aplicar 10% descuento
IF($[$DocTotal.0.0] > 1000) THEN {
    SetValue('DiscSum', $[$DocTotal.0.0] * 0.10)
}
```

### 3. Validar Fecha Futura

```csharp
// No permitir fechas futuras
IF($[$DocDate.0.0] > GetDate()) THEN {
    Msg('No se permiten fechas futuras');
    SetValue('DocDate', GetDate())
}
```

### 4. Contar Líneas

```csharp
// Mostrar cantidad de líneas en pedido
Msg('Cantidad de líneas: ' + Loop('38', 'Count()'))
```

### 5. Transferir Datos Entre Forms

```csharp
// Copiar dato de Pedido a Factura
Transfer('CardCode', 'CardCode', '171')
```

---

## Tips de Performance ⚡

### ✅ Buenas Prácticas

```sql
-- Usar índices en consultas frecuentes
CREATE INDEX IX_BTUN_RULES_FORMTYPE ON [@BTUN_RULES](U_FormType)

-- Evitar SELECT *
SELECT U_Action, U_Type FROM [@BTUN_RULES] WHERE ...

-- Filtrar por fecha
WHERE U_Date >= GETDATE() - 30
```

### ❌ Evitar

```sql
-- Mal: Consulta muy amplia
SELECT * FROM [@BTUN_RULES]

-- Mal: Sin condiciones
WHILE (1=1) BEGIN ... END

-- Mal: Múltiples queries en loop
-- Mejor: Traer datos en una sola consulta
```

---

## Contacto y Soporte 📞

### Recursos Online

- 📖 Documentación completa: `DOCUMENTACION_COMPLETA_B1TUNEUP.md`
- 💬 Foro: https://comunidad.b1tuneup.com
- 🐛 Reportar bugs: GitHub Issues
- 📧 Email: soporte@b1tuneup.com

### Canales Oficiales

- **Soporte Técnico**: tickets en portal
- **Ventas**: ventas@b1tuneup.com
- **Implementación**: consulting@b1tuneup.com

---

## Checklist de Implementación ✅

### Antes de Ir a Producción

- [ ] Backup de base de datos realizado
- [ ] Pruebas en entorno QA completadas
- [ ] Usuarios capacitados
- [ ] Documentación disponible
- [ ] Plan de rollback definido
- [ ] Monitoreo configurado
- [ ] Contactos de soporte actualizados

### Después de Implementar

- [ ] Verificar logs sin errores
- [ ] Performance aceptable
- [ ] Usuarios operativos
- [ ] Funcionalidades críticas probadas
- [ ] Documentación actualizada

---

**Versión**: 1.0  
**Actualizado**: Marzo 2026  
**Para**: Usuarios y Soporte de B1TuneUp

© 2026 B1TuneUp - Todos los derechos reservados
