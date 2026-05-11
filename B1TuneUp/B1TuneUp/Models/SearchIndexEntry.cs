using System;

namespace B1TuneUp.Models
{
    public class SearchIndexEntry
    {
        public string Code { get; set; }
        public string SearchCode { get; set; }
        public string SearchName { get; set; }
        public string ObjectType { get; set; }
        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Keywords { get; set; }
        public string DataJson { get; set; }
        public string Action { get; set; }
        public int BaseRank { get; set; } = 10;
        public DateTime IndexedAtUtc { get; set; } = DateTime.UtcNow;
        public string SourceHash { get; set; }
        public bool Active { get; set; } = true;
    }
}
