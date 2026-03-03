using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.Forms
{
    public class DragDropHelperForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _b1Form;
        private TextBox _txtSource;
        private TextBox _txtTarget;
        private System.Windows.Forms.Button _btnBind;

        public DragDropHelperForm(SAPbouiCOM.Form b1Form)
        {
            _b1Form = b1Form;
            Text = "Drag & Drop Helper";
            Width = 420;
            Height = 220;

            var lbl1 = new System.Windows.Forms.Label(); lbl1.Text = "Source Item ID:"; lbl1.Left = 10; lbl1.Top = 10; lbl1.Width = 100; this.Controls.Add(lbl1);
            _txtSource = new System.Windows.Forms.TextBox(); _txtSource.Left = 120; _txtSource.Top = 10; _txtSource.Width = 260; this.Controls.Add(_txtSource);

            var lbl2 = new System.Windows.Forms.Label(); lbl2.Text = "Target Item ID:"; lbl2.Left = 10; lbl2.Top = 40; lbl2.Width = 100; this.Controls.Add(lbl2);
            _txtTarget = new System.Windows.Forms.TextBox(); _txtTarget.Left = 120; _txtTarget.Top = 40; _txtTarget.Width = 260; this.Controls.Add(_txtTarget);

            _btnBind = new System.Windows.Forms.Button(); _btnBind.Text = "Bind"; _btnBind.Left = 10; _btnBind.Top = 80; _btnBind.Click += BtnBind_Click; this.Controls.Add(_btnBind);

            var btnClose = new System.Windows.Forms.Button(); btnClose.Text = "Close"; btnClose.Left = 100; btnClose.Top = 80; btnClose.Click += (s, e) => Close(); this.Controls.Add(btnClose);
        }

        private void BtnBind_Click(object sender, EventArgs e)
        {
            try
            {
                string src = _txtSource.Text;
                string tgt = _txtTarget.Text;
                if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(tgt)) return;

                // Register for item events: we attach a handler that copies value from source to target on change
                EventDispatcher.Instance.RegisterLocalItemChangeHandler(_b1Form, src, (frm, itemId) =>
                {
                    try
                    {
                        var itSrc = frm.Items.Item(src);
                        var itTgt = frm.Items.Item(tgt);
                        string val = "";
                        if (itSrc.Specific is SAPbouiCOM.EditText et) val = et.Value;
                        if (itTgt.Specific is SAPbouiCOM.EditText ett) ett.Value = val;
                    }
                    catch { }
                });

                B1App.Instance.Application.SetStatusBarMessage("Drag-bind registrado entre items.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error registrando bind: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
