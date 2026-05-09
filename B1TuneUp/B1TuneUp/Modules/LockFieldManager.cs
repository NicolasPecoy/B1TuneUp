using System;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    /// <summary>
    /// Gestiona el bloqueo/desbloqueo dinámico de campos en formularios SAP B1.
    /// Equivalente al "Lock Fields" del B1 Usability Package de Boyum IT.
    /// Permite definir reglas en la tabla @BTUN_LOCK para hacer campos ReadOnly, Hidden o Disabled
    /// según condiciones SQL evaluadas en tiempo de ejecución.
    /// </summary>
    public static class LockFieldManager
    {
        public static void ApplyOnLoad(Form oForm) => ApplyLocks(oForm, null, true);

        public static void ApplyOnChange(Form oForm, string triggerItemId) => ApplyLocks(oForm, triggerItemId, false);

        private static void ApplyLocks(Form oForm, string triggerItemId, bool onLoad)
        {
            if (oForm == null) return;
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string eventFilter = onLoad ? "Load" : "Change";
                string safeFormType = (oForm.TypeEx ?? string.Empty).Replace("'", "''");
                string safeEvent = eventFilter.Replace("'", "''");
                string safeTrigger = (triggerItemId ?? string.Empty).Replace("'", "''");
                string sql;

                if (B1App.Instance.IsHana)
                {
                    sql = $"SELECT * FROM \"@BTUN_LOCK\" WHERE \"U_FormType\" = '{safeFormType}' AND \"U_OnEvent\" = '{safeEvent}'";
                    if (!onLoad && !string.IsNullOrEmpty(triggerItemId))
                        sql += $" AND (\"U_TriggerItem\" = '{safeTrigger}' OR IFNULL(\"U_TriggerItem\",'') = '')";
                }
                else
                {
                    sql = $"SELECT * FROM [@BTUN_LOCK] WHERE [U_FormType] = '{safeFormType}' AND [U_OnEvent] = '{safeEvent}'";
                    if (!onLoad && !string.IsNullOrEmpty(triggerItemId))
                        sql += $" AND ([U_TriggerItem] = '{safeTrigger}' OR ISNULL([U_TriggerItem],'') = '')";
                }

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string itemId    = SapUiSafe.SafeField(rs, "U_ItemID");
                    string colId     = SapUiSafe.SafeField(rs, "U_ColID");
                    string lockType  = SapUiSafe.SafeField(rs, "U_LockType");
                    string condition = SapUiSafe.SafeField(rs, "U_Condition");

                    bool shouldLock = string.IsNullOrEmpty(condition)
                        || MacroEngine.CheckCondition(condition, oForm);

                    try { ApplyLock(oForm, itemId, colId, lockType, shouldLock); }
                    catch { }

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error aplicando bloqueos de campo: {ex.Message}",
                    BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void ApplyLock(Form oForm, string itemId, string colId, string lockType, bool locked)
        {
            Item item = SapUiSafe.TryGetItem(oForm, itemId);
            if (item == null) return;

            if (!string.IsNullOrEmpty(colId) && item.Type == BoFormItemTypes.it_MATRIX)
            {
                Matrix matrix = SapUiSafe.TryGetSpecific<Matrix>(item);
                if (matrix == null) return;
                Column col = matrix.Columns.Item(colId);
                if (lockType.ToUpper() == "HIDDEN")
                    col.Visible  = !locked;
                else
                    col.Editable = !locked;
            }
            else
            {
                switch (lockType.ToUpper())
                {
                    case "HIDDEN":
                        item.Visible  = !locked;
                        break;
                    case "READONLY":
                    case "DISABLED":
                        item.Enabled  = !locked;
                        break;
                }
            }
        }
    }
}
