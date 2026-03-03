using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using B1TuneUp.Modules;

namespace B1TuneUp.Modules.Forms
{
    public class DesignSurfaceForm : System.Windows.Forms.Form
    {
        private SAPbouiCOM.Form _b1Form;
        private Panel _canvas;
        private Dictionary<string, Panel> _panels = new Dictionary<string, Panel>();
        private HashSet<string> _selected = new HashSet<string>();
        private Dictionary<string, PanelMeta> _meta = new Dictionary<string, PanelMeta>();
        private bool _snapToGuides = true;
        private int _guideThreshold = 6;

        private bool _snapToGrid = true;
        private int _gridSize = 5;

        // drag/resize state
        private bool _isDragging = false;
        private bool _isResizing = false;
        private string _activeId;
        private Point _dragStart;
        private Rectangle _origRect;

        // undo/redo
        private Stack<Dictionary<string, Rectangle>> _undo = new Stack<Dictionary<string, Rectangle>>();
        private Stack<Dictionary<string, Rectangle>> _redo = new Stack<Dictionary<string, Rectangle>>();

        // copy/paste
        private List<ClipboardItem> _clipboard = new List<ClipboardItem>();

        // visual guides
        private Panel _vGuide;
        private Panel _hGuide;

        private class PanelMeta
        {
            public int FromPane;
            public int ToPane;
            public string Label;
            public string DataBind;
        }

        private class ClipboardItem
        {
            public string BaseId;
            public Rectangle Rect;
            public BoFormItemTypes Type;
            public string Label;
            public string DataBind;
            public int FromPane;
            public int ToPane;
        }

