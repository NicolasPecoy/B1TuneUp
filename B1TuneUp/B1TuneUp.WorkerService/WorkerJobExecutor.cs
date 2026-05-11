using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;

namespace B1TuneUp.WorkerService
{
    public sealed class WorkerJobExecutor
    {
        private readonly WorkerSettings _settings;

        public WorkerJobExecutor(WorkerSettings settings)
        {
            _settings = settings;
        }

        public string Execute(WorkerJob job)
        {
            string type = job.JobType ?? string.Empty;
            if (type.Equals("Http", StringComparison.OrdinalIgnoreCase)) return ExecuteHttp(job);
            if (type.Equals("Command", StringComparison.OrdinalIgnoreCase)) return ExecuteCommand(job.Payload, job.Parameters);
            if (type.Equals("ReportExport", StringComparison.OrdinalIgnoreCase) || type.Equals("CrystalReport", StringComparison.OrdinalIgnoreCase)) return ExecuteBridge(_settings.ReportRunnerCommand, job);
            if (type.Equals("PrintDelivery", StringComparison.OrdinalIgnoreCase)) return ExecuteBridge(_settings.PrintRunnerCommand, job);
            if (type.Equals("UniversalFunction", StringComparison.OrdinalIgnoreCase) || type.Equals("Macro", StringComparison.OrdinalIgnoreCase)) return ExecuteBridge(_settings.AddonBridgeCommand, job);
            if (type.Equals("DiPing", StringComparison.OrdinalIgnoreCase)) return ExecuteDiPing();
            throw new InvalidOperationException("Unsupported worker job type: " + type);
        }

        private string ExecuteHttp(WorkerJob job)
        {
            var payload = Parse(job.Parameters);
            string url = Read(payload, "url", job.Payload);
            string method = Read(payload, "method", "GET").ToUpperInvariant();
            string body = Read(payload, "body", string.Empty);
            string headers = Read(payload, "headers", string.Empty);
            if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("HTTP job requires url.");

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(new HttpMethod(method), url))
            {
                foreach (var header in SplitLines(headers))
                {
                    var index = header.IndexOf(':');
                    if (index > 0) request.Headers.TryAddWithoutValidation(header.Substring(0, index).Trim(), header.Substring(index + 1).Trim());
                }
                if (!string.IsNullOrWhiteSpace(body) && method != "GET") request.Content = new StringContent(body, Encoding.UTF8, Read(payload, "contentType", "application/json"));
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)response.StatusCode + ": " + content);
                return content.Length > 1000 ? content.Substring(0, 1000) : content;
            }
        }

        private string ExecuteCommand(string executable, string arguments)
        {
            if (string.IsNullOrWhiteSpace(executable)) throw new InvalidOperationException("Command job requires executable payload.");
            var start = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = File.Exists(executable) ? Path.GetDirectoryName(executable) : AppDomain.CurrentDomain.BaseDirectory
            };
            using (var process = Process.Start(start))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException(error);
                return string.IsNullOrWhiteSpace(output) ? "Command completed." : output.Trim();
            }
        }

        private string ExecuteBridge(string command, WorkerJob job)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new InvalidOperationException("No bridge command configured for " + job.JobType + ". Configure ReportRunnerCommand, PrintRunnerCommand or AddonBridgeCommand.");
            }
            string args = "--jobCode \"" + Escape(job.Code) + "\" --jobType \"" + Escape(job.JobType) + "\" --payload \"" + Escape(job.Payload) + "\"";
            return ExecuteCommand(command, args);
        }

        private string ExecuteDiPing()
        {
            using (var connector = new SapDiConnector(_settings))
            {
                connector.Connect();
                return "DI API connection succeeded.";
            }
        }

        private static Dictionary<string, object> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            try { return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); }
            catch { return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); }
        }

        private static string Read(Dictionary<string, object> values, string key, string fallback)
        {
            object value;
            return values != null && values.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : fallback;
        }

        private static IEnumerable<string> SplitLines(string value)
        {
            return (value ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\\\"");
        }
    }
}
