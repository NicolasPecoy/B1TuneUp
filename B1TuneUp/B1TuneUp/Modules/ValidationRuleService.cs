using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ValidationRuleService
    {
        public static IList<ValidationRuleEntry> GetAll()
        {
            var list = new List<ValidationRuleEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\",\"Name\",\"U_FormType\",\"U_ItemName\",\"U_Event\",\"U_Condition\",\"U_Action\",\"U_Severity\",\"U_Active\",\"U_User\",\"U_UserGroup\",\"U_Message\",\"U_Block\",\"U_Sequence\",\"U_PromptButtons\",\"U_Notes\" FROM \"@BTUN_VAL\" ORDER BY \"Code\""
                    : "SELECT [Code],[Name],[U_FormType],[U_ItemName],[U_Event],[U_Condition],[U_Action],[U_Severity],[U_Active],[U_User],[U_UserGroup],[U_Message],[U_Block],[U_Sequence],[U_PromptButtons],[U_Notes] FROM [@BTUN_VAL] ORDER BY [Code]";
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(new ValidationRuleEntry
                    {
                        Code = ReadString(rs, "Code"),
                        Name = ReadString(rs, "Name"),
                        FormType = ReadString(rs, "U_FormType"),
                        ItemName = ReadString(rs, "U_ItemName"),
                        EventType = ReadString(rs, "U_Event"),
                        Condition = ReadString(rs, "U_Condition"),
                        Action = ReadString(rs, "U_Action"),
                        Severity = string.IsNullOrWhiteSpace(ReadString(rs, "U_Severity")) ? "ERROR" : ReadString(rs, "U_Severity"),
                        Active = !string.Equals(ReadString(rs, "U_Active"), "N", StringComparison.OrdinalIgnoreCase),
                        AppliesToUser = ReadString(rs, "U_User"),
                        AppliesToUserGroup = ReadString(rs, "U_UserGroup"),
                        Message = ReadString(rs, "U_Message"),
                        BlockAlways = !string.Equals(ReadString(rs, "U_Block"), "N", StringComparison.OrdinalIgnoreCase),
                        Sequence = SafeInt(ReadString(rs, "U_Sequence"), 10),
                        PromptButtons = ReadString(rs, "U_PromptButtons"),
                        Notes = ReadString(rs, "U_Notes")
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

        public static ValidationRuleEntry Save(ValidationRuleEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                entry.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            }

            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_VAL");
                bool exists = table.GetByKey(entry.Code);
                if (!exists)
                {
                    table.Code = entry.Code;
                    table.Name = entry.Name ?? entry.Code;
                }
                else
                {
                    table.Name = entry.Name ?? entry.Code;
                }

                SetField(table, "U_FormType", entry.FormType);
                SetField(table, "U_ItemName", entry.ItemName);
                SetField(table, "U_Event", entry.EventType);
                SetField(table, "U_Condition", entry.Condition);
                SetField(table, "U_Action", entry.Action);
                SetField(table, "U_Severity", entry.Severity ?? "ERROR");
                SetField(table, "U_Active", entry.Active ? "Y" : "N");
                SetField(table, "U_User", entry.AppliesToUser);
                SetField(table, "U_UserGroup", entry.AppliesToUserGroup);
                SetField(table, "U_Message", entry.Message);
                SetField(table, "U_Block", entry.BlockAlways ? "Y" : "N");
                SetField(table, "U_Sequence", entry.Sequence.ToString());
                SetField(table, "U_PromptButtons", entry.PromptButtons);
                SetField(table, "U_Notes", entry.Notes);

                int res = exists ? table.Update() : table.Add();
                if (res != 0)
                {
                    string err = B1App.Instance.Company.GetLastErrorDescription();
                    throw new InvalidOperationException($"SAP SDK error: {err}");
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
            if (string.IsNullOrWhiteSpace(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_VAL");
                if (table.GetByKey(code))
                {
                    int res = table.Remove();
                    if (res != 0)
                    {
                        string err = B1App.Instance.Company.GetLastErrorDescription();
                        throw new InvalidOperationException($"SAP SDK error: {err}");
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(table);
            }
        }

        private static void SetField(UserTable table, string field, string value)
        {
            try { table.UserFields.Fields.Item(field).Value = value ?? string.Empty; }
            catch { }
        }

        private static string ReadString(Recordset rs, string field)
        {
            try { return rs.Fields.Item(field).Value?.ToString() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static int SafeInt(string input, int fallback)
        {
            return int.TryParse(input, out var value) ? value : fallback;
        }
    }
}
