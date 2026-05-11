namespace B1TuneUp.Models
{
    public class ValidationConditionBuilderState
    {
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string ColumnId { get; set; }
        public string Operator { get; set; } = "IsEmpty";
        public string CompareValue { get; set; }
        public string MessageEs { get; set; }
        public string MessageEn { get; set; }
        public string AutoFixMacro { get; set; }
        public string ConfirmText { get; set; }
        public string Severity { get; set; } = "ERROR";
        public string EventType { get; set; } = "DATA_ADD_BEFORE";
        public bool MatrixLineValidation { get; set; }
        public bool RequiresConfirmation { get; set; }
    }
}
