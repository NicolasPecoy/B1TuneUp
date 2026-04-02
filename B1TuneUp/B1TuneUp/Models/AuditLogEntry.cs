using System;

namespace B1TuneUp.Models
{
    public class AuditLogEntry
    {
        public string DocEntry { get; set; }
        public DateTime? Date { get; set; }
        public string Type { get; set; }
        public string Details { get; set; }
        public string Status { get; set; }
        public string User { get; set; }
    }
}
