using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ItemActionService
    {
        public static IList<ItemActionEntry> GetAll()
        {
            var list = new List<ItemActionEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"DocEntry\",\"U_FormType\",\"U_ItemID\",\"U_Event\",\"U_Action\" FROM \"@BTUN_ITEMACT\" ORDER BY \"U_FormType\",\"U_ItemID\""
                    : "SELECT DocEntry,U_FormType,U_ItemID,U_Event,U_Action FROM [@BTUN_ITEMACT] ORDER BY U_FormType,U_ItemID";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new ItemActionEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        FormType = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        ItemId = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        Event = rs.Fields.Item(3).Value?.ToString() ?? "Click",
                        Action = rs.Fields.Item(4).Value?.ToString() ?? string.Empty
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

        public static ItemActionEntry Save(ItemActionEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            bool isHana = B1App.Instance.IsHana;
            if (entry.DocEntry <= 0)
            {
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_ITEMACT\" (\"U_FormType\",\"U_ItemID\",\"U_Event\",\"U_Action\",\"U_CreatedAt\",\"U_UpdatedAt\") VALUES ('{Escape(entry.FormType)}','{Escape(entry.ItemId)}','{Escape(entry.Event)}','{Escape(entry.Action)}',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)"
                    : $"INSERT INTO [@BTUN_ITEMACT] (U_FormType,U_ItemID,U_Event,U_Action,U_CreatedAt,U_UpdatedAt) VALUES ('{Escape(entry.FormType)}','{Escape(entry.ItemId)}','{Escape(entry.Event)}','{Escape(entry.Action)}',GETDATE(),GETDATE())";
                ExecuteNonQuery(insertSql);
                entry.DocEntry = GetLastDocEntry();
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_ITEMACT\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_ItemID\"='{Escape(entry.ItemId)}',\"U_Event\"='{Escape(entry.Event)}',\"U_Action\"='{Escape(entry.Action)}',\"U_UpdatedAt\"=CURRENT_TIMESTAMP WHERE \"DocEntry\"={entry.DocEntry}"
                    : $"UPDATE [@BTUN_ITEMACT] SET U_FormType='{Escape(entry.FormType)}',U_ItemID='{Escape(entry.ItemId)}',U_Event='{Escape(entry.Event)}',U_Action='{Escape(entry.Action)}',U_UpdatedAt=GETDATE() WHERE DocEntry={entry.DocEntry}";
                ExecuteNonQuery(updateSql);
            }
            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string sql = isHana
                ? $"DELETE FROM \"@BTUN_ITEMACT\" WHERE \"DocEntry\"={docEntry}"
                : $"DELETE FROM [@BTUN_ITEMACT] WHERE DocEntry={docEntry}";
            ExecuteNonQuery(sql);
        }

        private static int GetLastDocEntry()
        {
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana ? "SELECT MAX(\"DocEntry\") FROM \"@BTUN_ITEMACT\"" : "SELECT MAX(DocEntry) FROM [@BTUN_ITEMACT]";
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
