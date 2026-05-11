using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class ProductLicenseActivationRequest
    {
        public string Product { get; set; } = "B1TuneUp";
        public string Customer { get; set; }
        public string CompanyDb { get; set; }
        public string InstallationNumber { get; set; }
        public string HardwareKey { get; set; }
        public string RequestedEdition { get; set; } = "Premium";
        public List<string> RequestedModules { get; set; } = new List<string>();
        public string RequestedOn { get; set; }
    }
}
