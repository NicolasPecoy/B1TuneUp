using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using SAPbobsCOM;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.Forms
{
    public class PivotGridForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _parentForm;
        private string _gridId;
        private DataGridView _gridView;

        public PivotGridForm(SAPbouiCOM.Form parentForm, string gridId, string sqlQuery)
        {
            _parentForm = parentForm;
            _gridId = gridId;
            Text = "Pivot / Grid Viewer";
            Width = 900;
            Height = 600;

            _gridView = new DataGridView();
            _gridView.Left = 10;
            _gridView.Top = 10;
            _gridView.Width = 860;
            _gridView.Height = 520;
            _gridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Controls.Add(_gridView);

            var btnRefresh = new System.Windows.Forms.Button();
            btnRefresh.Text = "Refrescar";
            btnRefresh.Left = 10;
            btnRefresh.Top = 540;
            btnRefresh.Click += (s, e) => LoadFromSql(sqlQuery);
            this.Controls.Add(btnRefresh);

            LoadFromSql(sqlQuery);
        }

        private void LoadFromSql(string sql)
        {
            try
            {
                if (string.IsNullOrEmpty(sql) && _parentForm != null && !string.IsNullOrEmpty(_gridId) && _parentForm.Items.Exists(_gridId))
                {
                    try
                    {
                        var grid = (SAPbouiCOM.Grid)_parentForm.Items.Item(_gridId).Specific;
                        // Try to extract data from grid's DataTable
                        var dt = new System.Data.DataTable();
                        for (int c = 0; c < grid.DataTable.Columns.Count; c++)
                        {
                            dt.Columns.Add(grid.DataTable.Columns.Item(c).Name);
                        }
                        for (int r = 0; r < grid.DataTable.Rows.Count; r++)
                        {
                            var row = dt.NewRow();
                            for (int c = 0; c < grid.DataTable.Columns.Count; c++)
                            {
                                row[c] = grid.DataTable.GetValue(c, r)?.ToString();
                            }
                            dt.Rows.Add(row);
                        }

                        _gridView.DataSource = dt;
                        return;
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(sql))
                {
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    try
                    {
                        rs.DoQuery(sql);
                        var dt = new System.Data.DataTable();
                        // create columns
                        for (int i = 0; i < rs.Fields.Count; i++) dt.Columns.Add(rs.Fields.Item(i).Name);
                        while (!rs.EoF)
                        {
                            var row = dt.NewRow();
                            for (int i = 0; i < rs.Fields.Count; i++) row[i] = rs.Fields.Item(i).Value?.ToString();
                            dt.Rows.Add(row);
                            rs.MoveNext();
                        }
                        _gridView.DataSource = dt;
                    }
                    finally { ComObjectManager.Release(rs); }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando datos: " + ex.Message);
            }
        }
    }
}
