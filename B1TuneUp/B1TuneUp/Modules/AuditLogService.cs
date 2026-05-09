using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class AuditLogService
    {
        public static IList<AuditLogEntry> GetEntries(DateTime? from = null, DateTime? to = null, string type = null, string status = null, string user = null)
        {
            var list = new List<AuditLogEntry>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = BuildQuery(from, to, type, status, user);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new AuditLogEntry
                    {
                        DocEntry = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0),
                        Date = SafeDateTime(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 1)),
                        Type = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Details = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3),
                        Status = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4),
                        User = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 5)
                    });
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return list;
        }

        private static string BuildQuery(DateTime? from, DateTime? to, string type, string status, string user)
        {
            var conditions = new List<string>();
            bool isHana = B1App.Instance.IsHana;
            string dateFormat = isHana ? "yyyy-MM-dd HH:mm" : "yyyyMMdd HH:mm";

            string dateColumn = QuoteIdentifier("U_Date", isHana);
            string typeColumn = QuoteIdentifier("U_Type", isHana);
            string statusColumn = QuoteIdentifier("U_Status", isHana);
            string userColumn = QuoteIdentifier("U_User", isHana);
            string docEntryColumn = QuoteIdentifier("Code", isHana);
            string detailsColumn = QuoteIdentifier("U_Details", isHana);
            string tableName = QuoteIdentifier("@BTUN_LOG", isHana);

            if (from.HasValue)
            {
                string date = from.Value.ToString(dateFormat);
                conditions.Add($"{dateColumn} >= '{date}'");
            }
            if (to.HasValue)
            {
                string date = to.Value.ToString(dateFormat);
                conditions.Add($"{dateColumn} <= '{date}'");
            }
            if (!string.IsNullOrEmpty(type))
            {
                string safeType = EscapeValue(type);
                conditions.Add($"{typeColumn} = '{safeType}'");
            }
            if (!string.IsNullOrEmpty(status))
            {
                string safeStatus = EscapeValue(status);
                conditions.Add($"{statusColumn} = '{safeStatus}'");
            }
            if (!string.IsNullOrEmpty(user))
            {
                string safeUser = EscapeValue(user);
                conditions.Add($"{userColumn} = '{safeUser}'");
            }

            string baseQuery = $"SELECT {docEntryColumn},{dateColumn},{typeColumn},{detailsColumn},{statusColumn},{userColumn} FROM {tableName}";

            if (conditions.Count > 0)
            {
                baseQuery += " WHERE " + string.Join(" AND ", conditions);
            }

            baseQuery += $" ORDER BY {docEntryColumn} DESC";
            return baseQuery;
        }

        private static string QuoteIdentifier(string identifier, bool isHana)
        {
            if (isHana)
            {
                return string.Concat((char)34, identifier, (char)34);
            }

            return string.Concat('[', identifier, ']');
        }

        private static string EscapeValue(string value)
        {
            return value?.Replace("'", "''") ?? string.Empty;
        }

        private static DateTime? SafeDateTime(object value)
        {
            if (value == null) return null;
            DateTime parsed;
            if (DateTime.TryParse(value.ToString(), out parsed)) return parsed;
            return null;
        }
    }
}
