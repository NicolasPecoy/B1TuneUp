using System;
using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class UiCustomizationPackage
    {
        public string FormType { get; set; }
        public string ExportedBy { get; set; }
        public DateTime ExportedAt { get; set; }
        public IList<UiCustomizationEntry> UiEntries { get; set; } = new List<UiCustomizationEntry>();
        public IList<ValidationRuleEntry> ValidationRules { get; set; } = new List<ValidationRuleEntry>();
        public IList<MandatoryFieldEntry> MandatoryRules { get; set; } = new List<MandatoryFieldEntry>();
        public IList<ActionPadEntry> ActionPads { get; set; } = new List<ActionPadEntry>();
        public IList<UiScopeDescriptor> Scopes { get; set; } = new List<UiScopeDescriptor>();
        public IList<UiPackageDependency> Dependencies { get; set; } = new List<UiPackageDependency>();
        public IList<UiInheritanceLink> InheritanceLinks { get; set; } = new List<UiInheritanceLink>();
    }

    public class UiScopeDescriptor
    {
        public string UserCode { get; set; }
        public string UserGroup { get; set; }
        public string Localization { get; set; }
        public string Variant { get; set; }
        public int EntryCount { get; set; }
    }

    public class UiPackageDependency
    {
        public string Token { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; } = true;
    }

    public class UiInheritanceLink
    {
        public string ParentCode { get; set; }
        public string ChildCode { get; set; }
    }
}
