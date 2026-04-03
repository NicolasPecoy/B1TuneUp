using System;
using System.Collections.Generic;
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
                    ? "SELECT \"DocEntry\",\"U_FormType\",\"U_UserCode\",\"U_Data\" FROM \"@BTUN_FSET\" ORDER BY \"U_FormType\",\"U_UserCode\""
                    : "SELECT DocEntry,U_FormType,U_UserCode,U_Data FROM [@BTUN_FSET] ORDER BY U_FormType,U_UserCode";
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
            if (entry.DocEntry <= 0)
            {
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_FSET\" (\"U_FormType\",\"U_UserCode\",\"U_Data\") VALUES ('{Escape(entry.FormType)}','{Escape(entry.UserCode)}','{Escape(entry.Data)}')"
                    : $"INSERT INTO [@BTUN_FSET] (U_FormType,U_UserCode,U_Data) VALUES ('{Escape(entry.FormType)}','{Escape(entry.UserCode)}','{Escape(entry.Data)}')";
                ExecuteNonQuery(insertSql);
                entry.DocEntry = GetLastDocEntry();
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_FSET\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_UserCode\"='{Escape(entry.UserCode)}',\"U_Data\"='{Escape(entry.Data)}' WHERE \"DocEntry\"={entry.DocEntry}"
                    : $"UPDATE [@BTUN_FSET] SET U_FormType='{Escape(entry.FormType)}',U_UserCode='{Escape(entry.UserCode)}',U_Data='{Escape(entry.Data)}' WHERE DocEntry={entry.DocEntry}";
                ExecuteNonQuery(updateSql);
            }
            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string sql = isHana
                ? $"DELETE FROM \"@BTUN_FSET\" WHERE \"DocEntry\"={docEntry}"
                : $"DELETE FROM [@BTUN_FSET] WHERE DocEntry={docEntry}";
            ExecuteNonQuery(sql);
        }

        private static int GetLastDocEntry()
        {
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana ? "SELECT MAX(\"DocEntry\") FROM \"@BTUN_FSET\"" : "SELECT MAX(DocEntry) FROM [@BTUN_FSET]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return Convert.ToInt32(rs.Fields.Item(0).Value);
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return 0;
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
