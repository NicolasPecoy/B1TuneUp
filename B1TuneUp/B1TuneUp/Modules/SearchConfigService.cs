using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class SearchConfigService
    {
        public static IList<SearchConfigEntry> GetAll()
        {
            var list = new List<SearchConfigEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"Name\",\"U_Query\",\"U_Action\" FROM \"@BTUN_SEARCH\" ORDER BY \"Name\""
                    : "SELECT [Code],[Name],[U_Query],[U_Action] FROM [@BTUN_SEARCH] ORDER BY [Name]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new SearchConfigEntry
                    {
                        Code = ReadString(rs, "Code"),
                        Name = ReadString(rs, "Name"),
                        Query = ReadString(rs, "U_Query"),
                        Action = ReadString(rs, "U_Action")
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

        public static SearchConfigEntry Save(SearchConfigEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                entry.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            }

            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_SEARCH");
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

                SetField(table, "U_Query", entry.Query);
                SetField(table, "U_Action", entry.Action);

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
                table = B1App.Instance.Company.UserTables.Item("BTUN_SEARCH");
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
    }
}
