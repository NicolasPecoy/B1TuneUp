using System;
using System.Collections.Generic;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using B1TuneUp.Modules.ProcessStepsUi;
using B1TuneUp.Modules.Forms;

namespace B1TuneUp.Modules
{
    /// <summary>
    /// Gestiona guías de proceso paso a paso asociadas a formularios SAP B1.
    /// Equivalente al "Process Steps" del B1 Usability Package de Boyum IT.
    ///
    /// La configuración se almacena en:
    ///   @BTUN_PSTEP  - Cabecera del proceso (nombre, formulario, autoshow)
    ///   @BTUN_PSTEPD - Pasos del proceso (orden, nombre, descripción, condición SQL, macro de acción)
    ///
    /// El estado de completitud de cada paso se evalúa en tiempo real mediante la
    /// condición SQL definida (U_DoneCondition). Si la consulta devuelve algún registro,
    /// el paso se considera completado.
    /// </summary>
    public static class ProcessStepsManager
    {
        /// <summary>
        /// Verifica si hay un proceso configurado con AutoShow=Y para el formulario y,
        /// de ser así, abre el diálogo de pasos. Llamar en et_FORM_LOAD (after).
        /// </summary>
        public static void CheckAndShowAutoProcess(Form oForm)
        {
            string processEntry = FindAutoShowProcess(oForm.TypeEx);
            if (!string.IsNullOrEmpty(processEntry))
                ShowProcessSteps(oForm, processEntry);
        }

        /// <summary>
        /// Abre el diálogo de Process Steps para el formulario y proceso indicados.
        /// Si processEntry es null o vacío, usa el primer proceso activo para el formType.
        /// </summary>
        public static void ShowProcessSteps(Form oForm, string processEntry = null)
        {
            try
            {
                if (string.IsNullOrEmpty(processEntry))
                    processEntry = FindFirstActiveProcess(oForm.TypeEx);

                if (string.IsNullOrEmpty(processEntry))
                {
                    B1App.Instance.Application.SetStatusBarMessage(
                        "No hay procesos configurados para este formulario.",
                        BoMessageTime.bmt_Short, false);
                    return;
                }

                var processInfo = LoadProcessInfo(processEntry);
                if (processInfo == null) return;

                var steps = LoadSteps(processEntry, oForm);
                ProcessStepsUi.ProcessStepsLauncher.Show(processInfo, steps, oForm, processEntry);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error abriendo Process Steps: {ex.Message}",
                    BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"ProcessStepsManager.ShowProcessSteps:{oForm?.TypeEx}:{processEntry}");
            }
        }

