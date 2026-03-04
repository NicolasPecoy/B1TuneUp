using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.Forms
{
    public class ItemEditorForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _b1Form;
        private Item _item;
        private System.Windows.Forms.NumericUpDown _nudLeft, _nudTop, _nudWidth, _nudHeight;
        private System.Windows.Forms.TextBox _txtUid;
        private System.Windows.Forms.Button _btnApply, _btnClose;
        private System.Windows.Forms.TextBox _txtAction;
        private System.Windows.Forms.Button _btnSaveAction;

        public ItemEditorForm(SAPbouiCOM.Form b1Form, Item item)
        {
            _b1Form = b1Form;
            _item = item;
            Text = LocalizationManager.GetString("ItemEditor.Title") + " - " + item.UniqueID;
            Width = 420; Height = 260;

            _txtUid = new System.Windows.Forms.TextBox() { Left = 10, Top = 10, Width = 380, ReadOnly = true, Text = item.UniqueID };
            Controls.Add(_txtUid);

            _nudLeft = new System.Windows.Forms.NumericUpDown() { Left = 10, Top = 50, Width = 80, Minimum = 0, Maximum = 2000, Value = item.Left };
            Controls.Add(_nudLeft);
            _nudTop = new System.Windows.Forms.NumericUpDown() { Left = 100, Top = 50, Width = 80, Minimum = 0, Maximum = 2000, Value = item.Top };
            Controls.Add(_nudTop);
            _nudWidth = new System.Windows.Forms.NumericUpDown() { Left = 190, Top = 50, Width = 80, Minimum = 0, Maximum = 2000, Value = item.Width };
            Controls.Add(_nudWidth);
            _nudHeight = new System.Windows.Forms.NumericUpDown() { Left = 280, Top = 50, Width = 80, Minimum = 0, Maximum = 2000, Value = item.Height };
            Controls.Add(_nudHeight);

            _btnApply = new System.Windows.Forms.Button() { Left = 10, Top = 100, Width = 100, Text = LocalizationManager.GetString("Btn.Apply") };
            _btnApply.Click += (s, e) => Apply();
            Controls.Add(_btnApply);
            _txtAction = new System.Windows.Forms.TextBox() { Left = 10, Top = 140, Width = 360 };
            var tt = new System.Windows.Forms.ToolTip();
            tt.SetToolTip(_txtAction, LocalizationManager.GetString("ItemEditor.ActionHint"));
            Controls.Add(_txtAction);
            _btnSaveAction = new System.Windows.Forms.Button() { Left = 10, Top = 170, Width = 120, Text = LocalizationManager.GetString("Btn.SaveAction") };
            _btnSaveAction.Click += (s, e) => SaveAction();
            Controls.Add(_btnSaveAction);

            _btnClose = new System.Windows.Forms.Button() { Left = 120, Top = 100, Width = 100, Text = LocalizationManager.GetString("Btn.Close") };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            // Load existing action if present
            try { _txtAction.Text = ItemActionManager.GetAction(_b1Form.TypeEx, _item.UniqueID); } catch { }
        }

        private void SaveAction()
        {
            try
            {
                var macro = _txtAction.Text ?? "";
                ItemActionManager.SaveAction(_b1Form.TypeEx, _item.UniqueID, macro);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando acción: " + ex.Message);
            }
        }

        private void Apply()
        {
            try
            {
                _item.Left = (int)_nudLeft.Value;
                _item.Top = (int)_nudTop.Value;
                _item.Width = (int)_nudWidth.Value;
                _item.Height = (int)_nudHeight.Value;
                try { _b1Form.Update(); } catch { try { _b1Form.Refresh(); } catch { } }
                B1App.Instance.Application.SetStatusBarMessage($"Item {_item.UniqueID} actualizado.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error aplicando cambios: " + ex.Message);
            }
        }
    }
}
