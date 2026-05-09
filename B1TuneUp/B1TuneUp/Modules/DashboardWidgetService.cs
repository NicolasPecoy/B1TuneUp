using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class DashboardWidgetService
    {
        public static IList<DashboardWidgetEntry> GetAll()
        {
            var list = new List<DashboardWidgetEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"Name\",\"U_WidgetType\",\"U_Title\",\"U_Query\",\"U_Width\",\"U_Height\",\"U_Position\",\"U_Color\" FROM \"@BTUN_DASH\" ORDER BY \"U_Position\""
                    : "SELECT [Code],[Name],[U_WidgetType],[U_Title],[U_Query],[U_Width],[U_Height],[U_Position],[U_Color] FROM [@BTUN_DASH] ORDER BY [U_Position]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new DashboardWidgetEntry
                    {
                        Code = ReadString(rs, "Code"),
                        Name = ReadString(rs, "Name"),
                        WidgetType = ReadString(rs, "U_WidgetType"),
                        Title = ReadString(rs, "U_Title"),
                        Query = ReadString(rs, "U_Query"),
                        Width = ReadInt(rs, "U_Width", 320),
                        Height = ReadInt(rs, "U_Height", 200),
                        Position = ReadInt(rs, "U_Position", 0),
                        Color = ReadString(rs, "U_Color")
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

        public static DashboardWidgetEntry Save(DashboardWidgetEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                entry.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            }

            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_DASH");
                bool exists = table.GetByKey(entry.Code);
                if (!exists)
                {
                    table.Code = entry.Code;
                    table.Name = entry.Name ?? entry.Code;
                }
                else
                {
                    table.Name = entry.Name ?? entry.Code;
                }

                SetField(table, "U_WidgetType", entry.WidgetType);
                SetField(table, "U_Title", entry.Title);
                SetField(table, "U_Query", entry.Query);
                SetField(table, "U_Width", entry.Width.ToString());
                SetField(table, "U_Height", entry.Height.ToString());
                SetField(table, "U_Position", entry.Position.ToString());
                SetField(table, "U_Color", entry.Color);

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

            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_DASH");
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

        private static void SetField(UserTable table, string field, string value)
        {
            try { table.UserFields.Fields.Item(field).Value = value ?? string.Empty; }
            catch { }
        }

        private static string ReadString(Recordset rs, string field)
        {
            try { return B1TuneUp.Utils.SapUiSafe.SafeField(rs, field); }
            catch { return string.Empty; }
        }

        private static int ReadInt(Recordset rs, string field, int fallback)
        {
            try
            {
                var raw = B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, field);
                if (raw == null) return fallback;
                if (int.TryParse(raw.ToString(), out var value)) return value;
            }
            catch { }
            return fallback;
        }
    }
}
