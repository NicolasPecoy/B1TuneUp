# Guia de madurez tipo B1UP para consultores

Actualizacion: Mayo 2026

Esta guia cubre las nuevas capacidades agregadas para acercar B1TuneUp al flujo de trabajo de consultoria esperado en Boyum B1 Usability Package: creacion rapida desde el formulario, busqueda de configuracion usada, transporte QA -> PROD, rollback, busqueda indexada, Universal Functions ampliadas y trazas de validacion.

## 1. Flujo de configuracion tipo consultor

En cualquier formulario SAP Business One, el menu contextual de B1TuneUp ahora incluye:

- **TuneUp: ver configuracion usada aqui**: inspecciona triggers, validaciones y busquedas asociadas al `FormType` actual.
- **TuneUp: crear trigger aqui**: crea un `UnifiedTriggerEntry` inactivo para el formulario e item actual, con `TraceEnabled = true`.
- **TuneUp: backup configuracion**: crea un snapshot completo en `@BTUN_TBOX` con prefijo `CONSULTANT_BACKUP_`.

Ejemplo:

1. Abrir Pedido de Cliente (`FormType 139`).
2. Click derecho sobre un campo o boton.
3. Elegir **TuneUp: crear trigger aqui**.
4. Completar `UniversalFunctionCode` o `Macro` desde Event Triggers.
5. Probar con trace activo y activar cuando el resultado sea correcto.

Servicios disponibles:

```csharp
ConsultantConfigurationService.FindUsedOnForm(form);
ConsultantConfigurationService.CreateTriggerFromCurrentContext("ITEM_PRESSED", "1");
ConsultantConfigurationService.CreateValidationFromCurrentContext("DATA_ADD_BEFORE", "ERROR");
ConsultantConfigurationService.DuplicateUniversalFunction("SYNC_BP");
ConsultantConfigurationService.BackupSnapshot("Antes de go-live ventas");
ConsultantConfigurationService.Rollback("CONSULTANT_BACKUP_20260511143000");
```

## 2. Transporte QA -> PROD y rollback

Exportar paquete:

```csharp
ConsultantConfigurationService.ExportPackage(
    @"C:\B1TuneUp\Packages\ventas-qa.json",
    formType: "139");
```

El paquete incluye Search Configurations, Universal Functions, Unified Triggers, Validation Rules y settings transportables. No incluye indices de busqueda ni backups internos.

Preview de importacion:

```csharp
var diff = ConsultantConfigurationService.PreviewImport(@"C:\B1TuneUp\Packages\ventas-qa.json");
```

Cada diferencia indica `Area`, `Code`, `Action`, `Conflict`, `CurrentSummary` e `IncomingSummary`.

Importar con backup automatico:

```csharp
ConsultantConfigurationService.ImportPackage(@"C:\B1TuneUp\Packages\ventas-qa.json");
```

Antes de aplicar cambios se genera un backup `CONSULTANT_BACKUP_yyyyMMddHHmmss`.

Rollback:

```csharp
ConsultantConfigurationService.Rollback("CONSULTANT_BACKUP_20260511143000");
```

## 3. B1 Search con indice persistente

`AdvancedSearchService` ahora consulta primero `SearchIndexService` y luego usa SQL directo como fallback.

Caracteristicas:

- Documentos persistentes en `@BTUN_TBOX` con prefijo `SEARCHIDX_`.
- Watermark por busqueda con prefijo `SEARCHWM_`.
- Ranking por titulo exacto, prefijo, subtitulo, keywords, favoritos e historial.
- Consolidacion de duplicados por `SearchCode + Key`.
- Permisos respetados via `AuthorizationScopeService`.

Reindexar todo:

```csharp
int indexed = SearchIndexService.RebuildAll(maxRowsPerConfig: 5000);
```

Reindexar una busqueda:

```csharp
var bpSearch = SearchConfigService.GetAll().First(x => x.Code == "BP_GLOBAL");
SearchIndexService.Rebuild(bpSearch, maxRows: 10000);
```

SQL recomendado:

