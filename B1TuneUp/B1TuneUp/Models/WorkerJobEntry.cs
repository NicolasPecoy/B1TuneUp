using System;

namespace B1TuneUp.Models
{
    public class WorkerJobEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string JobType { get; set; }
        public string Payload { get; set; }
        public string Parameters { get; set; }
        public string Status { get; set; } = "Pending";
        public int Priority { get; set; } = 100;
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DueAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string LastError { get; set; }
        public string Result { get; set; }
    }
}
