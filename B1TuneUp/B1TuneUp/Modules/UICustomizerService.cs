using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class UICustomizerService
    {
        private const string TableName = "@BTUN_UI";

        public static IList<UiCustomizationEntry> GetAll(string formType = null)
        {
            var list = new List<UiCustomizationEntry>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string filter = string.IsNullOrWhiteSpace(formType) ? "" : (B1App.Instance.IsHana ? $" WHERE \"U_FormType\" = '{formType.Replace("'", "''")}'" : $" WHERE [U_FormType] = '{formType.Replace("'", "''")}'");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"Code\",\"Name\",\"U_FormType\",\"U_ItemID\",\"U_Action\",\"U_Top\",\"U_Left\",\"U_Width\",\"U_Height\",\"U_Label\",\"U_FromPane\",\"U_ToPane\",\"UpdateDate\" FROM \"{TableName}\"{filter} ORDER BY \"U_FormType\",\"Code\""
                    : $"SELECT [Code],[Name],[U_FormType],[U_ItemID],[U_Action],[U_Top],[U_Left],[U_Width],[U_Height],[U_Label],[U_FromPane],[U_ToPane],[UpdateDate] FROM [{TableName}]{filter} ORDER BY [U_FormType],[Code]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(MapEntry(rs));
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        private static UiCustomizationEntry MapEntry(Recordset rs)
        {
            return new UiCustomizationEntry
            {
                Code = Convert.ToString(rs.Fields.Item(0).Value),
                Name = Convert.ToString(rs.Fields.Item(1).Value),
                FormType = Convert.ToString(rs.Fields.Item(2).Value),
                ItemId = Convert.ToString(rs.Fields.Item(3).Value),
                Action = Convert.ToString(rs.Fields.Item(4).Value),
                Top = ToNullableInt(rs.Fields.Item(5).Value),
                Left = ToNullableInt(rs.Fields.Item(6).Value),
                Width = ToNullableInt(rs.Fields.Item(7).Value),
                Height = ToNullableInt(rs.Fields.Item(8).Value),
                Label = Convert.ToString(rs.Fields.Item(9).Value),
                FromPane = ToNullableInt(rs.Fields.Item(10).Value),
                ToPane = ToNullableInt(rs.Fields.Item(11).Value),
                UpdatedAt = ToNullableDate(rs.Fields.Item(12).Value)
            };
        }

        public static UiCustomizationEntry Save(UiCustomizationEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_UI");
                bool exists = !string.IsNullOrEmpty(entry.Code) && table.GetByKey(entry.Code);
                if (!exists)
                {
                    entry.Code = string.IsNullOrWhiteSpace(entry.Code) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : entry.Code;
                    table.Code = entry.Code;
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.DisplayName : entry.Name;
                }
                else
                {
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.DisplayName : entry.Name;
                }

                table.UserFields.Fields.Item("U_FormType").Value = entry.FormType ?? string.Empty;
                table.UserFields.Fields.Item("U_ItemID").Value = entry.ItemId ?? string.Empty;
                table.UserFields.Fields.Item("U_Action").Value = entry.Action ?? "Hide";
                table.UserFields.Fields.Item("U_Top").Value = entry.Top ?? 0;
                table.UserFields.Fields.Item("U_Left").Value = entry.Left ?? 0;
                table.UserFields.Fields.Item("U_Width").Value = entry.Width ?? 0;
                table.UserFields.Fields.Item("U_Height").Value = entry.Height ?? 0;
                table.UserFields.Fields.Item("U_Label").Value = entry.Label ?? string.Empty;
                table.UserFields.Fields.Item("U_FromPane").Value = entry.FromPane ?? 0;
                table.UserFields.Fields.Item("U_ToPane").Value = entry.ToPane ?? 0;

                int res = exists ? table.Update() : table.Add();
                if (res != 0)
                {
                    string err = B1App.Instance.Company.GetLastErrorDescription();
                    throw new InvalidOperationException($"SAP SDK error: {err}");
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }
            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_UI");
                if (table.GetByKey(code))
                {
                    int res = table.Remove();
                    if (res != 0)
                    {
                        string err = B1App.Instance.Company.GetLastErrorDescription();
                        throw new InvalidOperationException($"SAP SDK error: {err}");
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }
        }

        public static IReadOnlyList<string> GetDistinctFormTypes()
        {
            var list = new List<string>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT DISTINCT \"U_FormType\" FROM \"@BTUN_UI\" ORDER BY \"U_FormType\""
                    : "SELECT DISTINCT [U_FormType] FROM [@BTUN_UI] ORDER BY [U_FormType]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var ft = Convert.ToString(rs.Fields.Item(0).Value);
                    if (!string.IsNullOrWhiteSpace(ft)) list.Add(ft);
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        public static void RefreshActiveForm()
        {
            try
            {
                var form = B1App.Instance.Application.Forms.ActiveForm;
                if (form != null)
                {
                    UICustomizer.ApplyCustomization(form);
                    form.Update();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error refrescando formulario: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        public static void OpenItemPlacement()
        {
            try
            {
                var form = B1App.Instance.Application.Forms.ActiveForm;
                ItemPlacementManager.OpenPlacementForm(form);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Item Placement no disponible: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null) return null;
            if (int.TryParse(value.ToString(), out int result)) return result;
            return null;
        }

        private static DateTime? ToNullableDate(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out DateTime dt)) return dt;
            return null;
        }
    }
}
