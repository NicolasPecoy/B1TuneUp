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
                    ? "SELECT * FROM \"@BTUN_SEARCH\" ORDER BY \"Name\""
                    : "SELECT * FROM [@BTUN_SEARCH] ORDER BY [Name]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new SearchConfigEntry
                    {
                        Code = ReadString(rs, "Code"),
                        Name = ReadString(rs, "Name"),
                        Query = ReadString(rs, "U_Query"),
                        Action = ReadString(rs, "U_Action"),
                        Description = ReadString(rs, "U_Desc"),
                        Category = ReadString(rs, "U_Category"),
                        Tags = ReadString(rs, "U_Tags"),
                        AllowedUsers = ReadString(rs, "U_AllowUsers"),
                        AllowedGroups = ReadString(rs, "U_AllowGrps"),
                        DeniedUsers = ReadString(rs, "U_DenyUsers"),
                        DeniedGroups = ReadString(rs, "U_DenyGrps"),
                        Favorite = string.Equals(ReadString(rs, "U_Favorite"), "Y", StringComparison.OrdinalIgnoreCase),
                        Active = !string.Equals(ReadString(rs, "U_Active"), "N", StringComparison.OrdinalIgnoreCase),
                        PageSize = SafeInt(ReadString(rs, "U_PageSize"), 50),
                        CacheSeconds = SafeInt(ReadString(rs, "U_CacheSec"), 30)
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
                SetField(table, "U_Desc", entry.Description);
                SetField(table, "U_Category", entry.Category);
                SetField(table, "U_Tags", entry.Tags);
                SetField(table, "U_AllowUsers", entry.AllowedUsers);
                SetField(table, "U_AllowGrps", entry.AllowedGroups);
                SetField(table, "U_DenyUsers", entry.DeniedUsers);
                SetField(table, "U_DenyGrps", entry.DeniedGroups);
                SetField(table, "U_Favorite", entry.Favorite ? "Y" : "N");
                SetField(table, "U_Active", entry.Active ? "Y" : "N");
                SetField(table, "U_PageSize", entry.PageSize.ToString());
                SetField(table, "U_CacheSec", entry.CacheSeconds.ToString());

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

        private static int SafeInt(string value, int fallback)
        {
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}
