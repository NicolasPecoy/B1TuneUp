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
    }
}
