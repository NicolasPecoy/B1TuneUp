using System;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class MasterDataManager
    {
        public static void RunMDM(string configID)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_MDM\" WHERE \"Code\" = '{configID}'"
                    : $"SELECT * FROM [@BTUN_MDM] WHERE [Code] = '{configID}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string selectSql = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_SelectSQL");
                    string actionMacro = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Action");

                    ProcessMDM(selectSql, actionMacro);
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void ProcessMDM(string selectSql, string actionMacro)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery(selectSql);
                int count = 0;
                while (!rs.EoF)
                {
                    // Crear un contexto con los datos del registro actual para pasarlo al macro
                    var recordContext = new System.Collections.Generic.Dictionary<string, object>();

                    // Extraer todos los campos del registro actual
                    for (int i = 0; i < rs.Fields.Count; i++)
                    {
                        string fieldName = B1TuneUp.Utils.SapUiSafe.SafeFieldName(rs, i);
                        object fieldValue = B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, i);
                        recordContext[fieldName] = fieldValue ?? "";
                    }

                    // Ejecutar el macro con el contexto de datos
                    ExecuteMacroWithContext(actionMacro, recordContext);
                    count++;
                    rs.MoveNext();
                }
                B1App.Instance.Application.SetStatusBarMessage($"MDM finalizado: {count} registros procesados.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error en MDM: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void ExecuteMacroWithContext(string macro, System.Collections.Generic.Dictionary<string, object> context)
        {
            // Reemplazar variables en el macro con los valores del contexto
            string processedMacro = macro;

            foreach (var kvp in context)
            {
                string placeholder = "$[" + kvp.Key + "]";
                processedMacro = processedMacro.Replace(placeholder, kvp.Value.ToString());
            }

            // Ejecutar el macro procesado
            MacroEngine.ExecuteMacro(processedMacro);
        }
    }
}
