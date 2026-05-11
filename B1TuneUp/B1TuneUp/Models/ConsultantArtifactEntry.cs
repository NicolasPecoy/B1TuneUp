using System;
using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class ConsultantArtifactEntry
    {
        public string Area { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string EventType { get; set; }
        public int Priority { get; set; }
        public bool Active { get; set; }
        public string Summary { get; set; }
        public string RawJson { get; set; }
        public IList<string> Dependencies { get; set; } = new List<string>();
    }

    public class ConsultantPackage
    {
        public string PackageVersion { get; set; } = "2.0";
        public string CreatedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
        public string CreatedBy { get; set; }
        public string Company { get; set; }
        public string SourceEnvironment { get; set; }
        public string Notes { get; set; }
        public IList<SearchConfigEntry> SearchConfigurations { get; set; } = new List<SearchConfigEntry>();
        public IList<UniversalFunctionEntry> UniversalFunctions { get; set; } = new List<UniversalFunctionEntry>();
        public IList<UnifiedTriggerEntry> Triggers { get; set; } = new List<UnifiedTriggerEntry>();
        public IList<ValidationRuleEntry> ValidationRules { get; set; } = new List<ValidationRuleEntry>();
        public IList<ToolboxSettingEntry> Settings { get; set; } = new List<ToolboxSettingEntry>();
    }

    public class ConsultantPackageDiffEntry
    {
        public string Area { get; set; }
        public string Code { get; set; }
        public string Action { get; set; }
        public bool Conflict { get; set; }
        public string CurrentSummary { get; set; }
        public string IncomingSummary { get; set; }
    }
}
