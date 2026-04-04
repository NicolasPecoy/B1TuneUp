using System;
using System.Reflection;
using SAPbobsCOM;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using B1TuneUp.B1TuneUp.Modules;
using B1TuneUp.Modules.ItemActionsUi;
using B1TuneUp.Modules.LayoutManagerUi;

namespace B1TuneUp.Modules
{
    public class MacroEngine
    {
        public static void ExecuteMacro(string macroCommand, Form activeForm = null, int rowOverride = -1)
        {
            // Un motor de macros simple que interpreta comandos básicos
            // Ejemplo: "Click(1); SetValue(4, 'Hello'); Loop(38, 'SetValue(U_MyField, $[$38.1.0])');"

            string[] commands = macroCommand.Split(';');
            foreach (var cmd in commands)
            {
                ProcessCommand(cmd.Trim(), activeForm, rowOverride);
            }
        }

        private static void ProcessCommand(string command, Form activeForm, int rowOverride = -1)
        {
            if (string.IsNullOrEmpty(command)) return;

            try
            {
                if (activeForm == null) activeForm = B1App.Instance.Application.Forms.ActiveForm;

                if (command.StartsWith("Msg("))
                {
                    string msg = ExtractParameter(command, "Msg");
                    B1App.Instance.Application.MessageBox(ProcessSqlVariables(msg, activeForm, rowOverride));
                }
                else if (command.StartsWith("Status("))
                {
                    string msg = ExtractParameter(command, "Status");
                    B1App.Instance.Application.SetStatusBarMessage(ProcessSqlVariables(msg, activeForm, rowOverride), BoMessageTime.bmt_Short, false);
                }
                else if (command.StartsWith("Click("))
                {
                    string itemId = ExtractParameter(command, "Click");
                    activeForm.Items.Item(itemId).Click();
                }
                else if (command.StartsWith("SetValue("))
                {
                    string parameters = ExtractParameter(command, "SetValue");
                    string[] parts = parameters.Split(',');
                    if (parts.Length >= 2)
                    {
                        string itemId = parts[0].Trim('\'', ' ');
                        string val = ProcessSqlVariables(parts[1].Trim('\'', ' '), activeForm, rowOverride);
                        Item item = activeForm.Items.Item(itemId);

                        if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_EXTEDIT)
                        {
                            ((EditText)item.Specific).Value = val;
                        }
                        else if (item.Type == BoFormItemTypes.it_MATRIX && itemId.Contains("."))
                        {
                            // Soporte para SetValue(38.U_MyField, value)
                            string[] itemParts = itemId.Split('.');
                            Matrix matrix = (Matrix)activeForm.Items.Item(itemParts[0]).Specific;
                            int row = rowOverride != -1 ? rowOverride : matrix.GetNextSelectedRow(0, BoOrderType.ot_SelectionOrder);
                            if (row == -1) row = 1;
                            ((EditText)matrix.Columns.Item(itemParts[1]).Cells.Item(row).Specific).Value = val;
                        }
                    }
                }
                else if (command.StartsWith("OpenForm("))
                {
                    string formType = ExtractParameter(command, "OpenForm");
                    B1App.Instance.Application.ActivateMenuItem(formType);
                }
                else if (command.StartsWith("Loop("))
                {
                    string parameters = ExtractParameter(command, "Loop");
                    int firstComma = parameters.IndexOf(',');
                    if (firstComma != -1)
                    {
                        string matrixId = parameters.Substring(0, firstComma).Trim('\'', ' ');
                        string innerMacro = parameters.Substring(firstComma + 1).Trim('\'', ' ');
                        ProcessLoop(matrixId, innerMacro, activeForm);
                    }
                }
                else if (command.StartsWith("SQLExecute("))
                {
                    string sql = ExtractParameter(command, "SQLExecute");
                    sql = ProcessSqlVariables(sql, activeForm, rowOverride);
                    ExecuteNonQuery(sql);
                }
                else if (command.StartsWith("Transfer("))
                {
                    // Transfer(FromItem, ToItem, ToFormType)
                    string parameters = ExtractParameter(command, "Transfer");
                    string[] parts = parameters.Split(',');
                    if (parts.Length >= 3)
                    {
                        string fromId = parts[0].Trim();
                        string toId = parts[1].Trim();
                        string targetFormType = parts[2].Trim();
                        ProcessTransfer(fromId, toId, targetFormType, activeForm);
                    }
                }
                else if (command.StartsWith("Disable("))
                {
                    string itemId = ExtractParameter(command, "Disable");
                    activeForm.Items.Item(itemId).Enabled = false;
                }
                else if (command.StartsWith("Enable("))
                {
                    string itemId = ExtractParameter(command, "Enable");
                    activeForm.Items.Item(itemId).Enabled = true;
                }
                else if (command.StartsWith("Focus("))
                {
                    string itemId = ExtractParameter(command, "Focus");
                    activeForm.Items.Item(itemId).Click(BoCellClickType.ct_Regular);
                }
                else if (command.StartsWith("ActivateTab("))
                {
                    string tabId = ExtractParameter(command, "ActivateTab");
                    ((Folder)activeForm.Items.Item(tabId).Specific).Select();
                }
                else if (command.StartsWith("Code("))
                {
                    string codeName = ExtractParameter(command, "Code");
                    DynamicCodeEngine.RunCode(codeName, activeForm);
                }
                else if (command.StartsWith("ShowPad()"))
                {
                    ActionPadManager.ShowPadForForm(activeForm);
                }
                else if (command.StartsWith("Launch("))
                {
                    string path = ExtractParameter(command, "Launch");
                    System.Diagnostics.Process.Start(path);
                }
                else if (command.StartsWith("Copy("))
                {
                    string text = ExtractParameter(command, "Copy");
                    text = ProcessSqlVariables(text, activeForm, rowOverride);
                    System.Windows.Forms.Clipboard.SetText(text);
                }
                else if (command.StartsWith("OpenSRF("))
                {
                    string parameters = ExtractParameter(command, "OpenSRF");
                    string[] parts = parameters.Split(',');
                    string file = parts[0].Trim('\'', ' ');
                    string uid = parts.Length > 1 ? parts[1].Trim('\'', ' ') : "";
                    FormLoader.LoadFromSRF(file, uid);
                }
                else if (command.StartsWith("Email("))
                {
                    string docEntry = ExtractParameter(command, "Email");
                    EmailManager.SendEmail(docEntry);
                }
                else if (command.StartsWith("QuickCreate("))
                {
                    // QuickCreate(FormType, FieldValues)
                    // Ej: QuickCreate(171, 'CardCode=C1;CardName=Test')
                    string parameters = ExtractParameter(command, "QuickCreate");
                    ProcessQuickCreate(parameters);
                }
                else if (command.StartsWith("Search()"))
                {
                    B1SearchManager.OpenSearchForm();
                }
                else if (command.StartsWith("Refresh()"))
                {
                    activeForm.Refresh();
                }
                else if (command.StartsWith("Freeze("))
                {
                    string freezeStr = ExtractParameter(command, "Freeze").ToUpper();
                    activeForm.Freeze(freezeStr == "TRUE" || freezeStr == "Y" || freezeStr == "1");
                }
                else if (command.StartsWith("Stop()"))
                {
                    throw new Exception("Macro detenida por comando Stop()");
                }
                else if (command.StartsWith("Close()"))
                {
                    activeForm.Close();
                }
                else if (command.StartsWith("Maximize()"))
                {
                    activeForm.State = BoFormStateEnum.fs_Maximized;
                }
                else if (command.StartsWith("Minimize()"))
                {
                    activeForm.State = BoFormStateEnum.fs_Minimized;
                }
                else if (command.StartsWith("Restore()"))
                {
                    activeForm.State = BoFormStateEnum.fs_Restore;
                }
                else if (command.StartsWith("IF("))
                {
                    ProcessIfCommand(command, activeForm, rowOverride);
                }
                else if (command.StartsWith("FileExists("))
                {
                    string path = ExtractParameter(command, "FileExists");
                    path = ProcessSqlVariables(path, activeForm, rowOverride);
                    bool exists = System.IO.File.Exists(path);
                    B1App.Instance.Application.SetStatusBarMessage($"Archivo {(exists ? "encontrado" : "no encontrado")}: {path}", BoMessageTime.bmt_Short, false);
                }
                else if (command.StartsWith("CreateFolder("))
                {
                    string path = ExtractParameter(command, "CreateFolder");
                    path = ProcessSqlVariables(path, activeForm, rowOverride);
                    if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                }
                else if (command.StartsWith("DeleteFile("))
                {
                    string path = ExtractParameter(command, "DeleteFile");
                    path = ProcessSqlVariables(path, activeForm, rowOverride);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                else if (command.StartsWith("RunMDM("))
                {
                    string mdmId = ExtractParameter(command, "RunMDM");
                    MasterDataManager.RunMDM(mdmId);
                }
                else if (command.StartsWith("Log("))
                {
                    string details = ExtractParameter(command, "Log");
                    details = ProcessSqlVariables(details, activeForm, rowOverride);
                    AuditLogManager.LogAction("UserLog", details);
                }
                else if (command.StartsWith("SaveForm("))
                {
                    activeForm.PaneLevel = 1;
                    activeForm.Items.Item(activeForm.UniqueID).Click(BoCellClickType.ct_Regular);
                    activeForm.Items.Item(activeForm.UniqueID).Click(BoCellClickType.ct_Regular);
                }
                else if (command.StartsWith("ExportToExcel("))
                {
                    string parameters = ExtractParameter(command, "ExportToExcel");
                    ProcessExportToExcel(parameters, activeForm);
                }
                else if (command.StartsWith("ImportFromExcel("))
                {
                    string parameters = ExtractParameter(command, "ImportFromExcel");
                    ProcessImportFromExcel(parameters, activeForm);
                }
                else if (command.StartsWith("Print("))
                {
                    string reportName = ExtractParameter(command, "Print");
                    ProcessPrint(reportName, activeForm);
                }
                else if (command.StartsWith("SendToPrinter("))
                {
                    string printerName = ExtractParameter(command, "SendToPrinter");
                    ProcessSendToPrinter(printerName, activeForm);
                }
                else if (command.StartsWith("ShowDashboard()"))
                {
                    DashboardManager.ShowDashboard();
                }
                else if (command.StartsWith("CreateTemplate()"))
                {
                    TemplateManager.CreateTemplate(activeForm);
                }
                else if (command.StartsWith("LoadTemplate()"))
                {
                    TemplateManager.LoadTemplate(activeForm);
                }
                else if (command.StartsWith("ManageTemplates()"))
                {
                    TemplateManager.ManageTemplates();
                }
                else if (command.StartsWith("RecurringInvoices()"))
                {
                    RecurringInvoiceManager.OpenRecurringInvoiceForm();
                }
                else if (command.StartsWith("LetterMerge()"))
                {
                    LetterMergeManager.OpenLetterMergeForm();
                }
                else if (command.StartsWith("ExchangeRates()"))
                {
                    ExchangeRateManager.OpenExchangeRatesForm();
                }
                else if (command.StartsWith("PLDExtensions()"))
                {
                    PLDExtensionsManager.OpenPLDExtensionsForm();
                }
                else if (command.StartsWith("ValidationSystem()"))
                {
                    ValidationManager.OpenValidationForm();
                }
                else if (command.StartsWith("PrintDelivery()"))
                {
                    PrintDeliveryManager.OpenPrintDeliveryForm();
                }
                else if (command.StartsWith("REST("))
                {
                    // REST(url, method, body, headers)
                    string parameters = ExtractParameter(command, "REST");
                    string[] parts = parameters.Split(',');
                    string url = parts.Length > 0 ? parts[0].Trim('\'', ' ') : "";
                    string method = parts.Length > 1 ? parts[1].Trim('\'', ' ') : "GET";
                    string body = parts.Length > 2 ? parts[2].Trim('\'', ' ') : null;
                    string headers = parts.Length > 3 ? parts[3].Trim('\'', ' ') : null;
                    string resp = IntegrationManager.CallRest(url, method, body, headers);
                    B1App.Instance.Application.SetStatusBarMessage(resp.Length > 200 ? resp.Substring(0, 200) + "..." : resp, BoMessageTime.bmt_Short, false);
                }
                else if (command.StartsWith("SOAP("))
                {
                    // SOAP(url, action, body)
                    string parameters = ExtractParameter(command, "SOAP");
                    string[] parts = parameters.Split(',');
                    string url = parts.Length > 0 ? parts[0].Trim('\'', ' ') : "";
                    string action = parts.Length > 1 ? parts[1].Trim('\'', ' ') : "";
                    string body = parts.Length > 2 ? parts[2].Trim('\'', ' ') : "";
                    string resp = IntegrationManager.CallSoap(url, action, body);
                    B1App.Instance.Application.SetStatusBarMessage(resp.Length > 200 ? resp.Substring(0, 200) + "..." : resp, BoMessageTime.bmt_Short, false);
                }
                else if (command.StartsWith("StartSync("))
                {
                    // StartSync(id, url, intervalSeconds, handlerMacro)
                    string parameters = ExtractParameter(command, "StartSync");
                    string[] parts = parameters.Split(',');
                    string id = parts.Length > 0 ? parts[0].Trim('\'', ' ') : "";
                    string url = parts.Length > 1 ? parts[1].Trim('\'', ' ') : "";
                    int interval = 60;
                    int.TryParse(parts.Length > 2 ? parts[2].Trim() : "60", out interval);
                    string handler = parts.Length > 3 ? parts[3].Trim('\'', ' ') : "";
                    IntegrationManager.StartRealTimeSync(id, url, interval, handler);
                    B1App.Instance.Application.SetStatusBarMessage($"Sync iniciado: {id}", BoMessageTime.bmt_Short, false);
                }
                else if (command.StartsWith("StopSync("))
                {
                    string id = ExtractParameter(command, "StopSync").Trim('\'', ' ');
                    IntegrationManager.StopRealTimeSync(id);
                    B1App.Instance.Application.SetStatusBarMessage($"Sync detenido: {id}", BoMessageTime.bmt_Short, false);
                }
                else if (command.StartsWith("ImportCsv("))
                {
                    // ImportCsv(filePath, mapping)
                    string parameters = ExtractParameter(command, "ImportCsv");
                    string[] parts = parameters.Split(',');
                    string file = parts.Length > 0 ? parts[0].Trim('\'', ' ') : "";
                    string mapping = parts.Length > 1 ? parts[1].Trim('\'', ' ') : "";
                    bool ok = IntegrationManager.ImportCsvToForm(file, mapping, activeForm);
                    B1App.Instance.Application.SetStatusBarMessage(ok ? "CSV importado" : "Error importando CSV", BoMessageTime.bmt_Short, !ok);
                }
                else if (command.StartsWith("ExportCsv("))
                {
                    // ExportCsv(filePath, gridId, mapping)
                    string parameters = ExtractParameter(command, "ExportCsv");
                    string[] parts = parameters.Split(',');
                    string file = parts.Length > 0 ? parts[0].Trim('\'', ' ') : "";
                    string gridId = parts.Length > 1 ? parts[1].Trim('\'', ' ') : "";
                    string mapping = parts.Length > 2 ? parts[2].Trim('\'', ' ') : "";
                    bool ok = IntegrationManager.ExportGridToCsv(file, activeForm, gridId, mapping);
                    B1App.Instance.Application.SetStatusBarMessage(ok ? "CSV exportado" : "Error exportando CSV", BoMessageTime.bmt_Short, !ok);
                }
                else if (command.StartsWith("OpenReportCustomization()"))
                {
                    ReportManager.OpenReportCustomizationForm();
                }
                else if (command.StartsWith("OpenMapper()"))
                {
                    DynamicMapperManager.OpenMappingManagerForm();
                }
                else if (command.StartsWith("OpenItemPlacement()"))
                {
                    UIEnhancementsManager.ShowAdvancedDashboard(); // placeholder to ensure UIEnhancementsManager is touched
                    // Open for active form
                    try { ItemPlacementManager.OpenPlacementForm(activeForm); } catch { ItemPlacementManager.OpenPlacementForm(null); }
                }
                else if (command.StartsWith("OpenQueryExport()"))
                {
                    QueryExportManager.OpenQueryExportWindow(activeForm);
                }
                else if (command.StartsWith("EditItem("))
                {
                    string itemId = ExtractParameter(command, "EditItem");
                    try { ItemEditorManager.OpenItemEditor(itemId, activeForm); } catch { ItemEditorManager.OpenItemEditor(itemId, null); }
                }
                else if (command.StartsWith("AddItem()"))
                {
                    ItemEditorManager.OpenAddItemForm(activeForm);
                }
                else if (command.StartsWith("DeleteItem("))
                {
                    string itemId = ExtractParameter(command, "DeleteItem");
                    try { ItemEditorManager.DeleteItem(itemId, activeForm); } catch { ItemEditorManager.DeleteItem(itemId, null); }
                }
                else if (command.StartsWith("ManageItemActions()"))
                {
                    ItemActionsLauncher.Show();
                }
                else if (command.StartsWith("OpenDesigner()"))
                {
                    try { var d = new Forms.DesignSurfaceForm(activeForm); d.Show(); } catch { var d = new Forms.DesignSurfaceForm(null); d.Show(); }
                }
                else if (command.StartsWith("ExportSRF("))
                {
                    string file = ExtractParameter(command, "ExportSRF");
                    try { ItemPlacementManager.ExportSrf(activeForm, file); } catch { }
                }
                else if (command.StartsWith("ImportSRF("))
                {
                    string file = ExtractParameter(command, "ImportSRF");
                    try { ItemPlacementManager.ImportSrf(file); } catch { }
                }
                else if (command.StartsWith("ManageLayouts()"))
                {
                    LayoutManagerLauncher.Show();
                }
                else if (command.StartsWith("ManageReports()"))
                {
                    ReportManager.ManageReportTemplates();
                }
                else if (command.StartsWith("ShowReportPreview("))
                {
                    string tpl = ExtractParameter(command, "ShowReportPreview");
                    var pars = ReportManager.GetReportParameters(tpl);
                    ReportManager.ShowAdvancedPrintPreview(tpl, pars);
                }
                else if (command.StartsWith("EnableDragDrop()"))
                {
                    UIEnhancementsManager.EnableDragAndDrop(activeForm);
                }
                else if (command.StartsWith("OpenRichText("))
                {
                    string itemId = ExtractParameter(command, "OpenRichText");
                    UIEnhancementsManager.OpenRichTextEditor(itemId, activeForm);
                }
                else if (command.StartsWith("EnhanceGrid("))
                {
                    string gridId = ExtractParameter(command, "EnhanceGrid");
                    UIEnhancementsManager.EnhanceGridWithPivot(activeForm, gridId);
                }
                else if (command.StartsWith("ScanBarcode("))
                {
                    string tgt = ExtractParameter(command, "ScanBarcode");
                    UIEnhancementsManager.ScanBarcode(tgt, activeForm);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en Macro: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"MacroEngine.ProcessCommand:{command}");
            }
        }

