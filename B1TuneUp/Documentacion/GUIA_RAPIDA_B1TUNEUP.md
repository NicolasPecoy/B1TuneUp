# Guía Rápida B1TuneUp (abril 2026)

## 1. Arranque Exprés (5 minutos)

1. **Instala** el add-on (`.\\installer\\deploy.ps1`).
2. **Abre SAP B1**, inicia el add-on y acepta la creación de tablas `@BTUN_*`.
3. **Configura idioma** desde `Módulos > B1TuneUp > Configuración` (Español por defecto).
4. **Verifica los menús**: deberían aparecer todos los estudios WPF bajo el menú B1TuneUp.

> Si un menú no aparece, abre Automation Dashboard y ejecuta "Recrear menús".

---

## 2. Mapa de Estudios WPF en un vistazo

| Estudio | Qué resuelve | Pasos rápidos |
|---------|--------------|--------------|
| Integration Studio | Conecta APIs REST/SOAP, transforma y agenda envíos. | 1) Crea escenario 2) Configura credenciales 3) Prueba y agenda |
| Scheduler Studio | Agenda macros, integraciones o DLLs. | 1) "Nuevo Job" 2) Define acción 3) Programa y prueba |
| MacroEngine / Rule Builder | Define reglas IF/THEN y macros con InvokeHandler. | 1) Selecciona FormType 2) Escribe macro 3) Probar en vivo |
| UiCustomizer + Item Placement | Ajusta formularios, añade botones y helpers UI. | 1) Captura formulario 2) Modifica layout 3) Publica versión |
| Validation & Mandatory Fields | Reglas de negocio y campos obligatorios. | 1) Filtra formulario 2) Define condición 3) Simula y guarda |
| Process Designer / Workflow | Modela ProcessSteps y asigna responsables. | 1) Arrastra nodos 2) Configura propiedades 3) Publica |
| Automation Dashboard | Controla menús, macros y despliegues por ambiente. | 1) Selecciona entorno 2) Marca elementos 3) Deploy/Test |
| Dashboard/Search/Macro | Diseña dashboards, búsquedas y macros globales. | 1) Crea widget 2) Define SQL 3) Publica |
| Form Enhancements Studio | Valores por defecto, locks y settings visuales. | 1) Selecciona FormType 2) Crea regla 3) Sincroniza |
| Template & Report Studio | Gestiona plantillas Crystal/PDF. | 1) Sube archivo 2) Configura parámetros 3) Vista previa |
| Email / Notification Designer | Diseña correos HTML y notificaciones. | 1) Edita plantilla 2) Envía prueba 3) Publica |
| Action Pad / Quick Copy / Item Actions | Paneles de acción, copiar documentos, botones custom. | 1) Diseña Pad 2) Configura Quick Copy 3) Asocia Item Action |
| Audit Log Viewer | Inspecciona `@BTUN_LOG`. | 1) Filtra 2) Exporta CSV 3) Abre registro |
| Toolbox Settings | Ajustes globales (SMTP, paths, flags). | 1) Selecciona compañía 2) Edita valores 3) Guardar |

---

## 3. Ejemplos paso a paso

### 3.1 Integración REST diaria

1. **Integration Studio ? "Nuevo"**: Nombre `CRM_ClientSync`.
2. **Definir endpoint**: `https://crm/api/customer`, método `GET`, header `Authorization: Bearer ...`.
3. **Variables**: agrega `CompanyDB`, `LastSync`.
4. **Prueba en vivo** con botón "Ejecutar".
5. **Agenda**: presiona "Enviar a Scheduler", frecuencia diaria 08:00.
6. **Audita**: revisa pestaña "Historial" o tabla `@BTUN_INTLOG`.

### 3.2 Botón con lógica personalizada (InvokeHandler)

1. **UiCustomizer**: captura `FormType 149`, agrega botón `btnEnviarLogistica`.
2. **Item Actions**: crea acción "Enviar a logística" con macro:
   ```
   InvokeHandler('B1TuneUp.CustomLogic.LogisticsHandler','Enviar','$[$DocEntry.0.0]','B1TuneUp.CustomLogic.dll')
   ```
