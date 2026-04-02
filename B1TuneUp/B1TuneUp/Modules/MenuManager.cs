using System;
using System.Collections.Generic;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class MenuManager
    {
        private const string IntegrationMenuId = "BTUN_INTUI";
        private static Dictionary<string, string> _menuActions = new Dictionary<string, string>();

        public static void LoadCustomMenus()
        {
            SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT * FROM \"@BTUN_MENUS\" ORDER BY \"U_Position\" ASC"
                    : "SELECT * FROM [@BTUN_MENUS] ORDER BY [U_Position] ASC";
                
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string parentId = rs.Fields.Item("U_ParentID").Value.ToString();
                    string menuId = rs.Fields.Item("U_MenuID").Value.ToString();
                    string name = rs.Fields.Item("U_Name").Value.ToString();
                    int pos = (int)rs.Fields.Item("U_Position").Value;
                    string action = rs.Fields.Item("U_Action").Value.ToString();

                    AddMenuItem(parentId, menuId, name, pos);
                    _menuActions[menuId] = action;

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando menús: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                Utils.ComObjectManager.Release(rs);
            }

            // Ensure default language menu is available
            try
            {
                EnsureLanguageMenu();
                EnsureIntegrationMenu();
            }
            catch { }
        }

        private static void AddMenuItem(string parentId, string menuId, string name, int position)
        {
            try
            {
                Menus menus = B1App.Instance.Application.Menus;
                if (!menus.Exists(menuId))
                {
                    MenuItem parentMenu = menus.Item(parentId);
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = menuId;
                    creationParams.String = name;
                    creationParams.Position = position;
                    parentMenu.SubMenus.AddEx(creationParams);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creando menú {menuId}: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureLanguageMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            const string langMenuId = "BTUN_LANG";
            if (menus.Exists(langMenuId)) return;

            // try to attach under Modules menu (43520) or fallback to root
            string parentId = "43520"; // typical Modules menu id
            MenuItem parentMenu = null;
            try
            {
                parentMenu = menus.Item(parentId);
            }
            catch
            {
                try { parentMenu = menus.Item(0); } catch { parentMenu = null; }
            }

            if (parentMenu == null) return;

            try
            {
                MenuCreationParams creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = langMenuId;
                creationParams.String = Utils.LocalizationManager.GetString("Menu.Language");
                creationParams.Position = 9999;
                parentMenu.SubMenus.AddEx(creationParams);
            }
            catch { }
        }

        private static void EnsureIntegrationMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(IntegrationMenuId)) return;

            const string defaultParent = "43520";
            MenuItem parentMenu = null;
            try { parentMenu = menus.Item(defaultParent); } catch { }
            if (parentMenu == null)
            {
                try { parentMenu = menus.Item("0"); } catch { }
            }
            if (parentMenu == null) return;

            try
            {
                var creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = IntegrationMenuId;
                creationParams.String = LocalizationManager.GetString("Menu.IntegrationConfigurator");
                creationParams.Enabled = true;
                creationParams.Position = 9010;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage(LocalizationManager.GetString("Integration.Menu.Description"), BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú de integraciones: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void HandleMenuEvent(ref MenuEvent pVal)
        {
            if (!pVal.BeforeAction && _menuActions.ContainsKey(pVal.MenuUID))
            {
                MacroEngine.ExecuteMacro(_menuActions[pVal.MenuUID]);
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == "BTUN_LANG")
            {
                try { new Forms.LanguageSelectorForm().ShowDialog(); } catch { }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == IntegrationMenuId)
            {
                try { IntegrationConfiguratorLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Integration Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
        }
    }
}
