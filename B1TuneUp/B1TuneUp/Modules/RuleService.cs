using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class RuleService
    {
        public static IList<B1Rule> GetAll()
        {
            var list = new List<B1Rule>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"Code\",\"Name\",\"U_FormType\",\"U_Type\",\"U_EventType\",\"U_Before\",\"U_Condition\",\"U_Action\" FROM \"@BTUN_RULES\" ORDER BY \"U_FormType\",\"Code\""
                    : "SELECT [Code],[Name],[U_FormType],[U_Type],[U_EventType],[U_Before],[U_Condition],[U_Action] FROM [@BTUN_RULES] ORDER BY [U_FormType],[Code]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(MapRule(rs));
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return list;
        }

        private static B1Rule MapRule(Recordset rs)
        {
            return new B1Rule
            {
                ID = Convert.ToString(rs.Fields.Item(0).Value),
                Name = Convert.ToString(rs.Fields.Item(1).Value),
                FormType = Convert.ToString(rs.Fields.Item(2).Value),
                Type = ParseRuleType(Convert.ToString(rs.Fields.Item(3).Value)),
                EventType = Convert.ToString(rs.Fields.Item(4).Value),
                BeforeAction = string.Equals(Convert.ToString(rs.Fields.Item(5).Value), "Y", StringComparison.OrdinalIgnoreCase),
                Condition = Convert.ToString(rs.Fields.Item(6).Value),
                Action = Convert.ToString(rs.Fields.Item(7).Value)
            };
        }

        private static RuleType ParseRuleType(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return RuleType.Macro;
            RuleType parsed;
            if (Enum.TryParse(raw, true, out parsed)) return parsed;
            return RuleType.Macro;
        }

        public static B1Rule Save(B1Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_RULES");
                bool exists = !string.IsNullOrEmpty(rule.ID) && table.GetByKey(rule.ID);
                if (!exists)
                {
                    rule.ID = string.IsNullOrEmpty(rule.ID) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : rule.ID;
                    table.Code = rule.ID;
                    table.Name = string.IsNullOrEmpty(rule.Name) ? rule.ID : rule.Name;
                }
                else
                {
                    table.Name = string.IsNullOrEmpty(rule.Name) ? rule.ID : rule.Name;
                }

                table.UserFields.Fields.Item("U_FormType").Value = rule.FormType ?? string.Empty;
                table.UserFields.Fields.Item("U_Type").Value = rule.Type.ToString();
                table.UserFields.Fields.Item("U_EventType").Value = rule.EventType ?? string.Empty;
                table.UserFields.Fields.Item("U_Before").Value = rule.BeforeAction ? "Y" : "N";
                table.UserFields.Fields.Item("U_Condition").Value = rule.Condition ?? string.Empty;
                table.UserFields.Fields.Item("U_Action").Value = rule.Action ?? string.Empty;

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
            return rule;
        }

        public static void Delete(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_RULES");
                if (table.GetByKey(ruleId))
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

        public static bool TestCondition(B1Rule rule)
        {
            if (rule == null) return false;
            return MacroEngine.CheckCondition(rule.Condition);
        }

        public static void ExecuteAction(B1Rule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.Action)) return;
            MacroEngine.ExecuteMacro(rule.Action);
        }

        public static IReadOnlyList<string> GetDistinctFormTypes()
        {
            var list = new List<string>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT DISTINCT \"U_FormType\" FROM \"@BTUN_RULES\" ORDER BY \"U_FormType\""
                    : "SELECT DISTINCT [U_FormType] FROM [@BTUN_RULES] ORDER BY [U_FormType]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var val = Convert.ToString(rs.Fields.Item(0).Value);
                    if (!string.IsNullOrEmpty(val)) list.Add(val);
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }
    }
}