        public DesignSurfaceForm(SAPbouiCOM.Form b1Form)
        {
            _b1Form = b1Form ?? B1App.Instance.Application.Forms.ActiveForm;
            Text = $"Visual Designer - {_b1Form?.TypeEx ?? "(none)"}";
            Width = 1000; Height = 800;

            _canvas = new Panel() { Left = 10, Top = 10, Width = 960, Height = 680, BackColor = Color.White, AutoScroll = true };
            Controls.Add(_canvas);

            _vGuide = new Panel() { Visible = false, BackColor = Color.Red, Width = 1, Height = _canvas.Height, Left = 0, Top = 0 };
            _hGuide = new Panel() { Visible = false, BackColor = Color.Red, Height = 1, Width = _canvas.Width, Left = 0, Top = 0 };
            _canvas.Controls.Add(_vGuide);
            _canvas.Controls.Add(_hGuide);

            var btnApply = new System.Windows.Forms.Button() { Left = 10, Top = 700, Width = 100, Text = "Apply" };
            btnApply.Click += (s, e) => ApplyToForm(); Controls.Add(btnApply);

            var btnSave = new System.Windows.Forms.Button() { Left = 120, Top = 700, Width = 120, Text = "Save Layout" };
            btnSave.Click += BtnSave_Click; Controls.Add(btnSave);

            // Alignment / distribution controls
            var btnAlignLeft = new System.Windows.Forms.Button() { Left = 660, Top = 700, Width = 80, Text = "Align L" };
            btnAlignLeft.Click += (s, e) => AlignLeft(); Controls.Add(btnAlignLeft);
            var btnAlignCenter = new System.Windows.Forms.Button() { Left = 745, Top = 700, Width = 80, Text = "Align C" };
            btnAlignCenter.Click += (s, e) => AlignCenter(); Controls.Add(btnAlignCenter);
            var btnAlignRight = new System.Windows.Forms.Button() { Left = 830, Top = 700, Width = 80, Text = "Align R" };
            btnAlignRight.Click += (s, e) => AlignRight(); Controls.Add(btnAlignRight);
            var btnAlignTop = new System.Windows.Forms.Button() { Left = 660, Top = 730, Width = 80, Text = "Align T" };
            btnAlignTop.Click += (s, e) => AlignTop(); Controls.Add(btnAlignTop);
            var btnAlignMiddle = new System.Windows.Forms.Button() { Left = 745, Top = 730, Width = 80, Text = "Align M" };
            btnAlignMiddle.Click += (s, e) => AlignMiddle(); Controls.Add(btnAlignMiddle);
            var btnAlignBottom = new System.Windows.Forms.Button() { Left = 830, Top = 730, Width = 80, Text = "Align B" };
            btnAlignBottom.Click += (s, e) => AlignBottom(); Controls.Add(btnAlignBottom);
            var btnDistH = new System.Windows.Forms.Button() { Left = 915, Top = 700, Width = 80, Text = "Dist H" };
            btnDistH.Click += (s, e) => DistributeHorizontally(); Controls.Add(btnDistH);
            var btnDistV = new System.Windows.Forms.Button() { Left = 915, Top = 730, Width = 80, Text = "Dist V" };
            btnDistV.Click += (s, e) => DistributeVertically(); Controls.Add(btnDistV);

            var btnUndo = new System.Windows.Forms.Button() { Left = 250, Top = 700, Width = 80, Text = "Undo" };
            btnUndo.Click += (s, e) => Undo(); Controls.Add(btnUndo);

            var btnRedo = new System.Windows.Forms.Button() { Left = 340, Top = 700, Width = 80, Text = "Redo" };
            btnRedo.Click += (s, e) => Redo(); Controls.Add(btnRedo);

            var btnCopy = new System.Windows.Forms.Button() { Left = 430, Top = 700, Width = 80, Text = "Copy" };
            btnCopy.Click += (s, e) => Copy(); Controls.Add(btnCopy);

            var btnPaste = new System.Windows.Forms.Button() { Left = 520, Top = 700, Width = 80, Text = "Paste" };
            btnPaste.Click += (s, e) => Paste(); Controls.Add(btnPaste);

            var chkSnap = new System.Windows.Forms.CheckBox() { Left = 610, Top = 705, Width = 120, Text = "Snap to Grid", Checked = _snapToGrid };
            chkSnap.CheckedChanged += (s, e) => _snapToGrid = chkSnap.Checked; Controls.Add(chkSnap);

            var lblGrid = new System.Windows.Forms.Label() { Left = 750, Top = 705, Width = 70, Text = "Grid:" };
            var nudGrid = new System.Windows.Forms.NumericUpDown() { Left = 800, Top = 702, Width = 50, Minimum = 1, Maximum = 50, Value = _gridSize };
            nudGrid.ValueChanged += (s, e) => _gridSize = (int)nudGrid.Value; Controls.Add(lblGrid); Controls.Add(nudGrid);

            // guide threshold and snap-to-guides
            var chkSnapGuides = new System.Windows.Forms.CheckBox() { Left = 860, Top = 705, Width = 140, Text = "Snap to Guides", Checked = true };
            chkSnapGuides.CheckedChanged += (s, e) => _snapToGuides = chkSnapGuides.Checked; Controls.Add(chkSnapGuides);
            var lblThresh = new System.Windows.Forms.Label() { Left = 1000, Top = 705, Width = 80, Text = "Thresh:" };
            var nudThresh = new System.Windows.Forms.NumericUpDown() { Left = 1060, Top = 702, Width = 50, Minimum = 1, Maximum = 30, Value = _guideThreshold };
            nudThresh.ValueChanged += (s, e) => _guideThreshold = (int)nudThresh.Value; Controls.Add(lblThresh); Controls.Add(nudThresh);

            LoadPanels();
        }

        private void AlignLeft()
        {
            if (_selected.Count < 2) return;
            int minLeft = _selected.Min(id => _panels[id].Left);
            PushUndoSnapshot();
            foreach (var id in _selected) _panels[id].Left = minLeft;
        }

        private void AlignCenter()
        {
            if (_selected.Count < 2) return;
            PushUndoSnapshot();
            int avgCenter = (int)_selected.Average(id => _panels[id].Left + _panels[id].Width / 2);
            foreach (var id in _selected) _panels[id].Left = avgCenter - _panels[id].Width / 2;
        }

        private void AlignRight()
        {
            if (_selected.Count < 2) return;
            int maxRight = _selected.Max(id => _panels[id].Left + _panels[id].Width);
            PushUndoSnapshot();
            foreach (var id in _selected) _panels[id].Left = maxRight - _panels[id].Width;
        }

        private void AlignTop()
        {
            if (_selected.Count < 2) return;
            int minTop = _selected.Min(id => _panels[id].Top);
            PushUndoSnapshot();
            foreach (var id in _selected) _panels[id].Top = minTop;
        }

