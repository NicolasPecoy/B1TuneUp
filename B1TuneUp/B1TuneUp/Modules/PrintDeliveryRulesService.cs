using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class PrintDeliveryRulesService
    {
        private const string Prefix = "PDRULE_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IList<PrintDeliveryRuleEntry> GetAll()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && s.Code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(Read)
                .Where(r => r != null)
                .OrderBy(r => r.DocType)
                .ThenBy(r => r.Name)
                .ToList();
        }

        public static PrintDeliveryRuleEntry Save(PrintDeliveryRuleEntry rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrWhiteSpace(rule.Code)) rule.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = Prefix + rule.Code,
                Category = "PrintDeliveryRules",
                Description = rule.Name ?? rule.Code,
                Value = JsonSerializer.Serialize(rule, JsonOptions)
            });
            AuditLogManager.LogAction("PrintDeliveryRule", $"Rule {rule.Code} saved.", "Save");
            return rule;
        }

        public static PrintDeliveryQueueEntry EnqueueFromRule(PrintDeliveryRuleEntry rule, string docEntry, string cardCode = null)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var queue = new PrintDeliveryQueueEntry
            {
                DocType = rule.DocType,
                DocEntry = docEntry,
                CardCode = cardCode ?? rule.CardCode,
                Language = rule.Language,
                Channel = rule.Channel,
                ReportCode = rule.ReportCode,
                SavePath = rule.SavePath,
                EmailSubject = rule.Name,
                EmailBody = rule.Notes,
                EmailTo = rule.EmailTemplateCode
            };
            return PrintDeliveryQueueService.Enqueue(queue);
        }

        public static void Resend(string queueCode)
        {
            var item = PrintDeliveryQueueService.GetAll().FirstOrDefault(q => string.Equals(q.Code, queueCode, StringComparison.OrdinalIgnoreCase));
            if (item == null) return;
            item.Status = "Pending";
            item.LastError = string.Empty;
            PrintDeliveryQueueService.Save(item);
            WorkerQueueService.Enqueue("PrintDelivery", item.Code, null, null, 50, $"Resend PrintDelivery {item.DocType}/{item.DocEntry}");
        }

        private static PrintDeliveryRuleEntry Read(ToolboxSettingEntry setting)
        {
            try
            {
                var rule = JsonSerializer.Deserialize<PrintDeliveryRuleEntry>(setting.Value ?? string.Empty, JsonOptions);
                if (rule != null && string.IsNullOrWhiteSpace(rule.Code)) rule.Code = setting.Code.Substring(Prefix.Length);
                return rule;
            }
            catch { return null; }
        }
    }
}
