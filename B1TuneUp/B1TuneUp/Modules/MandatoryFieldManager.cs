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
            if (oForm == null) return true;
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string safeFormType = (oForm.TypeEx ?? string.Empty).Replace("'", "''");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_MAND\" WHERE \"U_FormType\" = '{safeFormType}'"
                    : $"SELECT * FROM [@BTUN_MAND] WHERE [U_FormType] = '{safeFormType}'";

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
                            Matrix matrix = SapUiSafe.TryGetSpecific<Matrix>(matrixItem);
                            if (matrix == null)
                            {
                                ShowError($"La regla de obligatorio apunta a una matriz inexistente o inválida: {itemId}", matrixItem);
                                return false;
                            }
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
            return SapUiSafe.SafeItemValue(item);
        }

        private static Item GetItem(SAPbouiCOM.Form form, string itemId)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (string.IsNullOrWhiteSpace(itemId)) throw new InvalidOperationException("La regla de obligatorio no tiene ItemID configurado.");
            var item = SapUiSafe.TryGetItem(form, itemId);
            if (item == null) throw new InvalidOperationException($"No existe el ItemID '{itemId}' en el formulario {form.TypeEx}.");
            return item;
        }

        private static string GetCellValue(Matrix matrix, string colId, int row)
        {
            return SapUiSafe.SafeMatrixCell(matrix, colId, row);
        }

        private static string SafeField(Recordset rs, string field)
        {
            return SapUiSafe.SafeField(rs, field);
        }

        private static void ShowError(string msg, Item item)
        {
            B1App.Instance.Application.MessageBox(msg);
            try { item.Click(); } catch { }
        }
    }
}
