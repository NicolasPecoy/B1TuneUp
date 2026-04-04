using System;
using System.Collections.Generic;
using System.Globalization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class QuickCopyService
    {
        public static IList<QuickCopyEntry> GetAll()
        {
            var list = new List<QuickCopyEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"U_SrcFormType\",\"U_SrcObjType\",\"U_TgtObjType\",\"U_BtnLabel\",\"U_PostMacro\",\"U_Active\" FROM \"@BTUN_QCOPY\" ORDER BY \"Code\""
                    : "SELECT [Code],U_SrcFormType,U_SrcObjType,U_TgtObjType,U_BtnLabel,U_PostMacro,U_Active FROM [@BTUN_QCOPY] ORDER BY [Code]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new QuickCopyEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        SourceFormType = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        SourceObjectType = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        TargetObjectType = rs.Fields.Item(3).Value?.ToString() ?? string.Empty,
                        ButtonLabel = rs.Fields.Item(4).Value?.ToString() ?? string.Empty,
                        PostMacro = rs.Fields.Item(5).Value?.ToString() ?? string.Empty,
                        Active = (rs.Fields.Item(6).Value?.ToString() ?? "Y").Equals("Y", StringComparison.OrdinalIgnoreCase)
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

        public static QuickCopyEntry Save(QuickCopyEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            bool isHana = B1App.Instance.IsHana;
            string activeFlag = entry.Active ? "Y" : "N";
            string codeValue = entry.DocEntry > 0 ? entry.DocEntry.ToString(CultureInfo.InvariantCulture) : null;
            if (entry.DocEntry <= 0)
            {
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_QCOPY");
                entry.DocEntry = nextCode;
                codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = Escape($"{entry.SourceFormType}_{entry.SourceObjectType}_{entry.TargetObjectType}".Trim());
                if (string.IsNullOrEmpty(nameValue))
                {
                    nameValue = $"QCOPY_{codeValue}";
                }
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_QCOPY\" (\"Code\",\"Name\",\"U_SrcFormType\",\"U_SrcObjType\",\"U_TgtObjType\",\"U_BtnLabel\",\"U_PostMacro\",\"U_Active\") VALUES ('{codeValue}','{nameValue}','{Escape(entry.SourceFormType)}','{Escape(entry.SourceObjectType)}','{Escape(entry.TargetObjectType)}','{Escape(entry.ButtonLabel)}','{Escape(entry.PostMacro)}','{activeFlag}')"
                    : $"INSERT INTO [@BTUN_QCOPY] ([Code],[Name],U_SrcFormType,U_SrcObjType,U_TgtObjType,U_BtnLabel,U_PostMacro,U_Active) VALUES ('{codeValue}','{nameValue}','{Escape(entry.SourceFormType)}','{Escape(entry.SourceObjectType)}','{Escape(entry.TargetObjectType)}','{Escape(entry.ButtonLabel)}','{Escape(entry.PostMacro)}','{activeFlag}')";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_QCOPY\" SET \"U_SrcFormType\"='{Escape(entry.SourceFormType)}',\"U_SrcObjType\"='{Escape(entry.SourceObjectType)}',\"U_TgtObjType\"='{Escape(entry.TargetObjectType)}',\"U_BtnLabel\"='{Escape(entry.ButtonLabel)}',\"U_PostMacro\"='{Escape(entry.PostMacro)}',\"U_Active\"='{activeFlag}' WHERE \"Code\"='{codeValue}'"
                    : $"UPDATE [@BTUN_QCOPY] SET U_SrcFormType='{Escape(entry.SourceFormType)}',U_SrcObjType='{Escape(entry.SourceObjectType)}',U_TgtObjType='{Escape(entry.TargetObjectType)}',U_BtnLabel='{Escape(entry.ButtonLabel)}',U_PostMacro='{Escape(entry.PostMacro)}',U_Active='{activeFlag}' WHERE [Code]='{codeValue}'";
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
                ? $"DELETE FROM \"@BTUN_QCOPY\" WHERE \"Code\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_QCOPY] WHERE [Code]='{codeValue}'";
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
