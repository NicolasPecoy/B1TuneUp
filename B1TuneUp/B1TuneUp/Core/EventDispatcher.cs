using System;
using System.Collections.Generic;
using System.Linq;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Core
{
    public class EventDispatcher
    {
        private static EventDispatcher _instance;
        public static EventDispatcher Instance => _instance ?? (_instance = new EventDispatcher());

        private List<B1Rule> _rules;
        // Local handlers for item changes: key = FormUID|ItemUID
        private readonly Dictionary<string, List<Action<SAPbouiCOM.Form, string>>> _localItemChangeHandlers = new Dictionary<string, List<Action<SAPbouiCOM.Form, string>>>();

        private EventDispatcher()
        {
            _rules = new List<B1Rule>();
            LoadRules();
            MenuManager.LoadCustomMenus();
            ToolboxManager.ApplyToolboxSettings();
        }
        private void LoadRules()
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT * FROM \"@BTUN_RULES\""
                    : "SELECT * FROM [@BTUN_RULES]";

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    _rules.Add(new B1Rule
                    {
                        ID = rs.Fields.Item("DocEntry").Value.ToString(),
                        FormType = rs.Fields.Item("U_FormType").Value.ToString(),
                        Type = (RuleType)Enum.Parse(typeof(RuleType), rs.Fields.Item("U_Type").Value.ToString()),
                        EventType = rs.Fields.Item("U_EventType").Value.ToString(),
                        BeforeAction = rs.Fields.Item("U_Before").Value.ToString() == "Y",
                        Condition = rs.Fields.Item("U_Condition").Value.ToString(),
                        Action = rs.Fields.Item("U_Action").Value.ToString()
                    });
                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                // Si la tabla no existe aún o falla la carga, registramos el error
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando reglas: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, "EventDispatcher.LoadRules");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            // Regla por defecto para pruebas si no hay ninguna en BD
            if (_rules.Count == 0)
            {
                _rules.Add(new B1Rule
                {
                    ID = "R001",
                    FormType = "139",
                    Type = RuleType.Macro,
                    EventType = "et_ITEM_CLICK",
                    BeforeAction = true,
                    Action = "Msg('B1TuneUp activo en Pedidos');"
                });
            }
        }

        public void Init()
        {
            B1App.Instance.Application.ItemEvent += OnItemEvent;
            B1App.Instance.Application.MenuEvent += OnMenuEvent;
            B1App.Instance.Application.AppEvent += OnAppEvent;
            B1App.Instance.Application.FormDataEvent += OnFormDataEvent;
            B1App.Instance.Application.RightClickEvent += OnRightClickEvent;
            B1App.Instance.Application.LayoutKeyEvent += OnLayoutKeyBefore;
        }

        /// <summary>
        /// Register a local handler that will be invoked when the specified item on the given form changes.
        /// </summary>
        public void RegisterLocalItemChangeHandler(SAPbouiCOM.Form form, string itemId, Action<SAPbouiCOM.Form, string> handler)
        {
            if (form == null || string.IsNullOrEmpty(itemId) || handler == null) return;
            string key = form.UniqueID + "|" + itemId;
            lock (_localItemChangeHandlers)
            {
                if (!_localItemChangeHandlers.ContainsKey(key)) _localItemChangeHandlers[key] = new List<Action<SAPbouiCOM.Form, string>>();
                _localItemChangeHandlers[key].Add(handler);
            }
        }

        /// <summary>
        /// Unregister all local handlers for a specific form and item.
        /// </summary>
        public void UnregisterLocalItemChangeHandler(SAPbouiCOM.Form form, string itemId)
        {
            if (form == null || string.IsNullOrEmpty(itemId)) return;
            string key = form.UniqueID + "|" + itemId;
            lock (_localItemChangeHandlers)
            {
                if (_localItemChangeHandlers.ContainsKey(key))
                {
                    _localItemChangeHandlers.Remove(key);
                }
            }
        }

        public void UnregisterLocalItemChangeHandlers(string formUID)
        {
            if (string.IsNullOrEmpty(formUID)) return;
            string prefix = formUID + "|";
            lock (_localItemChangeHandlers)
            {
                foreach (var key in _localItemChangeHandlers.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    _localItemChangeHandlers.Remove(key);
                }
            }
        }

        private void OnLayoutKeyBefore(ref LayoutKeyInfo eventInfo, out bool BubbleEvent)
        {
            BubbleEvent = true;
            // Posibilidad de interceptar la llave de impresión para Crystal Reports
        }

        private void OnItemEvent(string FormUID, ref ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                // Manejar eventos del Action Pad
                if (pVal.FormTypeEx == "BTUN_PAD")
                {
                    ActionPadManager.HandleItemEvent(FormUID, pVal);
                    return;
                }

                // Manejar eventos de B1 Search
                if (pVal.FormTypeEx == "BTUN_SEARCH")
                {
                    if (pVal.ItemUID == "btnSrch" && pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction)
                    {
                        Form oForm = B1App.Instance.Application.Forms.Item(FormUID);
                        B1SearchManager.ExecuteSearch(oForm);
                    }
                    else if (pVal.ItemUID == "btnOpen" && pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction)
                    {
                        Form oForm = B1App.Instance.Application.Forms.Item(FormUID);
                        B1SearchManager.OpenSelectedRecord(oForm);
                    }
                    return;
                }

                // Manejar eventos de Toolbox
                ToolboxManager.HandleToolboxEvents(B1App.Instance.Application.Forms.Item(FormUID), pVal);

                var val = pVal;
                var matchingRules = _rules.Where(r =>
                    (r.FormType == val.FormTypeEx || string.IsNullOrEmpty(r.FormType)) &&
                    EventMatches(r.EventType, val.EventType) &&
                    (r.BeforeAction == val.BeforeAction || r.BeforeAction == false)
                );

                foreach (var rule in matchingRules)
                {
                    Form oForm = null;
                    try { oForm = B1App.Instance.Application.Forms.Item(FormUID); } catch { }

                    if (MacroEngine.CheckCondition(rule.Condition, oForm))
                    {
                        AuditLogManager.LogAction("RuleExecution", $"Rule ID: {rule.ID}, Type: {rule.Type}, Form: {rule.FormType}");
                        MacroEngine.ExecuteMacro(rule.Action, oForm);
                    }
                }

                // UI Customization Dinámica y valores por defecto en Load
                if (pVal.EventType == BoEventTypes.et_FORM_LOAD && !pVal.BeforeAction)
                {
                    Form oForm = B1App.Instance.Application.Forms.Item(FormUID);
                    UnregisterLocalItemChangeHandlers(FormUID);
                    UICustomizer.ApplyCustomization(oForm);
                    DefaultValueManager.ApplyOnLoad(oForm);
                    LockFieldManager.ApplyOnLoad(oForm);
                    FormSettingsManager.RestoreSettings(oForm);
                    QuickCopyManager.AddQuickCopyButtons(oForm);
                    ProcessStepsManager.CheckAndShowAutoProcess(oForm);
                    // Register saved item actions for this form so they execute on item changes/clicks
                    try
                    {
                        var actions = Modules.ItemActionManager.GetAllActions();
                        foreach (var kv in actions)
                        {
                            var parts = kv.Key.Split('|');
                            if (parts.Length != 2) continue;
                            var formType = parts[0];
                            var itemId = parts[1];
                            if (oForm.TypeEx == formType)
                            {
                                // Register a local change handler that executes the saved macro
                                RegisterLocalItemChangeHandler(oForm, itemId, (frm, id) =>
                                {
                                    try
                                    {
                                        var macro = ItemActionManager.GetAction(frm.TypeEx, id);
                                        if (!string.IsNullOrEmpty(macro)) MacroEngine.ExecuteMacro(macro, frm);
                                    }
                                    catch { }
                                });
                            }
                        }
                    }
                    catch { }
                }

                // Guardar ajustes de formulario al cerrarse
                if (pVal.EventType == BoEventTypes.et_FORM_CLOSE && pVal.BeforeAction)
                {
                    OnFormCloseSaveSettings(FormUID, pVal.BeforeAction);
                    UnregisterLocalItemChangeHandlers(FormUID);
                    QuickCopyManager.ClearForm(FormUID);
                }

                // Clic en botones de Quick Copy
                if (pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction
                    && pVal.ItemUID.StartsWith("BTQC"))
                {
                    try
                    {
                        Form oForm = B1App.Instance.Application.Forms.Item(FormUID);
                        QuickCopyManager.HandleButtonClick(FormUID, pVal.ItemUID, oForm);
                    }
                    catch { }
                }

                // Valores por defecto en Change (validate/combo select)
                if (!pVal.BeforeAction && (pVal.EventType == BoEventTypes.et_VALIDATE || pVal.EventType == BoEventTypes.et_COMBO_SELECT))
                {
                    try
                    {
                        Form oForm = B1App.Instance.Application.Forms.Item(FormUID);
                        DefaultValueManager.ApplyOnChange(oForm, pVal.ItemUID);
                        LockFieldManager.ApplyOnChange(oForm, pVal.ItemUID);
                        // Invoke any local handlers registered for this item
                        try
                        {
                            string key = oForm.UniqueID + "|" + pVal.ItemUID;
                            List<Action<SAPbouiCOM.Form, string>> handlers = null;
                            lock (_localItemChangeHandlers)
                            {
                                if (_localItemChangeHandlers.ContainsKey(key)) handlers = new List<Action<SAPbouiCOM.Form, string>>(_localItemChangeHandlers[key]);
                            }
                            if (handlers != null)
                            {
                                foreach (var h in handlers)
                                {
                                    try { h(oForm, pVal.ItemUID); } catch { }
                                }
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en ItemEvent: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"EventDispatcher.OnItemEvent:{pVal.EventType}");
            }
        }

        private void OnMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            // Clicks de menús principales
            MenuManager.HandleMenuEvent(ref pVal);
            // Clicks de menús contextuales (right-click)
            RightClickMenuManager.HandleMenuEvent(ref pVal, out BubbleEvent);
        }

        private void OnFormDataEvent(ref BusinessObjectInfo BusinessObjectInfo, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                // Validaciones antes de guardar
                if (BusinessObjectInfo.BeforeAction && (BusinessObjectInfo.EventType == BoEventTypes.et_FORM_DATA_ADD || BusinessObjectInfo.EventType == BoEventTypes.et_FORM_DATA_UPDATE))
                {
                    Form oForm = null;
                    try { oForm = B1App.Instance.Application.Forms.Item(BusinessObjectInfo.FormUID); } catch { }

                    if (oForm != null)
                    {
                        // 1. Validar campos obligatorios dinámicos
                        if (!MandatoryFieldManager.ValidateMandatoryFields(oForm))
                        {
                            BubbleEvent = false;
                            return;
                        }

                        // 2. Validar reglas personalizadas
                        var info = BusinessObjectInfo;
                        var matchingRules = _rules.Where(r =>
                            (r.FormType == info.FormTypeEx || string.IsNullOrEmpty(r.FormType)) &&
                            r.Type == RuleType.Validation &&
                            r.BeforeAction == true
                        );

                        foreach (var rule in matchingRules)
                        {
                            if (MacroEngine.CheckCondition(rule.Condition, oForm))
                            {
                                AuditLogManager.LogAction("ValidationRule", $"Rule ID: {rule.ID}, Blocking event for Form: {rule.FormType}");
                                BubbleEvent = false;
                                MacroEngine.ExecuteMacro(rule.Action, oForm);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en FormDataEvent: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"EventDispatcher.OnFormDataEvent:{BusinessObjectInfo.EventType}");
            }
        }

        private void OnAppEvent(BoAppEventTypes EventType)
        {
            if (EventType == BoAppEventTypes.aet_ShutDown || EventType == BoAppEventTypes.aet_ServerTerminition || EventType == BoAppEventTypes.aet_CompanyChanged)
            {
                Environment.Exit(0);
            }
        }

        // Guarda ajustes visuales cuando el formulario se cierra (BeforeAction = true → el form aún existe)
        private void OnFormCloseSaveSettings(string formUID, bool beforeAction)
        {
            if (!beforeAction) return;
            try
            {
                Form oForm = B1App.Instance.Application.Forms.Item(formUID);
                FormSettingsManager.SaveSettings(oForm);
            }
            catch { }
        }

        private static bool EventMatches(string configuredEvent, BoEventTypes runtimeEvent)
        {
            if (string.IsNullOrWhiteSpace(configuredEvent)) return true;
            string actual = runtimeEvent.ToString();
            if (string.Equals(configuredEvent, actual, StringComparison.OrdinalIgnoreCase)) return true;

            switch (configuredEvent.Trim().ToUpperInvariant())
            {
                case "ITEM_PRESSED":
                case "ET_ITEM_PRESSED":
                    return runtimeEvent == BoEventTypes.et_ITEM_PRESSED || runtimeEvent == BoEventTypes.et_CLICK;
                case "CLICK":
                case "ET_CLICK":
                    return runtimeEvent == BoEventTypes.et_CLICK || runtimeEvent == BoEventTypes.et_ITEM_PRESSED;
                case "DOUBLE_CLICK":
                case "ET_DOUBLE_CLICK":
                    return runtimeEvent == BoEventTypes.et_DOUBLE_CLICK;
                case "VALIDATE":
                case "ET_VALIDATE":
                    return runtimeEvent == BoEventTypes.et_VALIDATE;
                case "COMBO_SELECT":
                case "ET_COMBO_SELECT":
                    return runtimeEvent == BoEventTypes.et_COMBO_SELECT;
                default:
                    return false;
            }
        }

        private void OnRightClickEvent(ref ContextMenuInfo eventInfo, out bool BubbleEvent)
        {
            RightClickMenuManager.OnRightClickEvent(ref eventInfo, out BubbleEvent);
        }
    }
}
