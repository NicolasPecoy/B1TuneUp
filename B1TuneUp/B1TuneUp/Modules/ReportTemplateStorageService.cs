using System;
using System.Collections.Generic;
using System.Globalization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ReportTemplateStorageService
    {
        public static IList<ReportTemplateDefinition> GetTemplates(string search = null)
        {
            var list = new List<ReportTemplateDefinition>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                bool isHana = B1App.Instance.IsHana;
                string table = isHana ? "\"@BTUN_RPT\"" : "[@BTUN_RPT]";
                string qDocEntry = isHana ? "\"Code\"" : "[Code]";
                string qName = isHana ? "\"U_Name\"" : "[U_Name]";
                string qDesc = isHana ? "\"U_Desc\"" : "[U_Desc]";
                string qData = isHana ? "\"U_Data\"" : "[U_Data]";
                string qParams = isHana ? "\"U_Params\"" : "[U_Params]";
                string qCreatedAt = isHana ? "\"U_CreatedAt\"" : "[U_CreatedAt]";
                string qUpdatedAt = isHana ? "\"U_UpdatedAt\"" : "[U_UpdatedAt]";

                string sql = $"SELECT {qDocEntry},{qName},{qDesc},{qData},{qParams},{qCreatedAt},{qUpdatedAt} FROM {table}";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string needle = EscapeLike(search);
                    sql += $" WHERE ({qName} LIKE '%{needle}%' OR {qDesc} LIKE '%{needle}%')";
                }
                sql += $" ORDER BY {qName}";

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new ReportTemplateDefinition
                    {
                        DocEntry = ToInt(rs.Fields.Item(0).Value),
                        Name = Convert.ToString(rs.Fields.Item(1).Value),
                        Description = Convert.ToString(rs.Fields.Item(2).Value),
                        DataBase64 = Convert.ToString(rs.Fields.Item(3).Value),
                        Parameters = Convert.ToString(rs.Fields.Item(4).Value),
                        CreatedAt = ToDate(rs.Fields.Item(5).Value),
                        UpdatedAt = ToDate(rs.Fields.Item(6).Value)
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

        public static ReportTemplateDefinition Save(ReportTemplateDefinition report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (string.IsNullOrWhiteSpace(report.Name))
            {
                throw new InvalidOperationException("El nombre del template de reporte es obligatorio.");
            }

            bool isHana = B1App.Instance.IsHana;
            string table = isHana ? "\"@BTUN_RPT\"" : "[@BTUN_RPT]";
            string name = Escape(report.Name);
            string desc = Escape(report.Description);
            string data = Escape(report.DataBase64);
            string parameters = Escape(report.Parameters);
            string now = isHana ? "CURRENT_TIMESTAMP" : "GETDATE()";

            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                if (report.DocEntry.HasValue)
                {
                    string docEntry = report.DocEntry.Value.ToString(CultureInfo.InvariantCulture);
                    string updateSql = isHana
                        ? $"UPDATE {table} SET \"U_Name\"='{name}',\"U_Desc\"='{desc}',\"U_Data\"='{data}',\"U_Params\"='{parameters}',\"U_UpdatedAt\"={now} WHERE \"Code\"='{docEntry}'"
                        : $"UPDATE {table} SET [U_Name]='{name}',[U_Desc]='{desc}',[U_Data]='{data}',[U_Params]='{parameters}',[U_UpdatedAt]={now} WHERE [Code]='{docEntry}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_RPT");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string insertSql = isHana
                        ? $"INSERT INTO {table} (\"Code\",\"Name\",\"U_Name\",\"U_Desc\",\"U_Data\",\"U_Params\",\"U_CreatedAt\") VALUES ('{codeValue}','RPT_{name}','{name}','{desc}','{data}','{parameters}',{now})"
                        : $"INSERT INTO {table} ([Code],[Name],[U_Name],[U_Desc],[U_Data],[U_Params],[U_CreatedAt]) VALUES ('{codeValue}','RPT_{name}','{name}','{desc}','{data}','{parameters}',{now})";
                    rs.DoQuery(insertSql);
                }

                string selectSql = isHana
                    ? $"SELECT \"Code\",\"U_Name\",\"U_Desc\",\"U_Data\",\"U_Params\",\"U_CreatedAt\",\"U_UpdatedAt\" FROM {table} WHERE \"U_Name\"='{name}' ORDER BY \"Code\" DESC"
                    : $"SELECT [Code],[U_Name],[U_Desc],[U_Data],[U_Params],[U_CreatedAt],[U_UpdatedAt] FROM {table} WHERE [U_Name]='{name}' ORDER BY [Code] DESC";
                rs.DoQuery(selectSql);
                if (!rs.EoF)
                {
                    report.DocEntry = ToInt(rs.Fields.Item(0).Value);
                    report.Name = Convert.ToString(rs.Fields.Item(1).Value);
                    report.Description = Convert.ToString(rs.Fields.Item(2).Value);
                    report.DataBase64 = Convert.ToString(rs.Fields.Item(3).Value);
                    report.Parameters = Convert.ToString(rs.Fields.Item(4).Value);
                    report.CreatedAt = ToDate(rs.Fields.Item(5).Value);
                    report.UpdatedAt = ToDate(rs.Fields.Item(6).Value);
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return report;
        }

        public static void Delete(int? docEntry)
        {
            if (!docEntry.HasValue) return;
            bool isHana = B1App.Instance.IsHana;
            string table = isHana ? "\"@BTUN_RPT\"" : "[@BTUN_RPT]";
            string sql = isHana
                ? $"DELETE FROM {table} WHERE \"Code\"='{docEntry.Value}'"
                : $"DELETE FROM {table} WHERE [Code]='{docEntry.Value}'";

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

        private static int? ToInt(object value)
        {
            if (value == null) return null;
            if (int.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static DateTime? ToDate(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");

        private static string EscapeLike(string value) => Escape(value).Replace("%", "[%]").Replace("_", "[_]");
    }
}