        private void AlignMiddle()
        {
            if (_selected.Count < 2) return;
            PushUndoSnapshot();
            int avgMid = (int)_selected.Average(id => _panels[id].Top + _panels[id].Height / 2);
            foreach (var id in _selected) _panels[id].Top = avgMid - _panels[id].Height / 2;
        }

        private void AlignBottom()
        {
            if (_selected.Count < 2) return;
            PushUndoSnapshot();
            int maxBottom = _selected.Max(id => _panels[id].Top + _panels[id].Height);
            foreach (var id in _selected) _panels[id].Top = maxBottom - _panels[id].Height;
        }

        private void DistributeHorizontally()
        {
            if (_selected.Count < 3) return;
            PushUndoSnapshot();
            var list = _selected.Select(id => _panels[id]).OrderBy(p => p.Left).ToList();
            int left = list.First().Left;
            int right = list.Last().Left;
            int totalWidth = list.Sum(p => p.Width);
            int space = (right - left - totalWidth + list.Last().Width) / (list.Count - 1);
            int x = left;
            foreach (var p in list)
            {
                p.Left = x;
                x += p.Width + space;
            }
        }

        private void DistributeVertically()
        {
            if (_selected.Count < 3) return;
            PushUndoSnapshot();
            var list = _selected.Select(id => _panels[id]).OrderBy(p => p.Top).ToList();
            int top = list.First().Top;
            int bottom = list.Last().Top;
            int totalHeight = list.Sum(p => p.Height);
            int space = (bottom - top - totalHeight + list.Last().Height) / (list.Count - 1);
            int y = top;
            foreach (var p in list)
            {
                p.Top = y;
                y += p.Height + space;
            }
        }

        private void LoadPanels()
        {
            _canvas.Controls.Clear();
            _panels.Clear();
            _selected.Clear();

            try
            {
                for (int i = 0; i < _b1Form.Items.Count; i++)
                {
                    var it = _b1Form.Items.Item(i + 1);
                    string id = it.UniqueID;
                    var p = new Panel();
                    p.Left = it.Left; p.Top = it.Top; p.Width = it.Width; p.Height = it.Height;
                    p.BackColor = Color.FromArgb(40, Color.SkyBlue);
                    p.BorderStyle = BorderStyle.FixedSingle;
                    p.Tag = id;
                    var lbl = new Label() { Text = id, Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
                    p.Controls.Add(lbl);

                    // load metadata
                    var meta = new PanelMeta();
                    try { meta.FromPane = it.FromPane; meta.ToPane = it.ToPane; } catch { }
                    try
                    {
                        if (it.Specific is SAPbouiCOM.StaticText st) meta.Label = st.Caption;
                        else if (it.Specific is SAPbouiCOM.Button btn) meta.Label = btn.Caption;
                    }
                    catch { }
                    try
                    {
                        var prop = it.Specific.GetType().GetProperty("DataBind");
                        if (prop != null) meta.DataBind = prop.GetValue(it.Specific)?.ToString();
                    }
                    catch { }
                    _meta[id] = meta;

                    p.MouseDown += Panel_MouseDown;
                    p.MouseMove += Panel_MouseMove;
                    p.MouseUp += Panel_MouseUp;
                    p.MouseDoubleClick += Panel_MouseDoubleClick;

                    var grip = new Panel() { Width = 10, Height = 10, BackColor = Color.DarkGray, Cursor = Cursors.SizeNWSE };
                    grip.Left = Math.Max(0, p.Width - 12); grip.Top = Math.Max(0, p.Height - 12);
                    grip.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                    grip.MouseDown += (s, e) => { _isResizing = true; _activeId = id; _dragStart = Cursor.Position; _origRect = new Rectangle(p.Left, p.Top, p.Width, p.Height); PushUndoSnapshot(); };
                    grip.MouseMove += (s, e) => { if (_isResizing && _activeId == id) { var delta = Point.Subtract(Cursor.Position, new Size(_dragStart)); int nw = _origRect.Width + delta.X; int nh = _origRect.Height + delta.Y; p.Width = Math.Max(10, _snapToGrid ? (nw / _gridSize) * _gridSize : nw); p.Height = Math.Max(10, _snapToGrid ? (nh / _gridSize) * _gridSize : nh); } };
                    grip.MouseUp += (s, e) => { if (_isResizing) { _isResizing = false; _activeId = null; PushUndoSnapshot(); } };

                    p.Controls.Add(grip);

                    _canvas.Controls.Add(p);
                    _panels[id] = p;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading panels: " + ex.Message);
            }
        }

        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            var p = (Panel)sender;
            string id = (string)p.Tag;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (_selected.Contains(id)) _selected.Remove(id); else _selected.Add(id);
                UpdateSelectionVisual();
                return;
            }

            _isDragging = true;
            _activeId = id;
            _dragStart = Cursor.Position;
            _origRect = new Rectangle(p.Left, p.Top, p.Width, p.Height);
            PushUndoSnapshot();
            if (!_selected.Contains(id)) { _selected.Clear(); _selected.Add(id); UpdateSelectionVisual(); }
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            try
            {
                var p = (Panel)sender;
                var delta = Point.Subtract(Cursor.Position, new Size(_dragStart));
                int nx = _origRect.Left + delta.X;
                int ny = _origRect.Top + delta.Y;
                if (_snapToGrid)
                {
                    nx = (nx / _gridSize) * _gridSize;
                    ny = (ny / _gridSize) * _gridSize;
                }
                // show guides aligned to other items
                ShowAlignmentGuides(nx, ny, p);
                foreach (var id in _selected.ToList())
                {
                    if (_panels.TryGetValue(id, out var cp))
                    {
                        cp.Left = nx + (cp.Left - _origRect.Left);
                        cp.Top = ny + (cp.Top - _origRect.Top);
                    }
                }
            }
            catch { }
        }

