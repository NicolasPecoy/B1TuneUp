using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class ValidationTraceService
    {
        private const string Prefix = "VALTRACE_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static void Trace(ValidationTraceEntry entry)
        {
            if (entry == null) return;
            try
            {
                if (string.IsNullOrWhiteSpace(entry.Code)) entry.Code = Guid.NewGuid().ToString("N").ToUpperInvariant();
                ToolboxSettingService.Save(new ToolboxSettingEntry
                {
                    Code = Prefix + entry.Code,
                    Category = "ValidationTrace",
                    Description = $"{entry.RuleCode}:{entry.EventType}:{entry.FormType}",
                    Value = JsonSerializer.Serialize(entry, JsonOptions)
                });
                AuditLogManager.LogDetailedAction("ValidationTrace", $"{entry.RuleCode}: {entry.Reason}", entry.Blocked ? "Blocked" : "Trace", entry.UserCode, entry.FormType, entry.EventType);
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "ValidationTraceService.Trace");
            }
        }

        public static IList<ValidationTraceEntry> GetRecent(string formType = null, int take = 100)
        {
            return ToolboxSettingService.GetAll()
                .Where(s => (s.Code ?? string.Empty).StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(Read)
                .Where(e => e != null)
                .Where(e => string.IsNullOrWhiteSpace(formType) || string.Equals(e.FormType, formType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.CreatedAtUtc)
                .Take(Math.Max(1, take))
                .ToList();
        }

        private static ValidationTraceEntry Read(ToolboxSettingEntry setting)
        {
            try { return JsonSerializer.Deserialize<ValidationTraceEntry>(setting.Value ?? string.Empty, JsonOptions); }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"ValidationTraceService.Read:{setting.Code}");
                return null;
            }
        }
    }
}
