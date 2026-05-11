using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    public static class ConsultantConfigurationService
    {
        private const string BackupPrefix = "CONSULTANT_BACKUP_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IList<ConsultantArtifactEntry> FindUsedOnForm(Form form = null)
        {
            form = form ?? SapUiSafe.TryGetActiveForm();
            string formType = form?.TypeEx ?? string.Empty;
            var artifacts = new List<ConsultantArtifactEntry>();
            if (string.IsNullOrWhiteSpace(formType)) return artifacts;

            foreach (var trigger in UnifiedTriggerService.GetAll().Where(t => MatchesForm(t.FormType, formType)))
            {
                artifacts.Add(new ConsultantArtifactEntry
                {
                    Area = "Trigger",
                    Code = trigger.Code,
                    Name = trigger.Name,
                    FormType = trigger.FormType,
                    ItemId = trigger.ItemId,
                    EventType = trigger.EventType,
                    Priority = trigger.Priority,
                    Active = trigger.Active,
                    Summary = $"UF={trigger.UniversalFunctionCode}; Macro={(string.IsNullOrWhiteSpace(trigger.Macro) ? "-" : "Yes")}",
                    RawJson = JsonSerializer.Serialize(trigger, JsonOptions),
                    Dependencies = SplitCodes(trigger.UniversalFunctionCode).ToList()
                });
            }

            foreach (var rule in ValidationRuleService.GetAll().Where(r => MatchesForm(r.FormType, formType)))
            {
                artifacts.Add(new ConsultantArtifactEntry
                {
                    Area = "Validation",
                    Code = rule.Code,
                    Name = rule.Name,
                    FormType = rule.FormType,
                    ItemId = rule.ItemName,
                    EventType = rule.EventType,
                    Priority = rule.Sequence,
                    Active = rule.Active,
                    Summary = $"{rule.Severity}; {rule.Message}",
                    RawJson = JsonSerializer.Serialize(rule, JsonOptions),
                    Dependencies = ExtractUfReferences(rule.Action).ToList()
                });
            }

            foreach (var search in SearchConfigService.GetAll().Where(s => MatchesForm(s.FormType, formType)))
            {
                artifacts.Add(new ConsultantArtifactEntry
                {
                    Area = "Search",
                    Code = search.Code,
                    Name = search.Name,
                    FormType = search.FormType,
                    Priority = search.PageSize,
                    Active = search.Active,
                    Summary = $"{search.Category}; cache={search.CacheSeconds}s",
                    RawJson = JsonSerializer.Serialize(search, JsonOptions)
                });
            }

            return artifacts.OrderBy(a => a.Area).ThenBy(a => a.Priority).ThenBy(a => a.Code).ToList();
        }

        public static ValidationRuleEntry CreateValidationFromCurrentContext(string eventType = "ITEM_PRESSED", string severity = "ERROR")
        {
            var form = SapUiSafe.TryGetActiveForm();
            var entry = new ValidationRuleEntry
            {
                Code = BuildCode("VAL", form?.TypeEx, null),
                Name = $"Validation {form?.TypeEx ?? "Global"}",
                FormType = form?.TypeEx,
                EventType = eventType,
                Severity = severity,
                Active = false,
                Sequence = NextValidationSequence(form?.TypeEx),
                Condition = "SELECT 0",
                Message = "Nueva validacion creada desde el formulario actual.",
                Notes = $"Created from {SapUiSafe.DescribeForm(form)}"
            };
            return ValidationRuleService.Save(entry);
        }

        public static UnifiedTriggerEntry CreateTriggerFromCurrentContext(string eventType = "ITEM_PRESSED", string itemId = null)
        {
            var form = SapUiSafe.TryGetActiveForm();
            var entry = new UnifiedTriggerEntry
            {
                Code = BuildCode("TRG", form?.TypeEx, itemId),
                Name = $"Trigger {form?.TypeEx ?? "Global"}",
                FormType = form?.TypeEx,
                ItemId = itemId,
                EventType = eventType,
                BeforeAction = false,
                Active = false,
                Priority = NextTriggerPriority(form?.TypeEx),
                TraceEnabled = true,
                Macro = "Status('Trigger B1TuneUp ejecutado')"
            };
            return UnifiedTriggerService.Save(entry);
        }

        public static UniversalFunctionEntry DuplicateUniversalFunction(string code, string newCode = null)
        {
            var source = UniversalFunctionService.GetByCode(code);
            if (source == null) throw new InvalidOperationException($"Universal Function '{code}' no existe.");
            var clone = source.Clone();
            clone.Code = string.IsNullOrWhiteSpace(newCode) ? BuildCode(source.Code + "_COPY", null, null) : newCode;
            clone.Name = (source.Name ?? source.Code) + " Copy";
            clone.Active = false;
            clone.Notes = AppendLine(clone.Notes, $"Duplicated from {source.Code} at {DateTime.UtcNow:O}");
            return UniversalFunctionService.Save(clone);
        }

        public static ConsultantPackage BuildPackage(string formType = null)
        {
            return new ConsultantPackage
            {
                CreatedBy = SafeUser(),
                Company = SafeCompany(),
                SourceEnvironment = ToolboxSettingService.GetByCode("ENVIRONMENT_NAME")?.Value,
                SearchConfigurations = SearchConfigService.GetAll().Where(s => MatchesOptional(s.FormType, formType)).Select(s => s.Clone()).ToList(),
                UniversalFunctions = UniversalFunctionService.GetAll().Select(f => f.Clone()).ToList(),
                Triggers = UnifiedTriggerService.GetAll().Where(t => MatchesOptional(t.FormType, formType)).Select(t => t.Clone()).ToList(),
                ValidationRules = ValidationRuleService.GetAll().Where(r => MatchesOptional(r.FormType, formType)).Select(r => r.Clone()).ToList(),
                Settings = ToolboxSettingService.GetAll().Where(s => IsTransportSetting(s.Code)).ToList()
            };
        }

        public static void ExportPackage(string path, string formType = null)
        {
            EnsureDirectory(path);
            File.WriteAllText(path, JsonSerializer.Serialize(BuildPackage(formType), JsonOptions));
            AuditLogManager.LogAction("ConsultantPackage", $"Exported {path}", "Export");
        }

        public static IList<ConsultantPackageDiffEntry> PreviewImport(string path)
        {
            var package = ReadPackage(path);
            var diffs = new List<ConsultantPackageDiffEntry>();
            AddDiffs(diffs, "Search", package.SearchConfigurations, SearchConfigService.GetAll(), x => x.Code, DescribeSearch);
            AddDiffs(diffs, "UniversalFunction", package.UniversalFunctions, UniversalFunctionService.GetAll(), x => x.Code, DescribeUf);
            AddDiffs(diffs, "Trigger", package.Triggers, UnifiedTriggerService.GetAll(), x => x.Code, DescribeTrigger);
            AddDiffs(diffs, "Validation", package.ValidationRules, ValidationRuleService.GetAll(), x => x.Code, DescribeValidation);
            return diffs;
        }

        public static string BackupSnapshot(string reason = null)
        {
            string code = BackupPrefix + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string json = JsonSerializer.Serialize(BuildPackage(), JsonOptions);
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = code,
                Category = "ConsultantBackup",
                Description = reason ?? "Consultant configuration backup",
                Value = json
            });
            AuditLogManager.LogAction("ConsultantPackage", $"{code}: {reason}", "Backup");
            return code;
        }

        public static ConsultantPackage Rollback(string backupCode)
        {
            var setting = ToolboxSettingService.GetByCode(backupCode);
            if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
                throw new InvalidOperationException($"Backup '{backupCode}' no existe.");
            var package = JsonSerializer.Deserialize<ConsultantPackage>(setting.Value, JsonOptions)
                          ?? throw new InvalidOperationException("Backup invalido.");
            ApplyPackage(package, "Rollback");
            return package;
        }

        public static ConsultantPackage ImportPackage(string path, bool dryRun = false)
        {
            var package = ReadPackage(path);
            var diffs = PreviewImport(path);
            if (dryRun)
            {
                AuditLogManager.LogAction("ConsultantPackage", $"Dry-run {path}: {diffs.Count} diferencias.", "DryRun");
                return package;
            }

            BackupSnapshot("Before consultant package import");
            ApplyPackage(package, "Import");
            AuditLogManager.LogAction("ConsultantPackage", $"Imported {path}: {diffs.Count} diferencias.", "Import");
            return package;
        }

        private static void ApplyPackage(ConsultantPackage package, string operation)
        {
            foreach (var search in package.SearchConfigurations ?? new List<SearchConfigEntry>()) SearchConfigService.Save(search);
            foreach (var function in package.UniversalFunctions ?? new List<UniversalFunctionEntry>()) UniversalFunctionService.Save(function);
            foreach (var trigger in package.Triggers ?? new List<UnifiedTriggerEntry>()) UnifiedTriggerService.Save(trigger);
            foreach (var validation in package.ValidationRules ?? new List<ValidationRuleEntry>()) ValidationRuleService.Save(validation);
            foreach (var setting in package.Settings ?? new List<ToolboxSettingEntry>()) ToolboxSettingService.Save(setting);
            AuthorizationScopeService.Invalidate();
            AuditLogManager.LogAction("ConsultantPackage", $"{operation} applied.", operation);
        }

        private static void AddDiffs<T>(ICollection<ConsultantPackageDiffEntry> diffs, string area, IEnumerable<T> incoming, IEnumerable<T> current, Func<T, string> key, Func<T, string> describe)
        {
            var currentMap = (current ?? Enumerable.Empty<T>()).Where(x => !string.IsNullOrWhiteSpace(key(x))).ToDictionary(key, StringComparer.OrdinalIgnoreCase);
            foreach (var item in incoming ?? Enumerable.Empty<T>())
            {
                string code = key(item);
                currentMap.TryGetValue(code ?? string.Empty, out var existing);
                string incomingSummary = describe(item);
                string currentSummary = existing == null ? string.Empty : describe(existing);
                diffs.Add(new ConsultantPackageDiffEntry
                {
                    Area = area,
                    Code = code,
                    Action = existing == null ? "Create" : "Update",
                    Conflict = existing != null && !string.Equals(currentSummary, incomingSummary, StringComparison.Ordinal),
                    CurrentSummary = currentSummary,
                    IncomingSummary = incomingSummary
                });
            }
        }

        private static ConsultantPackage ReadPackage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new FileNotFoundException("Package not found.", path);
            return JsonSerializer.Deserialize<ConsultantPackage>(File.ReadAllText(path), JsonOptions)
                   ?? throw new InvalidOperationException("Package invalido.");
        }

        private static IEnumerable<string> ExtractUfReferences(string macro)
        {
            if (string.IsNullOrWhiteSpace(macro)) return Array.Empty<string>();
            var list = new List<string>();
            foreach (var command in MacroEngine.ParseMacroCommands(macro))
            {
                if (command.StartsWith("UF(", StringComparison.OrdinalIgnoreCase))
                {
                    string value = command.Substring(3).Trim().TrimEnd(')').Trim('\'', '"', ' ');
                    if (!string.IsNullOrWhiteSpace(value)) list.Add(value);
                }
            }
            return list;
        }

        private static bool MatchesForm(string configured, string formType) => string.IsNullOrWhiteSpace(configured) || string.Equals(configured, formType, StringComparison.OrdinalIgnoreCase);
        private static bool MatchesOptional(string configured, string filter) => string.IsNullOrWhiteSpace(filter) || string.Equals(configured, filter, StringComparison.OrdinalIgnoreCase);
        private static string[] SplitCodes(string value) => (value ?? string.Empty).Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        private static string BuildCode(string prefix, string formType, string itemId) => (prefix + "_" + (formType ?? "GLOBAL") + "_" + (itemId ?? DateTime.UtcNow.ToString("HHmmss"))).Replace(" ", "_").Replace("-", "_").ToUpperInvariant();
        private static int NextValidationSequence(string formType) => ValidationRuleService.GetAll().Where(r => MatchesOptional(r.FormType, formType)).Select(r => r.Sequence).DefaultIfEmpty(0).Max() + 10;
        private static int NextTriggerPriority(string formType) => UnifiedTriggerService.GetAll().Where(t => MatchesOptional(t.FormType, formType)).Select(t => t.Priority).DefaultIfEmpty(0).Max() + 10;
        private static bool IsTransportSetting(string code) => !string.IsNullOrWhiteSpace(code) && !code.StartsWith(BackupPrefix, StringComparison.OrdinalIgnoreCase) && !code.StartsWith("SEARCHIDX_", StringComparison.OrdinalIgnoreCase);
        private static string AppendLine(string value, string line) => string.IsNullOrWhiteSpace(value) ? line : value + Environment.NewLine + line;
        private static string DescribeSearch(SearchConfigEntry x) => $"{x.Name}|{x.FormType}|{x.Active}|{x.Query}|{x.Action}";
        private static string DescribeUf(UniversalFunctionEntry x) => $"{x.Name}|{x.Type}|{x.Active}|{x.Payload}|{x.Parameters}";
        private static string DescribeTrigger(UnifiedTriggerEntry x) => $"{x.Name}|{x.FormType}|{x.ItemId}|{x.EventType}|{x.BeforeAction}|{x.Priority}|{x.UniversalFunctionCode}|{x.Macro}";
        private static string DescribeValidation(ValidationRuleEntry x) => $"{x.Name}|{x.FormType}|{x.ItemName}|{x.EventType}|{x.Sequence}|{x.Condition}|{x.Action}";
        private static void EnsureDirectory(string path) { var dir = Path.GetDirectoryName(path); if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir); }
        private static string SafeUser() { try { return B1App.Instance.Company.UserName; } catch { return Environment.UserName; } }
        private static string SafeCompany() { try { return B1App.Instance.Company.CompanyName; } catch { return string.Empty; } }
    }
}
