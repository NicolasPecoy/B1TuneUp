namespace B1TuneUp.Models
{
    public class ItemActionEntry
    {
        public int DocEntry { get; set; }
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string Event { get; set; }
        public string Action { get; set; }

        public ItemActionEntry Clone() => (ItemActionEntry)MemberwiseClone();
    }
}
