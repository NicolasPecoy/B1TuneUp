using System;
using System.Collections.Generic;
using System.Linq;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class EmailTemplateService
    {
        private const string TableCode = "BTUN_EMAIL";

        public static IList<EmailTemplateEntry> GetAll(string search = null, string channelFilter = null, bool onlyActive = false)
        {
            var list = new List<EmailTemplateEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string tableName = isHana ? "\"@BTUN_EMAIL\"" : "[@BTUN_EMAIL]";
                string qName = isHana ? "\"Name\"" : "[Name]";
                string qSubject = isHana ? "\"U_Subject\"" : "[U_Subject]";
                string qCode = isHana ? "\"Code\"" : "[Code]";
                string qChannel = isHana ? "\"U_Channel\"" : "[U_Channel]";
                string qActive = isHana ? "\"U_Active\"" : "[U_Active]";

                var filters = new List<string>();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string term = Escape(search);
                    filters.Add($"({qName} LIKE '%{term}%' OR {qSubject} LIKE '%{term}%' OR {qCode} LIKE '%{term}%')");
                }
                if (!string.IsNullOrWhiteSpace(channelFilter) && !channelFilter.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                {
                    filters.Add($"{qChannel} = '{Escape(channelFilter)}'");
                }
                if (onlyActive)
                {
                    filters.Add($"({qActive} IS NULL OR {qActive} <> 'N')");
                }

                string sql = $"SELECT * FROM {tableName}";
                if (filters.Count > 0)
                {
                    sql += " WHERE " + string.Join(" AND ", filters);
                }
                sql += $" ORDER BY {qName}";

                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(Map(rs));
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return list;
        }

        public static EmailTemplateEntry Save(EmailTemplateEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            UserTable table = null;
            try
            {
                table = (UserTable)B1App.Instance.Company.UserTables.Item(TableCode);
                bool exists = !string.IsNullOrEmpty(entry.Code) && table.GetByKey(entry.Code);
                if (!exists)
                {
                    entry.Code = string.IsNullOrWhiteSpace(entry.Code)
                        ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()
                        : entry.Code;
                    table.Code = entry.Code;
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Code : entry.Name;
                }
                else
                {
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Code : entry.Name;
                }

                SetField(table, "U_Subject", entry.Subject);
                SetField(table, "U_Body", entry.Body);
                SetField(table, "U_To", entry.To);
                SetField(table, "U_CC", entry.Cc);
                SetField(table, "U_BCC", entry.Bcc);
                SetField(table, "U_Sender", entry.Sender);
                SetField(table, "U_Attach", entry.Attachment);
                SetField(table, "U_Channel", entry.Channel ?? "Email");
                SetField(table, "U_Priority", entry.Priority ?? "Normal");
                SetField(table, "U_Trigger", entry.Trigger);
                SetField(table, "U_Active", entry.Active ? "Y" : "N");

                int res = exists ? table.Update() : table.Add();
                if (res != 0)
                {
                    string err = B1App.Instance.Company.GetLastErrorDescription();
                    throw new InvalidOperationException($"SAP SDK error: {err}");
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }

            return GetByCode(entry.Code);
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            UserTable table = null;
            try
            {
                table = (UserTable)B1App.Instance.Company.UserTables.Item(TableCode);
                if (table.GetByKey(code))
                {
                    int res = table.Remove();
                    if (res != 0)
                    {
                        string err = B1App.Instance.Company.GetLastErrorDescription();
                        throw new InvalidOperationException($"SAP SDK error: {err}");
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }
        }

        public static EmailTemplateEntry GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string tableName = isHana ? "\"@BTUN_EMAIL\"" : "[@BTUN_EMAIL]";
                string column = isHana ? "\"Code\"" : "[Code]";
                string sql = $"SELECT * FROM {tableName} WHERE {column} = '{Escape(code)}'";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return Map(rs);
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return null;
        }

        public static EmailTemplateEntry EnsureSaved(EmailTemplateEntry entry)
        {
            if (entry == null) return null;
            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                return Save(entry);
            }

            return Save(entry);
        }

        public static void SendTest(EmailTemplateEntry entry)
        {
            if (entry == null) return;
            var updated = EnsureSaved(entry);
            if (updated?.DocEntry != null)
            {
                EmailManager.SendEmail(updated.DocEntry.Value.ToString());
            }
        }

        private static EmailTemplateEntry Map(Recordset rs)
        {
            return new EmailTemplateEntry
            {
                DocEntry = ReadInt(rs, "DocEntry"),
                Code = ReadString(rs, "Code"),
                Name = ReadString(rs, "Name"),
                Subject = ReadString(rs, "U_Subject"),
                Body = ReadString(rs, "U_Body"),
                To = ReadString(rs, "U_To"),
                Cc = ReadString(rs, "U_CC"),
                Bcc = ReadString(rs, "U_BCC"),
                Sender = ReadString(rs, "U_Sender"),
                Attachment = ReadString(rs, "U_Attach"),
                Channel = string.IsNullOrWhiteSpace(ReadString(rs, "U_Channel")) ? "Email" : ReadString(rs, "U_Channel"),
                Priority = string.IsNullOrWhiteSpace(ReadString(rs, "U_Priority")) ? "Normal" : ReadString(rs, "U_Priority"),
                Trigger = ReadString(rs, "U_Trigger"),
                Active = !string.Equals(ReadString(rs, "U_Active"), "N", StringComparison.OrdinalIgnoreCase),
                CreatedAt = ReadDate(rs, "CreateDate"),
                UpdatedAt = ReadDate(rs, "UpdateDate")
            };
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");

        private static string ReadString(Recordset rs, string field)
        {
            try { return Convert.ToString(rs.Fields.Item(field).Value); }
            catch { return string.Empty; }
        }

        private static int? ReadInt(Recordset rs, string field)
        {
            try
            {
                var value = rs.Fields.Item(field).Value;
                if (value == null) return null;
                if (int.TryParse(value.ToString(), out var parsed)) return parsed;
            }
            catch { }
            return null;
        }

        private static DateTime? ReadDate(Recordset rs, string field)
        {
            try
            {
                var value = rs.Fields.Item(field).Value;
                if (value == null) return null;
                if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            }
            catch { }
            return null;
        }

        private static void SetField(UserTable table, string fieldName, string value)
        {
            try
            {
                table.UserFields.Fields.Item(fieldName).Value = value ?? string.Empty;
            }
            catch
            {
                // Field might not exist yet; ignore to keep compatibility.
            }
        }
    }
}
