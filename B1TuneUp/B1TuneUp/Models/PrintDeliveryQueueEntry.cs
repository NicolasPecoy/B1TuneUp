using System;

namespace B1TuneUp.Models
{
    public class PrintDeliveryQueueEntry
    {
        public string Code { get; set; }
        public string DocType { get; set; }
        public string DocEntry { get; set; }
        public string CardCode { get; set; }
        public string Language { get; set; }
        public string Channel { get; set; } = "Email";
        public string ReportCode { get; set; }
        public string EmailTo { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public string SavePath { get; set; }
        public string Status { get; set; } = "Pending";
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        public string LastError { get; set; }
        public string OutputFile { get; set; }
    }
}
