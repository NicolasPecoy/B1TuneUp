using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ActionPadManager
    {
        private static Dictionary<string, string> _buttonActions = new Dictionary<string, string>();

        public static void ShowPadForForm(SAPbouiCOM.Form oForm)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_PAD\" WHERE \"U_FormType\" = '{oForm.TypeEx}'"
                    : $"SELECT * FROM [@BTUN_PAD] WHERE [U_FormType] = '{oForm.TypeEx}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string padEntry = rs.Fields.Item("DocEntry").Value.ToString();
                    string title = rs.Fields.Item("U_Title").Value.ToString();
                    string position = rs.Fields.Item("U_Position").Value.ToString();

                    CreatePadForm(padEntry, title, position, oForm);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en Action Pad: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void CreatePadForm(string padEntry, string title, string position, SAPbouiCOM.Form parentForm)
        {
            string formUID = $"PAD_{padEntry}_{parentForm.Title}";
            try
            {
                if (B1App.Instance.Application.Forms.Count > 0)
                {
                    try { var f = B1App.Instance.Application.Forms.Item(formUID); if (f != null) { f.Select(); return; } } catch { }
                }

                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_PAD";
                fcp.UniqueID = formUID;
                fcp.BorderStyle = BoFormBorderStyle.fbs_Fixed;

                SAPbouiCOM.Form padForm = B1App.Instance.Application.Forms.AddEx(fcp);
                padForm.Title = title;
                padForm.Width = 150;

                // Posicionamiento lateral
                if (position == "Right")
                {
                    padForm.Left = parentForm.Left + parentForm.Width + 5;
                    padForm.Top = parentForm.Top;
                }
                else
                {
                    padForm.Left = parentForm.Left - padForm.Width - 5;
                    padForm.Top = parentForm.Top;
                }

                AddButtons(padForm, padEntry);
                padForm.Visible = true;
            }
            catch { }
        }

        private static void AddButtons(SAPbouiCOM.Form padForm, string padEntry)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_PADB\" WHERE \"U_PadEntry\" = '{padEntry}' ORDER BY \"U_Order\" ASC"
                    : $"SELECT * FROM [@BTUN_PADB] WHERE [U_PadEntry] = '{padEntry}' ORDER BY [U_Order] ASC";

                rs.DoQuery(sql);
                int top = 10;
                while (!rs.EoF)
                {
                    string label = rs.Fields.Item("U_Label").Value.ToString();
                    string action = rs.Fields.Item("U_Action").Value.ToString();
                    string btnId = $"btn_{rs.Fields.Item("DocEntry").Value}";

                    Item item = padForm.Items.Add(btnId, BoFormItemTypes.it_BUTTON);
                    item.Top = top;
                    item.Left = 10;
                    item.Width = 120;
                    item.Height = 20;

                    SAPbouiCOM.Button btn = (SAPbouiCOM.Button)item.Specific;
                    btn.Caption = label;

                    _buttonActions[$"{padForm.UniqueID}_{btnId}"] = action;

                    top += 25;
                    rs.MoveNext();
                }
                padForm.Height = top + 20;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void HandleItemEvent(string formUID, ItemEvent pVal)
        {
            if (pVal.EventType == BoEventTypes.et_CLICK && !pVal.BeforeAction)
            {
                string key = $"{formUID}_{pVal.ItemUID}";
                if (_buttonActions.ContainsKey(key))
                {
                    MacroEngine.ExecuteMacro(_buttonActions[key]);
                }
            }
        }
    }
}
