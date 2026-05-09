using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
// Avoid System.Text.Json dependency for .NET Framework 4.8 - use simple manual serializer
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    /// <summary>
    /// DynamicMapperManager implements a mapping syntax to build queries and export results to CSV, XML, JSON.
    /// Mapping syntax supports sections separated by '|' or new lines: TABLES:, FIELDS:, JOINS:, WHERE:, ORDERBY:
    /// Example:
    /// TABLES: OINV inv | RDR1 r1
    /// FIELDS: inv.DocEntry, inv.CardCode, inv.DocDate, r1.ItemCode
    /// JOINS: INNER JOIN RDR1 r1 ON r1.DocEntry = inv.DocEntry
    /// WHERE: inv.DocDate >= '20230101'
    /// ORDERBY: inv.DocDate DESC
    /// </summary>
    public static class DynamicMapperManager
    {
        // Persistence helpers for mappings stored in UDT @BTUN_DMAPPER
        public static bool SaveMapping(string name, string definition, string description)
        {
            if (string.IsNullOrEmpty(name)) return false;
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                bool isHana = B1App.Instance.IsHana;
                string escapedName = name.Replace("'", "''");
                string checkSql = isHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_DMAPPER\" WHERE \"U_Name\" = '{escapedName}'"
                    : $"SELECT [Code] FROM [@BTUN_DMAPPER] WHERE [U_Name] = '{escapedName}'";

                rs.DoQuery(checkSql);
                string encoded = definition == null ? "" : definition.Replace("'", "''");
                string desc = description == null ? "" : description.Replace("'", "''");

                if (!rs.EoF)
                {
                    string codeValue = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0);
                    string updateSql = isHana
                        ? $"UPDATE \"@BTUN_DMAPPER\" SET \"U_Def\" = '{encoded}', \"U_Desc\" = '{desc}', \"U_UpdatedAt\" = CURRENT_TIMESTAMP WHERE \"Code\" = '{codeValue}'"
                        : $"UPDATE [@BTUN_DMAPPER] SET U_Def = '{encoded}', U_Desc = '{desc}', U_UpdatedAt = GETDATE() WHERE [Code] = '{codeValue}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_DMAPPER");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string nameValue = $"MAP_{escapedName}" ;
                    string insertSql = isHana
                        ? $"INSERT INTO \"@BTUN_DMAPPER\" (\"Code\",\"Name\",\"U_Name\", \"U_Desc\", \"U_Def\", \"U_CreatedAt\") VALUES ('{codeValue}','{nameValue}','{escapedName}', '{desc}', '{encoded}', CURRENT_TIMESTAMP)"
                        : $"INSERT INTO [@BTUN_DMAPPER] ([Code],[Name],U_Name, U_Desc, U_Def, U_CreatedAt) VALUES ('{codeValue}','{nameValue}','{escapedName}', '{desc}', '{encoded}', GETDATE())";
                    rs.DoQuery(insertSql);
                }

                B1App.Instance.Application.SetStatusBarMessage($"Mapping '{name}' guardado.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error guardando mapping: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static bool DeleteMapping(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string deleteSql = B1App.Instance.IsHana
                    ? $"DELETE FROM \"@BTUN_DMAPPER\" WHERE \"U_Name\" = '{name.Replace("'","''")}'"
                    : $"DELETE FROM [@BTUN_DMAPPER] WHERE [U_Name] = '{name.Replace("'","''")}'";
                rs.DoQuery(deleteSql);
                B1App.Instance.Application.SetStatusBarMessage($"Mapping '{name}' eliminado.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error eliminando mapping: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            finally { ComObjectManager.Release(rs); }
        }

        public static System.Data.DataTable GetAllMappings()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("U_Name");
            dt.Columns.Add("U_Desc");
            dt.Columns.Add("U_CreatedAt");
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_Name\", \"U_Desc\", \"U_CreatedAt\" FROM \"@BTUN_DMAPPER\" ORDER BY \"U_Name\""
                    : "SELECT U_Name, U_Desc, U_CreatedAt FROM [@BTUN_DMAPPER] ORDER BY U_Name";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var row = dt.NewRow();
                    row[0] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0);
                    row[1] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1);
                    row[2] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2);
                    dt.Rows.Add(row);
                    rs.MoveNext();
                }
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return dt;
        }

        public static string GetMappingDefinition(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Def\" FROM \"@BTUN_DMAPPER\" WHERE \"U_Name\" = '{name.Replace("'","''")}'"
                    : $"SELECT U_Def FROM [@BTUN_DMAPPER] WHERE [U_Name] = '{name.Replace("'","''")}'";
                rs.DoQuery(sql);
                if (!rs.EoF) return B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0);
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return "";
        }

        public static void OpenMappingManagerForm()
        {
            try
            {
                string formUID = "BTUN_DMAPPER_MNG_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_DMAPPER";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "Dynamic Mapper Manager";
                oForm.Width = 800;
                oForm.Height = 600;

                // Grid to list mappings
                Item gridItem = oForm.Items.Add("grdMap", BoFormItemTypes.it_GRID);
                gridItem.Top = 10;
                gridItem.Left = 10;
                gridItem.Width = 760;
                gridItem.Height = 460;
                SAPbouiCOM.Grid grid = SapUiSafe.TryGetSpecific<SAPbouiCOM.Grid>(gridItem);

                try
                {
                    grid.DataTable = oForm.DataSources.DataTables.Add("mapDt");
                    grid.DataTable.ExecuteQuery(B1App.Instance.IsHana ? "SELECT \"U_Name\", \"U_Desc\", \"U_CreatedAt\" FROM \"@BTUN_DMAPPER\" ORDER BY \"U_Name\"" : "SELECT U_Name, U_Desc, U_CreatedAt FROM [@BTUN_DMAPPER] ORDER BY U_Name");
                }
                catch { }

                // Buttons: New, Edit, Delete, Export
                Item btnNew = oForm.Items.Add("btnNew", BoFormItemTypes.it_BUTTON); btnNew.Left = 10; btnNew.Top = 480; btnNew.Width = 80; SapUiSafe.TrySetCaption(btnNew, "Nuevo");
                Item btnEdit = oForm.Items.Add("btnEdit", BoFormItemTypes.it_BUTTON); btnEdit.Left = 100; btnEdit.Top = 480; btnEdit.Width = 80; SapUiSafe.TrySetCaption(btnEdit, "Editar");
                Item btnDel = oForm.Items.Add("btnDel", BoFormItemTypes.it_BUTTON); btnDel.Left = 190; btnDel.Top = 480; btnDel.Width = 80; SapUiSafe.TrySetCaption(btnDel, "Eliminar");
                Item btnExport = oForm.Items.Add("btnExp", BoFormItemTypes.it_BUTTON); btnExport.Left = 280; btnExport.Top = 480; btnExport.Width = 120; SapUiSafe.TrySetCaption(btnExport, "Exportar CSV");

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo gestor de mappings: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }
        public static string BuildQuery(string mappingDefinition, Dictionary<string, string> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(mappingDefinition)) return "";
            // parameter substitution
            if (parameters != null)
            {
                foreach (var kv in parameters)
                {
                    mappingDefinition = mappingDefinition.Replace("{" + kv.Key + "}", kv.Value);
                }
            }

            // Split into sections
            var sections = mappingDefinition.Split(new[] { '\n', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));

            string tables = "";
            string fields = "*";
            string joins = "";
            string where = "";
            string orderby = "";

            foreach (var sec in sections)
            {
                var idx = sec.IndexOf(':');
                if (idx == -1) continue;
                var key = sec.Substring(0, idx).Trim().ToUpper();
                var val = sec.Substring(idx + 1).Trim();

                switch (key)
                {
                    case "TABLES":
                    case "TABLE":
                        tables = val;
                        break;
                    case "FIELDS":
                    case "FIELD":
                        fields = val;
                        break;
                    case "JOINS":
                    case "JOIN":
                        joins = val;
                        break;
                    case "WHERE":
                        where = val;
                        break;
                    case "ORDERBY":
                    case "ORDER":
                        orderby = val;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(tables))
            {
                // try to infer table from fields (e.g., prefix tbl.field)
                // If cannot, return empty
                return "";
            }

            // Build FROM clause
            // tables can be comma-separated or space-separated aliases
            string fromClause = tables;

            // Ensure proper quoting for HANA vs SQL Server? Keep as-is; the user is responsible for valid field names.
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(fields);
            sb.Append(" FROM ");
            sb.Append(fromClause);
            if (!string.IsNullOrWhiteSpace(joins))
            {
                sb.Append(" ");
                sb.Append(joins);
            }
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" WHERE ");
                sb.Append(where);
            }
            if (!string.IsNullOrWhiteSpace(orderby))
            {
                sb.Append(" ORDER BY ");
                sb.Append(orderby);
            }

            return sb.ToString();
        }

        public static System.Data.DataTable ExecuteQueryToDataTable(string sql)
        {
            var dt = new System.Data.DataTable();
            if (string.IsNullOrWhiteSpace(sql)) return dt;

            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                // create columns
                for (int i = 0; i < rs.Fields.Count; i++)
                {
                    string name = B1TuneUp.Utils.SapUiSafe.SafeFieldName(rs, i);
                    if (string.IsNullOrEmpty(name)) name = "F" + i;
                    dt.Columns.Add(name);
                }

                while (!rs.EoF)
                {
                    var row = dt.NewRow();
                    for (int i = 0; i < rs.Fields.Count; i++)
                    {
                        try { row[i] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, i); } catch { row[i] = null; }
                    }
                    dt.Rows.Add(row);
                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error ejecutando query dinámico: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return dt;
        }

        public static bool ExportDataTableToCsv(System.Data.DataTable dt, string filePath, bool includeHeader = true)
        {
            try
            {
                using (var sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    if (includeHeader)
                    {
                        var headers = dt.Columns.Cast<System.Data.DataColumn>().Select(c => EscapeCsv(c.ColumnName)).ToArray();
                        sw.WriteLine(string.Join(",", headers));
                    }

                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var vals = new List<string>();
                        foreach (System.Data.DataColumn col in dt.Columns)
                        {
                            var v = row[col]?.ToString() ?? "";
                            vals.Add(EscapeCsv(v));
                        }
                        sw.WriteLine(string.Join(",", vals));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error exportando CSV dinámico: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        public static string DataTableToJson(System.Data.DataTable dt)
        {
            if (dt == null) return "[]";
            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var row = dt.Rows[i];
                sb.Append("{");
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    var col = dt.Columns[c];
                    var val = row[col] == null ? "" : row[col].ToString();
                    sb.Append("\""); sb.Append(EscapeJson(col.ColumnName)); sb.Append("\":");
                    if (double.TryParse(val, out _)) sb.Append(val);
                    else if (val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("false", StringComparison.OrdinalIgnoreCase)) sb.Append(val.ToLower());
                    else { sb.Append("\""); sb.Append(EscapeJson(val)); sb.Append("\""); }
                    if (c < dt.Columns.Count - 1) sb.Append(",");
                }
                sb.Append("}");
                if (i < dt.Rows.Count - 1) sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static string DataTableToXml(System.Data.DataTable dt, string rootName = "Rows", string rowName = "Row")
        {
            var ds = new DataSet(rootName);
            ds.Tables.Add(dt.Copy());
            using (var sw = new StringWriter())
            {
                ds.WriteXml(sw, XmlWriteMode.WriteSchema);
                return sw.ToString();
            }
        }

        private static string EscapeCsv(string v)
        {
            if (v == null) return "";
            if (v.Contains("\"") || v.Contains(",") || v.Contains("\n") || v.Contains("\r"))
            {
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }
            return v;
        }

        // High-level helpers
        public static bool ExportMappingToCsv(string mappingDefinition, string filePath, Dictionary<string, string> parameters = null)
        {
            try
            {
                string sql = BuildQuery(mappingDefinition, parameters);
                if (string.IsNullOrWhiteSpace(sql)) return false;
                var dt = ExecuteQueryToDataTable(sql);
                return ExportDataTableToCsv(dt, filePath, true);
            }
            catch { return false; }
        }

        public static bool ExportMappingToJson(string mappingDefinition, string filePath, Dictionary<string, string> parameters = null)
        {
            try
            {
                string sql = BuildQuery(mappingDefinition, parameters);
                if (string.IsNullOrWhiteSpace(sql)) return false;
                var dt = ExecuteQueryToDataTable(sql);
                var json = DataTableToJson(dt);
                File.WriteAllText(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error exportando JSON dinámico: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        public static bool ExportMappingToXml(string mappingDefinition, string filePath, Dictionary<string, string> parameters = null)
        {
            try
            {
                string sql = BuildQuery(mappingDefinition, parameters);
                if (string.IsNullOrWhiteSpace(sql)) return false;
                var dt = ExecuteQueryToDataTable(sql);
                var xml = DataTableToXml(dt);
                File.WriteAllText(filePath, xml, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error exportando XML dinámico: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }
    }
}
