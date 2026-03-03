using System;
using System.Data;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Modules;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.Forms
{
    public class LayoutManagerForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.Button _btnExport, _btnImport, _btnRestore, _btnClose;

        public LayoutManagerForm()
        {
            Text = "Layout Manager"; Width = 800; Height = 600;
            _grid = new System.Windows.Forms.DataGridView() { Left = 10, Top = 10, Width = 760, Height = 480, ReadOnly = true, AllowUserToAddRows = false };
            Controls.Add(_grid);
            _btnExport = new System.Windows.Forms.Button() { Left = 10, Top = 500, Width = 100, Text = "Export SRF" };
            _btnExport.Click += BtnExport_Click; Controls.Add(_btnExport);

            _btnImport = new System.Windows.Forms.Button() { Left = 120, Top = 500, Width = 100, Text = "Import SRF" };
            _btnImport.Click += BtnImport_Click; Controls.Add(_btnImport);

            _btnRestore = new System.Windows.Forms.Button() { Left = 230, Top = 500, Width = 120, Text = "Restore from Layout" };
            _btnRestore.Click += BtnRestore_Click; Controls.Add(_btnRestore);

            _btnClose = new System.Windows.Forms.Button() { Left = 360, Top = 500, Width = 80, Text = "Close" };
            _btnClose.Click += (s, e) => Close(); Controls.Add(_btnClose);

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = ItemPlacementManager.GetLayouts("*");
                _grid.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading layouts: " + ex.Message);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            var row = _grid.SelectedRows[0];
            string name = row.Cells[0].Value?.ToString();
            string formType = row.Cells[1].Value?.ToString();
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "SRF files (*.srf)|*.srf|XML files (*.xml)|*.xml|All files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // restore SRF to file
                    ItemPlacementManager.RestoreSrfFromLayout(name, formType, sfd.FileName);
                    MessageBox.Show("Exported to " + sfd.FileName);
                }
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "SRF/XML files (*.srf;*.xml)|*.srf;*.xml|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var fname = ofd.FileName;
                    // ask name
                    var name = Prompt.ShowDialog("Layout name:", "Import Layout", System.IO.Path.GetFileNameWithoutExtension(fname));
                    if (string.IsNullOrEmpty(name)) return;
                    // save SRF into UDT
                    ItemPlacementManager.SaveSrfToLayout(B1App.Instance.Application.Forms.ActiveForm.TypeEx, name, fname);
                    LoadData();
                }
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            var row = _grid.SelectedRows[0];
            string name = row.Cells[0].Value?.ToString();
            string formType = row.Cells[1].Value?.ToString();
            ItemPlacementManager.LoadLayoutVersion(name, formType);
            MessageBox.Show("Layout applied: " + name);
        }
    }
}
