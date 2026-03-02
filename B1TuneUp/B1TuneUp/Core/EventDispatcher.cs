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
                    r.EventType == val.EventType.ToString() &&
                    r.BeforeAction == val.BeforeAction
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
                    UICustomizer.ApplyCustomization(oForm);
                    DefaultValueManager.ApplyOnLoad(oForm);
                }

                // Valores por defecto en Change (validate/combo select)
                if (!pVal.BeforeAction && (pVal.EventType == BoEventTypes.et_VALIDATE || pVal.EventType == BoEventTypes.et_COMBO_SELECT))
                {
                    try
                    {
                        Form oForm = B1App.Instance.Application.Forms.Item(FormUID);
                        DefaultValueManager.ApplyOnChange(oForm, pVal.ItemUID);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en ItemEvent: {ex.Message}", BoMessageTime.bmt_Short, true);
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

        private void OnAppEvent(BoAppEventTypes EventType)
        {
            if (EventType == BoAppEventTypes.aet_ShutDown || EventType == BoAppEventTypes.aet_ServerTerminition || EventType == BoAppEventTypes.aet_CompanyChanged)
            {
                Environment.Exit(0);
            }
        }

        private void OnRightClickEvent(ref ContextMenuInfo eventInfo, out bool BubbleEvent)
        {
            RightClickMenuManager.OnRightClickEvent(ref eventInfo, out BubbleEvent);
        }
    }
}
