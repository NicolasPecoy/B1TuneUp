using System;

namespace B1TuneUp.Models
{
    public enum RuleType
    {
        Validation,
        UICustomization,
        Macro
    }

    public class B1Rule
    {
        public string ID { get; set; }
        public string FormType { get; set; }
        public RuleType Type { get; set; }
        public string EventType { get; set; } // e.g. "et_FORM_LOAD", "et_ITEM_CLICK"
        public bool BeforeAction { get; set; }
        public string Condition { get; set; } // SQL or Simple expression
        public string Action { get; set; }    // Macro or SQL command
    }
}
