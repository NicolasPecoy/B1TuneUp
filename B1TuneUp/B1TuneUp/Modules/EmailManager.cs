using System;
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
                    string to = rs.Fields.Item("U_To").Value.ToString();
                    string subject = rs.Fields.Item("U_Subject").Value.ToString();
                    string body = rs.Fields.Item("U_Body").Value.ToString();
                    string attach = rs.Fields.Item("U_Attach").Value.ToString();

                    // Obtener configuración SMTP de Toolbox o similar
                    // Aquí simplificamos, asumiendo que se puede configurar.
                    Send(to, subject, body, attach);
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void Send(string to, string subject, string body, string attachmentPath)
        {
            try
            {
                // Obtener configuración SMTP desde la base de datos
                string smtpServer = GetSmtpSetting("Server", "smtp.gmail.com");
                int smtpPort = int.Parse(GetSmtpSetting("Port", "587"));
                string smtpUsername = GetSmtpSetting("Username", "");
                string smtpPassword = GetSmtpSetting("Password", "");
                string fromEmail = GetSmtpSetting("FromEmail", "b1tuneup@example.com");
                bool enableSsl = bool.Parse(GetSmtpSetting("EnableSSL", "true"));

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtpServer);

                mail.From = new MailAddress(fromEmail);
                mail.To.Add(to);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                if (!string.IsNullOrEmpty(attachmentPath) && System.IO.File.Exists(attachmentPath))
                {
                    Attachment attachment = new Attachment(attachmentPath);
                    mail.Attachments.Add(attachment);
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
