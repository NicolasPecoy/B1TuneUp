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
                        ID = SapUiSafe.SafeField(rs, "DocEntry"),
                        FormType = SapUiSafe.SafeField(rs, "U_FormType"),
                        Type = ParseRuleType(SapUiSafe.SafeField(rs, "U_Type")),
                        EventType = SapUiSafe.SafeField(rs, "U_EventType"),
                        BeforeAction = SapUiSafe.SafeField(rs, "U_Before") == "Y",
                        Condition = SapUiSafe.SafeField(rs, "U_Condition"),
                        Action = SapUiSafe.SafeField(rs, "U_Action")
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
                if (pVal.FormTypeEx == "BTUN_PAD" && ModuleActivationService.IsEnabled("ActionPad"))
                {
                    ActionPadManager.HandleItemEvent(FormUID, pVal);
                    return;
                }

                // Manejar eventos de B1 Search
                if (pVal.FormTypeEx == "BTUN_SEARCH" && ModuleActivationService.IsEnabled("Search"))
                {
                    if (pVal.ItemUID == "btnSrch" && pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction)
                    {
                        Form oForm = TryGetForm(FormUID);
                        if (oForm == null) return;
                        B1SearchManager.ExecuteSearch(oForm);
                    }
                    else if (pVal.ItemUID == "btnOpen" && pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction)
                    {
                        Form oForm = TryGetForm(FormUID);
                        if (oForm == null) return;
                        B1SearchManager.OpenSelectedRecord(oForm);
                    }
                    return;
                }

                // Manejar eventos de Toolbox
                Form currentForm = TryGetForm(FormUID);
                if (!UnifiedTriggerService.HandleItemEvent(currentForm, pVal))
                {
                    BubbleEvent = false;
                    return;
                }

                if (currentForm != null && ModuleActivationService.IsEnabled("Toolbox"))
                {
                    ToolboxManager.HandleToolboxEvents(currentForm, pVal);
                }

                var val = pVal;
                var matchingRules = _rules.Where(r =>
                    (r.FormType == val.FormTypeEx || string.IsNullOrEmpty(r.FormType)) &&
                    EventMatches(r.EventType, val.EventType) &&
                    (r.BeforeAction == val.BeforeAction || r.BeforeAction == false)
                );

                foreach (var rule in matchingRules)
                {
                    Form oForm = null;
                    oForm = currentForm ?? TryGetForm(FormUID);

                    if (MacroEngine.CheckCondition(rule.Condition, oForm))
                    {
                        AuditLogManager.LogAction("RuleExecution", $"Rule ID: {rule.ID}, Type: {rule.Type}, Form: {rule.FormType}");
                        MacroEngine.ExecuteMacro(rule.Action, oForm);
                    }
                }

                // UI Customization Dinámica y valores por defecto en Load
                if (pVal.EventType == BoEventTypes.et_FORM_LOAD && !pVal.BeforeAction)
                {
                    Form oForm = currentForm ?? TryGetForm(FormUID);
                    UnregisterLocalItemChangeHandlers(FormUID);
                    if (oForm == null) return;
                    if (ModuleActivationService.IsEnabled("UiCustomization")) UICustomizer.ApplyCustomization(oForm);
                    if (ModuleActivationService.IsEnabled("DefaultValues")) DefaultValueManager.ApplyOnLoad(oForm);
                    if (ModuleActivationService.IsEnabled("LockFields")) LockFieldManager.ApplyOnLoad(oForm);
                    FormSettingsManager.RestoreSettings(oForm);
                    if (ModuleActivationService.IsEnabled("QuickCopy")) QuickCopyManager.AddQuickCopyButtons(oForm);
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
                        Form oForm = TryGetForm(FormUID);
                        if (oForm == null) return;
                        QuickCopyManager.HandleButtonClick(FormUID, pVal.ItemUID, oForm);
                    }
                    catch { }
                }

                if (ModuleActivationService.IsEnabled("Validation") && !pVal.BeforeAction)
                {
                    Form oForm = currentForm ?? TryGetForm(FormUID);
                    if (oForm != null)
                    {
                        if (pVal.EventType == BoEventTypes.et_FORM_LOAD)
                        {
                            ValidationManager.ExecuteValidations(oForm, "FORM_LOAD");
                        }
                        else if (pVal.EventType == BoEventTypes.et_ITEM_PRESSED || pVal.EventType == BoEventTypes.et_CLICK)
                        {
                            ValidationManager.ExecuteValidations(oForm, "ITEM_PRESSED", itemId: pVal.ItemUID);
                        }
                    }
                }

                // Valores por defecto en Change (validate/combo select)
                if (!pVal.BeforeAction && (pVal.EventType == BoEventTypes.et_VALIDATE || pVal.EventType == BoEventTypes.et_COMBO_SELECT))
                {
                    try
                    {
                        Form oForm = currentForm ?? TryGetForm(FormUID);
                        if (oForm == null) return;
                        if (ModuleActivationService.IsEnabled("DefaultValues")) DefaultValueManager.ApplyOnChange(oForm, pVal.ItemUID);
                        if (ModuleActivationService.IsEnabled("LockFields")) LockFieldManager.ApplyOnChange(oForm, pVal.ItemUID);
                        if (ModuleActivationService.IsEnabled("Validation"))
                        {
                            ValidationManager.ExecuteValidations(
                                oForm,
                                pVal.EventType == BoEventTypes.et_VALIDATE ? "EDIT_VALIDATE" : "COMBO_SELECT",
                                itemId: pVal.ItemUID);
                        }
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
            if (!UnifiedTriggerService.HandleMenuEvent(pVal))
            {
                BubbleEvent = false;
                return;
            }
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
                Form triggerForm = TryGetForm(BusinessObjectInfo.FormUID);
                if (!UnifiedTriggerService.HandleDataEvent(triggerForm, BusinessObjectInfo))
                {
                    BubbleEvent = false;
                    return;
                }

                // Validaciones antes de guardar
                if (BusinessObjectInfo.BeforeAction && (BusinessObjectInfo.EventType == BoEventTypes.et_FORM_DATA_ADD || BusinessObjectInfo.EventType == BoEventTypes.et_FORM_DATA_UPDATE))
                {
                    Form oForm = null;
                    oForm = TryGetForm(BusinessObjectInfo.FormUID);

                    if (oForm != null)
                    {
                        // 1. Validar campos obligatorios dinámicos
                        if (ModuleActivationService.IsEnabled("MandatoryFields") && !MandatoryFieldManager.ValidateMandatoryFields(oForm))
                        {
                            BubbleEvent = false;
                            return;
                        }

                        if (ModuleActivationService.IsEnabled("Validation"))
                        {
                            string validationEvent = BusinessObjectInfo.EventType == BoEventTypes.et_FORM_DATA_ADD
                                ? "DATA_ADD_BEFORE"
                                : "DATA_UPDATE_BEFORE";
                            if (!ValidationManager.ExecuteValidations(oForm, validationEvent))
                            {
                                BubbleEvent = false;
                                return;
                            }
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
                Form oForm = TryGetForm(formUID);
                if (oForm == null) return;
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
            if (!ModuleActivationService.IsEnabled("RightClick"))
            {
                BubbleEvent = true;
                return;
            }
            if (!UnifiedTriggerService.HandleRightClickEvent(eventInfo))
            {
                BubbleEvent = false;
                return;
            }
            RightClickMenuManager.OnRightClickEvent(ref eventInfo, out BubbleEvent);
        }

        private static Form TryGetForm(string formUid)
        {
            if (string.IsNullOrWhiteSpace(formUid)) return null;
            return SapUiSafe.TryGetForm(formUid);
        }

        private static RuleType ParseRuleType(string value)
        {
            try
            {
                if (Enum.TryParse(value, out RuleType parsed)) return parsed;
            }
            catch { }
            return RuleType.Macro;
        }
    }
}
