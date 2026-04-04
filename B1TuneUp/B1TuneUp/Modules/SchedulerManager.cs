using System;
using System.Timers;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class SchedulerManager
    {
        private static Timer _timer;

        public static void Init()
        {
            _timer = new Timer(60000); // Revisar cada minuto
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckTasks();
        }

        public static void CheckTasks()
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                // Revisar tareas activas cuya diferencia de tiempo sea mayor al intervalo
                string sql = B1App.Instance.IsHana
                    ? "SELECT * FROM \"@BTUN_SCHED\" WHERE \"U_Active\" = 'Y'"
                    : "SELECT * FROM [@BTUN_SCHED] WHERE [U_Active] = 'Y'";
                
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string docEntry = rs.Fields.Item("Code").Value.ToString();
                    string action = rs.Fields.Item("U_Action").Value.ToString();
                    int interval = (int)rs.Fields.Item("U_Interval").Value;
                    DateTime lastRun = (DateTime)rs.Fields.Item("U_LastRun").Value;

                    if ((DateTime.Now - lastRun).TotalMinutes >= interval)
                    {
                        try
                        {
                            MacroEngine.ExecuteMacro(action);
                            UpdateLastRun(docEntry);
                        }
                        catch (Exception jobEx)
                        {
                            B1App.Instance.Application.SetStatusBarMessage($"Error en tarea programada {docEntry}: {jobEx.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                            ExceptionLogger.LogHandled(jobEx, $"SchedulerManager.Job:{docEntry}");
                        }
                    }
                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "SchedulerManager.CheckTasks");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void UpdateLastRun(string docEntry)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"UPDATE \"@BTUN_SCHED\" SET \"U_LastRun\" = CURRENT_TIMESTAMP WHERE \"Code\" = '{docEntry}'"
                    : $"UPDATE [@BTUN_SCHED] SET [U_LastRun] = GETDATE() WHERE [Code] = '{docEntry}'";
                rs.DoQuery(sql);
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"SchedulerManager.UpdateLastRun:{docEntry}");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }
    }
}
