using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.ActionPadInlineDesigner;
using B1TuneUp.Modules.ItemActionsUi;
using B1TuneUp.Modules.PlacementEnhancementUi;
using B1TuneUp.Modules.ValidationUi;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public class RightClickMenuManager
    {
        private static readonly Dictionary<string, RightClickActionDefinition> _rcActions =
            new Dictionary<string, RightClickActionDefinition>(StringComparer.OrdinalIgnoreCase);

        private static ContextSnapshot _currentContext = ContextSnapshot.Empty;

        public static void OnRightClickEvent(ref ContextMenuInfo eventInfo, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                SAPbouiCOM.Form oForm = TryResolveForm(eventInfo?.FormUID);
                if (oForm == null) return;
                if (eventInfo.BeforeAction)
                {
                    _currentContext = ContextSnapshot.From(oForm, eventInfo);
                    AddCustomContextMenus(oForm, eventInfo);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error manejando menú contextual: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.OnRightClickEvent:{eventInfo.FormUID}");
            }
        }

        private static void AddCustomContextMenus(SAPbouiCOM.Form oForm, ContextMenuInfo eventInfo)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                _rcActions.Clear();
                string safeFormType = (oForm?.TypeEx ?? string.Empty).Replace("'", "''");
                string safeItemUid = (eventInfo?.ItemUID ?? string.Empty).Replace("'", "''");

                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_RCLICK\" WHERE \"U_FormType\" = '{safeFormType}' AND (IFNULL(\"U_ItemID\", '') = '' OR \"U_ItemID\" = '{safeItemUid}')"
                    : $"SELECT * FROM [@BTUN_RCLICK] WHERE [U_FormType] = '{safeFormType}' AND (ISNULL([U_ItemID], '') = '' OR [U_ItemID] = '{safeItemUid}')";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string menuId = rs.Fields.Item("U_MenuID").Value.ToString();
                    string name = rs.Fields.Item("U_Name").Value.ToString();
                    string action = rs.Fields.Item("U_Action").Value.ToString();

                    var actionDef = RightClickActionDefinition.Parse(action);
                    if (!actionDef.HasValue)
                    {
                        RemoveMenu(menuId);
                        rs.MoveNext();
                        continue;
                    }

                    EnsureMenuExists(menuId, name);
                    _rcActions[menuId] = actionDef;
                    rs.MoveNext();
                }

                AddB1TuneUpContextMenuItems();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en menú contextual: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.AddCustomContextMenus:{oForm?.TypeEx}");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void AddB1TuneUpContextMenuItems()
        {
            try
            {
                EnsureContextMenu("BTUN_DASH_MENU", "B1TuneUp Dashboard", "ShowDashboard()");
                EnsureContextMenu("BTUN_CREATE_TMPL_MENU", "Crear Template", "CreateTemplate()");
                EnsureContextMenu("BTUN_LOAD_TMPL_MENU", "Cargar Template", "LoadTemplate()");

                EnsureContextMenu("BTUN_EDIT_ITEM", "Edit Item...", RightClickActionDefinition.Builtin(BuiltinRightClickAction.EditItem));
                EnsureContextMenu("BTUN_ADD_ITEM", "Add UI Component...", RightClickActionDefinition.Builtin(BuiltinRightClickAction.AddItem));
                EnsureContextMenu("BTUN_DELETE_ITEM", "Delete Item", RightClickActionDefinition.Builtin(BuiltinRightClickAction.DeleteItem));

                EnsureContextMenu("BTUN_ITEM_PLACEMENT", "Item Placement", RightClickActionDefinition.Builtin(BuiltinRightClickAction.ItemPlacement));
                EnsureContextMenu("BTUN_TUNEUP_OVERLAY", "Editar con TuneUp", RightClickActionDefinition.Builtin(BuiltinRightClickAction.InlineDesigner));
                EnsureContextMenu("BTUN_PAD_DESIGN", "Diseñar Action Pad", RightClickActionDefinition.Builtin(BuiltinRightClickAction.ActionPadDesigner));
                EnsureContextMenu("BTUN_MANAGE_ITEM_ACT", "Manage Item Actions", RightClickActionDefinition.Builtin(BuiltinRightClickAction.ManageItemActions));
                EnsureContextMenu("BTUN_FUNC_BUTTONS", "Function Buttons (TuneUp)", RightClickActionDefinition.Builtin(BuiltinRightClickAction.FunctionButtons));
                EnsureContextMenu("BTUN_VALID_MENU", "Validation System", RightClickActionDefinition.Builtin(BuiltinRightClickAction.ValidationDesigner));
                EnsureContextMenu("BTUN_VALID_QUICK", "Nueva Validation aquÃ­", RightClickActionDefinition.Builtin(BuiltinRightClickAction.QuickValidation));
                EnsureContextMenu("BTUN_MAND_QUICK", "Nuevo Mandatory aquÃ­", RightClickActionDefinition.Builtin(BuiltinRightClickAction.QuickMandatory));
                EnsureContextMenu("BTUN_MODULE_CFG", "Module Configuration", RightClickActionDefinition.Builtin(BuiltinRightClickAction.ModuleConfiguration));

                EnsureContextMenu("BTUN_OPEN_DESIGNER", "Open Visual Designer", "OpenDesigner()");
                EnsureContextMenu("BTUN_EXPORT_SRF", "Export SRF", "ExportSRF('')");
                EnsureContextMenu("BTUN_IMPORT_SRF", "Import SRF", "ImportSRF('')");
                EnsureContextMenu("BTUN_MANAGE_LAYOUTS", "Manage Layouts", "ManageLayouts()");

                EnsureContextMenu("BTUN_RECINV_MENU", "Recurring Invoices", "RecurringInvoices()");
                EnsureContextMenu("BTUN_LETTER_MENU", "Letter Merge", "LetterMerge()");
                EnsureContextMenu("BTUN_EXCHG_MENU", "Exchange Rates", "ExchangeRates()");
                EnsureContextMenu("BTUN_PLDEXT_MENU", "PLD Extensions", "PLDExtensions()");
                EnsureContextMenu("BTUN_PRDEL_MENU", "Print & Delivery", "PrintDelivery()");
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error agregando menús B1TuneUp: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, "RightClickMenuManager.AddB1Menus");
            }
        }

        private static void EnsureContextMenu(string menuId, string caption, string actionMacro)
            => EnsureContextMenu(menuId, caption, RightClickActionDefinition.Macro(actionMacro));

        private static void EnsureContextMenu(string menuId, string caption, RightClickActionDefinition action)
        {
            if (action == null || !action.HasValue) return;
            try
            {
                EnsureMenuExists(menuId, caption);
                _rcActions[menuId] = action;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error asegurando menú contextual {menuId}: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.EnsureContextMenu:{menuId}");
            }
        }

        private static void EnsureMenuExists(string menuId, string caption)
        {
            var menus = B1App.Instance.Application.Menus;
            if (menus.Exists(menuId)) return;

            MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
            creationParams.Type = BoMenuType.mt_STRING;
            creationParams.UniqueID = menuId;
            creationParams.String = caption;
            creationParams.Position = -1;

            menus.Item("1280").SubMenus.AddEx(creationParams);
        }

        private static void RemoveMenu(string menuId)
        {
            // SAP no permite eliminar menús globales de forma segura en runtime; omitimos la eliminación física
        }

        public static void HandleMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            if (pVal.BeforeAction) return;
            if (!_rcActions.TryGetValue(pVal.MenuUID, out var action) || action == null || !action.HasValue) return;

            try
            {
                ExecuteAction(action);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error ejecutando menú contextual: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.HandleMenuEvent:{pVal.MenuUID}");
            }
        }

        private static void ExecuteAction(RightClickActionDefinition action)
        {
            switch (action.Type)
            {
                case ContextActionType.Macro:
                    MacroEngine.ExecuteMacro(ExpandPlaceholders(action.Payload, _currentContext), _currentContext.ResolveForm());
                    break;
                case ContextActionType.Builtin:
                    ExecuteBuiltinAction(action.BuiltinAction, _currentContext);
                    break;
                case ContextActionType.Validation:
                    ExecuteValidationAction(action.Payload, _currentContext);
                    break;
                case ContextActionType.FunctionButton:
                    ExecuteFunctionButtonAction(action.Payload, _currentContext);
                    break;
            }
        }

        private static void ExecuteBuiltinAction(BuiltinRightClickAction builtin, ContextSnapshot ctx)
        {
            var form = ctx.ResolveForm();
            switch (builtin)
            {
                case BuiltinRightClickAction.EditItem:
                    if (string.IsNullOrEmpty(ctx.ItemUid))
                    {
                        B1App.Instance.Application.SetStatusBarMessage("Selecciona un item para editar.", BoMessageTime.bmt_Short, true);
                        return;
                    }
                    ItemEditorManager.OpenItemEditor(ctx.ItemUid, form);
                    break;
                case BuiltinRightClickAction.AddItem:
                    ItemEditorManager.OpenAddItemForm(form);
                    break;
                case BuiltinRightClickAction.DeleteItem:
                    if (string.IsNullOrEmpty(ctx.ItemUid))
                    {
                        B1App.Instance.Application.SetStatusBarMessage("Selecciona un item para eliminar.", BoMessageTime.bmt_Short, true);
                        return;
                    }
                    ItemEditorManager.DeleteItem(ctx.ItemUid, form);
                    break;
                case BuiltinRightClickAction.ItemPlacement:
                    UICustomizerService.OpenItemPlacement();
                    break;
                case BuiltinRightClickAction.InlineDesigner:
                    InlineDesignerManager.ShowOverlay(form, ctx.ItemUid);
                    break;
                case BuiltinRightClickAction.ActionPadDesigner:
                    ActionPadInlineDesignerManager.ShowOverlayForForm(form);
                    break;
                case BuiltinRightClickAction.ManageItemActions:
                    ItemActionsLauncher.Show();
                    break;
                case BuiltinRightClickAction.ValidationDesigner:
                    ExecuteValidationAction(ctx.ItemUid, ctx);
                    break;
                case BuiltinRightClickAction.QuickValidation:
                    ValidationDesignerLauncher.ShowQuickValidation(ctx.FormType, ctx.ItemUid, ctx.ColumnUid);
                    break;
                case BuiltinRightClickAction.QuickMandatory:
                    ValidationDesignerLauncher.ShowQuickMandatory(ctx.FormType, ctx.ItemUid, ctx.ColumnUid);
                    break;
                case BuiltinRightClickAction.ModuleConfiguration:
                    ToolboxUi.ToolboxDesignerLauncher.Show("Modules");
                    break;
                case BuiltinRightClickAction.FunctionButtons:
                    ExecuteFunctionButtonAction(ctx.ItemUid, ctx);
                    break;
            }
        }

        private static void ExecuteValidationAction(string payload, ContextSnapshot ctx)
        {
            string formType = ctx.FormType ?? ctx.ResolveForm()?.TypeEx;
            string itemFilter = string.IsNullOrWhiteSpace(payload) ? ctx.ItemUid : payload;
            ValidationDesignerLauncher.Show(formType, itemFilter);
        }

        private static void ExecuteFunctionButtonAction(string payload, ContextSnapshot ctx)
        {
            string formType = ctx.FormType ?? ctx.ResolveForm()?.TypeEx;
            string itemFilter = string.IsNullOrWhiteSpace(payload) ? ctx.ItemUid : payload;
            ItemActionsLauncher.Show(formType, itemFilter);
        }

        private static SAPbouiCOM.Form TryResolveForm(string formUid)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(formUid))
                    return B1App.Instance.Application.Forms.Item(formUid);
            }
            catch { }

            try { return B1App.Instance.Application.Forms.ActiveForm; }
            catch { return null; }
        }

        private static string ExpandPlaceholders(string macro, ContextSnapshot ctx)
        {
            if (string.IsNullOrWhiteSpace(macro)) return macro;
            string result = macro;
            result = ReplaceToken(result, "ItemUID", ctx.ItemUid);
            result = ReplaceToken(result, "FormUID", ctx.FormUid);
            result = ReplaceToken(result, "FormType", ctx.FormType ?? ctx.ResolveForm()?.TypeEx);
            result = ReplaceToken(result, "ColumnUID", ctx.ColumnUid);
            result = ReplaceToken(result, "Row", ctx.Row > 0 ? ctx.Row.ToString(CultureInfo.InvariantCulture) : string.Empty);
            return result;
        }

        private static string ReplaceToken(string value, string token, string replacement)
        {
            if (replacement == null) replacement = string.Empty;
            return value
                .Replace($"{{{{{token}}}}}", replacement)
                .Replace($"{{{token}}}", replacement);
        }

        private enum ContextActionType
        {
            None,
            Macro,
            Builtin,
            Validation,
            FunctionButton
        }

        private enum BuiltinRightClickAction
        {
            None,
            EditItem,
            AddItem,
            DeleteItem,
            ItemPlacement,
            InlineDesigner,
            ActionPadDesigner,
            ManageItemActions,
            ValidationDesigner,
            QuickValidation,
            QuickMandatory,
            ModuleConfiguration,
            FunctionButtons
        }

        private sealed class RightClickActionDefinition
        {
            public ContextActionType Type { get; }
            public string Payload { get; }
            public BuiltinRightClickAction BuiltinAction { get; }

            private RightClickActionDefinition(ContextActionType type, string payload = null, BuiltinRightClickAction builtin = BuiltinRightClickAction.None)
            {
                Type = type;
                Payload = payload;
                BuiltinAction = builtin;
            }

            public bool HasValue => Type != ContextActionType.None;

            public static RightClickActionDefinition None => new RightClickActionDefinition(ContextActionType.None);

            public static RightClickActionDefinition Macro(string macro)
                => string.IsNullOrWhiteSpace(macro) ? None : new RightClickActionDefinition(ContextActionType.Macro, macro);

            public static RightClickActionDefinition Builtin(BuiltinRightClickAction builtin)
                => builtin == BuiltinRightClickAction.None ? None : new RightClickActionDefinition(ContextActionType.Builtin, builtin: builtin);

            public static RightClickActionDefinition Validation(string payload)
                => new RightClickActionDefinition(ContextActionType.Validation, payload);

            public static RightClickActionDefinition Function(string payload)
                => new RightClickActionDefinition(ContextActionType.FunctionButton, payload);

            public static RightClickActionDefinition Parse(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return None;
                string trimmed = raw.Trim();
                if (trimmed.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
                    return Builtin(ParseBuiltin(trimmed.Substring(8).Trim()));
                if (trimmed.StartsWith("validation:", StringComparison.OrdinalIgnoreCase))
                    return Validation(trimmed.Substring(11).Trim());
                if (trimmed.StartsWith("function:", StringComparison.OrdinalIgnoreCase))
                    return Function(trimmed.Substring(10).Trim());
                return Macro(trimmed);
            }

            private static BuiltinRightClickAction ParseBuiltin(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return BuiltinRightClickAction.None;
                var normalized = value.Trim().ToLowerInvariant();
                switch (normalized)
                {
                    case "edititem": return BuiltinRightClickAction.EditItem;
                    case "additem": return BuiltinRightClickAction.AddItem;
                    case "deleteitem": return BuiltinRightClickAction.DeleteItem;
                    case "inline": return BuiltinRightClickAction.InlineDesigner;
                    case "actionpad": return BuiltinRightClickAction.ActionPadDesigner;
                    case "itemplacement": return BuiltinRightClickAction.ItemPlacement;
                    case "manageitemactions": return BuiltinRightClickAction.ManageItemActions;
                    case "validation": return BuiltinRightClickAction.ValidationDesigner;
                    case "quickvalidation": return BuiltinRightClickAction.QuickValidation;
                    case "quickmandatory": return BuiltinRightClickAction.QuickMandatory;
                    case "moduleconfiguration": return BuiltinRightClickAction.ModuleConfiguration;
                    case "functionbuttons": return BuiltinRightClickAction.FunctionButtons;
                    default: return BuiltinRightClickAction.None;
                }
            }
        }

        private readonly struct ContextSnapshot
        {
            public static ContextSnapshot Empty => new ContextSnapshot();

            public string FormUid { get; }
            public string FormType { get; }
            public string ItemUid { get; }
            public string ColumnUid { get; }
            public int Row { get; }

            private ContextSnapshot(string formUid, string formType, string itemUid, string columnUid, int row)
            {
                FormUid = formUid;
                FormType = formType;
                ItemUid = itemUid;
                ColumnUid = columnUid;
                Row = row;
            }

            public static ContextSnapshot From(SAPbouiCOM.Form form, ContextMenuInfo info)
            {
                return new ContextSnapshot(
                    form?.UniqueID ?? info?.FormUID ?? string.Empty,
                    form?.TypeEx ?? string.Empty,
                    info?.ItemUID ?? string.Empty,
                    info?.ColUID ?? string.Empty,
                    info?.Row ?? 0);
            }

            public SAPbouiCOM.Form ResolveForm()
            {
                try
                {
                    if (!string.IsNullOrEmpty(FormUid))
                        return B1App.Instance.Application.Forms.Item(FormUid);
                }
                catch { }
                try
                {
                    return B1App.Instance.Application.Forms.ActiveForm;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
