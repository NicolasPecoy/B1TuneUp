using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.Forms
{
    public class BarcodeScannerForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _parentForm;
        private string _targetItemId;
        private TextBox _txt;

        public BarcodeScannerForm(SAPbouiCOM.Form parentForm, string targetItemId)
        {
            _parentForm = parentForm;
            _targetItemId = targetItemId;

            Text = "Barcode Scanner";
            Width = 400;
            Height = 180;

            _txt = new TextBox();
            _txt.Left = 10;
            _txt.Top = 10;
            _txt.Width = 360;
            Controls.Add(_txt);

            var btnScan = new System.Windows.Forms.Button();
            btnScan.Text = "Simular Scan";
            btnScan.Left = 10;
            btnScan.Top = 50;
            btnScan.Click += BtnScan_Click;
            Controls.Add(btnScan);

            var btnClose = new System.Windows.Forms.Button();
            btnClose.Text = "Cerrar";
            btnClose.Left = 110;
            btnClose.Top = 50;
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            // For real scanner integration, you'd connect to hardware SDK. Here we accept manual input and push to the target field.
            try
            {
                string val = _txt.Text;
                if (_parentForm != null && !string.IsNullOrEmpty(_targetItemId) && _parentForm.Items.Exists(_targetItemId))
                {
                    var it = _parentForm.Items.Item(_targetItemId);
                    if (it.Specific is SAPbouiCOM.EditText et)
                    {
                        et.Value = val;
                        B1App.Instance.Application.SetStatusBarMessage("Valor escaneado insertado en campo.", BoMessageTime.bmt_Short, false);
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en escaneo: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
