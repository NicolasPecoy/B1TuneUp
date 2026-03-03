using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.Forms
{
    /// <summary>
    /// Minimal Item Placement design surface: lists items, allows selecting an item and changing Left/Top/Width/Height
    /// and applying changes back to the SAPbouiCOM.Form. Not a full drag-resize canvas but a safe editor MVP.
    /// </summary>
    public class ItemPlacementForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _b1Form;
        private System.Windows.Forms.ListBox _lstItems;
        private System.Windows.Forms.NumericUpDown _nudLeft, _nudTop, _nudWidth, _nudHeight;
        private System.Windows.Forms.Button _btnApply, _btnSave, _btnLoad, _btnClose, _btnRefresh, _btnPreview;
        private System.Windows.Forms.TextBox _txtLayoutName, _txtDesc;

        public ItemPlacementForm(SAPbouiCOM.Form b1Form)
        {
            _b1Form = b1Form;
            Text = $"Item Placement - {_b1Form.TypeEx}";
            Width = 800; Height = 600;

            _lstItems = new System.Windows.Forms.ListBox(); _lstItems.Left = 10; _lstItems.Top = 10; _lstItems.Width = 220; _lstItems.Height = 480; _lstItems.SelectedIndexChanged += LstItems_SelectedIndexChanged;
            Controls.Add(_lstItems);

            var lblLeft = new System.Windows.Forms.Label() { Left = 250, Top = 20, Text = "Left:" };
            _nudLeft = new System.Windows.Forms.NumericUpDown() { Left = 300, Top = 16, Width = 80, Minimum = 0, Maximum = 2000 };
            var lblTop = new System.Windows.Forms.Label() { Left = 250, Top = 60, Text = "Top:" };
            _nudTop = new System.Windows.Forms.NumericUpDown() { Left = 300, Top = 56, Width = 80, Minimum = 0, Maximum = 2000 };
            var lblWidth = new System.Windows.Forms.Label() { Left = 250, Top = 100, Text = "Width:" };
            _nudWidth = new System.Windows.Forms.NumericUpDown() { Left = 300, Top = 96, Width = 80, Minimum = 0, Maximum = 2000 };
            var lblHeight = new System.Windows.Forms.Label() { Left = 250, Top = 140, Text = "Height:" };
            _nudHeight = new System.Windows.Forms.NumericUpDown() { Left = 300, Top = 136, Width = 80, Minimum = 0, Maximum = 2000 };

            _btnApply = new System.Windows.Forms.Button() { Left = 400, Top = 20, Width = 120, Text = "Apply to Form" };
            _btnApply.Click += (s, e) => ApplyToForm();
            _btnPreview = new System.Windows.Forms.Button() { Left = 400, Top = 60, Width = 120, Text = "Preview (Refresh)" };
            _btnPreview.Click += (s, e) => { try { _b1Form.Refresh(); } catch { } };

            Controls.Add(lblLeft); Controls.Add(_nudLeft);
            Controls.Add(lblTop); Controls.Add(_nudTop);
            Controls.Add(lblWidth); Controls.Add(_nudWidth);
            Controls.Add(lblHeight); Controls.Add(_nudHeight);
            Controls.Add(_btnApply); Controls.Add(_btnPreview);

            _btnSave = new System.Windows.Forms.Button() { Left = 250, Top = 200, Width = 120, Text = "Save Layout" };
            _btnSave.Click += BtnSave_Click;
            _txtLayoutName = new System.Windows.Forms.TextBox() { Left = 380, Top = 200, Width = 200 };
            _txtDesc = new System.Windows.Forms.TextBox() { Left = 250, Top = 230, Width = 330, Height = 60, Multiline = true };

            _btnLoad = new System.Windows.Forms.Button() { Left = 250, Top = 320, Width = 120, Text = "Load Layout" };
            _btnLoad.Click += BtnLoad_Click;

            _btnRefresh = new System.Windows.Forms.Button() { Left = 380, Top = 320, Width = 120, Text = "Refresh Items" };
            _btnRefresh.Click += (s, e) => LoadItems();

            var btnDesigner = new System.Windows.Forms.Button() { Left = 520, Top = 320, Width = 120, Text = "Open Designer" };
            btnDesigner.Click += (s, e) => { var d = new DesignSurfaceForm(_b1Form); d.Show(); };
            Controls.Add(btnDesigner);

            _btnClose = new System.Windows.Forms.Button() { Left = 520, Top = 320, Width = 120, Text = "Close" };
            _btnClose.Click += (s, e) => Close();

            Controls.Add(_btnSave); Controls.Add(_txtLayoutName); Controls.Add(_txtDesc);
            Controls.Add(_btnLoad); Controls.Add(_btnRefresh); Controls.Add(_btnClose);

            LoadItems();
        }

        private void LoadItems()
        {
            _lstItems.Items.Clear();
            try
            {
                for (int i = 0; i < _b1Form.Items.Count; i++)
                {
                    var it = _b1Form.Items.Item(i + 1);
                    _lstItems.Items.Add(it.UniqueID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando items: " + ex.Message);
            }
        }

        private void LstItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (_lstItems.SelectedItem == null) return;
                string id = _lstItems.SelectedItem.ToString();
                if (!_b1Form.Items.Exists(id)) return;
                var it = _b1Form.Items.Item(id);
                _nudLeft.Value = it.Left;
                _nudTop.Value = it.Top;
                _nudWidth.Value = it.Width;
                _nudHeight.Value = it.Height;
            }
            catch { }
        }

        private void ApplyToForm()
        {
            try
            {
                if (_lstItems.SelectedItem == null) return;
                string id = _lstItems.SelectedItem.ToString();
                if (!_b1Form.Items.Exists(id)) return;
                var it = _b1Form.Items.Item(id);
                it.Left = (int)_nudLeft.Value;
                it.Top = (int)_nudTop.Value;
                it.Width = (int)_nudWidth.Value;
                it.Height = (int)_nudHeight.Value;
                try { _b1Form.Update(); } catch { try { _b1Form.Refresh(); } catch { } }
                B1App.Instance.Application.SetStatusBarMessage($"Item {id} actualizado.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error aplicando al formulario: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_txtLayoutName.Text)) { MessageBox.Show("Especifique un nombre de layout."); return; }
                // Build XML
                var doc = new XmlDocument();
                var root = doc.CreateElement("Layout"); doc.AppendChild(root);
                for (int i = 0; i < _b1Form.Items.Count; i++)
                {
                    var it = _b1Form.Items.Item(i + 1);
                    var n = doc.CreateElement("Item");
                    var aid = doc.CreateAttribute("id"); aid.Value = it.UniqueID; n.Attributes.Append(aid);
                    var aL = doc.CreateAttribute("left"); aL.Value = it.Left.ToString(); n.Attributes.Append(aL);
                    var aT = doc.CreateAttribute("top"); aT.Value = it.Top.ToString(); n.Attributes.Append(aT);
                    var aW = doc.CreateAttribute("width"); aW.Value = it.Width.ToString(); n.Attributes.Append(aW);
                    var aH = doc.CreateAttribute("height"); aH.Value = it.Height.ToString(); n.Attributes.Append(aH);
                    root.AppendChild(n);
                }
                string xml = doc.OuterXml;
                ItemPlacementManager.SaveLayout(_b1Form.TypeEx, _txtLayoutName.Text, xml, _txtDesc.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando layout: " + ex.Message);
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                // Show simple selection dialog
                var forms = ItemPlacementManager.GetLayouts(_b1Form.TypeEx);
                if (forms.Rows.Count == 0) { MessageBox.Show("No hay layouts guardados para este formulario."); return; }
                var dlg = new FormSelector(forms);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string name = dlg.SelectedName;
                    string xml = ItemPlacementManager.GetLayoutDefinition(name, _b1Form.TypeEx);
                    ItemPlacementManager.ApplyLayoutToForm(xml, _b1Form);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando layout: " + ex.Message);
            }
        }

        private class FormSelector : System.Windows.Forms.Form
        {
            public string SelectedName { get; private set; }
            private System.Windows.Forms.ListBox _lst;
            public FormSelector(System.Data.DataTable dt)
            {
                Text = "Select Layout"; Width = 400; Height = 400;
                _lst = new System.Windows.Forms.ListBox() { Left = 10, Top = 10, Width = 360, Height = 300 };
                foreach (System.Data.DataRow r in dt.Rows) _lst.Items.Add(r[0].ToString());
                var btn = new System.Windows.Forms.Button() { Left = 10, Top = 320, Width = 80, Text = "OK" };
                btn.Click += (s, e) => { SelectedName = _lst.SelectedItem?.ToString(); DialogResult = DialogResult.OK; Close(); };
                Controls.Add(_lst); Controls.Add(btn);
            }
        }
    }
}
