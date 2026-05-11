using System;
using SAPbobsCOM;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Utils
{
    public static class SapUiSafe
    {
        public static SAPbouiCOM.Form TryGetForm(string formUid)
        {
            if (string.IsNullOrWhiteSpace(formUid)) return null;
            try { return B1App.Instance?.Application?.Forms.Item(formUid); }
            catch { return null; }
        }

        public static SAPbouiCOM.Form TryGetForm(int index)
        {
            if (index < 0) return null;
            try { return B1App.Instance?.Application?.Forms.Item(index); }
            catch { return null; }
        }

        public static SAPbouiCOM.Form TryGetActiveForm()
        {
            try { return B1App.Instance?.Application?.Forms.ActiveForm; }
            catch { return null; }
        }

        public static Item TryGetItem(SAPbouiCOM.Form form, string itemId)
        {
            if (form == null || string.IsNullOrWhiteSpace(itemId)) return null;
            try { return form.Items.Item(itemId); }
            catch { return null; }
        }

        public static Item TryGetItem(SAPbouiCOM.Form form, int index)
        {
            if (form == null || index < 0) return null;
            try { return form.Items.Item(index); }
            catch { return null; }
        }

        public static T TryGetSpecific<T>(SAPbouiCOM.Form form, string itemId) where T : class
        {
            return TryGetSpecific<T>(TryGetItem(form, itemId));
        }

        public static T TryGetSpecific<T>(Item item) where T : class
        {
            try { return item?.Specific as T; }
            catch { return null; }
        }

        public static object TryGetSpecificObject(Item item)
        {
            try { return item?.Specific; }
            catch { return null; }
        }

        public static bool TrySetVisible(SAPbouiCOM.Form form, string itemId, bool visible)
        {
            var item = TryGetItem(form, itemId);
            if (item == null) return false;
            try
            {
                item.Visible = visible;
                return true;
            }
            catch { return false; }
        }

        public static bool TrySetEnabled(SAPbouiCOM.Form form, string itemId, bool enabled)
        {
            var item = TryGetItem(form, itemId);
            if (item == null) return false;
            try
            {
                item.Enabled = enabled;
                return true;
            }
            catch { return false; }
        }

        public static string SafeComboValue(SAPbouiCOM.ComboBox combo)
        {
            try { return combo?.Selected?.Value ?? string.Empty; }
            catch { return string.Empty; }
        }

        public static string SafeComboDescription(SAPbouiCOM.ComboBox combo)
        {
            try { return combo?.Selected?.Description ?? string.Empty; }
            catch { return string.Empty; }
        }

        public static string SafeItemValue(Item item)
        {
            if (item == null) return string.Empty;
            var editText = TryGetSpecific<EditText>(item);
            if (editText != null) return editText.Value ?? string.Empty;

            var combo = TryGetSpecific<SAPbouiCOM.ComboBox>(item);
            if (combo != null) return SafeComboValue(combo);

            var checkBox = TryGetSpecific<SAPbouiCOM.CheckBox>(item);
            if (checkBox != null) return checkBox.Checked ? "Y" : string.Empty;

            var staticText = TryGetSpecific<StaticText>(item);
            if (staticText != null) return staticText.Caption ?? string.Empty;

            return string.Empty;
        }

        public static bool TrySetCaption(Item item, string caption)
        {
            if (item == null) return false;
            var button = TryGetSpecific<SAPbouiCOM.Button>(item);
            if (button != null)
            {
                button.Caption = caption ?? string.Empty;
                return true;
            }

            var staticText = TryGetSpecific<StaticText>(item);
            if (staticText != null)
            {
                staticText.Caption = caption ?? string.Empty;
                return true;
            }

            var folder = TryGetSpecific<Folder>(item);
            if (folder != null)
            {
                folder.Caption = caption ?? string.Empty;
                return true;
            }

            return false;
        }

        public static bool TrySetEditValue(Item item, string value)
        {
            var editText = TryGetSpecific<EditText>(item);
            if (editText == null) return false;

            editText.Value = value ?? string.Empty;
            return true;
        }

        public static string SafeCaption(Item item)
        {
            if (item == null) return string.Empty;
            var editText = TryGetSpecific<EditText>(item);
            if (editText != null) return editText.Value ?? item.UniqueID;

            var staticText = TryGetSpecific<StaticText>(item);
            if (staticText != null) return staticText.Caption ?? item.UniqueID;

            var button = TryGetSpecific<SAPbouiCOM.Button>(item);
            if (button != null) return button.Caption ?? item.UniqueID;

            var folder = TryGetSpecific<Folder>(item);
            if (folder != null) return folder.Caption ?? item.UniqueID;

            return SafeSpecificProperty(item, "Caption") ?? item.UniqueID;
        }

        public static string SafeSpecificProperty(Item item, string propertyName)
        {
            if (item == null || string.IsNullOrWhiteSpace(propertyName)) return string.Empty;
            try
            {
                var specific = TryGetSpecificObject(item);
                var prop = specific?.GetType().GetProperty(propertyName);
                return prop?.GetValue(specific)?.ToString() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        public static bool TrySetSpecificProperty(Item item, string propertyName, object value)
        {
            if (item == null || string.IsNullOrWhiteSpace(propertyName)) return false;
            try
            {
                var specific = TryGetSpecificObject(item);
                var prop = specific?.GetType().GetProperty(propertyName);
                if (prop == null) return false;

                prop.SetValue(specific, value);
                return true;
            }
            catch { return false; }
        }

        public static string SafeMatrixCell(Matrix matrix, string colId, int row)
        {
            if (matrix == null || string.IsNullOrWhiteSpace(colId) || row < 1) return string.Empty;
            try
            {
                var cell = matrix.Columns.Item(colId).Cells.Item(row);
                object specific = cell?.Specific;
                if (specific is EditText editText) return editText.Value ?? string.Empty;
                if (specific is SAPbouiCOM.ComboBox combo) return SafeComboValue(combo);
                if (specific is SAPbouiCOM.CheckBox checkBox) return checkBox.Checked ? "Y" : string.Empty;
                if (specific is StaticText staticText) return staticText.Caption ?? string.Empty;
            }
            catch { }

            return string.Empty;
        }

        public static int SafeSelectedMatrixRow(Matrix matrix)
        {
            if (matrix == null) return -1;
            try
            {
                int row = matrix.GetNextSelectedRow(0, BoOrderType.ot_SelectionOrder);
                return row > 0 ? row : -1;
            }
            catch { return -1; }
        }

        public static string DescribeForm(SAPbouiCOM.Form form)
        {
            if (form == null) return "N/A";
            try { return $"{form.TypeEx}|{form.UniqueID}|{form.Title}"; }
            catch { return form.TypeEx ?? "N/A"; }
        }

        public static string DescribeItem(SAPbouiCOM.Form form, string itemId)
        {
            var item = TryGetItem(form, itemId);
            if (item == null) return itemId ?? string.Empty;
            try { return $"{item.UniqueID}|{item.Type}|{SafeCaption(item)}"; }
            catch { return itemId ?? string.Empty; }
        }

        public static bool TrySetMatrixCell(Matrix matrix, string colId, int row, string value)
        {
            if (matrix == null || string.IsNullOrWhiteSpace(colId) || row < 1) return false;
            try
            {
                object specific = matrix.Columns.Item(colId).Cells.Item(row).Specific;
                if (specific is EditText editText)
                {
                    editText.Value = value ?? string.Empty;
                    return true;
                }
                if (specific is SAPbouiCOM.ComboBox combo)
                {
                    try
                    {
                        combo.Select(value ?? string.Empty, BoSearchKey.psk_ByValue);
                        return true;
                    }
                    catch { return false; }
                }
                if (specific is SAPbouiCOM.CheckBox checkBox)
                {
                    string normalized = (value ?? string.Empty).Trim();
                    checkBox.Checked = normalized.Equals("Y", StringComparison.OrdinalIgnoreCase)
                                       || normalized.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                                       || normalized == "1";
                    return true;
                }
            }
            catch { }

            return false;
        }

        public static string SafeField(Recordset rs, string fieldName)
        {
            if (rs == null || string.IsNullOrWhiteSpace(fieldName)) return string.Empty;
            try { return rs.Fields.Item(fieldName).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }

        public static string SafeField(Recordset rs, string fieldName, string fallback)
        {
            var value = SafeField(rs, fieldName);
            return string.IsNullOrEmpty(value) ? fallback ?? string.Empty : value;
        }

        public static object SafeFieldValue(Recordset rs, string fieldName)
        {
            if (rs == null || string.IsNullOrWhiteSpace(fieldName)) return null;
            try { return rs.Fields.Item(fieldName).Value; }
            catch { return null; }
        }

        public static object SafeFieldValue(Recordset rs, string fieldName, object fallback)
        {
            return SafeFieldValue(rs, fieldName) ?? fallback;
        }

        public static string SafeField(Recordset rs, int index)
        {
            if (rs == null || index < 0) return string.Empty;
            try { return rs.Fields.Item(index).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }

        public static string SafeField(Recordset rs, int index, string fallback)
        {
            var value = SafeField(rs, index);
            return string.IsNullOrEmpty(value) ? fallback ?? string.Empty : value;
        }

        public static object SafeFieldValue(Recordset rs, int index)
        {
            if (rs == null || index < 0) return null;
            try { return rs.Fields.Item(index).Value; }
            catch { return null; }
        }

        public static object SafeFieldValue(Recordset rs, int index, object fallback)
        {
            return SafeFieldValue(rs, index) ?? fallback;
        }

        public static string SafeFieldName(Recordset rs, int index)
        {
            if (rs == null || index < 0) return string.Empty;
            try { return rs.Fields.Item(index).Name ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
