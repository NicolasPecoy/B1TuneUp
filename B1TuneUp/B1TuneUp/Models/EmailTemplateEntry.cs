using System;

namespace B1TuneUp.Models
{
    public class EmailTemplateEntry
    {
        public int? DocEntry { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public string Sender { get; set; }
        public string Attachment { get; set; }
        public string Channel { get; set; } = "Email";
        public string Priority { get; set; } = "Normal";
        public string Trigger { get; set; }
        public bool Active { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Code : Name;

        public EmailTemplateEntry Clone()
        {
            return new EmailTemplateEntry
            {
                DocEntry = DocEntry,
                Code = Code,
                Name = Name,
                Subject = Subject,
                Body = Body,
                To = To,
                Cc = Cc,
                Bcc = Bcc,
                Sender = Sender,
                Attachment = Attachment,
                Channel = Channel,
                Priority = Priority,
                Trigger = Trigger,
                Active = Active,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
