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

                // Add default B1TuneUp context menu items
                AddB1TuneUpContextMenuItems(oForm);
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

        private static void AddB1TuneUpContextMenuItems(SAPbouiCOM.Form oForm)
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