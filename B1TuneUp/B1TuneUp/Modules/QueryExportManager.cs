using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Modules
{
    public static class QueryExportManager
    {
        public static void OpenQueryExportWindow(SAPbouiCOM.Form activeForm = null)
        {
            try
            {
                if (activeForm == null)
                {
                    try { activeForm = B1App.Instance.Application.Forms.ActiveForm; } catch { activeForm = null; }
                }

                var win = new ExportSqlForm(activeForm);
                win.Show();
            }
            catch (Exception ex)
            {
                try { B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Query Exporter: {ex.Message}", BoMessageTime.bmt_Short, true); } catch { }
            }
        }

        private class ExportSqlForm : System.Windows.Forms.Form
        {
            private System.Windows.Forms.TextBox _txtSql;
            private System.Windows.Forms.Button _btnCsv;
            private System.Windows.Forms.Button _btnJson;
            private System.Windows.Forms.Button _btnXml;
            private System.Windows.Forms.Button _btnLoad;
            private System.Windows.Forms.Button _btnClose;
            private SAPbouiCOM.Form _b1Form;

            public ExportSqlForm(SAPbouiCOM.Form b1Form)
            {
                _b1Form = b1Form;
                Text = "Query Exporter";
                Width = 900;
                Height = 600;

                _txtSql = new System.Windows.Forms.TextBox();
                _txtSql.Multiline = true;
                _txtSql.ScrollBars = ScrollBars.Both;
                _txtSql.Left = 10; _txtSql.Top = 10; _txtSql.Width = 860; _txtSql.Height = 480;

                _btnLoad = new System.Windows.Forms.Button(); _btnLoad.Text = "Load from Query Manager"; _btnLoad.Left = 10; _btnLoad.Top = 500; _btnLoad.Width = 180; _btnLoad.Click += BtnLoad_Click;
                _btnCsv = new System.Windows.Forms.Button(); _btnCsv.Text = "Export CSV"; _btnCsv.Left = 200; _btnCsv.Top = 500; _btnCsv.Width = 100; _btnCsv.Click += BtnCsv_Click;
                _btnJson = new System.Windows.Forms.Button(); _btnJson.Text = "Export JSON"; _btnJson.Left = 310; _btnJson.Top = 500; _btnJson.Width = 100; _btnJson.Click += BtnJson_Click;
                _btnXml = new System.Windows.Forms.Button(); _btnXml.Text = "Export XML"; _btnXml.Left = 420; _btnXml.Top = 500; _btnXml.Width = 100; _btnXml.Click += BtnXml_Click;
                _btnClose = new System.Windows.Forms.Button(); _btnClose.Text = "Close"; _btnClose.Left = 760; _btnClose.Top = 500; _btnClose.Width = 100; _btnClose.Click += (s, e) => Close();

                Controls.Add(_txtSql);
                Controls.Add(_btnLoad);
                Controls.Add(_btnCsv);
                Controls.Add(_btnJson);
                Controls.Add(_btnXml);
                Controls.Add(_btnClose);
            }

            private void BtnLoad_Click(object sender, EventArgs e)
            {
                try
                {
                    if (_b1Form == null) { MessageBox.Show("No active SAP Business One form."); return; }

                    // Try common item ids used by Query Manager or UDFs
                    string[] tryIds = new[] { "txtQuery", "txtSQL", "edQuery", "edtQuery", "txt1" };
                    string loaded = null;

                    foreach (var id in tryIds)
                    {
                        try
                        {
                            if (_b1Form.Items.Exists(id))
                            {
                                var it = _b1Form.Items.Item(id);
                                if (it.Specific is SAPbouiCOM.EditText et) { loaded = et.Value; break; }
                                if (it.Specific is SAPbouiCOM.ComboBox cb) { loaded = cb.Selected?.Description ?? cb.Selected?.Value ?? ""; break; }
                                if (it.Specific is SAPbouiCOM.StaticText st) { loaded = st.Caption; break; }
                            }
                        }
                        catch { }
                    }

                    // Try to extract from grid selection (some Query Manager implementations list queries in a grid)
                    if (loaded == null)
                    {
                        try
                        {
                            for (int i = 0; i < _b1Form.Items.Count; i++)
                            {
                                var it = _b1Form.Items.Item(i + 1);
                                if (it.Type == BoFormItemTypes.it_GRID)
                                {
                                    var grid = (SAPbouiCOM.Grid)it.Specific;
                                    if (grid.Rows.SelectedRows.Count > 0)
                                    {
                                        try
                                        {
                                            object selObj = grid.Rows.SelectedRows.Item(0);
                                            int rowIndex = -1;
                                            try { rowIndex = (int)selObj; } catch { try { rowIndex = Convert.ToInt32(selObj); } catch { }
                                                if (rowIndex == -1)
                                                {
                                                    var prop = selObj.GetType().GetProperty("RowIndex");
                                                    if (prop != null) rowIndex = Convert.ToInt32(prop.GetValue(selObj));
                                                }
                                            }

                                            if (rowIndex != -1)
                                            {
                                                int r = grid.GetDataTableRowIndex(rowIndex);
                                                // try common column names
                                                string[] colTry = new[] { "Query", "U_Query", "SQL", "Qry" };
                                                foreach (var c in colTry)
                                                {
                                                    try { loaded = grid.DataTable.GetValue(c, r)?.ToString(); if (!string.IsNullOrEmpty(loaded)) break; } catch { }
                                                }
                                                if (!string.IsNullOrEmpty(loaded)) break;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(loaded)) _txtSql.Text = loaded;
                    else MessageBox.Show("No se pudo detectar la consulta en el Query Manager activo. Pegue la consulta manualmente en el cuadro y exporte.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error cargando desde Query Manager: " + ex.Message);
                }
            }

            private void BtnCsv_Click(object sender, EventArgs e)
            {
                try
                {
                    var sql = _txtSql.Text;
                    if (string.IsNullOrWhiteSpace(sql)) { MessageBox.Show("Escriba o cargue una consulta SQL."); return; }

                    var dt = DynamicMapperManager.ExecuteQueryToDataTable(sql);
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                        sfd.FileName = "export.csv";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            DynamicMapperManager.ExportDataTableToCsv(dt, sfd.FileName, true);
                            MessageBox.Show("Exportado CSV: " + sfd.FileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exportando CSV: " + ex.Message);
                }
            }

            private void BtnJson_Click(object sender, EventArgs e)
            {
                try
                {
                    var sql = _txtSql.Text;
                    if (string.IsNullOrWhiteSpace(sql)) { MessageBox.Show("Escriba o cargue una consulta SQL."); return; }

                    var dt = DynamicMapperManager.ExecuteQueryToDataTable(sql);
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                        sfd.FileName = "export.json";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            var json = DynamicMapperManager.DataTableToJson(dt);
                            File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
                            MessageBox.Show("Exportado JSON: " + sfd.FileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exportando JSON: " + ex.Message);
                }
            }

            private void BtnXml_Click(object sender, EventArgs e)
            {
                try
                {
                    var sql = _txtSql.Text;
                    if (string.IsNullOrWhiteSpace(sql)) { MessageBox.Show("Escriba o cargue una consulta SQL."); return; }

                    var dt = DynamicMapperManager.ExecuteQueryToDataTable(sql);
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                        sfd.FileName = "export.xml";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            var xml = DynamicMapperManager.DataTableToXml(dt);
                            File.WriteAllText(sfd.FileName, xml, Encoding.UTF8);
                            MessageBox.Show("Exportado XML: " + sfd.FileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exportando XML: " + ex.Message);
                }
            }
        }
    }
}
