namespace B1TuneUp.Models
{
    public class SearchConfigEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Query { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }

        public SearchConfigEntry Clone()
        {
            return (SearchConfigEntry)MemberwiseClone();
        }
    }
}
