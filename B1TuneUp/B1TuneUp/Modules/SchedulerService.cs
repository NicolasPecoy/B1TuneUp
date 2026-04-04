using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class SchedulerService
    {
        public static IList<SchedulerEntry> GetAll()
        {
            var list = new List<SchedulerEntry>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"Code\",\"Name\",\"U_Action\",\"U_Interval\",\"U_LastRun\",\"U_Active\" FROM \"@BTUN_SCHED\" ORDER BY \"Name\""
                    : "SELECT [Code],[Name],[U_Action],[U_Interval],[U_LastRun],[U_Active] FROM [@BTUN_SCHED] ORDER BY [Name]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new SchedulerEntry
                    {
                        Code = Convert.ToString(rs.Fields.Item(0).Value),
                        Name = Convert.ToString(rs.Fields.Item(1).Value),
                        Action = Convert.ToString(rs.Fields.Item(2).Value),
                        IntervalMinutes = SafeInt(rs.Fields.Item(3).Value, 60),
                        LastRun = SafeDateTime(rs.Fields.Item(4).Value),
                        Active = !string.Equals(Convert.ToString(rs.Fields.Item(5).Value), "N", StringComparison.OrdinalIgnoreCase)
                    });
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return list;
        }

        public static SchedulerEntry Save(SchedulerEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_SCHED");
                bool exists = !string.IsNullOrEmpty(entry.Code) && table.GetByKey(entry.Code);
                if (!exists)
                {
                    entry.Code = string.IsNullOrWhiteSpace(entry.Code) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : entry.Code;
                    table.Code = entry.Code;
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Action : entry.Name;
                }
                else
                {
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Action : entry.Name;
                }

                table.UserFields.Fields.Item("U_Action").Value = entry.Action ?? string.Empty;
                table.UserFields.Fields.Item("U_Interval").Value = entry.IntervalMinutes <= 0 ? 1 : entry.IntervalMinutes;
                table.UserFields.Fields.Item("U_Active").Value = entry.Active ? "Y" : "N";
                if (entry.LastRun.HasValue)
                {
                    table.UserFields.Fields.Item("U_LastRun").Value = entry.LastRun.Value;
                }

                int res = exists ? table.Update() : table.Add();
                if (res != 0)
                {
                    throw new InvalidOperationException($"SAP SDK error: {B1App.Instance.Company.GetLastErrorDescription()}");
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }

            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_SCHED");
                if (table.GetByKey(code))
                {
                    int res = table.Remove();
                    if (res != 0)
                    {
                        throw new InvalidOperationException($"SAP SDK error: {B1App.Instance.Company.GetLastErrorDescription()}");
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }
        }

        public static void ToggleActive(SchedulerEntry entry, bool active)
        {
            if (entry == null) return;
            entry.Active = active;
            Save(entry);
        }

        public static void RunNow(SchedulerEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Action)) return;
            try
            {
                MacroEngine.ExecuteMacro(entry.Action);
                entry.LastRun = DateTime.Now;
                Save(entry);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error ejecutando tarea: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"SchedulerService.RunNow:{entry?.Name}");
            }
        }

        private static int SafeInt(object value, int fallback)
        {
            if (value == null) return fallback;
            if (int.TryParse(value.ToString(), out int parsed)) return parsed;
            return fallback;
        }

        private static DateTime? SafeDateTime(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out DateTime dt)) return dt;
            return null;
        }
    }
}
