namespace B1TuneUp.Models
{
    public class DashboardWidgetEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string WidgetType { get; set; }
        public string Title { get; set; }
        public string Query { get; set; }
        public int Width { get; set; } = 320;
        public int Height { get; set; } = 200;
        public int Position { get; set; }
        public string Color { get; set; }

        public DashboardWidgetEntry Clone()
        {
            return (DashboardWidgetEntry)MemberwiseClone();
        }
    }
}
