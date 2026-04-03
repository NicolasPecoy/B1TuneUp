using System;
using System.Collections.Generic;
using System.Drawing;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules.UiDesigner;
using B1TuneUp.Modules.SchedulerUi;
using B1TuneUp.Modules.RuleBuilder;
using B1TuneUp.Modules.ProcessDesigner;
using B1TuneUp.Modules.AuditLogViewer;
using B1TuneUp.Modules.TemplateReportUi;
using B1TuneUp.Modules.EmailDesigner;
using B1TuneUp.Modules.ToolboxUi;
using B1TuneUp.Modules.ValidationUi;
using B1TuneUp.Modules.DashboardSearchMacroUi;
using B1TuneUp.Modules.ActionQuickUi;
using B1TuneUp.Modules.MacroEngineUi;
using B1TuneUp.Modules.FormEnhancementUi;
using B1TuneUp.Modules.AutomationDashboardUi;
using B1TuneUp.Modules.PlacementEnhancementUi;
using B1TuneUp.Utils;
using B1TuneUp.Modules.LanguageSelectorUi;

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
        private const string TemplateReportMenuId = "BTUN_TMPLRPT";
        private const string EmailDesignerMenuId = "BTUN_EMAILUI";
        private const string ToolboxMenuId = "BTUN_TOOLBOX";
        private const string ValidationMenuId = "BTUN_VALMAND";
        private const string DashboardSearchMacroMenuId = "BTUN_DSHMC";
        private const string ActionQuickMenuId = "BTUN_ACTPAD";
        private const string MacroEngineMenuId = "BTUN_MACENG";
        private const string FormEnhancementMenuId = "BTUN_FORMEX";
        private const string AutomationDashboardMenuId = "BTUN_AUTOD";
        private const string PlacementEnhancementMenuId = "BTUN_ITEMUI";
        private static Dictionary<string, string> _menuActions = new Dictionary<string, string>();
        private static readonly Color MenuIconBackground = ColorTranslator.FromHtml("#1F4E79");
        private static readonly Color MenuIconForeground = Color.White;

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
                EnsureTemplateReportMenu();
                EnsureEmailDesignerMenu();
                EnsureToolboxMenu();
                EnsureValidationMenu();
                EnsureDashboardSearchMacroMenu();
                EnsureActionQuickMenu();
                EnsureMacroEngineMenu();
                EnsureFormEnhancementMenu();
                EnsureAutomationDashboardMenu();
                EnsurePlacementEnhancementMenu();
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

        private static void EnsureTemplateReportMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(TemplateReportMenuId)) return;

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
                string caption = LocalizationManager.GetString("Menu.TemplateReportStudio");
                if (string.IsNullOrEmpty(caption) || caption == "Menu.TemplateReportStudio")
                {
                    caption = "Template & Report Studio";
                }
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = TemplateReportMenuId;
                creationParams.String = caption;
                creationParams.Position = 9016;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Template & Report Studio disponible desde el menú.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Template & Report: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureEmailDesignerMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(EmailDesignerMenuId)) return;

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
                creationParams.UniqueID = EmailDesignerMenuId;
                string caption = LocalizationManager.GetString("Menu.EmailDesigner");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.EmailDesigner")
                {
                    caption = "Email & Notification Designer";
                }
                creationParams.String = caption;
                creationParams.Position = 9017;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Email & Notification Designer disponible en el menú.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Email Designer: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureToolboxMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(ToolboxMenuId)) return;

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
                creationParams.UniqueID = ToolboxMenuId;
                string caption = LocalizationManager.GetString("Menu.ToolboxDesigner");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.ToolboxDesigner")
                {
                    caption = "Toolbox / Settings";
                }
                creationParams.String = caption;
                creationParams.Position = 9018;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Toolbox / Settings disponible en el menú.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Toolbox: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureValidationMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(ValidationMenuId)) return;

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
                creationParams.UniqueID = ValidationMenuId;
                string caption = LocalizationManager.GetString("Menu.ValidationDesigner");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.ValidationDesigner")
                {
                    caption = "Validation & Mandatory Fields";
                }
                creationParams.String = caption;
                creationParams.Position = 9019;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Validation & Mandatory Fields disponible en el menú.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menú Validation Manager: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureDashboardSearchMacroMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(DashboardSearchMacroMenuId)) return;

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
                creationParams.UniqueID = DashboardSearchMacroMenuId;
                string caption = LocalizationManager.GetString("Menu.DashboardSearchMacroStudio");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.DashboardSearchMacroStudio")
                {
                    caption = "Dashboard / Search / Macro Studio";
                }
                creationParams.String = caption;
                creationParams.Position = 9020;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Dashboard / Search / Macro Studio disponible en el menu.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menu Dashboard/Search/Macro: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureActionQuickMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(ActionQuickMenuId)) return;

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
                creationParams.UniqueID = ActionQuickMenuId;
                string caption = LocalizationManager.GetString("Menu.ActionQuickStudio");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.ActionQuickStudio")
                {
                    caption = "Action Pad / Quick Copy / Item Actions";
                }
                creationParams.String = caption;
                creationParams.Position = 9021;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Action Pad / Quick Copy / Item Actions disponible en el menu.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menu Action Pad Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureMacroEngineMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(MacroEngineMenuId)) return;

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
                creationParams.UniqueID = MacroEngineMenuId;
                string caption = LocalizationManager.GetString("Menu.MacroEngineStudio");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.MacroEngineStudio")
                {
                    caption = "Macro Engine Studio";
                }
                creationParams.String = caption;
                creationParams.Position = 9022;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Macro Engine Studio disponible en el menu.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menu Macro Engine: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureFormEnhancementMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(FormEnhancementMenuId)) return;

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
                creationParams.UniqueID = FormEnhancementMenuId;
                string caption = LocalizationManager.GetString("Menu.FormEnhancementStudio");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.FormEnhancementStudio")
                {
                    caption = "Form Enhancements Studio";
                }
                creationParams.String = caption;
                creationParams.Position = 9023;
                parentMenu.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Form Enhancements Studio disponible en el menu.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menu Form Enhancements: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsureAutomationDashboardMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(AutomationDashboardMenuId)) return;

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
                MenuCreationParams creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = AutomationDashboardMenuId;
                string caption = LocalizationManager.GetString("Menu.AutomationDashboard");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.AutomationDashboard")
                {
                    caption = "Automation Dashboard";
                }
                creationParams.String = caption;
                creationParams.Position = 9024;
                parent.SubMenus.AddEx(creationParams);
                app.SetStatusBarMessage("Automation Dashboard disponible en el menu.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menu Automation Dashboard: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EnsurePlacementEnhancementMenu()
        {
            var app = B1App.Instance.Application;
            var menus = app.Menus;
            if (menus.Exists(PlacementEnhancementMenuId)) return;

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
                MenuCreationParams creationParams = (MenuCreationParams)app.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = PlacementEnhancementMenuId;
                string caption = LocalizationManager.GetString("Menu.PlacementEnhancementStudio");
                if (string.IsNullOrWhiteSpace(caption) || caption == "Menu.PlacementEnhancementStudio")
                {
                    caption = "Item Placement & UI Enhancer";
                }
                creationParams.String = caption;
                creationParams.Position = 9025;
                parent.SubMenus.AddEx(creationParams);
                var status = LocalizationManager.GetString("PlacementEnhancement.Menu.Description");
                if (string.IsNullOrWhiteSpace(status) || status == "PlacementEnhancement.Menu.Description")
                {
                    status = "Item Placement & UI Enhancer disponible en el menu.";
                }
                app.SetStatusBarMessage(status, BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                app.SetStatusBarMessage($"Error creando menu Item Placement: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ApplyMenuIcon(SAPbouiCOM.MenuCreationParams creationParams, string key, string label)
        {
            try
            {
                creationParams.Image = MenuIconProvider.GetIcon(key, label, MenuIconBackground, MenuIconForeground);
            }
            catch { }
        }

        public static void HandleMenuEvent(ref MenuEvent pVal)
        {
            if (!pVal.BeforeAction && _menuActions.ContainsKey(pVal.MenuUID))
            {
                MacroEngine.ExecuteMacro(_menuActions[pVal.MenuUID]);
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == "BTUN_LANG")
            {
                try { LanguageSelectorUi.LanguageSelectorLauncher.Show(); } catch { }
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
            else if (!pVal.BeforeAction && pVal.MenuUID == TemplateReportMenuId)
            {
                try { TemplateReportLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Template & Report Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == EmailDesignerMenuId)
            {
                try { EmailDesignerLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Email & Notification Designer: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == ToolboxMenuId)
            {
                try { ToolboxDesignerLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Toolbox Settings: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == ValidationMenuId)
            {
                try { ValidationDesignerLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Validation Designer: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == DashboardSearchMacroMenuId)
            {
                try { DashboardSearchMacroLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Dashboard/Search/Macro Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == ActionQuickMenuId)
            {
                try { ActionQuickLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Action Pad Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == MacroEngineMenuId)
            {
                try { MacroEngineLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Macro Engine Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == FormEnhancementMenuId)
            {
                try { FormEnhancementLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Form Enhancements Studio: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == PlacementEnhancementMenuId)
            {
                try { PlacementEnhancementLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Item Placement & UI Enhancer: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
            else if (!pVal.BeforeAction && pVal.MenuUID == AutomationDashboardMenuId)
            {
                try { AutomationDashboardLauncher.Show(); }
                catch (Exception ex)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Automation Dashboard: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
            }
        }
    }
}
