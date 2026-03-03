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
            catch { }
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
                // Add Dashboard menu item
                string dashboardMenuId = "BTUN_DASH_MENU";
                if (!B1App.Instance.Application.Menus.Exists(dashboardMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = dashboardMenuId;
                    creationParams.String = "B1TuneUp Dashboard";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[dashboardMenuId] = "ShowDashboard()";
                }

                // Add Template menu items
                string createTemplateMenuId = "BTUN_CREATE_TMPL_MENU";
                if (!B1App.Instance.Application.Menus.Exists(createTemplateMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = createTemplateMenuId;
                    creationParams.String = "Crear Template";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[createTemplateMenuId] = "CreateTemplate()";
                }

                string loadTemplateMenuId = "BTUN_LOAD_TMPL_MENU";
                if (!B1App.Instance.Application.Menus.Exists(loadTemplateMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = loadTemplateMenuId;
                    creationParams.String = "Cargar Template";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[loadTemplateMenuId] = "LoadTemplate()";
                }

                // Edit specific item
                string editItemMenuId = "BTUN_EDIT_ITEM";
                if (!B1App.Instance.Application.Menus.Exists(editItemMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = editItemMenuId;
                    creationParams.String = "Edit Item...";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                string clickedItem = eventInfo != null ? eventInfo.ItemUID : "";
                _rcActions[editItemMenuId] = $"EditItem('{clickedItem}')";

                // Add new item
                string addItemMenuId = "BTUN_ADD_ITEM";
                if (!B1App.Instance.Application.Menus.Exists(addItemMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = addItemMenuId;
                    creationParams.String = "Add UI Component...";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[addItemMenuId] = "AddItem()";

                // Delete item
                string deleteItemMenuId = "BTUN_DELETE_ITEM";
                if (!B1App.Instance.Application.Menus.Exists(deleteItemMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = deleteItemMenuId;
                    creationParams.String = "Delete Item";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[deleteItemMenuId] = $"DeleteItem('{clickedItem}')";

                // Open Item Placement
                string itemPlacementMenuId = "BTUN_ITEM_PLACEMENT";
                if (!B1App.Instance.Application.Menus.Exists(itemPlacementMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = itemPlacementMenuId;
                    creationParams.String = "Item Placement";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[itemPlacementMenuId] = "OpenItemPlacement()";

                string manageActionsMenuId = "BTUN_MANAGE_ITEM_ACT";
                if (!B1App.Instance.Application.Menus.Exists(manageActionsMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = manageActionsMenuId;
                    creationParams.String = "Manage Item Actions";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[manageActionsMenuId] = "ManageItemActions()";

                string openDesignerMenuId = "BTUN_OPEN_DESIGNER";
                if (!B1App.Instance.Application.Menus.Exists(openDesignerMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = openDesignerMenuId;
                    creationParams.String = "Open Visual Designer";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[openDesignerMenuId] = "OpenDesigner()";

                string exportSrfMenuId = "BTUN_EXPORT_SRF";
                if (!B1App.Instance.Application.Menus.Exists(exportSrfMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = exportSrfMenuId;
                    creationParams.String = "Export SRF";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[exportSrfMenuId] = "ExportSRF('')";

                string importSrfMenuId = "BTUN_IMPORT_SRF";
                if (!B1App.Instance.Application.Menus.Exists(importSrfMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = importSrfMenuId;
                    creationParams.String = "Import SRF";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[importSrfMenuId] = "ImportSRF('')";

                string manageLayoutsMenuId = "BTUN_MANAGE_LAYOUTS";
                if (!B1App.Instance.Application.Menus.Exists(manageLayoutsMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = manageLayoutsMenuId;
                    creationParams.String = "Manage Layouts";
                    creationParams.Position = -1;
                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                }
                _rcActions[manageLayoutsMenuId] = "ManageLayouts()";

                // Add Recurring Invoices menu item
                string recurringInvoicesMenuId = "BTUN_RECINV_MENU";
                if (!B1App.Instance.Application.Menus.Exists(recurringInvoicesMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = recurringInvoicesMenuId;
                    creationParams.String = "Recurring Invoices";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[recurringInvoicesMenuId] = "RecurringInvoices()";
                }

                // Add Letter Merge menu item
                string letterMergeMenuId = "BTUN_LETTER_MENU";
                if (!B1App.Instance.Application.Menus.Exists(letterMergeMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = letterMergeMenuId;
                    creationParams.String = "Letter Merge";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[letterMergeMenuId] = "LetterMerge()";
                }

                // Add Exchange Rates menu item
                string exchangeRatesMenuId = "BTUN_EXCHG_MENU";
                if (!B1App.Instance.Application.Menus.Exists(exchangeRatesMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = exchangeRatesMenuId;
                    creationParams.String = "Exchange Rates";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[exchangeRatesMenuId] = "ExchangeRates()";
                }

                // Add PLD Extensions menu item
                string pldExtensionsMenuId = "BTUN_PLDEXT_MENU";
                if (!B1App.Instance.Application.Menus.Exists(pldExtensionsMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = pldExtensionsMenuId;
                    creationParams.String = "PLD Extensions";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[pldExtensionsMenuId] = "PLDExtensions()";
                }

                // Add Validation System menu item
                string validationMenuId = "BTUN_VALID_MENU";
                if (!B1App.Instance.Application.Menus.Exists(validationMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = validationMenuId;
                    creationParams.String = "Validation System";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[validationMenuId] = "ValidationSystem()";
                }

                // Add Print & Delivery menu item
                string printDeliveryMenuId = "BTUN_PRDEL_MENU";
                if (!B1App.Instance.Application.Menus.Exists(printDeliveryMenuId))
                {
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = printDeliveryMenuId;
                    creationParams.String = "Print & Delivery";
                    creationParams.Position = -1;

                    B1App.Instance.Application.Menus.Item("1280").SubMenus.AddEx(creationParams);
                    _rcActions[printDeliveryMenuId] = "PrintDelivery()";
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error agregando menús B1TuneUp: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void HandleMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            if (!pVal.BeforeAction && _rcActions.ContainsKey(pVal.MenuUID))
            {
                MacroEngine.ExecuteMacro(_rcActions[pVal.MenuUID]);
            }
        }
    }
}