3. **Desarrolla handler** (ver sección 6) y copia DLL junto al add-on.
4. **Prueba** el botón desde SAP; revisa Audit Log si falla.

### 3.3 Validación condicional

1. **Validation Studio** ? Formulario 133 (Orden de compra).
2. Evento `Before Add`, condición SQL:
   ```sql
   SELECT CASE WHEN '$[$U_TipoDoc.0.0]' = 'IMP' AND ISNULL('$[$Incoterms.0.0]', '') = '' THEN 'FAIL' ELSE 'OK' END
   ```
3. Acción: mensaje "Completa Incoterms" + `Stop()`.
4. Simula con formulario abierto y guarda.

### 3.4 Crear dashboard + búsqueda

1. **Dashboard/Search/Macro Studio**.
2. Pestaña Dashboard ? "Nuevo Widget" ? Query de ventas del día.
3. Pestaña Search ? define campos `CardCode`, `CardName`, `Balance`.
4. Publica y, si quieres compartir, usa Automation Dashboard para desplegar en QA/PROD.

### 3.5 Plantilla de email transaccional

1. **Email Designer** ? "Nueva plantilla".
2. Cuerpo HTML con variables `{{CardName}}`, `{{DocTotal}}`.
3. Botón "Enviar prueba" usando SMTP del Toolbox.
4. Liga la plantilla a un escenario en Scheduler o a una regla en MacroEngine (`EmailTemplate('ALTA_CLIENTE')`).

---

## 4. Tu primera macro moderna

```sql
INSERT INTO [@BTUN_RULES]
    (U_FormType, U_Type, U_EventType, U_Before, U_Action)
VALUES
    ('139', 'Macro', 'et_FORM_LOAD', 0,
     "Status('Listo para vender'); InvokeHandler('B1TuneUp.CustomLogic.OnLoadHandler')");
```

- `Status` muestra mensaje en la barra inferior.
- `InvokeHandler` permite ejecutar C# externo (si no especificas método usa `Execute`).

---

## 5. Referencias mínimas de comandos

| Acción | Macro |
|--------|-------|
| Mostrar mensaje modal | `Msg('Texto')` |
| Mensaje barra estado | `Status('Procesando...')` |
| Setear valor | `SetValue('ItemUID','valor')` |
| Click botón | `Click('btnOK')` |
| Abrir estudio WPF | `ShowStudio('Integration')` |
| Ejecutar integración | `RunIntegration('CRM_ClientSync')` |
| Llamada REST | `REST(url, method, body, headers)` |
| Programar desde macro | `Schedule('NombreJob','Macro()', minutos)` |

Variables dinámicas siguen la sintaxis `$[$Item.Col.Row]`.

---

## 6. Plantilla de handler para InvokeHandler

```csharp
namespace B1TuneUp.CustomLogic
{
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
}
```

Copiar la DLL junto al add-on o registrar ruta en Toolbox ? Paths.

---

## 7. Troubleshooting rápido

| Problema | Revisión |
|----------|----------|
| Menú no aparece | Automation Dashboard ? "Recrear menús"; revisar `LocalizationManager`.
| El WPF no abre | Validar que `Microsoft.WebView2` esté instalado y logs en `%appdata%\B1TuneUp`.
| Macro no corre | Tabla `@BTUN_LOG`, columna `U_Detail`; habilitar modo debug en Toolbox.
| Error InvokeHandler | Confirmar nombre de clase, método y DLL; revisar dependencias.

---

## 8. Checklist antes de ir a producción

- [ ] Automation Dashboard sincronizado (menús/macros/reportes).
- [ ] Backups de tablas `@BTUN_*` exportados.
- [ ] Estudios críticos probados (Integration, Validation, Scheduler, Dashboards).
- [ ] Handlers personalizados almacenados en repositorio y versionados.
- [ ] Usuarios finales capacitados (mínimo 1 hora recorriendo los estudios relevantes).

---

**Actualizado:** 3 de abril de 2026 – Equipo B1TuneUp.
