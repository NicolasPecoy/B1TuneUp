namespace B1TuneUp.Models
{
    public class UniversalFunctionEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } = "Macro";
        public string Payload { get; set; }
        public string Parameters { get; set; }
        public bool Active { get; set; } = true;
        public string AllowedUsers { get; set; }
        public string AllowedGroups { get; set; }
        public string DeniedUsers { get; set; }
        public string DeniedGroups { get; set; }
        public string Notes { get; set; }

        public UniversalFunctionEntry Clone()
        {
            return (UniversalFunctionEntry)MemberwiseClone();
        }
    }
}