        private static void ProcessIfCommand(string command, Form activeForm, int rowOverride)
        {
            // IF(ConditionSQL) THEN { Macro1 } ELSE { Macro2 }
            // Sintaxis simple para este ejemplo
            int thenIdx = command.IndexOf(" THEN ");
            if (thenIdx == -1) return;

            string condition = command.Substring(3, thenIdx - 3).Trim('(', ')', ' ');
            string rest = command.Substring(thenIdx + 6);

            int elseIdx = rest.IndexOf(" ELSE ");
            string thenMacro = "";
            string elseMacro = "";

            if (elseIdx != -1)
            {
                thenMacro = rest.Substring(0, elseIdx).Trim('{', '}', ' ');
                elseMacro = rest.Substring(elseIdx + 6).Trim('{', '}', ' ');
            }
            else
            {
                thenMacro = rest.Trim('{', '}', ' ');
            }

            if (CheckCondition(condition, activeForm))
            {
                ExecuteMacro(thenMacro, activeForm, rowOverride);
            }
            else if (!string.IsNullOrEmpty(elseMacro))
            {
                ExecuteMacro(elseMacro, activeForm, rowOverride);
            }
        }

        private static void ProcessQuickCreate(string parameters)
        {
            try
            {
                int firstComma = parameters.IndexOf(',');
                if (firstComma != -1)
                {
                    string formType = parameters.Substring(0, firstComma).Trim();
                    string fieldValues = parameters.Substring(firstComma + 1).Trim('\'', ' ');

                    // Abrir el formulario y asignar valores
                    B1App.Instance.Application.ActivateMenuItem(formType);
                    Form oForm = B1App.Instance.Application.Forms.ActiveForm;

                    string[] pairs = fieldValues.Split(';');
                    foreach (var pair in pairs)
                    {
                        string[] kv = pair.Split('=');
                        if (kv.Length == 2)
                        {
                            string itemId = kv[0].Trim();
                            string val = kv[1].Trim();
                            ((EditText)oForm.Items.Item(itemId).Specific).Value = val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en QuickCreate: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteNonQuery(string sql)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try { rs.DoQuery(sql); }
            finally { ComObjectManager.Release(rs); }
        }

        private static void ProcessTransfer(string fromId, string toId, string targetFormType, Form activeForm)
        {
            try
            {
                string val = "";
                Item item = activeForm.Items.Item(fromId);
                if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_EXTEDIT)
                    val = ((EditText)item.Specific).Value;

                // Buscar la ventana destino
                for (int i = 0; i < B1App.Instance.Application.Forms.Count; i++)
                {
                    Form targetForm = B1App.Instance.Application.Forms.Item(i);
                    if (targetForm.TypeEx == targetFormType)
                    {
                        Item targetItem = targetForm.Items.Item(toId);
                        if (targetItem.Type == BoFormItemTypes.it_EDIT || targetItem.Type == BoFormItemTypes.it_EXTEDIT)
                            ((EditText)targetItem.Specific).Value = val;
                        break;
                    }
                }
            }
            catch { }
        }

        private static void InvokeCustomHandler(string parameters, Form activeForm, int rowOverride)
        {
            if (string.IsNullOrWhiteSpace(parameters)) return;

            try
            {
                var parts = parameters.Split(',');
                string typeName = parts.Length > 0 ? parts[0].Trim('\'', ' ', '\t') : string.Empty;
                string methodName = parts.Length > 1 ? parts[1].Trim('\'', ' ', '\t') : "Execute";
                string argPayload = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                string assemblyHint = parts.Length > 3 ? parts[3].Trim('\'', ' ', '\t') : null;

                if (string.IsNullOrWhiteSpace(typeName))
                {
                    B1App.Instance.Application.SetStatusBarMessage("InvokeHandler: tipo no especificado.", BoMessageTime.bmt_Short, true);
                    return;
                }

                if (!string.IsNullOrEmpty(argPayload))
                {
                    argPayload = ProcessSqlVariables(argPayload.Trim('\'', ' ', '\t'), activeForm, rowOverride);
                }

                string[] handlerArgs = string.IsNullOrEmpty(argPayload) ? Array.Empty<string>() : argPayload.Split('|');
                Type handlerType = ResolveHandlerType(typeName, assemblyHint);
                if (handlerType == null)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"InvokeHandler: tipo '{typeName}' no encontrado.", BoMessageTime.bmt_Short, true);
                    return;
                }

                var binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                MethodInfo methodInfo = handlerType.GetMethod(methodName, binding);
                if (methodInfo == null)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"InvokeHandler: metodo '{methodName}' no encontrado en {typeName}.", BoMessageTime.bmt_Short, true);
                    return;
                }

                object instance = methodInfo.IsStatic ? null : Activator.CreateInstance(handlerType);
                object[] invokeParams = BuildHandlerParameters(methodInfo.GetParameters(), activeForm, handlerArgs, rowOverride);
                methodInfo.Invoke(instance, invokeParams);
            }
            catch (TargetInvocationException tex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"InvokeHandler error: {tex.InnerException?.Message ?? tex.Message}", BoMessageTime.bmt_Short, true);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"InvokeHandler error: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static object[] BuildHandlerParameters(ParameterInfo[] parameters, Form activeForm, string[] handlerArgs, int rowOverride)
        {
            if (parameters == null || parameters.Length == 0) return Array.Empty<object>();
            var values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.ParameterType == typeof(Form)) values[i] = activeForm;
                else if (p.ParameterType == typeof(Application)) values[i] = B1App.Instance.Application;
                else if (p.ParameterType == typeof(SAPbobsCOM.Company)) values[i] = B1App.Instance.Company;
                else if (p.ParameterType == typeof(string[])) values[i] = handlerArgs;
                else if (p.ParameterType == typeof(string)) values[i] = handlerArgs != null ? string.Join("|", handlerArgs) : string.Empty;
                else if (p.ParameterType == typeof(int)) values[i] = rowOverride;
                else values[i] = null;
            }
            return values;
        }

        private static Type ResolveHandlerType(string typeName, string assemblyHint)
        {
            Type resolved = null;
            try
            {
                resolved = Type.GetType(typeName, false);
                if (resolved != null) return resolved;
                if (!string.IsNullOrWhiteSpace(assemblyHint))
                {
                    resolved = Type.GetType($"{typeName}, {assemblyHint}", false);
                    if (resolved != null) return resolved;
                    if (System.IO.File.Exists(assemblyHint))
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(assemblyHint);
                            resolved = asm.GetType(typeName, false);
                            if (resolved != null) return resolved;
                        }
                        catch { }
                    }
                }
                resolved = typeof(MacroEngine).Assembly.GetType(typeName, false);
            }
            catch { }
            return resolved;
        }

        private static void ProcessLoop(string matrixId, string innerMacro, Form activeForm)
        {
            Item item = activeForm.Items.Item(matrixId);
            if (item.Type == BoFormItemTypes.it_MATRIX)
            {
                Matrix matrix = (Matrix)item.Specific;
                for (int i = 1; i <= matrix.RowCount; i++)
                {
                    ExecuteMacro(innerMacro, activeForm, i);
                }
            }
        }

        private static string ExtractParameter(string command, string funcName)
        {
            int start = funcName.Length + 1;
            int end = command.LastIndexOf(')');
            if (start < end)
            {
                return command.Substring(start, end - start).Trim('\'');
            }
            return string.Empty;
        }

        public static bool CheckCondition(string sql, Form activeForm = null)
        {
            if (string.IsNullOrEmpty(sql)) return true;

            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                // Reemplazar variables dinámicas como $[$38.1.0] por valores del formulario actual
                string processedSql = ProcessSqlVariables(sql, activeForm);
                rs.DoQuery(processedSql);

                if (rs.RecordCount > 0)
                {
                    string result = rs.Fields.Item(0).Value.ToString().ToUpper();
                    return result == "TRUE" || result == "Y" || result == "1" || result == "VALID";
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string ProcessSqlVariables(string input, Form activeForm, int rowOverride = -1)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (activeForm == null) activeForm = B1App.Instance.Application.Forms.ActiveForm;

            // Patrón básico: $[$Item.Col.Type]
            try
            {
                int startIdx = input.IndexOf("$[$");
                while (startIdx != -1)
                {
                    int endIdx = input.IndexOf("]", startIdx);
                    if (endIdx == -1) break;

                    string fullTag = input.Substring(startIdx, endIdx - startIdx + 1);
                    string content = fullTag.Substring(3, fullTag.Length - 4); // Quitar $[$ y ]
                    string[] parts = content.Split('.');

                    string itemId = parts[0];
                    string colId = parts.Length > 1 ? parts[1] : "";

                    string val = "";
                    Item item = activeForm.Items.Item(itemId);

                    if (item.Type == BoFormItemTypes.it_MATRIX && !string.IsNullOrEmpty(colId))
                    {
                        Matrix matrix = (Matrix)item.Specific;
                        int row = rowOverride;
                        if (row == -1)
                        {
                            row = matrix.GetNextSelectedRow(0, BoOrderType.ot_SelectionOrder);
                            if (row == -1) row = 1;
                        }

                        if (row > 0 && row <= matrix.RowCount)
                        {
                            val = ((EditText)matrix.Columns.Item(colId).Cells.Item(row).Specific).Value;
                        }
                    }
                    else if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_EXTEDIT || item.Type == BoFormItemTypes.it_COMBO_BOX)
                    {
                        if (item.Type == BoFormItemTypes.it_COMBO_BOX)
                            val = ((ComboBox)item.Specific).Selected?.Value ?? "";
                        else
                            val = ((EditText)item.Specific).Value;
                    }

                    input = input.Replace(fullTag, val);
                    startIdx = input.IndexOf("$[$", startIdx + val.Length);
                }
            }
            catch (Exception) { }

            return input;
        }

        private static void ProcessExportToExcel(string parameters, Form activeForm)
        {
            try
            {
                // Parse parameters (e.g., filename, sheet name, etc.)
                string fileName = parameters;
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                }

                // Look for grid in the form to export data
                foreach (Item item in activeForm.Items)
                {
                    if (item.Type == BoFormItemTypes.it_GRID)
                    {
                        B1App.Instance.Application.SetStatusBarMessage("Exportación a Excel no disponible en esta versión", BoMessageTime.bmt_Short, true);
                        return;
                    }
                }

                B1App.Instance.Application.SetStatusBarMessage("No se encontró grilla para exportar", BoMessageTime.bmt_Short, true);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error exportando: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ProcessImportFromExcel(string parameters, Form activeForm)
        {
            try
            {
                // Import functionality would depend on specific form structure
                B1App.Instance.Application.SetStatusBarMessage("Importación desde Excel no implementada", BoMessageTime.bmt_Short, true);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error importando desde Excel: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ProcessPrint(string reportName, Form activeForm)
        {
            try
            {
                // Print functionality
                // In SAP B1, printing is usually done through specific menu items or document reports
                B1App.Instance.Application.ActivateMenuItem("2026"); // Standard print menu item
                B1App.Instance.Application.SetStatusBarMessage($"Impresión iniciada para: {reportName}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error imprimiendo: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ProcessSendToPrinter(string printerName, Form activeForm)
        {
            try
            {
                // Send to specific printer - simplified approach
                B1App.Instance.Application.ActivateMenuItem("2026"); // Standard print menu item
                B1App.Instance.Application.SetStatusBarMessage($"Documento enviado a la impresora: {printerName}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error enviando a impresora: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
