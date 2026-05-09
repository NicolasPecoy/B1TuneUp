using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class WorkerQueueService
    {
        private const string Prefix = "WORKERJOB_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static WorkerJobEntry Enqueue(string jobType, string payload, string parameters = null, DateTime? dueAt = null, int priority = 100, string name = null)
        {
            var job = new WorkerJobEntry
            {
                Code = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                Name = name ?? jobType,
                JobType = jobType,
                Payload = payload,
                Parameters = parameters,
                DueAt = dueAt,
                Priority = priority
            };
            Save(job);
            AuditLogManager.LogAction("WorkerQueue", $"Job {job.Code} enqueued: {jobType}", "Queued");
            return job;
        }

        public static IList<WorkerJobEntry> GetAll()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && s.Code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(Read)
                .Where(j => j != null)
                .OrderByDescending(j => j.CreatedAt)
                .ToList();
        }

        public static IList<WorkerJobEntry> GetPending(int take = 25)
        {
            var now = DateTime.Now;
            return GetAll()
                .Where(j => string.Equals(j.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(j.Status, "Retry", StringComparison.OrdinalIgnoreCase))
                .Where(j => !j.DueAt.HasValue || j.DueAt.Value <= now)
                .OrderBy(j => j.Priority)
                .ThenBy(j => j.CreatedAt)
                .Take(take)
                .ToList();
        }

        public static void Save(WorkerJobEntry job)
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = Prefix + job.Code,
                Category = "Worker",
                Description = job.Name ?? job.JobType,
                Value = JsonSerializer.Serialize(job, JsonOptions)
            });
        }

        public static void MarkStarted(WorkerJobEntry job)
        {
            job.Status = "Running";
            job.StartedAt = DateTime.Now;
            Save(job);
        }

        public static void MarkDone(WorkerJobEntry job, string result)
        {
            job.Status = "Done";
            job.FinishedAt = DateTime.Now;
            job.Result = result;
            Save(job);
            AuditLogManager.LogAction("WorkerQueue", $"Job {job.Code} done: {result}", "Done");
        }

        public static void MarkFailed(WorkerJobEntry job, Exception ex)
        {
            job.RetryCount++;
            job.LastError = ex.Message;
            job.FinishedAt = DateTime.Now;
            job.Status = job.RetryCount <= job.MaxRetries ? "Retry" : "Failed";
            if (job.Status == "Retry") job.DueAt = DateTime.Now.AddMinutes(Math.Max(1, job.RetryCount * 2));
            Save(job);
            ExceptionLogger.LogHandled(ex, $"WorkerQueueService.Job:{job.Code}:{job.JobType}");
        }

        private static WorkerJobEntry Read(ToolboxSettingEntry setting)
        {
            try { return JsonSerializer.Deserialize<WorkerJobEntry>(setting.Value ?? string.Empty, JsonOptions); }
            catch { return null; }
        }
    }
}
