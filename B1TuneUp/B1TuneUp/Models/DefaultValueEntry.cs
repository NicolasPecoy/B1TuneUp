namespace B1TuneUp.Models
{
    public class DefaultValueEntry
    {
        public int DocEntry { get; set; }
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string ColumnId { get; set; }
        public string OnEvent { get; set; }
        public string Query { get; set; }

        public DefaultValueEntry Clone() => (DefaultValueEntry)MemberwiseClone();
    }
}
