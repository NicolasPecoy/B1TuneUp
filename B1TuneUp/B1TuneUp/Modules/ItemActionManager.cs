using System;
using System.Collections.Generic;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbouiCOM;
using System.Globalization;

namespace B1TuneUp.Modules
{
    public static class ItemActionManager
    {


        public static bool SaveAction(string formType, string itemId, string actionMacro)
        {
            if (string.IsNullOrEmpty(formType) || string.IsNullOrEmpty(itemId)) return false;
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                bool isHana = B1App.Instance.IsHana;
                string table = isHana ? "\"@BTUN_ITEMACT\"" : "[@BTUN_ITEMACT]";
                string safeForm = formType.Replace("'", "''");
                string safeItem = itemId.Replace("'", "''");

                string act = (actionMacro ?? string.Empty).Replace("'", "''");
                string eventType = "Change";
                try
                {
                    if (!string.IsNullOrEmpty(actionMacro))
                    {
                        var parts = actionMacro.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            eventType = parts[0];
                            act = parts[1].Replace("'", "''");
                        }
                    }
                }
                catch { }

                string safeEvent = eventType.Replace("'", "''");
                string checkSql = isHana
                    ? $"SELECT \"Code\" FROM {table} WHERE \"U_FormType\"='{safeForm}' AND \"U_ItemID\"='{safeItem}' AND \"U_Event\"='{safeEvent}'"
                    : $"SELECT [Code] FROM {table} WHERE [U_FormType]='{safeForm}' AND [U_ItemID]='{safeItem}' AND [U_Event]='{safeEvent}'";
                rs.DoQuery(checkSql);

                if (!rs.EoF)
                {
                    string codeValue = rs.Fields.Item(0).Value?.ToString() ?? string.Empty;
                    string updateSql = isHana
                        ? $"UPDATE {table} SET \"U_Action\"='{act}', \"U_UpdatedAt\"=CURRENT_TIMESTAMP WHERE \"Code\"='{codeValue}'"
                        : $"UPDATE {table} SET [U_Action]='{act}', [U_UpdatedAt]=GETDATE() WHERE [Code]='{codeValue}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_ITEMACT");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string name = $"{formType}_{itemId}_{eventType}".Replace("'", "''");
                    string insertSql = isHana
                        ? $"INSERT INTO {table} (\"Code\",\"Name\",\"U_FormType\",\"U_ItemID\",\"U_Action\",\"U_CreatedAt\",\"U_Event\") VALUES ('{codeValue}','{name}','{safeForm}','{safeItem}','{act}',CURRENT_TIMESTAMP,'{safeEvent}')"
                        : $"INSERT INTO {table} ([Code],[Name],[U_FormType],[U_ItemID],[U_Action],[U_CreatedAt],[U_Event]) VALUES ('{codeValue}','{name}','{safeForm}','{safeItem}','{act}',GETDATE(),'{safeEvent}')";
                    rs.DoQuery(insertSql);
                }
                B1App.Instance.Application.SetStatusBarMessage($"Action saved for {itemId}", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                try
                {
                    for (int i = 0; i < B1App.Instance.Application.Forms.Count; i++)
                    {
                        var f = B1App.Instance.Application.Forms.Item(i);
                        if (f.TypeEx == formType)
                        {
                            EventDispatcher.Instance.UnregisterLocalItemChangeHandler(f, itemId);
                            EventDispatcher.Instance.RegisterLocalItemChangeHandler(f, itemId, (frm, id) =>
                            {
                                try
                                {
                                    var macro = GetAction(frm.TypeEx, id);
                                    if (!string.IsNullOrEmpty(macro)) MacroEngine.ExecuteMacro(macro, frm);
                                }
                                catch { }
                            });
                        }
                    }
                }
                catch { }
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving item action: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }
        public static string GetAction(string formType, string itemId)
        {
            if (string.IsNullOrEmpty(formType) || string.IsNullOrEmpty(itemId)) return "";
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Action\" FROM \"@BTUN_ITEMACT\" WHERE \"U_FormType\"='{formType.Replace("'","''")}' AND \"U_ItemID\"='{itemId.Replace("'","''")}'"
                    : $"SELECT U_Action FROM [@BTUN_ITEMACT] WHERE [U_FormType]='{formType.Replace("'","''")}' AND [U_ItemID]='{itemId.Replace("'","''")}'";
                rs.DoQuery(sql);
                if (!rs.EoF) return rs.Fields.Item(0).Value?.ToString() ?? "";
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return "";
        }

