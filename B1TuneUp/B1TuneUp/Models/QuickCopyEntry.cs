namespace B1TuneUp.Models
{
    public class QuickCopyEntry
    {
        public int DocEntry { get; set; }
        public string SourceFormType { get; set; }
        public string SourceObjectType { get; set; }
        public string TargetObjectType { get; set; }
        public string ButtonLabel { get; set; }
        public string PostMacro { get; set; }
        public bool Active { get; set; } = true;

        public QuickCopyEntry Clone() => (QuickCopyEntry)MemberwiseClone();
    }
}
