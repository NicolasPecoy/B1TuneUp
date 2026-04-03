using System;

namespace B1TuneUp.Models
{
    public class ValidationRuleEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string FormType { get; set; }
        public string ItemName { get; set; }
        public string EventType { get; set; }
        public string Condition { get; set; }
        public string Action { get; set; }
        public string Severity { get; set; } = "ERROR";
        public bool Active { get; set; } = true;
        public string AppliesToUser { get; set; }
        public string AppliesToUserGroup { get; set; }
        public string Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ValidationRuleEntry Clone()
        {
            return (ValidationRuleEntry)MemberwiseClone();
        }
    }
}
