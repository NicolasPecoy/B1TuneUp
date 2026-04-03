namespace B1TuneUp.Models
{
    public class MacroScriptEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public string Parameters { get; set; }

        public MacroScriptEntry Clone()
        {
            return (MacroScriptEntry)MemberwiseClone();
        }
    }
}
