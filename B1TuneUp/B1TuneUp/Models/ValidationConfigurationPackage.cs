using System;
using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class ValidationConfigurationPackage
    {
        public string FormType { get; set; }
        public DateTime ExportedAtUtc { get; set; }
        public string ExportedBy { get; set; }
        public List<ValidationRuleEntry> ValidationRules { get; set; } = new List<ValidationRuleEntry>();
        public List<MandatoryFieldEntry> MandatoryRules { get; set; } = new List<MandatoryFieldEntry>();
    }
}
