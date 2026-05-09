using System;
using System.Collections.Generic;
using System.Globalization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class TemplateStorageService
    {
        public static IList<FormTemplateDefinition> GetTemplates(string formTypeFilter = null, string search = null)
        {
            var list = new List<FormTemplateDefinition>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                bool isHana = B1App.Instance.IsHana;
                string table = isHana ? "\"@BTUN_TMPL\"" : "[@BTUN_TMPL]";
                string qDocEntry = isHana ? "\"Code\"" : "[Code]";
                string qName = isHana ? "\"U_Name\"" : "[U_Name]";
                string qDesc = isHana ? "\"U_Desc\"" : "[U_Desc]";
                string qFormType = isHana ? "\"U_FormType\"" : "[U_FormType]";
                string qData = isHana ? "\"U_Data\"" : "[U_Data]";
                string qCreatedBy = isHana ? "\"U_CreatedBy\"" : "[U_CreatedBy]";
                string qCreatedAt = isHana ? "\"U_CreatedAt\"" : "[U_CreatedAt]";
                string qUpdatedAt = isHana ? "\"U_UpdatedAt\"" : "[U_UpdatedAt]";

                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(formTypeFilter))
                {
                    where.Add($"{qFormType} LIKE '%{EscapeLike(formTypeFilter)}%'");
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string needle = EscapeLike(search);
                    where.Add($"({qName} LIKE '%{needle}%' OR {qDesc} LIKE '%{needle}%')");
                }

                string sql = $"SELECT {qDocEntry},{qName},{qDesc},{qFormType},{qData},{qCreatedBy},{qCreatedAt},{qUpdatedAt} FROM {table}";
                if (where.Count > 0)
                {
                    sql += " WHERE " + string.Join(" AND ", where);
                }
                sql += $" ORDER BY {qName}";

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new FormTemplateDefinition
                    {
                        DocEntry = ConvertToInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 0)),
                        Name = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1),
                        Description = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        FormType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3),
                        SerializedData = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4),
                        CreatedBy = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 5),
                        CreatedAt = ConvertToDate(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 6)),
                        UpdatedAt = ConvertToDate(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 7))
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

        public static FormTemplateDefinition Save(FormTemplateDefinition template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                throw new InvalidOperationException("El nombre del template es obligatorio.");
            }

            bool isHana = B1App.Instance.IsHana;
            string table = isHana ? "\"@BTUN_TMPL\"" : "[@BTUN_TMPL]";

            string name = Escape(template.Name);
            string desc = Escape(template.Description);
            string formType = Escape(template.FormType);
            string data = Escape(template.SerializedData);
            string createdBy = Escape(string.IsNullOrWhiteSpace(template.CreatedBy) ? B1App.Instance.Company.UserName : template.CreatedBy);
            string now = isHana ? "CURRENT_TIMESTAMP" : "GETDATE()";

            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                if (template.DocEntry.HasValue)
                {
                    string docEntry = template.DocEntry.Value.ToString(CultureInfo.InvariantCulture);
                    string updateSql = isHana
                        ? $"UPDATE {table} SET \"U_Name\"='{name}',\"U_Desc\"='{desc}',\"U_FormType\"='{formType}',\"U_Data\"='{data}',\"U_UpdatedAt\"={now} WHERE \"Code\"='{docEntry}'"
                        : $"UPDATE {table} SET [U_Name]='{name}',[U_Desc]='{desc}',[U_FormType]='{formType}',[U_Data]='{data}',[U_UpdatedAt]={now} WHERE [Code]='{docEntry}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_TMPL");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string insertSql = isHana
                        ? $"INSERT INTO {table} (\"Code\",\"Name\",\"U_Name\",\"U_Desc\",\"U_FormType\",\"U_Data\",\"U_CreatedBy\",\"U_CreatedAt\") VALUES ('{codeValue}','TMPL_{name}','{name}','{desc}','{formType}','{data}','{createdBy}',{now})"
                        : $"INSERT INTO {table} ([Code],[Name],[U_Name],[U_Desc],[U_FormType],[U_Data],[U_CreatedBy],[U_CreatedAt]) VALUES ('{codeValue}','TMPL_{name}','{name}','{desc}','{formType}','{data}','{createdBy}',{now})";
                    rs.DoQuery(insertSql);
                }

                // Reload to capture DocEntry/updated timestamps
                string selectSql = isHana
                    ? $"SELECT \"Code\",\"U_Name\",\"U_Desc\",\"U_FormType\",\"U_Data\",\"U_CreatedBy\",\"U_CreatedAt\",\"U_UpdatedAt\" FROM {table} WHERE \"U_Name\"='{name}' ORDER BY \"Code\" DESC"
                    : $"SELECT [Code],[U_Name],[U_Desc],[U_FormType],[U_Data],[U_CreatedBy],[U_CreatedAt],[U_UpdatedAt] FROM {table} WHERE [U_Name]='{name}' ORDER BY [Code] DESC";
                rs.DoQuery(selectSql);
                if (!rs.EoF)
                {
                    template.DocEntry = ConvertToInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 0));
                    template.Name = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1);
                    template.Description = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2);
                    template.FormType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3);
                    template.SerializedData = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4);
                    template.CreatedBy = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 5);
                    template.CreatedAt = ConvertToDate(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 6));
                    template.UpdatedAt = ConvertToDate(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 7));
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return template;
        }

        public static void Delete(int? docEntry)
        {
            if (!docEntry.HasValue) return;
            bool isHana = B1App.Instance.IsHana;
            string table = isHana ? "\"@BTUN_TMPL\"" : "[@BTUN_TMPL]";
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

        private static int? ConvertToInt(object value)
        {
            if (value == null) return null;
            if (int.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static DateTime? ConvertToDate(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");

        private static string EscapeLike(string value) => Escape(value).Replace("%", "[%]").Replace("_", "[_]");
    }
}
