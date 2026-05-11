namespace B1TuneUp.Models
{
    public class PrintDeliveryRuleEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; } = true;
        public string DocType { get; set; }
        public string CardCode { get; set; }
        public string Language { get; set; }
        public string Branch { get; set; }
        public string Channel { get; set; } = "Email";
        public string ReportCode { get; set; }
        public string EmailTemplateCode { get; set; }
        public string SavePath { get; set; }
        public string AttachmentsQuery { get; set; }
        public string ConditionSql { get; set; }
        public string ScheduleCode { get; set; }
        public string Notes { get; set; }
    }
}
