using System;
using System.Collections.Generic;
using System.Globalization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class MenuConfigService
    {
        public static IList<MenuConfigEntry> GetAll()
        {
            var list = new List<MenuConfigEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"U_ParentID\",\"U_MenuID\",\"U_Name\",\"U_Position\",\"U_Action\" FROM \"@BTUN_MENUS\" ORDER BY \"U_Position\""
                    : "SELECT [Code],U_ParentID,U_MenuID,U_Name,U_Position,U_Action FROM [@BTUN_MENUS] ORDER BY U_Position";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new MenuConfigEntry
                    {
                        DocEntry = Convert.ToInt32(rs.Fields.Item(0).Value),
                        ParentId = rs.Fields.Item(1).Value?.ToString() ?? string.Empty,
                        MenuId = rs.Fields.Item(2).Value?.ToString() ?? string.Empty,
                        Caption = rs.Fields.Item(3).Value?.ToString() ?? string.Empty,
                        Position = Convert.ToInt32(rs.Fields.Item(4).Value),
                        Action = rs.Fields.Item(5).Value?.ToString() ?? string.Empty
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

        public static MenuConfigEntry Save(MenuConfigEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            bool isHana = B1App.Instance.IsHana;
            string codeValue = entry.DocEntry > 0
                ? entry.DocEntry.ToString(CultureInfo.InvariantCulture)
                : null;
            if (entry.DocEntry <= 0)
            {
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_MENUS");
                entry.DocEntry = nextCode;
                codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string name = $"MENU_{Esc(entry.MenuId ?? codeValue)}";
                string insertSql = isHana
                    ? $"INSERT INTO \"@BTUN_MENUS\" (\"Code\",\"Name\",\"U_ParentID\",\"U_MenuID\",\"U_Name\",\"U_Position\",\"U_Action\") VALUES ('{codeValue}','{name}','{Esc(entry.ParentId)}','{Esc(entry.MenuId)}','{Esc(entry.Caption)}',{entry.Position},'{Esc(entry.Action)}')"
                    : $"INSERT INTO [@BTUN_MENUS] ([Code],[Name],U_ParentID,U_MenuID,U_Name,U_Position,U_Action) VALUES ('{codeValue}','{name}','{Esc(entry.ParentId)}','{Esc(entry.MenuId)}','{Esc(entry.Caption)}',{entry.Position},'{Esc(entry.Action)}')";
                ExecuteNonQuery(insertSql);
            }
            else
            {
                string updateSql = isHana
                    ? $"UPDATE \"@BTUN_MENUS\" SET \"U_ParentID\"='{Esc(entry.ParentId)}',\"U_MenuID\"='{Esc(entry.MenuId)}',\"U_Name\"='{Esc(entry.Caption)}',\"U_Position\"={entry.Position},\"U_Action\"='{Esc(entry.Action)}' WHERE \"Code\"='{codeValue}'"
                    : $"UPDATE [@BTUN_MENUS] SET U_ParentID='{Esc(entry.ParentId)}',U_MenuID='{Esc(entry.MenuId)}',U_Name='{Esc(entry.Caption)}',U_Position={entry.Position},U_Action='{Esc(entry.Action)}' WHERE [Code]='{codeValue}'";
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
                ? $"DELETE FROM \"@BTUN_MENUS\" WHERE \"Code\"='{codeValue}'"
                : $"DELETE FROM [@BTUN_MENUS] WHERE [Code]='{codeValue}'";
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

        private static string Esc(string value) => (value ?? string.Empty).Replace("'", "''");
    }
}
