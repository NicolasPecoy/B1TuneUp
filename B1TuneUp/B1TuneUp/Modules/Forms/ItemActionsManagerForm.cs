using System;
using System.Data;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules;

namespace B1TuneUp.Modules.Forms
{
    public class ItemActionsManagerForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.Button _btnEdit, _btnDelete, _btnClose, _btnRefresh, _btnAdd;

        public ItemActionsManagerForm()
        {
            Text = "Item Actions Manager";
            Width = 800; Height = 600;

            _grid = new System.Windows.Forms.DataGridView() { Left = 10, Top = 10, Width = 760, Height = 480, ReadOnly = true, AllowUserToAddRows = false };
            Controls.Add(_grid);

            _btnRefresh = new System.Windows.Forms.Button() { Left = 10, Top = 500, Width = 80, Text = "Refresh" };
            _btnRefresh.Click += (s, e) => LoadData(); Controls.Add(_btnRefresh);

            _btnAdd = new System.Windows.Forms.Button() { Left = 100, Top = 500, Width = 80, Text = "Add" };
            _btnAdd.Click += (s, e) => { var f = new Forms.AddItemForm(B1App.Instance.Application.Forms.ActiveForm); f.ShowDialog(); }; Controls.Add(_btnAdd);

            _btnEdit = new System.Windows.Forms.Button() { Left = 190, Top = 500, Width = 80, Text = "Edit" };
            _btnEdit.Click += BtnEdit_Click; Controls.Add(_btnEdit);

            _btnDelete = new System.Windows.Forms.Button() { Left = 280, Top = 500, Width = 80, Text = "Delete" };
            _btnDelete.Click += BtnDelete_Click; Controls.Add(_btnDelete);

            _btnClose = new System.Windows.Forms.Button() { Left = 360, Top = 500, Width = 80, Text = "Close" };
            _btnClose.Click += (s, e) => Close(); Controls.Add(_btnClose);

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = ItemActionManager.GetAllActionsTable();
                _grid.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading actions: " + ex.Message);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (_grid.SelectedRows.Count == 0) return;
                var row = _grid.SelectedRows[0];
                string formType = row.Cells[0].Value?.ToString();
                string itemId = row.Cells[1].Value?.ToString();
                ItemEditorManager.OpenItemEditor(itemId, B1App.Instance.Application.Forms.ActiveForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error editing action: " + ex.Message);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_grid.SelectedRows.Count == 0) return;
                var row = _grid.SelectedRows[0];
                string formType = row.Cells[0].Value?.ToString();
                string itemId = row.Cells[1].Value?.ToString();
                if (MessageBox.Show($"Delete action for {itemId}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ItemActionManager.DeleteAction(formType, itemId);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting action: " + ex.Message);
            }
        }
    }
}
