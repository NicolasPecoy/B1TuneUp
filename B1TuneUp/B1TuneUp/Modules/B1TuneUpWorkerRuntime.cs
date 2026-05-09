using System;
using System.Threading;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class B1TuneUpWorkerRuntime
    {
        private static readonly object Sync = new object();
        private static Timer _timer;
        private static bool _running;

        public static bool IsRunning => _timer != null;

        public static void Start()
        {
            lock (Sync)
            {
                if (_timer != null) return;
                _timer = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
                AuditLogManager.LogAction("WorkerRuntime", "Worker runtime started.", "Started");
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                _timer?.Dispose();
                _timer = null;
                AuditLogManager.LogAction("WorkerRuntime", "Worker runtime stopped.", "Stopped");
            }
        }

        public static void Tick()
        {
            if (_running) return;
            _running = true;
            try
            {
                foreach (var job in WorkerQueueService.GetPending())
                {
                    Execute(job);
                }
            }
            finally
            {
                _running = false;
            }
        }

        private static void Execute(WorkerJobEntry job)
        {
            try
            {
                WorkerQueueService.MarkStarted(job);
                string result;
                if (job.JobType.Equals("Macro", StringComparison.OrdinalIgnoreCase))
                {
                    MacroEngine.ExecuteMacro(job.Payload);
                    result = "Macro executed.";
                }
                else if (job.JobType.Equals("UniversalFunction", StringComparison.OrdinalIgnoreCase))
                {
                    result = UniversalFunctionService.Execute(job.Payload);
                }
                else if (job.JobType.Equals("Integration", StringComparison.OrdinalIgnoreCase))
                {
                    MacroEngine.ExecuteMacro(job.Payload);
                    result = "Integration macro executed.";
                }
                else if (job.JobType.Equals("ReportExport", StringComparison.OrdinalIgnoreCase))
                {
                    result = CrystalReportEngineService.ExportToPdf(job.Payload, job.Parameters);
                }
                else if (job.JobType.Equals("PrintDelivery", StringComparison.OrdinalIgnoreCase))
                {
                    result = PrintDeliveryQueueService.ProcessQueueItem(job.Payload);
                }
                else
                {
                    throw new InvalidOperationException($"Worker job type not supported: {job.JobType}");
                }
                WorkerQueueService.MarkDone(job, result);
            }
            catch (Exception ex)
            {
                WorkerQueueService.MarkFailed(job, ex);
            }
        }
    }
}
