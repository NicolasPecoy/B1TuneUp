namespace B1TuneUp.Models
{
    public class UnifiedTriggerEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public bool Active { get; set; } = true;
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string ColumnId { get; set; }
        public string EventType { get; set; } = "FORM_LOAD";
        public bool BeforeAction { get; set; }
        public string Condition { get; set; }
        public string UniversalFunctionCode { get; set; }
        public string Macro { get; set; }
        public string AllowedUsers { get; set; }
        public string AllowedGroups { get; set; }
        public string DeniedUsers { get; set; }
        public string DeniedGroups { get; set; }
        public bool TraceEnabled { get; set; } = true;
        public string Notes { get; set; }

        public UnifiedTriggerEntry Clone()
        {
            return (UnifiedTriggerEntry)MemberwiseClone();
        }
    }
}