        private void ShowAlignmentGuides(int nx, int ny, Panel moving)
        {
            _vGuide.Visible = false; _hGuide.Visible = false;
            foreach (var kv in _panels)
            {
                if (kv.Value == moving) continue;
                // vertical align (left/right/center)
                if (Math.Abs(kv.Value.Left - nx) < 6)
                {
                    _vGuide.Left = kv.Value.Left; _vGuide.Top = 0; _vGuide.Height = _canvas.Height; _vGuide.Visible = true; break;
                }
                if (Math.Abs((kv.Value.Left + kv.Value.Width) - (nx + moving.Width)) < 6)
                {
                    _vGuide.Left = kv.Value.Left + kv.Value.Width; _vGuide.Top = 0; _vGuide.Height = _canvas.Height; _vGuide.Visible = true; break;
                }
                int center = kv.Value.Left + kv.Value.Width / 2;
                if (Math.Abs(center - (nx + moving.Width / 2)) < 6)
                {
                    _vGuide.Left = center; _vGuide.Top = 0; _vGuide.Height = _canvas.Height; _vGuide.Visible = true; break;
                }
            }
            foreach (var kv in _panels)
            {
                if (kv.Value == moving) continue;
                if (Math.Abs(kv.Value.Top - ny) < 6)
                {
                    _hGuide.Top = kv.Value.Top; _hGuide.Left = 0; _hGuide.Width = _canvas.Width; _hGuide.Visible = true; break;
                }
                if (Math.Abs((kv.Value.Top + kv.Value.Height) - (ny + moving.Height)) < 6)
                {
                    _hGuide.Top = kv.Value.Top + kv.Value.Height; _hGuide.Left = 0; _hGuide.Width = _canvas.Width; _hGuide.Visible = true; break;
                }
                int mid = kv.Value.Top + kv.Value.Height / 2;
                if (Math.Abs(mid - (ny + moving.Height / 2)) < 6)
                {
                    _hGuide.Top = mid; _hGuide.Left = 0; _hGuide.Width = _canvas.Width; _hGuide.Visible = true; break;
                }
            }
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging) { _isDragging = false; _activeId = null; PushUndoSnapshot(); }
        }

