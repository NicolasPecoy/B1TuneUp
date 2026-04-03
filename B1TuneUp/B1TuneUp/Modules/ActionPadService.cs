using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

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
                    ? "SELECT \"DocEntry\",\"U_FormType\",\"U_Title\",\"U_Position\" FROM \"@BTUN_PAD\" ORDER BY \"DocEntry\""
                    : "SELECT DocEntry,U_FormType,U_Title,U_Position FROM [@BTUN_PAD] ORDER BY DocEntry";
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
            if (entry.DocEntry <= 0)
            {
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_PAD\" (\"U_FormType\",\"U_Title\",\"U_Position\") VALUES ('{Escape(entry.FormType)}','{Escape(entry.Title)}','{Escape(entry.Position)}')"
                    : $"INSERT INTO [@BTUN_PAD] (U_FormType,U_Title,U_Position) VALUES ('{Escape(entry.FormType)}','{Escape(entry.Title)}','{Escape(entry.Position)}')";
                ExecuteNonQuery(insertSql);
                entry.DocEntry = GetLastPadDocEntry();
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_PAD\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_Title\"='{Escape(entry.Title)}',\"U_Position\"='{Escape(entry.Position)}' WHERE \"DocEntry\"={entry.DocEntry}"
                    : $"UPDATE [@BTUN_PAD] SET U_FormType='{Escape(entry.FormType)}',U_Title='{Escape(entry.Title)}',U_Position='{Escape(entry.Position)}' WHERE DocEntry={entry.DocEntry}";
                ExecuteNonQuery(updateSql);
            }

            // Replace child buttons
            string deleteSql = isHana
                ? $"DELETE FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"={entry.DocEntry}"
                : $"DELETE FROM [@BTUN_PADB] WHERE U_PadEntry={entry.DocEntry}";
            ExecuteNonQuery(deleteSql);

            foreach (var button in entry.Buttons)
            {
                string insertButton = isHana
                    ? $"INSERT INTO \"@BTUN_PADB\" (\"U_PadEntry\",\"U_Label\",\"U_Action\",\"U_Order\") VALUES ({entry.DocEntry},'{Escape(button.Label)}','{Escape(button.Action)}',{button.Order})"
                    : $"INSERT INTO [@BTUN_PADB] (U_PadEntry,U_Label,U_Action,U_Order) VALUES ({entry.DocEntry},'{Escape(button.Label)}','{Escape(button.Action)}',{button.Order})";
                ExecuteNonQuery(insertButton);
            }

            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string deleteChildren = isHana
                ? $"DELETE FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"={docEntry}"
                : $"DELETE FROM [@BTUN_PADB] WHERE U_PadEntry={docEntry}";
            ExecuteNonQuery(deleteChildren);

            string deletePad = isHana
                ? $"DELETE FROM \"@BTUN_PAD\" WHERE \"DocEntry\"={docEntry}"
                : $"DELETE FROM [@BTUN_PAD] WHERE DocEntry={docEntry}";
            ExecuteNonQuery(deletePad);
        }

        private static IEnumerable<ActionPadButtonEntry> GetButtons(int docEntry)
        {
            var list = new List<ActionPadButtonEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? $"SELECT \"DocEntry\",\"U_Label\",\"U_Action\",\"U_Order\" FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\"={docEntry} ORDER BY \"U_Order\""
                    : $"SELECT DocEntry,U_Label,U_Action,U_Order FROM [@BTUN_PADB] WHERE U_PadEntry={docEntry} ORDER BY U_Order";
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

        private static int GetLastPadDocEntry()
        {
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana ? "SELECT MAX(\"DocEntry\") FROM \"@BTUN_PAD\"" : "SELECT MAX(DocEntry) FROM [@BTUN_PAD]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return Convert.ToInt32(rs.Fields.Item(0).Value);
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return 0;
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
