using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.Forms
{
    public class RichTextEditorForm : System.Windows.Forms.Form
    {
        private string _itemId;
        private SAPbouiCOM.Form _parentForm;
        private TextBox _txt;

        public RichTextEditorForm(string itemId, SAPbouiCOM.Form parentForm, string initialText)
        {
            _itemId = itemId;
            _parentForm = parentForm;
            Text = "Rich Text Editor";
            Width = 700;
            Height = 500;

            _txt = new TextBox();
            _txt.Multiline = true;
            _txt.ScrollBars = ScrollBars.Both;
            _txt.Width = 660;
            _txt.Height = 420;
            _txt.Left = 10;
            _txt.Top = 10;
            _txt.Text = initialText;

            var btnSave = new System.Windows.Forms.Button();
            btnSave.Text = "Guardar";
            btnSave.Left = 10;
            btnSave.Top = 440;
            btnSave.Click += BtnSave_Click;

            var btnClose = new System.Windows.Forms.Button();
            btnClose.Text = "Cerrar";
            btnClose.Left = 100;
            btnClose.Top = 440;
            btnClose.Click += (s, e) => Close();

            this.Controls.Add(_txt);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_parentForm != null && _parentForm.Items.Exists(_itemId))
                {
                    var it = _parentForm.Items.Item(_itemId);
                    if (it.Specific is SAPbouiCOM.EditText et)
                    {
                        et.Value = _txt.Text;
                        B1App.Instance.Application.SetStatusBarMessage("Texto guardado en el campo.", BoMessageTime.bmt_Short, false);
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error guardando texto: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
