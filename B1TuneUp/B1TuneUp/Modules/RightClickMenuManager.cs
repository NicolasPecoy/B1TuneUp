using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public class RightClickMenuManager
    {
        private static Dictionary<string, string> _rcActions = new Dictionary<string, string>();

        public static void OnRightClickEvent(ref ContextMenuInfo eventInfo, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.Item(eventInfo.FormUID);
                if (eventInfo.BeforeAction)
                {
                    AddCustomContextMenus(oForm, eventInfo);
                }
                else
                {
                    // No necesitamos limpiar manualmente aquí, ya que SAP lo hace al cerrar el menú,
                    // pero mantenemos la consistencia.
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
                // Limpiamos acciones previas del diccionario para este formulario
                // (En una implementación real, podríamos usar el MenuID como clave única global)

                string sql;
                if (B1App.Instance.IsHana)
                {
                    sql = $"SELECT * FROM \"@BTUN_RCLICK\" WHERE \"U_FormType\" = '{oForm.TypeEx}' AND (IFNULL(\"U_ItemID\", '') = '' OR \"U_ItemID\" = '{eventInfo.ItemUID}')";
                }
                else
                {
                    sql = $"SELECT * FROM [@BTUN_RCLICK] WHERE [U_FormType] = '{oForm.TypeEx}' AND (ISNULL([U_ItemID], '') = '' OR [U_ItemID] = '{eventInfo.ItemUID}')";
                }

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string menuId = rs.Fields.Item("U_MenuID").Value.ToString();
                    string name = rs.Fields.Item("U_Name").Value.ToString();
                    string action = rs.Fields.Item("U_Action").Value.ToString();

                    if (!B1App.Instance.Application.Menus.Exists(menuId))
                    {
                        MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                        creationParams.Type = BoMenuType.mt_STRING;
                        creationParams.UniqueID = menuId;
                        creationParams.String = name;
                        creationParams.Position = -1; // Al final

                        B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    }

                    _rcActions[menuId] = action;
                    rs.MoveNext();
                }

                // Add default B1TuneUp context menu items (pass eventInfo so actions can include ItemUID)
                AddB1TuneUpContextMenuItems(oForm, eventInfo);
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

        private static void AddB1TuneUpContextMenuItems(SAPbouiCOM.Form oForm, ContextMenuInfo eventInfo)
        {
            try
            {
                string clickedItem = eventInfo != null ? eventInfo.ItemUID ?? string.Empty : string.Empty;

                EnsureContextMenu("BTUN_DASH_MENU", "B1TuneUp Dashboard", "ShowDashboard()");
                EnsureContextMenu("BTUN_CREATE_TMPL_MENU", "Crear Template", "CreateTemplate()");
                EnsureContextMenu("BTUN_LOAD_TMPL_MENU", "Cargar Template", "LoadTemplate()");

                EnsureContextMenu("BTUN_EDIT_ITEM", "Edit Item...", $"EditItem('{clickedItem}')");
                EnsureContextMenu("BTUN_ADD_ITEM", "Add UI Component...", "AddItem()");
                EnsureContextMenu("BTUN_DELETE_ITEM", "Delete Item", $"DeleteItem('{clickedItem}')");

                EnsureContextMenu("BTUN_ITEM_PLACEMENT", "Item Placement", "OpenItemPlacement()");
                EnsureContextMenu("BTUN_MANAGE_ITEM_ACT", "Manage Item Actions", "ManageItemActions()");
                EnsureContextMenu("BTUN_OPEN_DESIGNER", "Open Visual Designer", "OpenDesigner()");
                EnsureContextMenu("BTUN_EXPORT_SRF", "Export SRF", "ExportSRF('')");
                EnsureContextMenu("BTUN_IMPORT_SRF", "Import SRF", "ImportSRF('')");
                EnsureContextMenu("BTUN_MANAGE_LAYOUTS", "Manage Layouts", "ManageLayouts()");

                EnsureContextMenu("BTUN_RECINV_MENU", "Recurring Invoices", "RecurringInvoices()");
                EnsureContextMenu("BTUN_LETTER_MENU", "Letter Merge", "LetterMerge()");
                EnsureContextMenu("BTUN_EXCHG_MENU", "Exchange Rates", "ExchangeRates()");
                EnsureContextMenu("BTUN_PLDEXT_MENU", "PLD Extensions", "PLDExtensions()");
                EnsureContextMenu("BTUN_VALID_MENU", "Validation System", "ValidationSystem()");
                EnsureContextMenu("BTUN_PRDEL_MENU", "Print & Delivery", "PrintDelivery()");
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error agregando menús B1TuneUp: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.AddB1Menus:{oForm?.TypeEx}");
            }
        }

        private static void EnsureContextMenu(string menuId, string caption, string actionMacro)
        {
            try
            {
                var menus = B1App.Instance.Application.Menus;
                if (!menus.Exists(menuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = menuId;
                    creationParams.String = caption;
                    creationParams.Position = -1;

                    menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[menuId] = actionMacro ?? string.Empty;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error asegurando menú contextual {menuId}: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.EnsureContextMenu:{menuId}");
            }
        }

        public static void HandleMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            if (!pVal.BeforeAction && _rcActions.ContainsKey(pVal.MenuUID))
            {
                try
                {
                    MacroEngine.ExecuteMacro(_rcActions[pVal.MenuUID]);
                }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error ejecutando menú contextual: {ex.Message}", BoMessageTime.bmt_Short, true);
                    ExceptionLogger.LogHandled(ex, $"RightClickMenuManager.HandleMenuEvent:{pVal.MenuUID}");
                }
            }
        }
    }
}
