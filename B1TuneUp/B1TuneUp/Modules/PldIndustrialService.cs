using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class PldIndustrialService
    {
        private const string RulePrefix = "PLDRULE_";
        private const string SpoolPrefix = "SPOOL_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static IList<PldLayoutRuleEntry> GetRules()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => (s.Code ?? string.Empty).StartsWith(RulePrefix, StringComparison.OrdinalIgnoreCase))
                .Select(ReadRule)
                .Where(r => r != null)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Code)
                .ToList();
        }

        public static PldLayoutRuleEntry SaveRule(PldLayoutRuleEntry rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.Code)) rule.Code = "PLD_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = RulePrefix + rule.Code,
                Category = "CrystalPLD",
                Description = "PLD/Crystal layout rule",
                Value = JsonSerializer.Serialize(rule, JsonOptions)
            });
            AuditLogManager.LogAction("CrystalPLD", $"Rule {rule.Code} saved.", "Save");
            return rule;
        }

        public static IList<SpoolJobEntry> GetSpool()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => (s.Code ?? string.Empty).StartsWith(SpoolPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(ReadSpool)
                .Where(s => s != null)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
        }

        public static PldLayoutRuleEntry SelectLayout(string docType, string cardCode, string language, string branch)
        {
            return GetRules()
                .Where(r => r.Active)
                .Where(r => Matches(r.DocType, docType))
                .Where(r => Matches(r.CardCode, cardCode))
                .Where(r => Matches(r.Language, language))
                .Where(r => Matches(r.Branch, branch))
                .OrderBy(r => r.Priority)
                .FirstOrDefault();
        }

        public static SpoolJobEntry Enqueue(string docType, string docEntry, string cardCode, string language, string branch)
        {
            var rule = SelectLayout(docType, cardCode, language, branch);
            if (rule == null) throw new InvalidOperationException("No hay regla PLD/Crystal aplicable.");
            var spool = new SpoolJobEntry
            {
                Code = "SPOOL_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(),
                DocType = docType,
                DocEntry = docEntry,
                CardCode = cardCode,
                LayoutCode = rule.LayoutCode,
                LayoutType = rule.LayoutType,
                PrinterName = rule.PrinterName,
                OutputFile = BuildOutput(rule, docType, docEntry),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            SaveSpool(spool);
            WorkerQueueService.Enqueue("ReportExport", spool.LayoutCode, JsonSerializer.Serialize(spool, JsonOptions), null, rule.Priority, "PLD/Crystal spool " + docType + "/" + docEntry);
            AuditLogManager.LogAction("CrystalPLD", $"Spool {spool.Code} queued for {docType}/{docEntry}.", "Queue");
            return spool;
        }

        public static IList<SpoolJobEntry> EnqueueMass(IEnumerable<string> docEntries, string docType, string cardCode = null, string language = null, string branch = null)
        {
            var list = new List<SpoolJobEntry>();
            foreach (var docEntry in docEntries ?? Enumerable.Empty<string>())
            {
                try { list.Add(Enqueue(docType, docEntry, cardCode, language, branch)); }
                catch (Exception ex) { ExceptionLogger.LogHandled(ex, "PldIndustrialService.EnqueueMass:" + docEntry); }
            }
            return list;
        }

        private static void SaveSpool(SpoolJobEntry entry)
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = SpoolPrefix + entry.Code,
                Category = "CrystalPLD",
                Description = "PLD/Crystal spool job",
                Value = JsonSerializer.Serialize(entry, JsonOptions)
            });
        }

        private static string BuildOutput(PldLayoutRuleEntry rule, string docType, string docEntry)
        {
            string dir = string.IsNullOrWhiteSpace(rule.ExportPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Spool") : rule.ExportPath;
            return Path.Combine(dir, $"{SafeFile(docType)}_{SafeFile(docEntry)}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }

        private static PldLayoutRuleEntry ReadRule(ToolboxSettingEntry setting)
        {
            try
            {
                var rule = JsonSerializer.Deserialize<PldLayoutRuleEntry>(setting.Value ?? string.Empty, JsonOptions);
                if (rule != null && string.IsNullOrWhiteSpace(rule.Code)) rule.Code = (setting.Code ?? string.Empty).Substring(RulePrefix.Length);
                return rule;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "PldIndustrialService.ReadRule:" + setting.Code);
                return null;
            }
        }

        private static SpoolJobEntry ReadSpool(ToolboxSettingEntry setting)
        {
            try { return JsonSerializer.Deserialize<SpoolJobEntry>(setting.Value ?? string.Empty, JsonOptions); }
            catch (Exception ex) { ExceptionLogger.LogHandled(ex, "PldIndustrialService.ReadSpool:" + setting.Code); return null; }
        }

        private static bool Matches(string ruleValue, string actual)
        {
            return string.IsNullOrWhiteSpace(ruleValue) || ruleValue == "*" || string.Equals(ruleValue, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeFile(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) value = (value ?? string.Empty).Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "NA" : value;
        }
    }
}
