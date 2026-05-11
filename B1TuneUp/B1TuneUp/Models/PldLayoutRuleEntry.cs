namespace B1TuneUp.Models
{
    public class PldLayoutRuleEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; } = true;
        public string DocType { get; set; }
        public string CardCode { get; set; }
        public string Language { get; set; }
        public string Branch { get; set; }
        public string LayoutCode { get; set; }
        public string LayoutType { get; set; } = "PLD";
        public string PrinterName { get; set; }
        public string ExportPath { get; set; }
        public int Priority { get; set; } = 50;
        public string Notes { get; set; }
    }
}
