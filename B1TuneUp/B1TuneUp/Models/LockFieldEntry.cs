namespace B1TuneUp.Models
{
    public class LockFieldEntry
    {
        public int DocEntry { get; set; }
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string ColumnId { get; set; }
        public string TriggerItem { get; set; }
        public string OnEvent { get; set; }
        public string LockType { get; set; }
        public string Condition { get; set; }

        public LockFieldEntry Clone() => (LockFieldEntry)MemberwiseClone();
    }
}
