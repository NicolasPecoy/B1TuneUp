namespace B1TuneUp.Models
{
    public class SearchConfigEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Query { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string FormType { get; set; }
        public string AutocompleteField { get; set; }
        public string ResultActions { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public string AllowedUsers { get; set; }
        public string AllowedGroups { get; set; }
        public string DeniedUsers { get; set; }
        public string DeniedGroups { get; set; }
        public bool Favorite { get; set; }
        public bool Active { get; set; } = true;
        public int PageSize { get; set; } = 50;
        public int CacheSeconds { get; set; } = 30;

        public SearchConfigEntry Clone()
        {
            return (SearchConfigEntry)MemberwiseClone();
        }
    }
}
