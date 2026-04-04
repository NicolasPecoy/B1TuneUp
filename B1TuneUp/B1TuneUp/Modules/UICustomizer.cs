using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public class UICustomizer
    {
        public static void ApplyCustomization(SAPbouiCOM.Form oForm)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_UI\" WHERE \"U_FormType\" = '{oForm.TypeEx}' ORDER BY \"U_Priority\" ASC"
                    : $"SELECT * FROM [@BTUN_UI] WHERE [U_FormType] = '{oForm.TypeEx}' ORDER BY U_Priority ASC";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string action = rs.Fields.Item("U_Action").Value.ToString();
                    string itemId = rs.Fields.Item("U_ItemID").Value.ToString();
                    string userFilter = SafeString(rs.Fields.Item("U_UserCode")?.Value);
                    string groupFilter = SafeString(rs.Fields.Item("U_UserGroup")?.Value);
                    string conditionSql = SafeString(rs.Fields.Item("U_Condition")?.Value);

                    if (!MatchesCurrentUser(userFilter, groupFilter))
                    {
                        rs.MoveNext();
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(conditionSql) && !MacroEngine.CheckCondition(conditionSql, oForm))
                    {
                        rs.MoveNext();
                        continue;
                    }

                    try
                    {
                        if (action == "Hide")
                        {
                            oForm.Items.Item(itemId).Visible = false;
                        }
                        else if (action == "Move")
                        {
                            Item item = oForm.Items.Item(itemId);
                            item.Top = (int)rs.Fields.Item("U_Top").Value;
                            item.Left = (int)rs.Fields.Item("U_Left").Value;
                        }
                        else if (action == "Resize")
                        {
                            Item item = oForm.Items.Item(itemId);
                            item.Width = (int)rs.Fields.Item("U_Width").Value;
                            item.Height = (int)rs.Fields.Item("U_Height").Value;
                        }
                        else if (action == "ChangeLabel")
                        {
                            Item item = oForm.Items.Item(itemId);
                            if (item.Specific is StaticText lbl)
                            {
                                lbl.Caption = rs.Fields.Item("U_Label").Value.ToString();
                            }
                            else if (item.Specific is EditText txt)
                            {
                                txt.Value = rs.Fields.Item("U_Label").Value.ToString();
                            }
                            else if (item.Specific is SAPbouiCOM.Button btn)
                            {
                                btn.Caption = rs.Fields.Item("U_Label").Value.ToString();
                            }
                        }
                        else if (action == "Enable")
                        {
                            oForm.Items.Item(itemId).Enabled = true;
                        }
                        else if (action == "Disable")
                        {
                            oForm.Items.Item(itemId).Enabled = false;
                        }
                        else if (action == "AddButton")
                        {
                            string label = rs.Fields.Item("U_Label").Value.ToString();
                            int top = (int)rs.Fields.Item("U_Top").Value;
                            int left = (int)rs.Fields.Item("U_Left").Value;
                            int width = (int)rs.Fields.Item("U_Width").Value;
                            int height = (int)rs.Fields.Item("U_Height").Value;
                            string relativeTo = rs.Fields.Item("U_ItemID").Value.ToString();

                            AddButton(oForm, $"btn_{Guid.NewGuid().ToString().Substring(0, 5)}", label, left, top, width, height, relativeTo);
                        }
                        else if (action == "AddFolder")
                        {
                            string caption = rs.Fields.Item("U_Label").Value.ToString();
                            int top = (int)rs.Fields.Item("U_Top").Value;
                            int left = (int)rs.Fields.Item("U_Left").Value;
                            int width = (int)rs.Fields.Item("U_Width").Value;
                            int height = (int)rs.Fields.Item("U_Height").Value;
                            string folderId = $"fld_{Guid.NewGuid().ToString().Substring(0, 5)}";

                            AddFolder(oForm, folderId, caption, left, top, width, height);
                        }
                        else if (action == "AddEditText")
                        {
                            string label = rs.Fields.Item("U_Label").Value.ToString();
                            int top = (int)rs.Fields.Item("U_Top").Value;
                            int left = (int)rs.Fields.Item("U_Left").Value;
                            int width = (int)rs.Fields.Item("U_Width").Value;
                            int height = (int)rs.Fields.Item("U_Height").Value;
                            string editTextId = $"txt_{Guid.NewGuid().ToString().Substring(0, 5)}";

                            AddEditText(oForm, editTextId, label, left, top, width, height);
                        }
                    }
                    catch { }

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando UI Customization: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static readonly object _contextLock = new object();
        private static UserContext _userContext;

        private static bool MatchesCurrentUser(string userFilter, string groupFilter)
        {
            if (string.IsNullOrWhiteSpace(userFilter) && string.IsNullOrWhiteSpace(groupFilter))
                return true;

            var context = GetUserContext();
            if (!AllowListContains(userFilter, context.UserCode, context.UserName))
                return false;
            if (!AllowListContains(groupFilter, context.GroupCodes.ToArray()))
                return false;
            return true;
        }

        private static UserContext GetUserContext()
        {
            if (_userContext != null) return _userContext;
            lock (_contextLock)
            {
                if (_userContext != null) return _userContext;
                var ctx = new UserContext
                {
                    UserCode = SafeString(() => B1App.Instance.Company.UserName),
                    UserName = SafeString(() => B1App.Instance.Company.UserName)
                };

                try
                {
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string sql = B1App.Instance.IsHana
                        ? $"SELECT G.\"GroupCode\" FROM OUSR U INNER JOIN USR6 UG ON U.\"USERID\" = UG.\"USERID\" INNER JOIN OUGR G ON UG.\"GroupCode\" = G.\"GroupCode\" WHERE U.\"USER_CODE\" = '{ctx.UserCode}'"
                        : $"SELECT G.GroupCode FROM OUSR U WITH (NOLOCK) INNER JOIN USR6 UG ON U.USERID = UG.USERID INNER JOIN OUGR G ON UG.GroupCode = G.GroupCode WHERE U.USER_CODE = '{ctx.UserCode}'";
                    rs.DoQuery(sql);
                    while (!rs.EoF)
                    {
                        string code = rs.Fields.Item(0).Value?.ToString();
                        if (!string.IsNullOrEmpty(code)) ctx.GroupCodes.Add(code);
                        rs.MoveNext();
                    }
                    ComObjectManager.Release(rs);
                }
                catch { }

                _userContext = ctx;
                return ctx;
            }
        }

        private static bool AllowListContains(string filter, params string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            if (candidates == null) candidates = Array.Empty<string>();

            var tokens = filter.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string value = token.Trim();
                if (value == "*") return true;
                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrEmpty(candidate) && value.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static string SafeString(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private static string SafeString(Func<string> getter)
        {
            try { return getter() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private class UserContext
        {
            public string UserCode { get; set; }
            public string UserName { get; set; }
            public List<string> GroupCodes { get; } = new List<string>();
        }

        public static void AddButton(SAPbouiCOM.Form oForm, string itemId, string caption, int left, int top, int width, int height, string fromItemId = "")
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(itemId, BoFormItemTypes.it_BUTTON);
                oItem.Left = left;
                oItem.Top = top;
                oItem.Width = width;
                oItem.Height = height;

                SAPbouiCOM.Button oBtn = (SAPbouiCOM.Button)oItem.Specific;
                oBtn.Caption = caption;

                if (!string.IsNullOrEmpty(fromItemId))
                {
                    Item fromItem = oForm.Items.Item(fromItemId);
                    oItem.Top = fromItem.Top;
                    oItem.Height = fromItem.Height;
                    oItem.Left = fromItem.Left + fromItem.Width + 5;
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir botón: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }

        public static void HideItem(SAPbouiCOM.Form oForm, string itemId)
        {
            try
            {
                oForm.Items.Item(itemId).Visible = false;
            }
            catch { }
        }

        public static void AddTab(SAPbouiCOM.Form oForm, string tabId, string caption, string afterTabId)
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(tabId, BoFormItemTypes.it_FOLDER);
                Folder oFolder = (Folder)oItem.Specific;
                oFolder.Caption = caption;
                oFolder.GroupWith(afterTabId);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir pestaña: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }

        public static void MoveItem(SAPbouiCOM.Form oForm, string itemId, int left, int top)
        {
            try
            {
                Item oItem = oForm.Items.Item(itemId);
                oItem.Left = left;
                oItem.Top = top;
            }
            catch { }
        }

        public static void AddFolder(SAPbouiCOM.Form oForm, string itemId, string caption, int left, int top, int width, int height)
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(itemId, BoFormItemTypes.it_FOLDER);
                oItem.Left = left;
                oItem.Top = top;
                oItem.Width = width;
                oItem.Height = height;

                Folder oFolder = (Folder)oItem.Specific;
                oFolder.Caption = caption;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir carpeta: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }

        public static void AddEditText(SAPbouiCOM.Form oForm, string itemId, string value, int left, int top, int width, int height)
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(itemId, BoFormItemTypes.it_EDIT);
                oItem.Left = left;
                oItem.Top = top;
                oItem.Width = width;
                oItem.Height = height;

                EditText oEdit = (EditText)oItem.Specific;
                oEdit.Value = value;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir campo de texto: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }
    }
}
