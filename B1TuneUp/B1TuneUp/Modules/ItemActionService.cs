using System;
using System.Collections.Generic;
using System.Globalization;
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
                    ? "SELECT \"Code\",\"U_FormType\",\"U_ItemID\",\"U_Event\",\"U_Action\" FROM \"@BTUN_ITEMACT\" ORDER BY \"U_FormType\",\"U_ItemID\""
                    : "SELECT [Code],U_FormType,U_ItemID,U_Event,U_Action FROM [@BTUN_ITEMACT] ORDER BY U_FormType,U_ItemID";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new ItemActionEntry
                    {
                        DocEntry = Convert.ToInt32(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 0)),
                        FormType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1),
                        ItemId = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Event = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3) ?? "Click",
                        Action = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4)
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
            string codeValue = entry.DocEntry > 0 ? entry.DocEntry.ToString(CultureInfo.InvariantCulture) : null;
            if (entry.DocEntry <= 0)
            {
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_ITEMACT");
                entry.DocEntry = nextCode;
                codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = Escape($"{entry.FormType}_{entry.ItemId}_{entry.Event}".Trim());
                if (string.IsNullOrEmpty(nameValue))
                {
                    nameValue = $"ITEMACT_{codeValue}";
                }
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_ITEMACT\" (\"Code\",\"Name\",\"U_FormType\",\"U_ItemID\",\"U_Event\",\"U_Action\",\"U_CreatedAt\",\"U_UpdatedAt\") VALUES ('{codeValue}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.ItemId)}','{Escape(entry.Event)}','{Escape(entry.Action)}',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)"
                    : $"INSERT INTO [@BTUN_ITEMACT] ([Code],[Name],U_FormType,U_ItemID,U_Event,U_Action,U_CreatedAt,U_UpdatedAt) VALUES ('{codeValue}','{nameValue}','{Escape(entry.FormType)}','{Escape(entry.ItemId)}','{Escape(entry.Event)}','{Escape(entry.Action)}',GETDATE(),GETDATE())";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_ITEMACT\" SET \"U_FormType\"='{Escape(entry.FormType)}',\"U_ItemID\"='{Escape(entry.ItemId)}',\"U_Event\"='{Escape(entry.Event)}',\"U_Action\"='{Escape(entry.Action)}',\"U_UpdatedAt\"=CURRENT_TIMESTAMP WHERE \"Code\"='{codeValue}'"
                    : $"UPDATE [@BTUN_ITEMACT] SET U_FormType='{Escape(entry.FormType)}',U_ItemID='{Escape(entry.ItemId)}',U_Event='{Escape(entry.Event)}',U_Action='{Escape(entry.Action)}',U_UpdatedAt=GETDATE() WHERE [Code]='{codeValue}'";
                ExecuteNonQuery(updateSql);
            }
            return entry;
        }

        public static void Delete(int docEntry)
        {
            if (docEntry <= 0) return;
            bool isHana = B1App.Instance.IsHana;
            string codeValue = docEntry.ToString(CultureInfo.InvariantCulture);
            string sql = isHana
                ? $"DELETE FROM \"@BTUN_ITEMACT\" WHERE \"Code\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_ITEMACT] WHERE [Code]='{codeValue}'";
            ExecuteNonQuery(sql);
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
