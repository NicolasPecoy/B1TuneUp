using System;

namespace B1TuneUp.Models
{
    public class LayoutDefinitionEntry
    {
        public string LayoutName { get; set; }
        public string FormType { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string Owner { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? Version { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(FormType)
            ? LayoutName ?? string.Empty
            : $"{FormType} - {LayoutName}";

        public string MetadataLine
            => string.IsNullOrWhiteSpace(Owner)
                ? CreatedAt?.ToString("g") ?? string.Empty
                : $"{Owner} - {CreatedAt:dd/MM/yyyy HH:mm}";
    }
}
