using System;
using System.Collections.Generic;
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
                    ? "SELECT \"DocEntry\",\"U_SrcFormType\",\"U_SrcObjType\",\"U_TgtObjType\",\"U_BtnLabel\",\"U_PostMacro\",\"U_Active\" FROM \"@BTUN_QCOPY\" ORDER BY \"DocEntry\""
                    : "SELECT DocEntry,U_SrcFormType,U_SrcObjType,U_TgtObjType,U_BtnLabel,U_PostMacro,U_Active FROM [@BTUN_QCOPY] ORDER BY DocEntry";
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
            if (entry.DocEntry <= 0)
            {
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_QCOPY\" (\"U_SrcFormType\",\"U_SrcObjType\",\"U_TgtObjType\",\"U_BtnLabel\",\"U_PostMacro\",\"U_Active\") VALUES ('{Escape(entry.SourceFormType)}','{Escape(entry.SourceObjectType)}','{Escape(entry.TargetObjectType)}','{Escape(entry.ButtonLabel)}','{Escape(entry.PostMacro)}','{activeFlag}')"
                    : $"INSERT INTO [@BTUN_QCOPY] (U_SrcFormType,U_SrcObjType,U_TgtObjType,U_BtnLabel,U_PostMacro,U_Active) VALUES ('{Escape(entry.SourceFormType)}','{Escape(entry.SourceObjectType)}','{Escape(entry.TargetObjectType)}','{Escape(entry.ButtonLabel)}','{Escape(entry.PostMacro)}','{activeFlag}')";
                ExecuteNonQuery(insertSql);
                entry.DocEntry = GetLastDocEntry();
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_QCOPY\" SET \"U_SrcFormType\"='{Escape(entry.SourceFormType)}',\"U_SrcObjType\"='{Escape(entry.SourceObjectType)}',\"U_TgtObjType\"='{Escape(entry.TargetObjectType)}',\"U_BtnLabel\"='{Escape(entry.ButtonLabel)}',\"U_PostMacro\"='{Escape(entry.PostMacro)}',\"U_Active\"='{activeFlag}' WHERE \"DocEntry\"={entry.DocEntry}"
                    : $"UPDATE [@BTUN_QCOPY] SET U_SrcFormType='{Escape(entry.SourceFormType)}',U_SrcObjType='{Escape(entry.SourceObjectType)}',U_TgtObjType='{Escape(entry.TargetObjectType)}',U_BtnLabel='{Escape(entry.ButtonLabel)}',U_PostMacro='{Escape(entry.PostMacro)}',U_Active='{activeFlag}' WHERE DocEntry={entry.DocEntry}";
                ExecuteNonQuery(updateSql);
            }

            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string sql = isHana
                ? $"DELETE FROM \"@BTUN_QCOPY\" WHERE \"DocEntry\"={docEntry}"
                : $"DELETE FROM [@BTUN_QCOPY] WHERE DocEntry={docEntry}";
            ExecuteNonQuery(sql);
        }

        private static int GetLastDocEntry()
        {
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana ? "SELECT MAX(\"DocEntry\") FROM \"@BTUN_QCOPY\"" : "SELECT MAX(DocEntry) FROM [@BTUN_QCOPY]";
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
