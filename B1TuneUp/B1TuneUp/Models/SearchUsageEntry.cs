using System;

namespace B1TuneUp.Models
{
    public class SearchUsageEntry
    {
        public string Code { get; set; }
        public string UserCode { get; set; }
        public string SearchCode { get; set; }
        public string SearchText { get; set; }
        public string ResultKey { get; set; }
        public bool Favorite { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
