using System;
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
                    ? $"SELECT * FROM \"@BTUN_UI\" WHERE \"U_FormType\" = '{oForm.TypeEx}'"
                    : $"SELECT * FROM [@BTUN_UI] WHERE [U_FormType] = '{oForm.TypeEx}'";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string action = rs.Fields.Item("U_Action").Value.ToString();
                    string itemId = rs.Fields.Item("U_ItemID").Value.ToString();

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
