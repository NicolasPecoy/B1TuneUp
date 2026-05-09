using System;
using System.Windows.Forms;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class DefaultValueManager
    {
        public static void ApplyOnLoad(SAPbouiCOM.Form oForm)
        {
            ApplyDefaults(oForm, null, true);
        }

        public static void ApplyOnChange(SAPbouiCOM.Form oForm, string itemId)
        {
            ApplyDefaults(oForm, itemId, false);
        }

        private static void ApplyDefaults(SAPbouiCOM.Form oForm, string itemId, bool onLoad)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            Recordset rsVal = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string eventFilter = onLoad ? "Load" : "Change";
                string safeFormType = EscapeSqlValue(oForm.TypeEx);
                string safeEvent = EscapeSqlValue(eventFilter);
                string safeItemId = EscapeSqlValue(itemId);
                string sql;
                if (B1App.Instance.IsHana)
                {
                    sql = $"SELECT * FROM \"@BTUN_DEFAULTS\" WHERE \"U_FormType\" = '{safeFormType}' AND \"U_OnEvent\" = '{safeEvent}'";
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        sql += $" AND (\"U_ItemID\" = '{safeItemId}' OR IFNULL(\"U_ItemID\", '') = '')";
                    }
                }
                else
                {
                    sql = $"SELECT * FROM [@BTUN_DEFAULTS] WHERE [U_FormType] = '{safeFormType}' AND [U_OnEvent] = '{safeEvent}'";
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        sql += $" AND ([U_ItemID] = '{safeItemId}' OR ISNULL([U_ItemID], '') = '')";
                    }
                }

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string targetItem = SafeField(rs, "U_ItemID");
                    string colId = SafeField(rs, "U_ColID");
                    string query = SafeField(rs, "U_Query");

                    // Procesar la consulta para reemplazar variables dinámicas
                    string processedQuery = ProcessQueryVariables(query, oForm, true);

                    // Ejecutar consulta y obtener valor
                    string value = ExecuteScalar(rsVal, processedQuery);

                    try
                    {
                        if (!string.IsNullOrEmpty(colId) && oForm.Items.Item(targetItem).Type == BoFormItemTypes.it_MATRIX)
                        {
                            Matrix m = (Matrix)oForm.Items.Item(targetItem).Specific;
                            int row = m.GetNextSelectedRow(0, BoOrderType.ot_SelectionOrder);
                            if (row < 1) row = 1;
                            ((EditText)m.Columns.Item(colId).Cells.Item(row).Specific).Value = value;
                        }
                        else
                        {
                            Item item = oForm.Items.Item(targetItem);
                            if (item.Specific is EditText et)
                            {
                                et.Value = value;
                            }
                            else if (item.Specific is SAPbouiCOM.ComboBox cb)
                            {
                                try { cb.Select(value, BoSearchKey.psk_ByValue); } catch { }
                            }
                            else if (item.Specific is SAPbouiCOM.Button btn)
                            {
                                btn.Caption = value;
                            }
                            else if (item.Specific is SAPbouiCOM.CheckBox chk)
                            {
                                chk.Checked = value.ToUpper() == "Y" || value.ToUpper() == "TRUE" || value == "1";
                            }
                            else if (item.Specific is StaticText lbl)
                            {
                                lbl.Caption = value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionLogger.LogHandled(ex, $"DefaultValueManager.ApplyItem:{oForm?.TypeEx}:{targetItem}");
                    }

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando valores por defecto: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
                ComObjectManager.Release(rsVal);
            }
        }

        private static string ExecuteScalar(Recordset rs, string query)
        {
            try
            {
                rs.DoQuery(query);
                if (rs.RecordCount > 0)
                {
                    return rs.Fields.Item(0).Value != null ? rs.Fields.Item(0).Value.ToString() : string.Empty;
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en consulta de DefaultValue: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            return string.Empty;
        }

        private static string ProcessQueryVariables(string query, SAPbouiCOM.Form oForm, bool escapeForSql)
        {
            // Reemplaza variables dinámicas como $[FieldName] con valores del formulario actual
            string processedQuery = query;

            // Busca patrones $[FieldName] o $[Form.Field] en la consulta
            int searchStart = 0;
            while (searchStart < processedQuery.Length)
            {
                int startIndex = processedQuery.IndexOf("$[", searchStart);
                if (startIndex == -1) break;

                int endIndex = processedQuery.IndexOf("]", startIndex);
                if (endIndex != -1 && endIndex > startIndex)
                {
                    string fullTag = processedQuery.Substring(startIndex, endIndex - startIndex + 1);
                    string fieldRef = processedQuery.Substring(startIndex + 2, endIndex - startIndex - 2); // Quitar $[

                    string fieldValue = GetFieldValue(oForm, fieldRef);
                    if (escapeForSql)
                    {
                        fieldValue = EscapeSqlValue(fieldValue);
                    }
                    processedQuery = processedQuery.Replace(fullTag, fieldValue);

                    // Avanzar posición más allá del lugar donde hicimos el reemplazo
                    searchStart = startIndex + fieldValue.Length;
                }
                else
                {
                    break; // No closing bracket found
                }

                if (searchStart >= processedQuery.Length) break; // Prevenir bucle infinito
            }

            return processedQuery;
        }

        private static string GetFieldValue(SAPbouiCOM.Form oForm, string fieldRef)
        {
            try
            {
                // Si el campo está en formato ItemID.ColumnID (para matrices)
                if (fieldRef.Contains("."))
                {
                    string[] parts = fieldRef.Split('.');
                    string itemId = parts[0];
                    string colId = parts[1];

                    Item item = oForm.Items.Item(itemId);
                    if (item.Type == BoFormItemTypes.it_MATRIX)
                    {
                        Matrix matrix = (Matrix)item.Specific;
                        int row = matrix.GetNextSelectedRow(0, BoOrderType.ot_SelectionOrder);
                        if (row < 1) row = 1;
                        return ((EditText)matrix.Columns.Item(colId).Cells.Item(row).Specific).Value ?? "";
                    }
                }
                else
                {
                    // Campo normal
                    Item item = oForm.Items.Item(fieldRef);
                    if (item.Specific is EditText et)
                    {
                        return et.Value ?? "";
                    }
                    else if (item.Specific is SAPbouiCOM.ComboBox cb)
                    {
                        return cb.Selected?.Value ?? "";
                    }
                    else if (item.Specific is SAPbouiCOM.CheckBox chk)
                    {
                        return chk.Checked ? "Y" : "N";
                    }
                }
            }
            catch
            {
                // Si hay error al obtener el valor, retornar cadena vacía
            }

            return "";
        }

        private static string SafeField(Recordset rs, string field)
        {
            try { return rs.Fields.Item(field).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string EscapeSqlValue(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }
    }
}
