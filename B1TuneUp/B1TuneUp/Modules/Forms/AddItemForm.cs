using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.Forms
{
    public class AddItemForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _b1Form;
        private System.Windows.Forms.TextBox _txtId;
        private System.Windows.Forms.ComboBox _cmbType;
        private System.Windows.Forms.Button _btnAdd, _btnClose;

        public AddItemForm(SAPbouiCOM.Form b1Form)
        {
            _b1Form = b1Form;
            Text = LocalizationManager.GetString("AddItem.Title");
            Width = 420; Height = 200;

            _txtId = new System.Windows.Forms.TextBox() { Left = 10, Top = 10, Width = 380 };
            Controls.Add(_txtId);

            _cmbType = new System.Windows.Forms.ComboBox() { Left = 10, Top = 40, Width = 200 };
            _cmbType.Items.AddRange(new string[] { "EditText", "Button", "StaticText", "ComboBox" });
            Controls.Add(_cmbType);

            _btnAdd = new System.Windows.Forms.Button() { Left = 10, Top = 80, Width = 100, Text = LocalizationManager.GetString("Btn.Add") };
            _btnAdd.Click += BtnAdd_Click;
            Controls.Add(_btnAdd);

            var btnEditAfter = new System.Windows.Forms.Button() { Left = 120, Top = 80, Width = 140, Text = LocalizationManager.GetString("Btn.EditAfterAdd") };
            btnEditAfter.Click += (s, e) => { BtnAdd_Click(s, e); if (!string.IsNullOrWhiteSpace(_txtId.Text) && _b1Form != null) ItemEditorManager.OpenItemEditor(_txtId.Text.Trim(), _b1Form); };
            Controls.Add(btnEditAfter);

            _btnClose = new System.Windows.Forms.Button() { Left = 120, Top = 80, Width = 100, Text = LocalizationManager.GetString("Btn.Close") };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                string id = _txtId.Text.Trim();
                if (string.IsNullOrEmpty(id)) { MessageBox.Show("Ingrese un ID"); return; }
                if (_b1Form.Items.Exists(id)) { MessageBox.Show("ID ya existe"); return; }

                var type = _cmbType.SelectedItem?.ToString() ?? "EditText";
                // Use Items.Add with an ID and type
                BoFormItemTypes itemType = BoFormItemTypes.it_EDIT;
                if (type == "StaticText") itemType = BoFormItemTypes.it_STATIC;
                else if (type == "Button") itemType = BoFormItemTypes.it_BUTTON;
                else if (type == "ComboBox") itemType = BoFormItemTypes.it_COMBO_BOX;

                var newItem = _b1Form.Items.Add(id, itemType);
                // Set default position and size
                try
                {
                    newItem.Left = 10; newItem.Top = 120; newItem.Width = 120; newItem.Height = 20;
                }
                catch { }

                MessageBox.Show("Item agregado. Ajuste propiedades desde Item Editor si es necesario.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error agregando item: " + ex.Message);
            }
        }
    }
}
