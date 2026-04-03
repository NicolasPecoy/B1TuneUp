using System;
using System.Collections.Generic;
using System.Net.Mail;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using Attachment = System.Net.Mail.Attachment;

namespace B1TuneUp.Modules
{
    public static class EmailManager
    {
        public static void SendEmail(string docEntry)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_EMAIL\" WHERE \"DocEntry\" = '{docEntry}'"
                    : $"SELECT * FROM [@BTUN_EMAIL] WHERE [DocEntry] = '{docEntry}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string to = ReadField(rs, "U_To");
                    string cc = ReadField(rs, "U_CC");
                    string bcc = ReadField(rs, "U_BCC");
                    string subject = ReadField(rs, "U_Subject");
                    string body = ReadField(rs, "U_Body");
                    string attach = ReadField(rs, "U_Attach");
                    string channel = ReadField(rs, "U_Channel");
                    string priority = ReadField(rs, "U_Priority");
                    string sender = ReadField(rs, "U_Sender");
                    bool active = !string.Equals(ReadField(rs, "U_Active"), "N", StringComparison.OrdinalIgnoreCase);

                    if (!active)
                    {
                        B1App.Instance.Application.SetStatusBarMessage("Plantilla de email inactiva, no se envía notificación.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                        return;
                    }

                    switch ((channel ?? "Email").Trim())
                    {
                        case "SAPMessage":
                            SendSapMessage(to, subject, body);
                            break;
                        case "Desktop":
                            SendDesktopToast(subject, body);
                            break;
                        default:
                            Send(to, cc, bcc, subject, body, attach, sender, priority);
                            break;
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void Send(string to, string cc, string bcc, string subject, string body, string attachmentPath, string senderOverride, string priority)
        {
            try
            {
                // Obtener configuración SMTP desde la base de datos
                string smtpServer = GetSmtpSetting("Server", "smtp.gmail.com");
                int smtpPort = int.Parse(GetSmtpSetting("Port", "587"));
                string smtpUsername = GetSmtpSetting("Username", "");
                string smtpPassword = GetSmtpSetting("Password", "");
                string fromEmail = string.IsNullOrWhiteSpace(senderOverride)
                    ? GetSmtpSetting("FromEmail", "b1tuneup@example.com")
                    : senderOverride;
                bool enableSsl = bool.Parse(GetSmtpSetting("EnableSSL", "true"));

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtpServer);

                mail.From = new MailAddress(fromEmail);
                foreach (var address in SplitRecipients(to))
                {
                    mail.To.Add(address);
                }
                foreach (var address in SplitRecipients(cc))
                {
                    mail.CC.Add(address);
                }
                foreach (var address in SplitRecipients(bcc))
                {
                    mail.Bcc.Add(address);
                }
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;
                mail.Priority = ParsePriority(priority);

                foreach (var path in SplitAttachments(attachmentPath))
                {
                    if (System.IO.File.Exists(path))
                    {
                        Attachment attachment = new Attachment(path);
                        mail.Attachments.Add(attachment);
                    }
                }

                SmtpServer.Port = smtpPort;
                SmtpServer.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                SmtpServer.EnableSsl = enableSsl;

                SmtpServer.Send(mail);
                B1App.Instance.Application.SetStatusBarMessage($"Email enviado correctamente a {to}", SAPbouiCOM.BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error enviando email: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        private static void SendSapMessage(string recipients, string subject, string body)
        {
            var app = B1App.Instance.Application;
            var users = string.Join(", ", SplitRecipients(recipients));
            var preview = string.IsNullOrWhiteSpace(subject) ? body : $"{subject} - {body}";
            if (string.IsNullOrWhiteSpace(users))
            {
                app.SetStatusBarMessage($"[SAP Message] {preview}", SAPbouiCOM.BoMessageTime.bmt_Short, false);
            }
            else
            {
                app.SetStatusBarMessage($"[SAP Message] Destinatarios: {users}. Contenido: {preview}", SAPbouiCOM.BoMessageTime.bmt_Short, false);
            }
        }

        private static void SendDesktopToast(string subject, string body)
        {
            var message = string.IsNullOrWhiteSpace(subject) ? body : $"{subject} - {body}";
            B1App.Instance.Application.SetStatusBarMessage(message, SAPbouiCOM.BoMessageTime.bmt_Short, false);
        }

        private static MailPriority ParsePriority(string priority)
        {
            switch (priority)
            {
                case "High":
                    return MailPriority.High;
                case "Low":
                    return MailPriority.Low;
                default:
                    return MailPriority.Normal;
            }
        }

        private static IEnumerable<string> SplitRecipients(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            var tokens = raw.Split(new[] { ';', '|', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var address = token.Trim();
                if (!string.IsNullOrEmpty(address))
                {
                    yield return address;
                }
            }
        }

        private static IEnumerable<string> SplitAttachments(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            var tokens = raw.Split(new[] { '|', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var path = token.Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    yield return path;
                }
            }
        }

        private static string ReadField(Recordset rs, string fieldName)
        {
            try { return rs.Fields.Item(fieldName).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string GetSmtpSetting(string settingName, string defaultValue)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = 'SMTP_{settingName}'"
                    : $"SELECT [U_Value] FROM [@BTUN_TBOX] WHERE [U_Code] = 'SMTP_{settingName}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return rs.Fields.Item(0).Value.ToString();
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }
    }
}




