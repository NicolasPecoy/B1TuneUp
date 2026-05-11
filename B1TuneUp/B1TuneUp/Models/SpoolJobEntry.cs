using System;

namespace B1TuneUp.Models
{
    public class SpoolJobEntry
    {
        public string Code { get; set; }
        public string DocType { get; set; }
        public string DocEntry { get; set; }
        public string CardCode { get; set; }
        public string LayoutCode { get; set; }
        public string LayoutType { get; set; }
        public string PrinterName { get; set; }
        public string OutputFile { get; set; }
        public string Status { get; set; } = "Pending";
        public string LastError { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
