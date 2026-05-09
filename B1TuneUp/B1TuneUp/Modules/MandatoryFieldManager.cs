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
                    string itemId = SafeField(rs, "U_ItemID");
                    string colId = SafeField(rs, "U_ColumnID");
                    string condition = SafeField(rs, "U_Condition");
                    string errorMsg = SafeField(rs, "U_ErrorMsg");

                    if (MacroEngine.CheckCondition(condition, oForm))
                    {
                        if (string.IsNullOrEmpty(colId))
                        {
                            // Campo de cabecera
                            Item item = GetItem(oForm, itemId);
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
                            Item matrixItem = GetItem(oForm, itemId);
                            Matrix matrix = (Matrix)matrixItem.Specific;
                            for (int i = 1; i <= matrix.RowCount; i++)
                            {
                                string val = GetCellValue(matrix, colId, i);
                                if (string.IsNullOrEmpty(val))
                                {
                                    ShowError($"{errorMsg} (Fila {i})", matrixItem);
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
                ExceptionLogger.LogHandled(ex, $"MandatoryFieldManager.ValidateMandatoryFields:{oForm?.TypeEx}");
                return false;
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

        private static Item GetItem(SAPbouiCOM.Form form, string itemId)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (string.IsNullOrWhiteSpace(itemId)) throw new InvalidOperationException("La regla de obligatorio no tiene ItemID configurado.");
            return form.Items.Item(itemId);
        }

        private static string GetCellValue(Matrix matrix, string colId, int row)
        {
            if (matrix == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(colId)) return string.Empty;
            object specific = matrix.Columns.Item(colId).Cells.Item(row).Specific;
            if (specific is EditText et) return et.Value ?? string.Empty;
            if (specific is SAPbouiCOM.ComboBox cb) return cb.Selected?.Value ?? string.Empty;
            if (specific is SAPbouiCOM.CheckBox chk) return chk.Checked ? "Y" : string.Empty;
            return string.Empty;
        }

        private static string SafeField(Recordset rs, string field)
        {
            try { return rs.Fields.Item(field).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static void ShowError(string msg, Item item)
        {
            B1App.Instance.Application.MessageBox(msg);
            try { item.Click(); } catch { }
        }
    }
}
