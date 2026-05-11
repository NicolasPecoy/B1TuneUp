using System;
using System.Web.Script.Serialization;

namespace B1TuneUp.WorkerService
{
    public sealed class WorkerJob
    {
        public string Code { get; set; }
        public string JobType { get; set; }
        public string Payload { get; set; }
        public string Parameters { get; set; }
        public string Status { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string LastError { get; set; }
        public string LastResult { get; set; }

        public static WorkerJob FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new WorkerJob();
            var serializer = new JavaScriptSerializer();
            var job = serializer.Deserialize<WorkerJob>(json) ?? new WorkerJob();
            if (string.IsNullOrWhiteSpace(job.Status)) job.Status = "Pending";
            return job;
        }

        public string ToJson()
        {
            return new JavaScriptSerializer().Serialize(this);
        }
    }
}