        private void Panel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var p = (Panel)sender; var id = (string)p.Tag;
            try { ItemEditorManager.OpenItemEditor(id, _b1Form); } catch { }
        }

        private void UpdateSelectionVisual()
        {
            foreach (var kv in _panels)
            {
                kv.Value.BackColor = _selected.Contains(kv.Key) ? Color.FromArgb(120, Color.Orange) : Color.FromArgb(40, Color.SkyBlue);
            }
        }

        private void PushUndoSnapshot()
        {
            var snap = new Dictionary<string, Rectangle>();
            foreach (var kv in _panels) snap[kv.Key] = new Rectangle(kv.Value.Left, kv.Value.Top, kv.Value.Width, kv.Value.Height);
            _undo.Push(snap);
            _redo.Clear();
        }

        private void Undo()
        {
            if (_undo.Count == 0) return;
            var cur = new Dictionary<string, Rectangle>(); foreach (var kv in _panels) cur[kv.Key] = new Rectangle(kv.Value.Left, kv.Value.Top, kv.Value.Width, kv.Value.Height);
            _redo.Push(cur);
            var snap = _undo.Pop();
            ApplySnapshot(snap);
        }

        private void Redo()
        {
            if (_redo.Count == 0) return;
            var snap = _redo.Pop();
            var cur = new Dictionary<string, Rectangle>(); foreach (var kv in _panels) cur[kv.Key] = new Rectangle(kv.Value.Left, kv.Value.Top, kv.Value.Width, kv.Value.Height);
            _undo.Push(cur);
            ApplySnapshot(snap);
        }

        private void ApplySnapshot(Dictionary<string, Rectangle> snap)
        {
            foreach (var kv in snap)
            {
                if (_panels.TryGetValue(kv.Key, out var p))
                {
                    p.Left = kv.Value.Left; p.Top = kv.Value.Top; p.Width = kv.Value.Width; p.Height = kv.Value.Height;
                }
            }
        }

        private void ApplyToForm()
        {
            try
            {
                foreach (var kv in _panels)
                {
                    string id = kv.Key;
                    var r = new Rectangle(kv.Value.Left, kv.Value.Top, kv.Value.Width, kv.Value.Height);
                    if (_b1Form.Items.Exists(id))
                    {
                        var it = _b1Form.Items.Item(id);
                        it.Left = r.Left; it.Top = r.Top; it.Width = r.Width; it.Height = r.Height;
                        // apply extra metadata where possible
                        try
                        {
                            if (_meta.TryGetValue(id, out var m))
                            {
                                it.FromPane = m.FromPane; it.ToPane = m.ToPane;
                                try
                                {
                                    if (it.Specific is SAPbouiCOM.StaticText st) st.Caption = m.Label;
                                    else if (it.Specific is SAPbouiCOM.Button btn) btn.Caption = m.Label;
                                    else if (it.Specific is SAPbouiCOM.EditText et && !string.IsNullOrEmpty(m.Label)) et.Value = m.Label;
                                }
                                catch { }
                                try
                                {
                                    var prop = it.Specific.GetType().GetProperty("DataBind");
                                    if (prop != null && !string.IsNullOrEmpty(m.DataBind)) prop.SetValue(it.Specific, m.DataBind);
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
                try { _b1Form.Update(); } catch { try { _b1Form.Refresh(); } catch { } }
                B1App.Instance.Application.SetStatusBarMessage("Designer applied to form.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying to form: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var name = Prompt.ShowDialog("Layout name:", "Save Layout", "Layout1");
                if (string.IsNullOrEmpty(name)) return;
                var desc = Prompt.ShowDialog("Description:", "Save Layout", "");
                var doc = new XmlDocument(); var root = doc.CreateElement("Layout"); doc.AppendChild(root);
                foreach (var kv in _panels)
                {
                    var it = doc.CreateElement("Item");
                    var aid = doc.CreateAttribute("id"); aid.Value = kv.Key; it.Attributes.Append(aid);
                    var aL = doc.CreateAttribute("left"); aL.Value = kv.Value.Left.ToString(); it.Attributes.Append(aL);
                    var aT = doc.CreateAttribute("top"); aT.Value = kv.Value.Top.ToString(); it.Attributes.Append(aT);
                    var aW = doc.CreateAttribute("width"); aW.Value = kv.Value.Width.ToString(); it.Attributes.Append(aW);
                    var aH = doc.CreateAttribute("height"); aH.Value = kv.Value.Height.ToString(); it.Attributes.Append(aH);
                    // persist extra properties if available
                    try
                    {
                        var b1It = _b1Form.Items.Item(kv.Key);
                        var pf = doc.CreateAttribute("fromPane"); pf.Value = b1It.FromPane.ToString(); it.Attributes.Append(pf);
                        var pt = doc.CreateAttribute("toPane"); pt.Value = b1It.ToPane.ToString(); it.Attributes.Append(pt);
                        var lbl = doc.CreateAttribute("label");
                        string labelVal = "";
                        try
                        {
                            if (b1It.Specific is SAPbouiCOM.StaticText st) labelVal = st.Caption;
                            else if (b1It.Specific is SAPbouiCOM.Button btn) labelVal = btn.Caption;
                            else if (b1It.Specific is SAPbouiCOM.EditText et) labelVal = et.Value ?? "";
                            else
                            {
                                var prop = b1It.Specific.GetType().GetProperty("Caption");
                                if (prop != null) labelVal = prop.GetValue(b1It.Specific)?.ToString() ?? "";
                            }
                        }
                        catch { }
                        lbl.Value = labelVal;
                        it.Attributes.Append(lbl);
                        var bind = doc.CreateAttribute("dataBind");
                        string bindVal = "";
                        try
                        {
                            var prop = b1It.Specific.GetType().GetProperty("DataBind");
                            if (prop != null) bindVal = prop.GetValue(b1It.Specific)?.ToString() ?? "";
                        }
                        catch { }
                        bind.Value = bindVal;
                        it.Attributes.Append(bind);
                    }
                    catch { }
                    root.AppendChild(it);
                }
                string xml = doc.OuterXml;
                ItemPlacementManager.SaveLayout(_b1Form.TypeEx, name, xml, desc);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving layout: " + ex.Message);
            }
        }

        private void Copy()
        {
            _clipboard.Clear();
            foreach (var id in _selected)
            {
                if (_panels.TryGetValue(id, out var p))
                {
                    try
                    {
                        var it = _b1Form.Items.Item(id);
                        var ci = new ClipboardItem()
                        {
                            BaseId = id,
                            Rect = new Rectangle(p.Left, p.Top, p.Width, p.Height),
                            Type = it.Type,
                            Label = "",
                            DataBind = "",
                            FromPane = 0,
                            ToPane = 0
                        };
                        try { ci.FromPane = it.FromPane; ci.ToPane = it.ToPane; } catch { }
                        try { if (it.Specific is SAPbouiCOM.StaticText st) ci.Label = st.Caption; else if (it.Specific is SAPbouiCOM.Button btn) ci.Label = btn.Caption; else if (it.Specific is SAPbouiCOM.EditText et) ci.Label = et.Value ?? ""; } catch { }
                        try { var prop = it.Specific.GetType().GetProperty("DataBind"); if (prop != null) ci.DataBind = prop.GetValue(it.Specific)?.ToString() ?? ""; } catch { }
                        _clipboard.Add(ci);
                    }
                    catch
                    {
                        var ci = new ClipboardItem() { BaseId = id, Rect = new Rectangle(p.Left, p.Top, p.Width, p.Height), Type = BoFormItemTypes.it_EDIT };
                        _clipboard.Add(ci);
                    }
                }
            }
            B1App.Instance.Application.SetStatusBarMessage($"Copied {_clipboard.Count} items.", BoMessageTime.bmt_Short, false);
        }

        private void Paste()
        {
            try
            {
                foreach (var ci in _clipboard)
                {
                    string baseId = ci.BaseId;
                    string newId = baseId + "_CP_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                    try
                    {
                        var newItem = _b1Form.Items.Add(newId, ci.Type);
                        newItem.Left = ci.Rect.Left + 10; newItem.Top = ci.Rect.Top + 10; newItem.Width = ci.Rect.Width; newItem.Height = ci.Rect.Height;
                        try { newItem.FromPane = ci.FromPane; newItem.ToPane = ci.ToPane; } catch { }
                        try
                        {
                            if (!string.IsNullOrEmpty(ci.Label))
                            {
                                if (newItem.Specific is SAPbouiCOM.StaticText st) st.Caption = ci.Label;
                                else if (newItem.Specific is SAPbouiCOM.Button btn) btn.Caption = ci.Label;
                                else if (newItem.Specific is SAPbouiCOM.EditText et) et.Value = ci.Label;
                            }
                        }
                        catch { }
                        try { var prop = newItem.Specific.GetType().GetProperty("DataBind"); if (prop != null && !string.IsNullOrEmpty(ci.DataBind)) prop.SetValue(newItem.Specific, ci.DataBind); } catch { }

                        // update meta
                        var meta = new PanelMeta() { FromPane = ci.FromPane, ToPane = ci.ToPane, Label = ci.Label, DataBind = ci.DataBind };
                        _meta[newId] = meta;
                    }
                    catch { }
                }
                LoadPanels();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error pasting items: " + ex.Message);
            }
        }
    }
}
