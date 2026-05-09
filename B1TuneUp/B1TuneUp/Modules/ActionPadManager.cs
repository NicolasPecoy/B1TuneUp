using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ActionPadManager
    {
        private static readonly Dictionary<string, string> _buttonActions = new Dictionary<string, string>();

        public static void ShowPadForForm(SAPbouiCOM.Form oForm)
        {
            if (oForm == null) return;
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string safeFormType = (oForm.TypeEx ?? string.Empty).Replace("'", "''");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_PAD\" WHERE \"U_FormType\" = '{safeFormType}'"
                    : $"SELECT * FROM [@BTUN_PAD] WHERE [U_FormType] = '{safeFormType}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string padEntry = SapUiSafe.SafeField(rs, "Code");
                    var config = new PadConfig
                    {
                        Title = SapUiSafe.SafeField(rs, "U_Title"),
                        Position = SapUiSafe.SafeField(rs, "U_Position"),
                        Columns = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Columns"), 1),
                        ButtonWidth = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_BtnWidth"), 120),
                        ButtonHeight = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_BtnHeight"), 22),
                        DockMode = SapUiSafe.SafeField(rs, "U_DockMode"),
                        FollowForm = string.Equals(SapUiSafe.SafeField(rs, "U_FollowForm"), "Y", StringComparison.OrdinalIgnoreCase)
                    };

                    CreatePadForm(padEntry, config, oForm);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en Action Pad: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void CreatePadForm(string padEntry, PadConfig config, SAPbouiCOM.Form parentForm)
        {
            string formUID = $"PAD_{padEntry}_{parentForm.Title}";
            try
            {
                if (B1App.Instance.Application.Forms.Count > 0)
                {
                    try { var f = SapUiSafe.TryGetForm(formUID); if (f != null) { f.Select(); return; } } catch { }
                }

                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_PAD";
                fcp.UniqueID = formUID;
                fcp.BorderStyle = BoFormBorderStyle.fbs_Fixed;

                SAPbouiCOM.Form padForm = B1App.Instance.Application.Forms.AddEx(fcp);
                padForm.Title = config.Title;
                padForm.Width = config.EstimateWidth();

                // Posicionamiento lateral
                if (config.Position == "Right")
                {
                    padForm.Left = parentForm.Left + parentForm.Width + 5;
                    padForm.Top = parentForm.Top;
                }
                else
                {
                    padForm.Left = parentForm.Left - padForm.Width - 5;
                    padForm.Top = parentForm.Top;
                }
                AddButtons(padForm, padEntry, config);
                padForm.Visible = true;
            }
            catch { }
        }

        private static void AddButtons(SAPbouiCOM.Form padForm, string padEntry, PadConfig config)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\" = '{padEntry}' ORDER BY \"U_Order\" ASC"
                    : $"SELECT * FROM [@BTUN_PADB] WHERE [U_PadEntry] = '{padEntry}' ORDER BY [U_Order] ASC";

                rs.DoQuery(sql);
                int index = 0;
                int maxBottom = 0;
                while (!rs.EoF)
                {
                    string label = SapUiSafe.SafeField(rs, "U_Label");
                    string action = SapUiSafe.SafeField(rs, "U_Action");
                    string tooltip = SapUiSafe.SafeField(rs, "U_Tooltip");
                    string colorHex = SapUiSafe.SafeField(rs, "U_Color");
                    int explicitRow = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_GridRow"), -1);
                    int explicitCol = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_GridCol"), -1);
                    string btnId = $"btn_{SapUiSafe.SafeField(rs, "Code")}";

                    var (left, top) = CalculateButtonPosition(config, index, explicitRow, explicitCol);

                    Item item = padForm.Items.Add(btnId, BoFormItemTypes.it_BUTTON);
                    item.Top = top;
                    item.Left = left;
                    item.Width = config.ButtonWidth;
                    item.Height = config.ButtonHeight;
                    if (!string.IsNullOrEmpty(colorHex))
                    {
                        int color = ParseColor(colorHex);
                        if (color > 0)
                        {
                            try { item.BackColor = color; } catch { }
                        }
                    }

                    SAPbouiCOM.Button btn = SapUiSafe.TryGetSpecific<SAPbouiCOM.Button>(item);
                    if (btn == null)
                    {
                        rs.MoveNext();
                        continue;
                    }
                    btn.Caption = label;
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        item.Description = tooltip;
                    }

                    _buttonActions[$"{padForm.UniqueID}_{btnId}"] = action;

                    maxBottom = Math.Max(maxBottom, top + config.ButtonHeight);
                    index++;
                    rs.MoveNext();
                }
                padForm.Height = maxBottom + config.Padding * 2;
                padForm.Width = config.EstimateWidth();
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void HandleItemEvent(string formUID, ItemEvent pVal)
        {
            if (pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction)
            {
                string key = $"{formUID}_{pVal.ItemUID}";
                if (_buttonActions.ContainsKey(key))
                {
                    MacroEngine.ExecuteMacro(_buttonActions[key]);
                }
            }
        }

        private static (int Left, int Top) CalculateButtonPosition(PadConfig config, int index, int explicitRow, int explicitCol)
        {
            int columns = Math.Max(1, config.Columns);
            int row = explicitRow >= 0 ? explicitRow : index / columns;
            int col = explicitCol >= 0 ? explicitCol : index % columns;

            int left = config.Padding + col * (config.ButtonWidth + config.HSpacing);
            int top = config.Padding + row * (config.ButtonHeight + config.VSpacing);
            return (left, top);
        }

        private static int SafeInt(object value, int fallback)
        {
            try
            {
                if (value == null) return fallback;
                if (int.TryParse(value.ToString(), out var parsed)) return parsed;
                double dbl;
                if (double.TryParse(value.ToString(), out dbl)) return Convert.ToInt32(dbl);
                return fallback;
            }
            catch { return fallback; }
        }

        private static int ParseColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return -1;
            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var color))
            {
                // SAP uses BGR order
                int r = (color >> 16) & 0xFF;
                int g = (color >> 8) & 0xFF;
                int b = color & 0xFF;
                return (b << 16) | (g << 8) | r;
            }
            return -1;
        }

        private class PadConfig
        {
            public string Title { get; set; }
            public string Position { get; set; }
            public int Columns { get; set; } = 1;
            public int ButtonWidth { get; set; } = 120;
            public int ButtonHeight { get; set; } = 22;
            public string DockMode { get; set; }
            public bool FollowForm { get; set; }
            public int Padding { get; } = 10;
            public int HSpacing { get; } = 6;
            public int VSpacing { get; } = 6;

            public int EstimateWidth()
            {
                int columns = Math.Max(1, Columns);
                return Padding * 2 + columns * ButtonWidth + (columns - 1) * HSpacing;
            }
        }
    }
}
