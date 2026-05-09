using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    public static class UnifiedTriggerService
    {
        private const string Prefix = "TRIGGER_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static readonly string[] SupportedEvents =
        {
            "FORM_LOAD",
            "ITEM_PRESSED",
            "CLICK",
            "VALIDATE",
            "COMBO_SELECT",
            "CHOOSE_FROM_LIST",
            "MATRIX_LINK_PRESSED",
            "MENU",
            "RIGHT_CLICK",
            "DATA_ADD",
            "DATA_UPDATE",
            "DATA_FIND"
        };

        public static IList<UnifiedTriggerEntry> GetAll()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && s.Code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(ReadEntry)
                .Where(e => e != null)
                .OrderBy(e => e.Code)
                .ToList();
        }

        public static UnifiedTriggerEntry Save(UnifiedTriggerEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code)) throw new InvalidOperationException("El codigo del trigger es obligatorio.");
            entry.Code = NormalizeCode(entry.Code);
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = Prefix + entry.Code,
                Category = "Triggers",
                Description = entry.Name ?? entry.Code,
                Value = JsonSerializer.Serialize(entry, JsonOptions)
            });
            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            ToolboxSettingService.Delete(Prefix + NormalizeCode(code));
        }

        public static bool HandleItemEvent(Form form, ItemEvent pVal)
        {
            if (!ModuleActivationService.IsEnabled("EventSystem")) return true;
            string eventName = MapItemEvent(pVal.EventType);
            return ExecuteMatching(form, eventName, pVal.BeforeAction, pVal.ItemUID, pVal.ColUID, pVal.Row);
        }

        public static bool HandleMenuEvent(MenuEvent pVal)
        {
            if (!ModuleActivationService.IsEnabled("EventSystem")) return true;
            return ExecuteMatching(SapUiSafe.TryGetActiveForm(), "MENU", pVal.BeforeAction, pVal.MenuUID, null, -1);
        }

        public static bool HandleRightClickEvent(ContextMenuInfo eventInfo)
        {
            if (!ModuleActivationService.IsEnabled("EventSystem")) return true;
            return ExecuteMatching(SapUiSafe.TryGetForm(eventInfo.FormUID), "RIGHT_CLICK", eventInfo.BeforeAction, eventInfo.ItemUID, eventInfo.ColUID, eventInfo.Row);
        }

        public static bool HandleDataEvent(Form form, BusinessObjectInfo info)
        {
            if (!ModuleActivationService.IsEnabled("EventSystem")) return true;
            string eventName = info.EventType == BoEventTypes.et_FORM_DATA_ADD ? "DATA_ADD"
                : info.EventType == BoEventTypes.et_FORM_DATA_UPDATE ? "DATA_UPDATE"
                : info.EventType == BoEventTypes.et_FORM_DATA_LOAD ? "DATA_FIND"
                : info.EventType.ToString();
            return ExecuteMatching(form, eventName, info.BeforeAction, null, null, -1);
        }

        private static bool ExecuteMatching(Form form, string eventName, bool beforeAction, string itemId, string columnId, int row)
        {
            bool allow = true;
            foreach (var trigger in GetAll().Where(t => Matches(t, form, eventName, beforeAction, itemId, columnId)))
            {
                try
                {
                    if (!AuthorizationScopeService.MatchesScope(trigger.AllowedUsers, trigger.AllowedGroups, trigger.DeniedUsers, trigger.DeniedGroups))
                    {
                        Trace(trigger, form, "Skipped", "Authorization scope rejected.");
                        continue;
                    }

                    if (!MacroEngine.CheckCondition(trigger.Condition, form))
                    {
                        Trace(trigger, form, "Skipped", "Condition evaluated false.");
                        continue;
                    }

                    Trace(trigger, form, "Executing", eventName);
                    string result = string.Empty;
                    if (!string.IsNullOrWhiteSpace(trigger.UniversalFunctionCode))
                    {
                        result = UniversalFunctionService.Execute(trigger.UniversalFunctionCode, form, row);
                    }
                    else if (!string.IsNullOrWhiteSpace(trigger.Macro))
                    {
                        MacroEngine.ExecuteMacro(trigger.Macro, form, row);
                    }

                    Trace(trigger, form, "Success", result);
                }
                catch (UnauthorizedAccessException ex)
                {
                    allow = false;
                    Trace(trigger, form, "Blocked", ex.Message);
                    ExceptionLogger.LogHandled(ex, $"UnifiedTriggerService.Execute:{trigger.Code}");
                }
                catch (Exception ex)
                {
                    allow = false;
                    Trace(trigger, form, "Error", ex.Message);
                    ExceptionLogger.LogHandled(ex, $"UnifiedTriggerService.Execute:{trigger.Code}");
                }
            }

            return allow;
        }

        private static bool Matches(UnifiedTriggerEntry trigger, Form form, string eventName, bool beforeAction, string itemId, string columnId)
        {
            if (trigger == null || !trigger.Active) return false;
            if (!string.IsNullOrWhiteSpace(trigger.FormType) && !string.Equals(trigger.FormType, form?.TypeEx, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(trigger.EventType) && !EventMatches(trigger.EventType, eventName)) return false;
            if (trigger.BeforeAction != beforeAction) return false;
            if (!string.IsNullOrWhiteSpace(trigger.ItemId) && !string.Equals(trigger.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(trigger.ColumnId) && !string.Equals(trigger.ColumnId, columnId, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static bool EventMatches(string configured, string actual)
        {
            if (string.IsNullOrWhiteSpace(configured)) return true;
            configured = configured.Trim();
            return string.Equals(configured, actual, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(configured, "ET_" + actual, StringComparison.OrdinalIgnoreCase);
        }

        private static string MapItemEvent(BoEventTypes eventType)
        {
            if (eventType == BoEventTypes.et_FORM_LOAD) return "FORM_LOAD";
            if (eventType == BoEventTypes.et_ITEM_PRESSED) return "ITEM_PRESSED";
            if (eventType == BoEventTypes.et_CLICK) return "CLICK";
            if (eventType == BoEventTypes.et_VALIDATE) return "VALIDATE";
            if (eventType == BoEventTypes.et_COMBO_SELECT) return "COMBO_SELECT";
            if (eventType == BoEventTypes.et_CHOOSE_FROM_LIST) return "CHOOSE_FROM_LIST";
            if (eventType == BoEventTypes.et_MATRIX_LINK_PRESSED) return "MATRIX_LINK_PRESSED";
            return eventType.ToString();
        }

        private static void Trace(UnifiedTriggerEntry trigger, Form form, string status, string detail)
        {
            if (trigger == null || !trigger.TraceEnabled) return;
            AuditLogManager.LogDetailedAction("UnifiedTrigger", $"{trigger.Code}: {detail}", status, SafeUser(), form?.TypeEx ?? "N/A", trigger.EventType);
        }

        private static string SafeUser()
        {
            try { return B1App.Instance.Company.UserName; }
            catch { return string.Empty; }
        }

        private static UnifiedTriggerEntry ReadEntry(ToolboxSettingEntry setting)
        {
            try
            {
                var entry = JsonSerializer.Deserialize<UnifiedTriggerEntry>(setting.Value ?? string.Empty, JsonOptions);
                if (entry == null) return null;
                entry.Code = NormalizeCode(entry.Code);
                return entry;
            }
            catch
            {
                return new UnifiedTriggerEntry
                {
                    Code = NormalizeCode(setting.Code?.Substring(Prefix.Length)),
                    Name = setting.Description,
                    Macro = setting.Value
                };
            }
        }

        private static string NormalizeCode(string code)
        {
            code = (code ?? string.Empty).Trim();
            if (code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) code = code.Substring(Prefix.Length);
            return code.Replace(" ", "_").ToUpperInvariant();
        }
    }
}
