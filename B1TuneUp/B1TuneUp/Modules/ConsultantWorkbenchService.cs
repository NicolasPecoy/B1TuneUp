using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class ConsultantWorkbenchService
    {
        public static IList<ConfigurationSearchResult> Search(string term, string formType = null)
        {
            term = term ?? string.Empty;
            var results = new List<ConfigurationSearchResult>();
            results.AddRange(ModuleActivationService.GetAll().Select(m => new ConfigurationSearchResult
            {
                Area = "Module",
                Code = m.Key,
                Name = m.Name,
                Active = m.Enabled,
                Detail = m.Description
            }));
            results.AddRange(UniversalFunctionService.GetAll().Select(f => new ConfigurationSearchResult
            {
                Area = "UniversalFunction",
                Code = f.Code,
                Name = f.Name,
                Category = f.Category,
                Tags = f.Tags,
                Active = f.Active,
                Detail = $"{f.Type}: {Trim(f.Payload)}"
            }));
            results.AddRange(UnifiedTriggerService.GetAll().Select(t => new ConfigurationSearchResult
            {
                Area = "Trigger",
                Code = t.Code,
                Name = t.Name,
                Category = t.Category,
                Tags = t.Tags,
                Active = t.Active,
                FormType = t.FormType,
                Detail = $"{t.EventType} {t.ItemId} {t.UniversalFunctionCode}"
            }));
            results.AddRange(AuthorizationAdminService.GetGroups().Select(g => new ConfigurationSearchResult
            {
                Area = "Authorization",
                Code = g.Code,
                Name = g.Name,
                Active = true,
                Detail = g.Users
            }));
            results.AddRange(ToolboxSettingService.GetAll().Select(s => new ConfigurationSearchResult
            {
                Area = "Setting",
                Code = s.Code,
                Name = s.Description,
                Category = s.Category,
                Active = true,
                Detail = Trim(s.Value)
            }));

            return results
                .Where(r => string.IsNullOrWhiteSpace(formType) || string.IsNullOrWhiteSpace(r.FormType) || string.Equals(r.FormType, formType, StringComparison.OrdinalIgnoreCase))
                .Where(r => Matches(r, term))
                .OrderBy(r => r.Area)
                .ThenBy(r => r.Code)
                .Take(500)
                .ToList();
        }

        public static IList<ConfigurationSearchResult> GetForActiveForm()
        {
            var form = SapUiSafe.TryGetActiveForm();
            return Search(string.Empty, form?.TypeEx);
        }

        public static void SetActive(string area, string code, bool active)
        {
            if (string.Equals(area, "UniversalFunction", StringComparison.OrdinalIgnoreCase))
            {
                var item = UniversalFunctionService.GetByCode(code);
                if (item == null) return;
                item.Active = active;
                UniversalFunctionService.Save(item);
            }
            else if (string.Equals(area, "Trigger", StringComparison.OrdinalIgnoreCase))
            {
                var item = UnifiedTriggerService.GetAll().FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
                if (item == null) return;
                item.Active = active;
                UnifiedTriggerService.Save(item);
            }
            else if (string.Equals(area, "Module", StringComparison.OrdinalIgnoreCase))
            {
                var item = ModuleActivationService.GetModule(code);
                if (item == null) return;
                item.Enabled = active;
                ModuleActivationService.Save(item);
            }
            AuditLogManager.LogAction("ConsultantWorkbench", $"{area}:{code} active={active}", "Bulk");
        }

        public static void Duplicate(string area, string code)
        {
            if (string.Equals(area, "UniversalFunction", StringComparison.OrdinalIgnoreCase))
            {
                var item = UniversalFunctionService.GetByCode(code);
                if (item == null) return;
                item = item.Clone();
                item.Code = item.Code + "_COPY";
                item.Name = (item.Name ?? item.Code) + " Copy";
                UniversalFunctionService.Save(item);
            }
            else if (string.Equals(area, "Trigger", StringComparison.OrdinalIgnoreCase))
            {
                var item = UnifiedTriggerService.GetAll().FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
                if (item == null) return;
                item = item.Clone();
                item.Code = item.Code + "_COPY";
                item.Name = (item.Name ?? item.Code) + " Copy";
                UnifiedTriggerService.Save(item);
            }
            AuditLogManager.LogAction("ConsultantWorkbench", $"{area}:{code} duplicado.", "Duplicate");
        }

        public static IList<TestRunResult> RunTests(string area = null)
        {
            var results = new List<TestRunResult>();
            if (string.IsNullOrWhiteSpace(area) || area.Equals("UniversalFunction", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var function in UniversalFunctionService.GetAll().Where(f => f.Active))
                {
                    results.Add(Test(function.Code, "UniversalFunction", () => UniversalFunctionService.Execute(function.Code)));
                }
            }

            if (string.IsNullOrWhiteSpace(area) || area.Equals("Trigger", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var trigger in UnifiedTriggerService.GetAll().Where(t => t.Active))
                {
                    results.Add(Test(trigger.Code, "Trigger", () =>
                    {
                        bool ok = string.IsNullOrWhiteSpace(trigger.Condition) || MacroEngine.CheckCondition(trigger.Condition, SapUiSafe.TryGetActiveForm());
                        return ok ? "Condition OK" : "Condition false";
                    }));
                }
            }
            return results;
        }

        private static TestRunResult Test(string code, string area, Func<string> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                string detail = action();
                sw.Stop();
                AuditLogManager.LogAction("TestRunner", $"{area}:{code} OK in {sw.ElapsedMilliseconds}ms", "Success");
                return new TestRunResult { Area = area, Code = code, Status = "OK", DurationMs = sw.ElapsedMilliseconds, Detail = detail };
            }
            catch (Exception ex)
            {
                sw.Stop();
                ExceptionLogger.LogHandled(ex, $"ConsultantWorkbenchService.Test:{area}:{code}");
                return new TestRunResult { Area = area, Code = code, Status = "Error", DurationMs = sw.ElapsedMilliseconds, Detail = ex.Message };
            }
        }

        private static bool Matches(ConfigurationSearchResult result, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(result.Area, term) || Contains(result.Code, term) || Contains(result.Name, term)
                   || Contains(result.Category, term) || Contains(result.Tags, term) || Contains(result.Detail, term)
                   || Contains(result.FormType, term);
        }

        private static bool Contains(string value, string term)
            => (value ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string Trim(string value)
        {
            value = value ?? string.Empty;
            return value.Length > 220 ? value.Substring(0, 220) + "..." : value;
        }
    }
}
