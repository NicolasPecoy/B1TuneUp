namespace B1TuneUp.Models
{
    public class ModuleConfigurationEntry
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; } = true;
        public string AllowedUsers { get; set; }
        public string AllowedGroups { get; set; }
        public string DeniedUsers { get; set; }
        public string DeniedGroups { get; set; }
        public string Notes { get; set; }
        public int SortOrder { get; set; }

        public ModuleConfigurationEntry Clone()
        {
            return (ModuleConfigurationEntry)MemberwiseClone();
        }
    }
}
