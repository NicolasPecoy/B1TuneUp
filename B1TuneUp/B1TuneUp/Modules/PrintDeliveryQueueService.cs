using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class PrintDeliveryQueueService
    {
        private const string Prefix = "PDQUEUE_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static PrintDeliveryQueueEntry Enqueue(PrintDeliveryQueueEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            entry.Code = string.IsNullOrWhiteSpace(entry.Code) ? Guid.NewGuid().ToString("N").ToUpperInvariant() : entry.Code;
            entry.Status = "Pending";
            Save(entry);
            WorkerQueueService.Enqueue("PrintDelivery", entry.Code, null, null, 50, $"PrintDelivery {entry.DocType}/{entry.DocEntry}");
            AuditLogManager.LogAction("PrintDelivery", $"Queued {entry.Code} {entry.DocType}/{entry.DocEntry}", "Queued");
            return entry;
        }

        public static IList<PrintDeliveryQueueEntry> GetAll()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && s.Code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(Read)
                .Where(e => e != null)
                .OrderByDescending(e => e.CreatedAt)
                .ToList();
        }

        public static string ProcessQueueItem(string code)
        {
            var entry = GetAll().FirstOrDefault(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));
            if (entry == null) throw new InvalidOperationException($"Print Delivery queue item '{code}' no existe.");
            try
            {
                entry.Status = "Running";
                Save(entry);
                string output = BuildOutput(entry);
                if (entry.Channel.Equals("Email", StringComparison.OrdinalIgnoreCase)) SendEmail(entry, output);
                else if (entry.Channel.Equals("Print", StringComparison.OrdinalIgnoreCase)) CrystalReportEngineService.Print(entry.ReportCode, BuildReportParameters(entry));
                else if (entry.Channel.Equals("Save", StringComparison.OrdinalIgnoreCase)) { /* BuildOutput already saved */ }
                else throw new InvalidOperationException($"Canal Print & Delivery no soportado: {entry.Channel}");

                entry.OutputFile = output;
                entry.Status = "Done";
                entry.ProcessedAt = DateTime.Now;
                Save(entry);
                AuditLogManager.LogAction("PrintDelivery", $"Done {entry.Code}: {output}", "Done");
                return output;
            }
            catch (Exception ex)
            {
                entry.RetryCount++;
                entry.LastError = ex.Message;
                entry.Status = entry.RetryCount <= entry.MaxRetries ? "Pending" : "Failed";
                Save(entry);
                ExceptionLogger.LogHandled(ex, $"PrintDeliveryQueueService.Process:{entry.Code}");
                throw;
            }
        }

        public static void Save(PrintDeliveryQueueEntry entry)
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = Prefix + entry.Code,
                Category = "PrintDelivery",
                Description = $"{entry.DocType}/{entry.DocEntry} {entry.Channel}",
                Value = JsonSerializer.Serialize(entry, JsonOptions)
            });
        }

        private static string BuildOutput(PrintDeliveryQueueEntry entry)
        {
            string folder = string.IsNullOrWhiteSpace(entry.SavePath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintDelivery")
                : entry.SavePath;
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"{entry.DocType}_{entry.DocEntry}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            return CrystalReportEngineService.ExportToPdf(entry.ReportCode, BuildReportParameters(entry), file);
        }

        private static string BuildReportParameters(PrintDeliveryQueueEntry entry)
        {
            return $"DocType={entry.DocType}|DocEntry={entry.DocEntry}|CardCode={entry.CardCode}|Language={entry.Language}";
        }

        private static void SendEmail(PrintDeliveryQueueEntry entry, string attachment)
        {
            using (var message = new MailMessage())
            {
                message.To.Add(entry.EmailTo);
                message.Subject = string.IsNullOrWhiteSpace(entry.EmailSubject) ? $"Document {entry.DocType}/{entry.DocEntry}" : entry.EmailSubject;
                message.Body = entry.EmailBody ?? string.Empty;
                if (File.Exists(attachment)) message.Attachments.Add(new Attachment(attachment));
                SendMail(message);
            }
        }

        private static void SendMail(MailMessage mail)
        {
            string smtpServer = GetSmtpSetting("Server", "smtp.gmail.com");
            int smtpPort = int.Parse(GetSmtpSetting("Port", "587"));
            string smtpUsername = GetSmtpSetting("Username", "");
            string smtpPassword = GetSmtpSetting("Password", "");
            string fromEmail = GetSmtpSetting("FromEmail", "b1tuneup@example.com");
            bool enableSsl = bool.Parse(GetSmtpSetting("EnableSSL", "true"));
            if (mail.From == null) mail.From = new MailAddress(fromEmail);
            using (var smtp = new SmtpClient(smtpServer))
            {
                smtp.Port = smtpPort;
                smtp.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                smtp.EnableSsl = enableSsl;
                smtp.Send(mail);
            }
        }

        private static string GetSmtpSetting(string name, string fallback)
        {
            return ToolboxSettingService.GetByCode("SMTP_" + name)?.Value ?? fallback;
        }

        private static PrintDeliveryQueueEntry Read(ToolboxSettingEntry setting)
        {
            try { return JsonSerializer.Deserialize<PrintDeliveryQueueEntry>(setting.Value ?? string.Empty, JsonOptions); }
            catch { return null; }
        }
    }
}
