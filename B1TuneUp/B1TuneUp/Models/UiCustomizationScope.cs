using System;

namespace B1TuneUp.Models
{
    public class UiCustomizationScope
    {
        public string UserCode { get; set; }
        public string UserGroup { get; set; }
        public string Localization { get; set; }
        public string Variant { get; set; }
        public string DependsOn { get; set; }
        public string InheritFrom { get; set; }
        public int Priority { get; set; } = 10;
    }
}
