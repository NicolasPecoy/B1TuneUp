namespace B1TuneUp.Models
{
    public class PlacementDesignerItem
    {
        public string ItemId { get; set; }
        public string Caption { get; set; }
        public string ItemType { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int FromPane { get; set; }
        public int ToPane { get; set; }
        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;
    }
}
