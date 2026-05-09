using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class ConfigurationPackage
    {
        public string PackageVersion { get; set; } = "1.0";
        public string CreatedAt { get; set; }
        public string Company { get; set; }
        public List<ModuleConfigurationEntry> Modules { get; set; } = new List<ModuleConfigurationEntry>();
        public List<ToolboxSettingEntry> Settings { get; set; } = new List<ToolboxSettingEntry>();
        public List<UniversalFunctionEntry> UniversalFunctions { get; set; } = new List<UniversalFunctionEntry>();
        public List<AuthorizationGroupEntry> AuthorizationGroups { get; set; } = new List<AuthorizationGroupEntry>();
        public List<UnifiedTriggerEntry> Triggers { get; set; } = new List<UnifiedTriggerEntry>();
    }
}
