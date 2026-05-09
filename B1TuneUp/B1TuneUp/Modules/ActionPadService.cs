using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;
using System.Globalization;

namespace B1TuneUp.Modules
{
    public static class ActionPadService
    {
        public static IList<ActionPadEntry> GetAll()
        {
            var list = new List<ActionPadEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"U_FormType\",\"U_Title\",\"U_Position\",\"U_Columns\",\"U_BtnWidth\",\"U_BtnHeight\",\"U_DockMode\",\"U_FollowForm\" FROM \"@BTUN_PAD\" ORDER BY \"U_Position\""
                    : "SELECT [Code],U_FormType,U_Title,U_Position,U_Columns,U_BtnWidth,U_BtnHeight,U_DockMode,U_FollowForm FROM [@BTUN_PAD] ORDER BY U_Position";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var pad = new ActionPadEntry
                    {
                        DocEntry = Convert.ToInt32(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 0)),
                        FormType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1),
                        Title = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Position = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3) ?? "Right",
                        Columns = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Columns"), 1),
                        ButtonWidth = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_BtnWidth"), 120),
                        ButtonHeight = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_BtnHeight"), 22),
                        DockMode = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DockMode", "Floating"),
                        FollowForm = !string.Equals(B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_FollowForm"), "N", StringComparison.OrdinalIgnoreCase)
                    };
                    foreach (var button in GetButtons(pad.DocEntry))
                    {
                        pad.Buttons.Add(button);
                    }
                    list.Add(pad);
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        public static ActionPadEntry Save(ActionPadEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            bool isHana = B1App.Instance.IsHana;
            string padCode = entry.DocEntry > 0 ? entry.DocEntry.ToString(CultureInfo.InvariantCulture) : null;
            if (entry.DocEntry <= 0)
            {
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_PAD");
                entry.DocEntry = nextCode;
                padCode = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = $"PAD_{padCode}";
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_PAD\" (\"Code\",\"Name\",\"U_FormType\",\"U_Title\",\"U_Position\",\"U_Columns\",\"U_BtnWidth\",\"U_BtnHeight\",\"U_DockMode\",\"U_FollowForm\") VALUES ('{padCode}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.Title)}','{Escape(entry.Position)}',{entry.Columns},{entry.ButtonWidth},{entry.ButtonHeight},'{Escape(entry.DockMode)}','{(entry.FollowForm ? "Y" : "N")}')"
                    : $"INSERT INTO [@BTUN_PAD] ([Code],[Name],U_FormType,U_Title,U_Position,U_Columns,U_BtnWidth,U_BtnHeight,U_DockMode,U_FollowForm) VALUES ('{padCode}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.Title)}','{Escape(entry.Position)}',{entry.Columns},{entry.ButtonWidth},{entry.ButtonHeight},'{Escape(entry.DockMode)}','{(entry.FollowForm ? "Y" : "N")}')";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_PAD\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_Title\"='{Escape(entry.Title)}',\"U_Position\"='{Escape(entry.Position)}',\"U_Columns\"={entry.Columns},\"U_BtnWidth\"={entry.ButtonWidth},\"U_BtnHeight\"={entry.ButtonHeight},\"U_DockMode\"='{Escape(entry.DockMode)}',\"U_FollowForm\"='{(entry.FollowForm ? "Y" : "N")}' WHERE \"Code\"='{padCode}'"
                    : $"UPDATE [@BTUN_PAD] SET U_FormType='{Escape(entry.FormType)}',U_Title='{Escape(entry.Title)}',U_Position='{Escape(entry.Position)}',U_Columns={entry.Columns},U_BtnWidth={entry.ButtonWidth},U_BtnHeight={entry.ButtonHeight},U_DockMode='{Escape(entry.DockMode)}',U_FollowForm='{(entry.FollowForm ? "Y" : "N")}' WHERE [Code]='{padCode}'";
                ExecuteNonQuery(updateSql);
            }

            string deleteSql = isHana
                ? $"DELETE FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"='{padCode}'"
                : $"DELETE FROM [@BTUN_PADB] WHERE U_PadEntry='{padCode}'";
            ExecuteNonQuery(deleteSql);

            for (int i = 0; i < entry.Buttons.Count; i++)
            {
                var button = entry.Buttons[i];
                int buttonCode = UserTableCodeGenerator.GetNext("@BTUN_PADB");
                button.DocEntry = buttonCode;
                string buttonCodeValue = buttonCode.ToString(CultureInfo.InvariantCulture);
                string buttonName = $"PADBTN_{padCode}_{buttonCodeValue}";
                int order = button.Order > 0 ? button.Order : (i + 1) * 10;
                int gridRow = button.GridRow;
                int gridCol = button.GridCol;
                int colSpan = button.ColSpan <= 0 ? 1 : button.ColSpan;
                int rowSpan = button.RowSpan <= 0 ? 1 : button.RowSpan;
                string tooltip = Escape(button.Tooltip);
                string icon = Escape(button.Icon);
                string color = Escape(button.Color);
                string hotkey = Escape(button.HotKey);
                string insertButton = isHana
                    ? $"INSERT INTO \"@BTUN_PADB\" (\"Code\",\"Name\",\"U_PadEntry\",\"U_Label\",\"U_Action\",\"U_Order\",\"U_Tooltip\",\"U_Icon\",\"U_Color\",\"U_HotKey\",\"U_GridRow\",\"U_GridCol\",\"U_ColSpan\",\"U_RowSpan\",\"U_Left\",\"U_Top\",\"U_Width\",\"U_Height\") VALUES ('{buttonCodeValue}','{buttonName}','{padCode}','{Escape(button.Label)}','{Escape(button.Action)}',{order},'{tooltip}','{icon}','{color}','{hotkey}',{gridRow},{gridCol},{colSpan},{rowSpan},{SafeDouble(button.Left)},{SafeDouble(button.Top)},{SafeDouble(button.Width, entry.ButtonWidth)},{SafeDouble(button.Height, entry.ButtonHeight)})"
                    : $"INSERT INTO [@BTUN_PADB] ([Code],[Name],U_PadEntry,U_Label,U_Action,U_Order,U_Tooltip,U_Icon,U_Color,U_HotKey,U_GridRow,U_GridCol,U_ColSpan,U_RowSpan,U_Left,U_Top,U_Width,U_Height) VALUES ('{buttonCodeValue}','{buttonName}','{padCode}','{Escape(button.Label)}','{Escape(button.Action)}',{order},'{tooltip}','{icon}','{color}','{hotkey}',{gridRow},{gridCol},{colSpan},{rowSpan},{SafeDouble(button.Left)},{SafeDouble(button.Top)},{SafeDouble(button.Width, entry.ButtonWidth)},{SafeDouble(button.Height, entry.ButtonHeight)})";
                ExecuteNonQuery(insertButton);
            }

            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string codeValue = docEntry.ToString(CultureInfo.InvariantCulture);
            string deleteChildren = isHana
                ? $"DELETE FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_PADB] WHERE U_PadEntry='{codeValue}'";
            ExecuteNonQuery(deleteChildren);

            string deletePad = isHana
                ? $"DELETE FROM \"@BTUN_PAD\" WHERE \"Code\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_PAD] WHERE [Code]='{codeValue}'";
            ExecuteNonQuery(deletePad);
        }

