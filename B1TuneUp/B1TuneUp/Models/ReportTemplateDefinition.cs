using System;

namespace B1TuneUp.Models
{
    public class ReportTemplateDefinition
    {
        public int? DocEntry { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DataBase64 { get; set; }
        public string Parameters { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ReportTemplateDefinition Clone()
        {
            return new ReportTemplateDefinition
            {
                DocEntry = DocEntry,
                Name = Name,
                Description = Description,
                DataBase64 = DataBase64,
                Parameters = Parameters,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
