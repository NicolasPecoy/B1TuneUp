using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class MandatoryFieldService
    {
        public static IList<MandatoryFieldEntry> GetAll()
        {
            var list = new List<MandatoryFieldEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"Name\",\"U_FormType\",\"U_ItemID\",\"U_ColumnID\",\"U_Condition\",\"U_ErrorMsg\" FROM \"@BTUN_MAND\" ORDER BY \"Code\""
                    : "SELECT [Code],[Name],[U_FormType],[U_ItemID],[U_ColumnID],[U_Condition],[U_ErrorMsg] FROM [@BTUN_MAND] ORDER BY [Code]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new MandatoryFieldEntry
                    {
                        Code = ReadString(rs, "Code"),
                        Name = ReadString(rs, "Name"),
                        FormType = ReadString(rs, "U_FormType"),
                        ItemId = ReadString(rs, "U_ItemID"),
                        ColumnId = ReadString(rs, "U_ColumnID"),
                        Condition = ReadString(rs, "U_Condition"),
                        ErrorMessage = ReadString(rs, "U_ErrorMsg")
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

        public static MandatoryFieldEntry Save(MandatoryFieldEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                entry.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            }

            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_MAND");
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

                SetField(table, "U_FormType", entry.FormType);
                SetField(table, "U_ItemID", entry.ItemId);
                SetField(table, "U_ColumnID", entry.ColumnId);
                SetField(table, "U_Condition", entry.Condition);
                SetField(table, "U_ErrorMsg", entry.ErrorMessage);

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
                table = B1App.Instance.Company.UserTables.Item("BTUN_MAND");
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
            try { return rs.Fields.Item(field).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
