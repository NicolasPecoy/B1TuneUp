using System;

namespace B1TuneUp.Models
{
    public class FormTemplateDefinition
    {
        public int? DocEntry { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FormType { get; set; }
        public string SerializedData { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public FormTemplateDefinition Clone()
        {
            return new FormTemplateDefinition
            {
                DocEntry = DocEntry,
                Name = Name,
                Description = Description,
                FormType = FormType,
                SerializedData = SerializedData,
                CreatedBy = CreatedBy,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
