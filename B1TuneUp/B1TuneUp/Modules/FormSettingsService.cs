using System;
using System.Collections.Generic;
using System.Globalization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class FormSettingsService
    {
        public static IList<FormSettingEntry> GetAll()
        {
            var list = new List<FormSettingEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"U_FormType\",\"U_UserCode\",\"U_Data\" FROM \"@BTUN_FSET\" ORDER BY \"U_FormType\",\"U_UserCode\""
                    : "SELECT [Code],U_FormType,U_UserCode,U_Data FROM [@BTUN_FSET] ORDER BY U_FormType,U_UserCode";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new FormSettingEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        FormType = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        UserCode = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        Data = rs.Fields.Item(3).Value?.ToString() ?? string.Empty
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

        public static FormSettingEntry Save(FormSettingEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            bool isHana = B1App.Instance.IsHana;
            string codeValue = entry.DocEntry > 0 ? entry.DocEntry.ToString(CultureInfo.InvariantCulture) : null;
            if (entry.DocEntry <= 0)
            {
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_FSET");
                entry.DocEntry = nextCode;
                codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = Escape($"{entry.FormType}_{entry.UserCode}".Trim());
                if (string.IsNullOrEmpty(nameValue))
                {
                    nameValue = $"FSET_{codeValue}";
                }
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_FSET\" (\"Code\",\"Name\",\"U_FormType\",\"U_UserCode\",\"U_Data\") VALUES ('{codeValue}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.UserCode)}','{Escape(entry.Data)}')"
                    : $"INSERT INTO [@BTUN_FSET] ([Code],[Name],U_FormType,U_UserCode,U_Data) VALUES ('{codeValue}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.UserCode)}','{Escape(entry.Data)}')";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_FSET\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_UserCode\"='{Escape(entry.UserCode)}',\"U_Data\"='{Escape(entry.Data)}' WHERE \"Code\"='{codeValue}'"
                    : $"UPDATE [@BTUN_FSET] SET U_FormType='{Escape(entry.FormType)}',U_UserCode='{Escape(entry.UserCode)}',U_Data='{Escape(entry.Data)}' WHERE [Code]='{codeValue}'";
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
                ? $"DELETE FROM \"@BTUN_FSET\" WHERE \"Code\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_FSET] WHERE [Code]='{codeValue}'";
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