        public static string GetAction(string formType, string itemId, string eventType)
        {
            if (string.IsNullOrEmpty(formType) || string.IsNullOrEmpty(itemId)) return "";
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Action\" FROM \"@BTUN_ITEMACT\" WHERE \"U_FormType\"='{formType.Replace("'","''")}' AND \"U_ItemID\"='{itemId.Replace("'","''")}' AND \"U_Event\" = '{eventType.Replace("'","''")}'"
                    : $"SELECT U_Action FROM [@BTUN_ITEMACT] WHERE [U_FormType]='{formType.Replace("'","''")}' AND [U_ItemID]='{itemId.Replace("'","''")}' AND [U_Event] = '{eventType.Replace("'","''")}'";
                rs.DoQuery(sql);
                if (!rs.EoF) return rs.Fields.Item(0).Value?.ToString() ?? "";
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return "";
        }

        public static Dictionary<string, string> GetAllActions()
        {
            var dict = new Dictionary<string, string>();
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_FormType\", \"U_ItemID\", \"U_Action\" FROM \"@BTUN_ITEMACT\""
                    : "SELECT U_FormType, U_ItemID, U_Action FROM [@BTUN_ITEMACT]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string f = rs.Fields.Item(0).Value?.ToString() ?? "";
                    string it = rs.Fields.Item(1).Value?.ToString() ?? "";
                    string act = rs.Fields.Item(2).Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(f) && !string.IsNullOrEmpty(it)) dict[$"{f}|{it}"] = act;
                    rs.MoveNext();
                }
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return dict;
        }

        public static System.Data.DataTable GetAllActionsTable()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("FormType");
            dt.Columns.Add("ItemID");
            dt.Columns.Add("Action");
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_FormType\", \"U_ItemID\", \"U_Action\" FROM \"@BTUN_ITEMACT\" ORDER BY \"U_FormType\", \"U_ItemID\""
                    : "SELECT U_FormType, U_ItemID, U_Action FROM [@BTUN_ITEMACT] ORDER BY U_FormType, U_ItemID";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var row = dt.NewRow();
                    row[0] = rs.Fields.Item(0).Value?.ToString();
                    row[1] = rs.Fields.Item(1).Value?.ToString();
                    row[2] = rs.Fields.Item(2).Value?.ToString();
                    dt.Rows.Add(row);
                    rs.MoveNext();
                }
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return dt;
        }

        public static bool DeleteAction(string formType, string itemId)
        {
            if (string.IsNullOrEmpty(formType) || string.IsNullOrEmpty(itemId)) return false;
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string deleteSql = B1App.Instance.IsHana
                    ? $"DELETE FROM \"@BTUN_ITEMACT\" WHERE \"U_FormType\"='{formType.Replace("'","''")}' AND \"U_ItemID\"='{itemId.Replace("'","''")}'"
                    : $"DELETE FROM [@BTUN_ITEMACT] WHERE [U_FormType]='{formType.Replace("'","''")}' AND [U_ItemID]='{itemId.Replace("'","''")}'";
                rs.DoQuery(deleteSql);

                // Unregister handlers from open forms
                try
                {
                    for (int i = 0; i < B1App.Instance.Application.Forms.Count; i++)
                    {
                        var f = B1App.Instance.Application.Forms.Item(i);
                        if (f.TypeEx == formType)
                        {
                            EventDispatcher.Instance.UnregisterLocalItemChangeHandler(f, itemId);
                        }
                    }
                }
                catch { }

                B1App.Instance.Application.SetStatusBarMessage($"Action for {itemId} deleted.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error deleting action: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            finally { ComObjectManager.Release(rs); }
        }
    }
}
