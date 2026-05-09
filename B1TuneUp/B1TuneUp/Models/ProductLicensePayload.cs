using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class ProductLicensePayload
    {
        public string Product { get; set; } = "B1TuneUp";
        public string LicenseId { get; set; }
        public string Customer { get; set; }
        public string Edition { get; set; } = "Premium";
        public string CompanyDb { get; set; }
        public string InstallationNumber { get; set; }
        public string HardwareKey { get; set; }
        public string IssuedOn { get; set; }
        public string ExpiresOn { get; set; }
        public List<string> Modules { get; set; } = new List<string>();
        public int MaxUsers { get; set; }
        public string Notes { get; set; }
    }
}