        /// <summary>
        /// Devuelve la lista de pasos con su estado de completitud evaluado en tiempo real.
        /// Útil para refrescar el estado desde el diálogo.
        /// </summary>
        public static List<ProcessStep> LoadSteps(string processEntry, Form oForm)
        {
            var steps = new List<ProcessStep>();
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_PSTEPD\" WHERE \"U_ProcessEntry\" = '{EscSql(processEntry)}' ORDER BY \"U_StepOrder\""
                    : $"SELECT * FROM [@BTUN_PSTEPD] WHERE [U_ProcessEntry] = '{EscSql(processEntry)}' ORDER BY [U_StepOrder]";

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string stepEntry    = rs.Fields.Item("Code").Value.ToString();
                    string stepName     = rs.Fields.Item("U_StepName").Value.ToString();
                    string stepDesc     = rs.Fields.Item("U_StepDesc").Value.ToString();
                    string doneCond     = rs.Fields.Item("U_DoneCondition").Value.ToString();
                    string action       = rs.Fields.Item("U_Action").Value.ToString();
                    bool   mandatory    = rs.Fields.Item("U_Mandatory").Value.ToString() == "Y";
                    int    order        = 0;
                    int.TryParse(rs.Fields.Item("U_StepOrder").Value.ToString(), out order);

                    bool isDone = EvaluateCondition(doneCond, oForm);

                    steps.Add(new ProcessStep
                    {
                        DocEntry  = stepEntry,
                        Order     = order,
                        Name      = stepName,
                        Desc      = stepDesc,
                        Action    = action,
                        Mandatory = mandatory,
                        IsDone    = isDone
                    });

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error cargando pasos del proceso: {ex.Message}",
                    BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"ProcessStepsManager.LoadSteps:{processEntry}");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return steps;
        }

        /// <summary>
        /// Ejecuta la macro de acción de un paso específico.
        /// </summary>
        public static void ExecuteStepAction(ProcessStep step, Form oForm)
        {
            if (string.IsNullOrEmpty(step.Action)) return;
            try
            {
                MacroEngine.ExecuteMacro(step.Action, oForm);
                AuditLogManager.LogAction("ProcessStep",
                    $"Paso ejecutado: '{step.Name}' en formulario {oForm.TypeEx}");
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error ejecutando acción del paso: {ex.Message}",
                    BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"ProcessStepsManager.ExecuteStepAction:{step?.Name}");
            }
        }

        // ─── Privados ─────────────────────────────────────────────────────────────

        private static string FindAutoShowProcess(string formType)
        {
            return QuerySingleField(
                B1App.Instance.IsHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_PSTEP\" WHERE \"U_FormType\" = '{EscSql(formType)}' AND \"U_Active\" = 'Y' AND \"U_AutoShow\" = 'Y' LIMIT 1"
                    : $"SELECT TOP 1 [Code] FROM [@BTUN_PSTEP] WHERE [U_FormType] = '{EscSql(formType)}' AND [U_Active] = 'Y' AND [U_AutoShow] = 'Y'");
        }

        private static string FindFirstActiveProcess(string formType)
        {
            return QuerySingleField(
                B1App.Instance.IsHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_PSTEP\" WHERE \"U_FormType\" = '{EscSql(formType)}' AND \"U_Active\" = 'Y' LIMIT 1"
                    : $"SELECT TOP 1 [Code] FROM [@BTUN_PSTEP] WHERE [U_FormType] = '{EscSql(formType)}' AND [U_Active] = 'Y'");
        }

        private static ProcessInfo LoadProcessInfo(string processEntry)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_PSTEP\" WHERE \"Code\" = '{EscSql(processEntry)}'"
                    : $"SELECT * FROM [@BTUN_PSTEP] WHERE [Code] = '{EscSql(processEntry)}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return new ProcessInfo
                    {
                        DocEntry = processEntry,
                        Name     = rs.Fields.Item("U_Name").Value.ToString(),
                        Desc     = rs.Fields.Item("U_Desc").Value.ToString(),
                        FormType = rs.Fields.Item("U_FormType").Value.ToString()
                    };
                }
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return null;
        }

        /// <summary>
        /// Evalúa una condición SQL: si devuelve algún registro, el paso está completo.
        /// Condición vacía se considera incompleta (pendiente).
        /// </summary>
        private static bool EvaluateCondition(string condition, Form oForm)
        {
            if (string.IsNullOrWhiteSpace(condition)) return false;
            try
            {
                return MacroEngine.CheckCondition(condition, oForm);
            }
            catch { return false; }
        }

        private static string QuerySingleField(string sql)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery(sql);
                if (!rs.EoF) return rs.Fields.Item(0).Value?.ToString() ?? string.Empty;
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return string.Empty;
        }

        private static string EscSql(string v) => v?.Replace("'", "''") ?? string.Empty;
    }

    // ─── Modelos ─────────────────────────────────────────────────────────────────

    public class ProcessStep
    {
        public string DocEntry  { get; set; }
        public int    Order     { get; set; }
        public string Name      { get; set; }
        public string Desc      { get; set; }
        public string Action    { get; set; }
        public bool   Mandatory { get; set; }
        public bool   IsDone    { get; set; }
    }

    public class ProcessInfo
    {
        public string DocEntry  { get; set; }
        public string Name      { get; set; }
        public string Desc      { get; set; }
        public string FormType  { get; set; }
    }
}
