using System;
using System.IO;

namespace B1TuneUp.WorkerService
{
    public sealed class WatchdogMonitor
    {
        private readonly WorkerSettings _settings;

        public WatchdogMonitor(WorkerSettings settings)
        {
            _settings = settings;
        }

        public void WriteHeartbeat(string status)
        {
            try
            {
                Directory.CreateDirectory(_settings.LogDirectory);
                File.WriteAllText(Path.Combine(_settings.LogDirectory, "B1TuneUp.Worker.heartbeat.json"),
                    "{ \"workerId\": \"" + Escape(_settings.WorkerId) + "\", \"status\": \"" + Escape(status) + "\", \"utc\": \"" + DateTime.UtcNow.ToString("O") + "\" }");
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("Unable to write worker heartbeat.", ex);
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
