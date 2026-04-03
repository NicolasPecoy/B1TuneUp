namespace B1TuneUp.Models
{
    public class MenuConfigEntry
    {
        public int DocEntry { get; set; }
        public string ParentId { get; set; }
        public string MenuId { get; set; }
        public string Caption { get; set; }
        public int Position { get; set; }
        public string Action { get; set; }
        public bool Enabled { get; set; } = true;

        public MenuConfigEntry Clone() => (MenuConfigEntry)MemberwiseClone();
    }
}