        private static IEnumerable<ActionPadButtonEntry> GetButtons(int docEntry)
        {
            var list = new List<ActionPadButtonEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string codeValue = docEntry.ToString(CultureInfo.InvariantCulture);
                string sql = isHana
                    ? $"SELECT \"Code\",\"U_Label\",\"U_Action\",\"U_Order\",\"U_Tooltip\",\"U_Icon\",\"U_Color\",\"U_HotKey\",\"U_GridRow\",\"U_GridCol\",\"U_ColSpan\",\"U_RowSpan\",\"U_Left\",\"U_Top\",\"U_Width\",\"U_Height\" FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"='{codeValue}' ORDER BY \"U_Order\""
                    : $"SELECT [Code],U_Label,U_Action,U_Order,U_Tooltip,U_Icon,U_Color,U_HotKey,U_GridRow,U_GridCol,U_ColSpan,U_RowSpan,U_Left,U_Top,U_Width,U_Height FROM [@BTUN_PADB] WHERE U_PadEntry='{codeValue}' ORDER BY U_Order";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new ActionPadButtonEntry
                    {
                        DocEntry = Convert.ToInt32(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 0)),
                        PadEntry = docEntry,
                        Label = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1),
                        Action = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Order = Convert.ToInt32(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 3)),
                        Tooltip = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Tooltip"),
                        Icon = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Icon"),
                        Color = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Color"),
                        HotKey = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_HotKey"),
                        GridRow = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_GridRow"), -1),
                        GridCol = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_GridCol"), -1),
                        ColSpan = Math.Max(1, SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_ColSpan"), 1)),
                        RowSpan = Math.Max(1, SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_RowSpan"), 1)),
                        Left = SafeDouble(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Left"), 0),
                        Top = SafeDouble(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Top"), 0),
                        Width = SafeDouble(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Width"), 120),
                        Height = SafeDouble(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Height"), 22)
                    });
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        private static void ExecuteNonQuery(string sql)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static int SafeInt(object value, int fallback)
        {
            try
            {
                if (value == null || value == DBNull.Value) return fallback;
                if (int.TryParse(value.ToString(), out var parsed)) return parsed;
                if (double.TryParse(value.ToString(), out var dbl)) return Convert.ToInt32(dbl);
            }
            catch
            {
                // ignored
            }
            return fallback;
        }

        private static double SafeDouble(object value, double fallback = 0)
        {
            try
            {
                if (value == null || value == DBNull.Value) return fallback;
                if (double.TryParse(value.ToString(), out var parsed)) return parsed;
            }
            catch
            {
            }
            return fallback;
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");
    }
}
