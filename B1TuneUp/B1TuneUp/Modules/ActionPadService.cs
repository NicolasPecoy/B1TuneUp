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
                    ? "SELECT \"Code\",\"U_FormType\",\"U_Title\",\"U_Position\" FROM \"@BTUN_PAD\" ORDER BY \"U_Position\""
                    : "SELECT [Code],U_FormType,U_Title,U_Position FROM [@BTUN_PAD] ORDER BY U_Position";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var pad = new ActionPadEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        FormType = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        Title = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        Position = rs.Fields.Item(3).Value?.ToString() ?? "Right"
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
                    ? $"INSERT INTO \"@BTUN_PAD\" (\"Code\",\"Name\",\"U_FormType\",\"U_Title\",\"U_Position\") VALUES ('{padCode}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.Title)}','{Escape(entry.Position)}')"
                    : $"INSERT INTO [@BTUN_PAD] ([Code],[Name],U_FormType,U_Title,U_Position) VALUES ('{padCode}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.Title)}','{Escape(entry.Position)}')";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_PAD\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_Title\"='{Escape(entry.Title)}',\"U_Position\"='{Escape(entry.Position)}' WHERE \"Code\"='{padCode}'"
                    : $"UPDATE [@BTUN_PAD] SET U_FormType='{Escape(entry.FormType)}',U_Title='{Escape(entry.Title)}',U_Position='{Escape(entry.Position)}' WHERE [Code]='{padCode}'";
                ExecuteNonQuery(updateSql);
            }

            string deleteSql = isHana
                ? $"DELETE FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"='{padCode}'"
                : $"DELETE FROM [@BTUN_PADB] WHERE U_PadEntry='{padCode}'";
            ExecuteNonQuery(deleteSql);

            foreach (var button in entry.Buttons)
            {
                int buttonCode = UserTableCodeGenerator.GetNext("@BTUN_PADB");
                button.DocEntry = buttonCode;
                string buttonCodeValue = buttonCode.ToString(CultureInfo.InvariantCulture);
                string buttonName = $"PADBTN_{padCode}_{buttonCodeValue}";
                string insertButton = isHana
                    ? $"INSERT INTO \"@BTUN_PADB\" (\"Code\",\"Name\",\"U_PadEntry\",\"U_Label\",\"U_Action\",\"U_Order\") VALUES ('{buttonCodeValue}','{buttonName}','{padCode}','{Escape(button.Label)}','{Escape(button.Action)}',{button.Order})"
                    : $"INSERT INTO [@BTUN_PADB] ([Code],[Name],U_PadEntry,U_Label,U_Action,U_Order) VALUES ('{buttonCodeValue}','{buttonName}','{padCode}','{Escape(button.Label)}','{Escape(button.Action)}',{button.Order})";
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
                    ? $"SELECT \"Code\",\"U_Label\",\"U_Action\",\"U_Order\" FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"='{codeValue}' ORDER BY \"U_Order\""
                    : $"SELECT [Code],U_Label,U_Action,U_Order FROM [@BTUN_PADB] WHERE U_PadEntry='{codeValue}' ORDER BY U_Order";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new ActionPadButtonEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        PadEntry = docEntry,
                        Label = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        Action = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        Order = Convert.ToInt32(rs.Fields.Item(3).Value)
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

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");
    }
}
