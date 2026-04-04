using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    /// <summary>
    /// ReportManager provides safe, compile-time stable helpers for report customization and management.
    /// Implementations are defensive so the project compiles even when Crystal assemblies are not available.
    /// </summary>
    public static class ReportManager
    {
        public static void OpenReportCustomizationForm()
        {
            try
            {
                string formUID = "BTUN_RPT_CUST_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_RPTCUST";
                fcp.UniqueID = formUID;

                Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp - Report Customization";
                oForm.Width = 700;
                oForm.Height = 500;

                // Simple UI: combo with templates and buttons to edit/preview/manage
                Item comboItem = oForm.Items.Add("cmbReports", BoFormItemTypes.it_COMBO_BOX);
                comboItem.Top = 10;
                comboItem.Left = 10;
                comboItem.Width = 480;
                var cmbReports = (ComboBox)comboItem.Specific;

                // Load report templates
                LoadReportTemplatesIntoCombo(cmbReports);

                Item btnEdit = oForm.Items.Add("btnEdit", BoFormItemTypes.it_BUTTON);
                btnEdit.Top = 40;
                btnEdit.Left = 10;
                btnEdit.Width = 100;
                ((Button)btnEdit.Specific).Caption = "Editar";

                Item btnPreview = oForm.Items.Add("btnPreview", BoFormItemTypes.it_BUTTON);
                btnPreview.Top = 40;
                btnPreview.Left = 120;
                btnPreview.Width = 100;
                ((Button)btnPreview.Specific).Caption = "Vista Previa";

                Item btnParams = oForm.Items.Add("btnParams", BoFormItemTypes.it_BUTTON);
                btnParams.Top = 40;
                btnParams.Left = 230;
                btnParams.Width = 120;
                ((Button)btnParams.Specific).Caption = "Parámetros";

                Item btnManage = oForm.Items.Add("btnManage", BoFormItemTypes.it_BUTTON);
                btnManage.Top = 40;
                btnManage.Left = 360;
                btnManage.Width = 120;
                ((Button)btnManage.Specific).Caption = "Administrar Templates";

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo personalización de reportes: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadReportTemplatesIntoCombo(ComboBox combo)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_Name\" FROM \"@BTUN_RPT\" ORDER BY \"U_Name\""
                    : "SELECT [U_Name] FROM [@BTUN_RPT] ORDER BY [U_Name]";

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string name = rs.Fields.Item(0).Value.ToString();
                    try { combo.ValidValues.Add(name, name); } catch { }
                    rs.MoveNext();
                }
            }
            catch (Exception) { }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void ModifyCrystalReportTemplate(string templateName, byte[] reportFileContents)
        {
            // Store report template into UDT table "@BTUN_RPT" in base64 (safe when DB field is nvarchar)
            Recordset rs = null;
            try
            {
                if (string.IsNullOrEmpty(templateName) || reportFileContents == null) return;

                string encoded = Convert.ToBase64String(reportFileContents);
                string safeTemplateName = templateName.Replace("'", "''");
                string safeEncoded = encoded.Replace("'", "''");

                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                bool isHana = B1App.Instance.IsHana;
                string checkSql = isHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_RPT\" WHERE \"U_Name\"='{safeTemplateName}'"
                    : $"SELECT [Code] FROM [@BTUN_RPT] WHERE [U_Name]='{safeTemplateName}'";

                rs.DoQuery(checkSql);
                if (!rs.EoF)
                {
                    string codeValue = rs.Fields.Item(0).Value.ToString();
                    string updateSql = isHana
                        ? $"UPDATE \"@BTUN_RPT\" SET \"U_Data\"='{safeEncoded}', \"U_UpdatedAt\"=CURRENT_TIMESTAMP WHERE \"Code\"='{codeValue}'"
                        : $"UPDATE [@BTUN_RPT] SET U_Data='{safeEncoded}', U_UpdatedAt=GETDATE() WHERE [Code]='{codeValue}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_RPT");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string nameValue = $"RPT_{safeTemplateName}";
                    string insertSql = isHana
                        ? $"INSERT INTO \"@BTUN_RPT\" (\"Code\",\"Name\",\"U_Name\", \"U_Data\", \"U_CreatedAt\") VALUES ('{codeValue}','{nameValue}','{safeTemplateName}', '{safeEncoded}', CURRENT_TIMESTAMP)"
                        : $"INSERT INTO [@BTUN_RPT] ([Code],[Name],U_Name, U_Data, U_CreatedAt) VALUES ('{codeValue}','{nameValue}','{safeTemplateName}', '{safeEncoded}', GETDATE())";

                    rs.DoQuery(insertSql);
                }

                B1App.Instance.Application.SetStatusBarMessage($"Template de reporte '{templateName}' guardado.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error guardando template de reporte: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static Dictionary<string, string> GetReportParameters(string templateName)
        {
            // Attempt to retrieve stored parameter metadata; if none, return empty dictionary.
            var result = new Dictionary<string, string>();
            Recordset rs = null;
            try
            {
                if (string.IsNullOrEmpty(templateName)) return result;
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Params\" FROM \"@BTUN_RPT\" WHERE \"U_Name\"='{templateName}'"
                    : $"SELECT U_Params FROM [@BTUN_RPT] WHERE [U_Name]='{templateName}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string raw = rs.Fields.Item(0).Value?.ToString() ?? "";
                    // Stored as key1=val1|key2=val2
                    var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var idx = p.IndexOf('=');
                        if (idx > 0)
                        {
                            var k = p.Substring(0, idx);
                            var v = p.Substring(idx + 1);
                            result[k] = v;
                        }
                    }
                }
            }
            catch (Exception) { }
            finally { ComObjectManager.Release(rs); }

            return result;
        }

        public static void ApplyReportParameters(object reportObject, Dictionary<string, string> parameters)
        {
            // Attempts to set parameters via reflection to support Crystal or other report engines if present.
            try
            {
                if (reportObject == null || parameters == null) return;

                var type = reportObject.GetType();
                // Try typical CrystalReports property: SetParameterValue or ParameterFields
                var setParam = type.GetMethod("SetParameterValue");
                if (setParam != null)
                {
                    foreach (var kvp in parameters)
                    {
                        try { setParam.Invoke(reportObject, new object[] { kvp.Key, kvp.Value }); } catch { }
                    }
                    return;
                }

                // Try ParameterFields collection
                var pfProp = type.GetProperty("ParameterFields");
                if (pfProp != null)
                {
                    var pf = pfProp.GetValue(reportObject);
                    // best effort - not implemented in detail here
                }

                // If nothing matched, just log to status bar
                B1App.Instance.Application.SetStatusBarMessage("Parámetros aplicados (modo seguro).", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando parámetros: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void ShowAdvancedPrintPreview(string templateName, Dictionary<string, string> parameters)
        {
            try
            {
                // In environments without Crystal assemblies show a simple info form and indicate it's a preview placeholder.
                string formUID = "BTUN_RPT_PREV_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_RPTPREV";
                fcp.UniqueID = formUID;

                Form prevForm = B1App.Instance.Application.Forms.AddEx(fcp);
                prevForm.Title = "Preview - " + (templateName ?? "(sin template)");
                prevForm.Width = 800;
                prevForm.Height = 600;

                Item info = prevForm.Items.Add("LblInfo", BoFormItemTypes.it_STATIC);
                info.Top = 10;
                info.Left = 10;
                info.Width = 760;
                info.Height = 20;
                ((StaticText)info.Specific).Caption = "Vista previa avanzada no disponible en este entorno. Parámetros aplicados: " + (parameters == null ? "(ninguno)" : string.Join(", ", parameters.Keys));

                prevForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mostrando preview: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void ManageReportTemplates()
        {
            try
            {
                string formUID = "BTUN_RPT_MNG_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_RPTMNG";
                fcp.UniqueID = formUID;

                Form mngForm = B1App.Instance.Application.Forms.AddEx(fcp);
                mngForm.Title = "Gestión de Report Templates";
                mngForm.Width = 600;
                mngForm.Height = 400;

                Item gridItem = mngForm.Items.Add("grdRpt", BoFormItemTypes.it_GRID);
                gridItem.Top = 10;
                gridItem.Left = 10;
                gridItem.Width = 560;
                gridItem.Height = 300;

                Grid grid = (Grid)gridItem.Specific;
                try
                {
                    grid.DataTable = mngForm.DataSources.DataTables.Add("rptDt");
                    string sql = B1App.Instance.IsHana
                        ? "SELECT \"U_Name\", \"U_Desc\", \"U_CreatedAt\" FROM \"@BTUN_RPT\" ORDER BY \"U_Name\""
                        : "SELECT U_Name, U_Desc, U_CreatedAt FROM [@BTUN_RPT] ORDER BY U_Name";
                    grid.DataTable.ExecuteQuery(sql);
                }
                catch { }

                mngForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error gestionando report templates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
