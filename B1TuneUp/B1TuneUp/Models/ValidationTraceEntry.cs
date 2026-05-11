using System;

namespace B1TuneUp.Models
{
    public class ValidationTraceEntry
    {
        public string Code { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string ColumnId { get; set; }
        public int Row { get; set; }
        public string EventType { get; set; }
        public string Severity { get; set; }
        public bool ConditionResult { get; set; }
        public bool Blocked { get; set; }
        public string Reason { get; set; }
        public string ConditionSql { get; set; }
        public string Message { get; set; }
        public string UserCode { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
