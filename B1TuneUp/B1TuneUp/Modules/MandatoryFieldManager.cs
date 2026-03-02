using System;
using System.Windows.Forms;
using SAPbobsCOM;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class MandatoryFieldManager
    {
        public static bool ValidateMandatoryFields(SAPbouiCOM.Form oForm)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_MAND\" WHERE \"U_FormType\" = '{oForm.TypeEx}'"
                    : $"SELECT * FROM [@BTUN_MAND] WHERE [U_FormType] = '{oForm.TypeEx}'";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string itemId = rs.Fields.Item("U_ItemID").Value.ToString();
                    string colId = rs.Fields.Item("U_ColumnID").Value.ToString();
                    string condition = rs.Fields.Item("U_Condition").Value.ToString();
                    string errorMsg = rs.Fields.Item("U_ErrorMsg").Value.ToString();

                    if (MacroEngine.CheckCondition(condition, oForm))
                    {
                        if (string.IsNullOrEmpty(colId))
                        {
                            // Campo de cabecera
                            Item item = oForm.Items.Item(itemId);
                            string val = GetItemValue(item);
                            if (string.IsNullOrEmpty(val))
                            {
                                ShowError(errorMsg, item);
                                return false;
                            }
                        }
                        else
                        {
                            // Campo de matriz
                            Matrix matrix = (Matrix)oForm.Items.Item(itemId).Specific;
                            for (int i = 1; i <= matrix.RowCount; i++)
                            {
                                string val = ((EditText)matrix.Columns.Item(colId).Cells.Item(i).Specific).Value;
                                if (string.IsNullOrEmpty(val))
                                {
                                    ShowError($"{errorMsg} (Fila {i})", (Item)matrix);
                                    return false;
                                }
                            }
                        }
                    }
                    rs.MoveNext();
                }
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en validación de obligatorios: {ex.Message}", BoMessageTime.bmt_Short, true);
                return true;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string GetItemValue(Item item)
        {
            if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_EXTEDIT)
                return ((EditText)item.Specific).Value;
            if (item.Type == BoFormItemTypes.it_COMBO_BOX)
                return ((SAPbouiCOM.ComboBox)item.Specific).Selected?.Value;
            return "";
        }

        private static void ShowError(string msg, Item item)
        {
            B1App.Instance.Application.MessageBox(msg);
            try { item.Click(); } catch { }
        }
    }
}