```sql
SELECT
  CardCode AS [Key],
  CardName AS [Title],
  CardCode + ' - ' + ISNULL(LicTradNum, '') AS [Subtitle],
  CardCode,
  CardName,
  LicTradNum,
  Phone1
FROM OCRD
WHERE CardType IN ('C','S')
```

## 4. Universal Functions ampliadas

Tipos nuevos:

- `ContentCreator`
- `SQLReport`
- `FileExporter`
- `FileImporter`
- `CreateActivity`
- `Dashboard`
- `InternalMessage`
- `DataLauncher`

### ContentCreator

Payload:

```text
Cliente {{cliente}}
Total {{total}}
```

Parameters:

```json
{
  "targetFile": "C:\\B1TuneUp\\out\\pedido.txt",
  "cliente": "$[$4.0.0]",
  "total": "$[$29.0.0]"
}
```

### SQLReport

Payload:

```sql
SELECT TOP 100 CardCode, CardName, Balance FROM OCRD ORDER BY CardName
```

Parameters:

```json
{
  "targetFile": "C:\\B1TuneUp\\reports\\clientes.csv",
  "delimiter": ","
}
```

### FileImporter

Payload:

```text
C:\B1TuneUp\imports\clientes.txt
```

Parameters:

```json
{
  "mode": "MacroPerLine",
  "macro": "Log('Import line ${line}')"
}
```

### DataLauncher

Parameters:

```json
{
  "macro": "UF('SYNC_BP');Status('Cliente sincronizado')"
}
```

Test runner:

```csharp
var test = UniversalFunctionService.Test("SYNC_BP", SapUiSafe.TryGetActiveForm());
```

Devuelve `UniversalFunctionTestResult` y registra resultado en `AuditLogManager`.

## 5. Validation System profundo

Eventos cubiertos:

- `ITEM_PRESSED`
- `EDIT_VALIDATE`
- `COMBO_SELECT`
- `CHOOSE_FROM_LIST`
- `DATA_ADD_BEFORE`
- `DATA_UPDATE_BEFORE`
- `FORM_LOAD`

Para eventos de item se pasa `ItemUID`, `ColUID`, `Row` y `BeforeAction`.

Reglas por matriz:

```text
ItemName = 38.1
```

Condicion:

```sql
SELECT CASE WHEN '$[38.1]' = '' THEN 1 ELSE 0 END
```

El motor lee la fila del evento; si no hay fila, usa la fila seleccionada o la primera.

Trazas:

```csharp
var recent = ValidationTraceService.GetRecent("139", 50);
```

Cada `ValidationTraceEntry` guarda regla, formulario, item, columna, fila, evento, severidad, condicion procesada, resultado, bloqueo, mensaje y usuario.

## 6. Diagnostico y logs

Los puntos nuevos usan:

- `AuditLogManager.LogAction`
- `AuditLogManager.LogDetailedAction`
- `ExceptionLogger.LogHandled`

Areas auditadas:

- `ConsultantPackage`
- `B1SearchIndex`
- `UniversalFunction`
- `UniversalFunctionTest`
- `ValidationTrace`
- `UnifiedTrigger`

## 7. Checklist de go-live

1. Ejecutar `ConfigurationCenterService.RunDiagnostics()`.
2. Crear backup: `ConsultantConfigurationService.BackupSnapshot("Pre go-live")`.
3. Exportar paquete desde QA.
4. Ejecutar `PreviewImport` en PROD.
5. Importar paquete.
6. Reindexar busquedas: `SearchIndexService.RebuildAll()`.
7. Probar UF criticas con `UniversalFunctionService.Test`.
8. Activar trazas en triggers nuevos.
9. Ejecutar casos de validacion con usuarios reales.
10. Revisar `ValidationTraceService.GetRecent`.
11. Si algo bloquea mal, ejecutar rollback del backup.

## 8. Archivos principales

- `Modules/ConsultantConfigurationService.cs`
- `Modules/SearchIndexService.cs`
- `Modules/UniversalFunctionService.cs`
- `Modules/ValidationTraceService.cs`
- `Modules/AdvancedSearchService.cs`
- `Modules/RightClickMenuManager.cs`
- `Core/EventDispatcher.cs`
- `Utils/SapUiSafe.cs`
