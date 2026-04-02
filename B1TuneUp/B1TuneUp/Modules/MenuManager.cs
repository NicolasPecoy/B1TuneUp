using System;
using System.Collections.Generic;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules.UiDesigner;
using B1TuneUp.Modules.SchedulerUi;
using B1TuneUp.Modules.RuleBuilder;
using B1TuneUp.Modules.ProcessDesigner;
using B1TuneUp.Modules.AuditLogViewer;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class MenuManager
    {
        private const string IntegrationMenuId = "BTUN_INTUI";
        private const string UiDesignerMenuId = "BTUN_UICFG";
        private const string SchedulerMenuId = "BTUN_SCHEDUI";
        private const string RuleBuilderMenuId = "BTUN_RULESUI";
        private const string ProcessDesignerMenuId = "BTUN_PSDESIGN";
        private const string AuditLogMenuId = "BTUN_LOGVIEW";
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
                EnsureUiDesignerMenu();
                EnsureSchedulerMenu();
                EnsureRuleBuilderMenu();
                EnsureProcessDesignerMenu();
                EnsureAuditLogMenu();
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

        private static void EnsureUiDesignerMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(UiDesignerMenuId)) return;

            const string parentId = "43520";
            MenuItem parentMenu = null;
            try { parentMenu = menus.Item(parentId); } catch { }
            if (parentMenu == null)
            {
                try { parentMenu = menus.Item("0"); } catch { }
            }
            if (parentMenu == null) return;

            try
            {
                MenuCreationParams creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = UiDesignerMenuId;
                creationParams.String = LocalizationManager.GetString("Menu.UiCustomizer");
                creationParams.Position = 9011;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage(LocalizationManager.GetString("UiCustomizer.Menu.Description"), BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú UI Customizer: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureSchedulerMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(SchedulerMenuId)) return;

            string parentId = "43520";
            MenuItem parentMenu = null;
            try { parentMenu = menus.Item(parentId); } catch { }
            if (parentMenu == null)
            {
                try { parentMenu = menus.Item("0"); } catch { }
            }
            if (parentMenu == null) return;

            try
            {
                MenuCreationParams creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = SchedulerMenuId;
                creationParams.String = LocalizationManager.GetString("Menu.SchedulerStudio");
                creationParams.Position = 9012;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage(LocalizationManager.GetString("Scheduler.Menu.Description"), BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Scheduler Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureRuleBuilderMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(RuleBuilderMenuId)) return;

            string parentId = "43520";
            MenuItem parent = null;
            try { parent = menus.Item(parentId); } catch { }
            if (parent == null)
            {
                try { parent = menus.Item("0"); } catch { }
            }
            if (parent == null) return;

            try
            {
                var creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = RuleBuilderMenuId;
                creationParams.String = LocalizationManager.GetString("Menu.RuleBuilder");
                creationParams.Position = 9013;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage(LocalizationManager.GetString("RuleBuilder.Menu.Description"), BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Rule Builder: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureProcessDesignerMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(ProcessDesignerMenuId)) return;

            string parentId = "43520";
            MenuItem parent = null;
            try { parent = menus.Item(parentId); } catch { }
            if (parent == null)
            {
                try { parent = menus.Item("0"); } catch { }
            }
            if (parent == null) return;

            try
            {
                var creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = ProcessDesignerMenuId;
                creationParams.String = LocalizationManager.GetString("Menu.ProcessDesigner");
                creationParams.Position = 9014;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage(LocalizationManager.GetString("ProcessDesigner.Menu.Description"), BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Process Designer: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureAuditLogMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(AuditLogMenuId)) return;

            string parentId = "43520";
            MenuItem parent = null;
            try { parent = menus.Item(parentId); } catch { }
            if (parent == null)
            {
                try { parent = menus.Item("0"); } catch { }
            }
            if (parent == null) return;

            try
            {
                var creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = AuditLogMenuId;
                creationParams.String = LocalizationManager.GetString("Menu.AuditLog");
                creationParams.Position = 9015;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage(LocalizationManager.GetString("AuditLog.Menu.Description"), BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Audit Log: {ex.Message}", BoMessageTime.bmt_Short, true);
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
            else if (!pVal.BeforeAction && pVal.MenuUID == UiDesignerMenuId)
            {
                try { UiCustomizerLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo UI Customizer: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == SchedulerMenuId)
            {
                try { SchedulerUi.SchedulerLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Scheduler Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == RuleBuilderMenuId)
            {
                try { RuleBuilderLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Rule Builder: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == ProcessDesignerMenuId)
            {
                try { ProcessDesignerLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Process Designer: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == AuditLogMenuId)
            {
                try { AuditLogLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Log Viewer: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
        }
    }
}
