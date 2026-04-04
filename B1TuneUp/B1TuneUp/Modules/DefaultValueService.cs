using System;
using System.Collections.Generic;
using System.Globalization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class DefaultValueService
    {
        public static IList<DefaultValueEntry> GetAll()
        {
            var list = new List<DefaultValueEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"U_FormType\",\"U_ItemID\",\"U_ColID\",\"U_OnEvent\",\"U_Query\" FROM \"@BTUN_DEFAULTS\" ORDER BY \"U_FormType\",\"U_ItemID\""
                    : "SELECT [Code],U_FormType,U_ItemID,U_ColID,U_OnEvent,U_Query FROM [@BTUN_DEFAULTS] ORDER BY U_FormType,U_ItemID";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new DefaultValueEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        FormType = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        ItemId = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        ColumnId = rs.Fields.Item(3).Value?.ToString() ?? string.Empty,
                        OnEvent = rs.Fields.Item(4).Value?.ToString() ?? "Load",
                        Query = rs.Fields.Item(5).Value?.ToString() ?? string.Empty
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

        public static DefaultValueEntry Save(DefaultValueEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            bool isHana = B1App.Instance.IsHana;
            string codeValue = entry.DocEntry > 0 ? entry.DocEntry.ToString(CultureInfo.InvariantCulture) : null;

            if (entry.DocEntry <= 0)
            {
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_DEFAULTS");
                entry.DocEntry = nextCode;
                codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = Escape($"{entry.FormType}_{entry.ItemId}_{entry.ColumnId}".Trim());
                if (string.IsNullOrEmpty(nameValue))
                {
                    nameValue = $"DEF_{codeValue}";
                }

                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_DEFAULTS\" (\"Code\",\"Name\",\"U_FormType\",\"U_ItemID\",\"U_ColID\",\"U_OnEvent\",\"U_Query\") VALUES ('{codeValue}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.ItemId)}','{Escape(entry.ColumnId)}','{Escape(entry.OnEvent)}','{Escape(entry.Query)}')"
                    : $"INSERT INTO [@BTUN_DEFAULTS] ([Code],[Name],U_FormType,U_ItemID,U_ColID,U_OnEvent,U_Query) VALUES ('{codeValue}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.ItemId)}','{Escape(entry.ColumnId)}','{Escape(entry.OnEvent)}','{Escape(entry.Query)}')";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_DEFAULTS\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_ItemID\"='{Escape(entry.ItemId)}',\"U_ColID\"='{Escape(entry.ColumnId)}',\"U_OnEvent\"='{Escape(entry.OnEvent)}',\"U_Query\"='{Escape(entry.Query)}' WHERE \"Code\"='{codeValue}'"
                    : $"UPDATE [@BTUN_DEFAULTS] SET U_FormType='{Escape(entry.FormType)}',U_ItemID='{Escape(entry.ItemId)}',U_ColID='{Escape(entry.ColumnId)}',U_OnEvent='{Escape(entry.OnEvent)}',U_Query='{Escape(entry.Query)}' WHERE [Code]='{codeValue}'";
                ExecuteNonQuery(updateSql);
            }
            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string codeValue = docEntry.ToString(CultureInfo.InvariantCulture);
            string sql = isHana
                ? $"DELETE FROM \"@BTUN_DEFAULTS\" WHERE \"Code\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_DEFAULTS] WHERE [Code]='{codeValue}'";
            ExecuteNonQuery(sql);
        }

        private static void ExecuteNonQuery(string sql)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");
    }
}
