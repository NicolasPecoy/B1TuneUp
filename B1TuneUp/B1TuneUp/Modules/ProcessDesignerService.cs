using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ProcessDesignerService
    {
        public static IList<ProcessDefinition> GetAll()
        {
            var list = new List<ProcessDefinition>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"Code\",\"Name\",\"U_FormType\",\"U_Desc\",\"U_Active\",\"U_AutoShow\" FROM \"@BTUN_PSTEP\" ORDER BY \"U_FormType\",\"Name\""
                    : "SELECT [Code],[Name],[U_FormType],[U_Desc],[U_Active],[U_AutoShow] FROM [@BTUN_PSTEP] ORDER BY [U_FormType],[Name]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var process = new ProcessDefinition
                    {
                        Code = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0),
                        Name = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1),
                        DocEntry = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0),
                        FormType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Description = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3),
                        Active = !string.Equals(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4), "N", StringComparison.OrdinalIgnoreCase),
                        AutoShow = string.Equals(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 5), "Y", StringComparison.OrdinalIgnoreCase)
                    };
                    LoadSteps(process);
                    list.Add(process);
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        private static void LoadSteps(ProcessDefinition process)
        {
            if (process == null || string.IsNullOrEmpty(process.Code)) return;
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"Code\",\"U_StepOrder\",\"U_StepName\",\"U_StepDesc\",\"U_DoneCondition\",\"U_Action\",\"U_Mandatory\" FROM \"@BTUN_PSTEPD\" WHERE \"U_ProcessEntry\" = '{process.Code}' ORDER BY \"U_StepOrder\""
                    : $"SELECT [Code],[U_StepOrder],[U_StepName],[U_StepDesc],[U_DoneCondition],[U_Action],[U_Mandatory] FROM [@BTUN_PSTEPD] WHERE [U_ProcessEntry] = '{process.Code}' ORDER BY [U_StepOrder]";
                rs.DoQuery(sql);
                process.Steps.Clear();
                while (!rs.EoF)
                {
                    process.Steps.Add(new ProcessStepDefinition
                    {
                        DocEntry = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0),
                        Order = SafeInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 1)),
                        Name = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Description = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3),
                        DoneCondition = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4),
                        Action = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 5),
                        Mandatory = string.Equals(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 6), "Y", StringComparison.OrdinalIgnoreCase)
                    });
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static ProcessDefinition Save(ProcessDefinition process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            UserTable header = null;
            try
            {
                header = B1App.Instance.Company.UserTables.Item("BTUN_PSTEP");
                bool exists = !string.IsNullOrEmpty(process.Code) && header.GetByKey(process.Code);
                if (!exists)
                {
                    process.Code = string.IsNullOrWhiteSpace(process.Code) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : process.Code;
                    header.Code = process.Code;
                    header.Name = string.IsNullOrWhiteSpace(process.Name) ? process.Code : process.Name;
                }
                else
                {
                    header.Name = string.IsNullOrWhiteSpace(process.Name) ? process.Code : process.Name;
                }

                header.UserFields.Fields.Item("U_Name").Value = process.Name ?? string.Empty;
                header.UserFields.Fields.Item("U_Desc").Value = process.Description ?? string.Empty;
                header.UserFields.Fields.Item("U_FormType").Value = process.FormType ?? string.Empty;
                header.UserFields.Fields.Item("U_Active").Value = process.Active ? "Y" : "N";
                header.UserFields.Fields.Item("U_AutoShow").Value = process.AutoShow ? "Y" : "N";

                int res = exists ? header.Update() : header.Add();
                if (res != 0)
                {
                    throw new InvalidOperationException($"SAP SDK error: {B1App.Instance.Company.GetLastErrorDescription()}");
                }

                process.DocEntry = process.Code;
                SaveSteps(process);
            }
            finally
            {
                ComObjectManager.Release(header);
            }

            return process;
        }

        private static void SaveSteps(ProcessDefinition process)
        {
            if (process == null || string.IsNullOrEmpty(process.Code)) return;
            Recordset rs = null;
            UserTable table = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string deleteSql = B1App.Instance.IsHana
                    ? $"DELETE FROM \"@BTUN_PSTEPD\" WHERE \"U_ProcessEntry\" = '{process.Code}'"
                    : $"DELETE FROM [@BTUN_PSTEPD] WHERE [U_ProcessEntry] = '{process.Code}'";
                rs.DoQuery(deleteSql);

                table = B1App.Instance.Company.UserTables.Item("BTUN_PSTEPD");
                foreach (var step in process.Steps)
                {
                    table.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
                    table.Name = $"{process.Code}_{step.Order}";
                    table.UserFields.Fields.Item("U_ProcessEntry").Value = process.Code;
                    table.UserFields.Fields.Item("U_StepOrder").Value = step.Order;
                    table.UserFields.Fields.Item("U_StepName").Value = step.Name ?? string.Empty;
                    table.UserFields.Fields.Item("U_StepDesc").Value = step.Description ?? string.Empty;
                    table.UserFields.Fields.Item("U_DoneCondition").Value = step.DoneCondition ?? string.Empty;
                    table.UserFields.Fields.Item("U_Action").Value = step.Action ?? string.Empty;
                    table.UserFields.Fields.Item("U_Mandatory").Value = step.Mandatory ? "Y" : "N";
                    int res = table.Add();
                    if (res != 0)
                    {
                        throw new InvalidOperationException($"SAP SDK error guardando paso: {B1App.Instance.Company.GetLastErrorDescription()}");
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
                ComObjectManager.Release(table);
            }
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            Recordset rs = null;
            try
            {
                // Delete detail first
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string deleteSteps = B1App.Instance.IsHana
                    ? $"DELETE FROM \"@BTUN_PSTEPD\" WHERE \"U_ProcessEntry\" = '{code}'"
                    : $"DELETE FROM [@BTUN_PSTEPD] WHERE [U_ProcessEntry] = '{code}'";
                rs.DoQuery(deleteSteps);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            UserTable header = null;
            try
            {
                header = B1App.Instance.Company.UserTables.Item("BTUN_PSTEP");
                if (header.GetByKey(code))
                {
                    int res = header.Remove();
                    if (res != 0)
                    {
                        throw new InvalidOperationException($"SAP SDK error: {B1App.Instance.Company.GetLastErrorDescription()}");
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(header);
            }
        }

        private static int SafeInt(object value)
        {
            if (value == null) return 0;
            int parsed;
            if (int.TryParse(value.ToString(), out parsed)) return parsed;
            return 0;
        }
    }
}
