using System;
using System.Collections.Generic;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class ValidationAdvancedDesignerService
    {
        public static readonly string[] Operators = { "IsEmpty", "Equals", "NotEquals", "GreaterThan", "LessThan", "Contains", "SqlRaw" };
        public static readonly string[] Severities = { "ERROR", "WARNING", "INFO", "CONFIRM", "AUTOFIX" };

        public static ValidationRuleEntry BuildRule(ValidationConditionBuilderState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            string condition = BuildCondition(state);
            string action = BuildAction(state);
            var rule = new ValidationRuleEntry
            {
                Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(),
                Name = $"{state.FormType}.{state.ItemId}.{state.Operator}",
                FormType = state.FormType,
                ItemName = string.IsNullOrWhiteSpace(state.ColumnId) ? state.ItemId : $"{state.ItemId}.{state.ColumnId}",
                EventType = state.EventType,
                Condition = condition,
                Action = action,
                Severity = state.Severity,
                Message = LocalizedMessage(state),
                PromptButtons = state.RequiresConfirmation ? "Yes|No" : string.Empty,
                BlockAlways = state.Severity == "ERROR",
                Active = true
            };
            return rule;
        }

        public static IList<ValidationTemplateEntry> GetTemplates()
        {
            return new List<ValidationTemplateEntry>
            {
                new ValidationTemplateEntry { Code = "SALES_BP_REQUIRED", Process = "Sales", Name = "Cliente obligatorio", Severity = "ERROR", Condition = "SELECT CASE WHEN '{value}' = '' THEN 'Y' ELSE 'N' END", Message = "Seleccione un cliente." },
                new ValidationTemplateEntry { Code = "PURCHASE_PROJECT_REQUIRED", Process = "Purchasing", Name = "Proyecto obligatorio", Severity = "WARNING", Condition = "SELECT CASE WHEN '{value}' = '' THEN 'Y' ELSE 'N' END", Message = "Complete proyecto antes de continuar." },
                new ValidationTemplateEntry { Code = "MATRIX_QTY_POSITIVE", Process = "Inventory", Name = "Cantidad positiva en matriz", Severity = "ERROR", Condition = "SELECT CASE WHEN CAST('{value}' AS DECIMAL(19,6)) <= 0 THEN 'Y' ELSE 'N' END", Message = "La cantidad debe ser mayor a cero." },
                new ValidationTemplateEntry { Code = "BP_AUTOFIX_GROUP", Process = "BusinessPartner", Name = "Auto-fix grupo cliente", Severity = "AUTOFIX", Condition = "SELECT CASE WHEN '{value}' = '' THEN 'Y' ELSE 'N' END", Action = "SetValue('GroupCode','100')", Message = "Grupo asignado automaticamente." }
            };
        }

        private static string BuildCondition(ValidationConditionBuilderState state)
        {
            if (state.Operator == "SqlRaw") return state.CompareValue ?? string.Empty;
            string token = string.IsNullOrWhiteSpace(state.ColumnId)
                ? $"$[{state.ItemId}.0.0]"
                : $"$[{state.ItemId}.{state.ColumnId}.0]";
            switch (state.Operator)
            {
                case "Equals": return $"SELECT CASE WHEN '{token}' = '{Escape(state.CompareValue)}' THEN 'Y' ELSE 'N' END";
                case "NotEquals": return $"SELECT CASE WHEN '{token}' <> '{Escape(state.CompareValue)}' THEN 'Y' ELSE 'N' END";
                case "GreaterThan": return $"SELECT CASE WHEN CAST('{token}' AS DECIMAL(19,6)) > {Escape(state.CompareValue)} THEN 'Y' ELSE 'N' END";
                case "LessThan": return $"SELECT CASE WHEN CAST('{token}' AS DECIMAL(19,6)) < {Escape(state.CompareValue)} THEN 'Y' ELSE 'N' END";
                case "Contains": return $"SELECT CASE WHEN '{token}' LIKE '%{Escape(state.CompareValue)}%' THEN 'Y' ELSE 'N' END";
                default: return $"SELECT CASE WHEN '{token}' = '' THEN 'Y' ELSE 'N' END";
            }
        }

        private static string BuildAction(ValidationConditionBuilderState state)
        {
            if (state.Severity == "AUTOFIX" && !string.IsNullOrWhiteSpace(state.AutoFixMacro)) return state.AutoFixMacro;
            if (state.RequiresConfirmation) return $"Confirm('{Escape(state.ConfirmText ?? state.MessageEs)}')";
            return $"Msg('{Escape(state.MessageEs ?? state.MessageEn ?? "Validation failed")}')";
        }

        private static string LocalizedMessage(ValidationConditionBuilderState state)
        {
            if (!string.IsNullOrWhiteSpace(state.MessageEs) && !string.IsNullOrWhiteSpace(state.MessageEn))
                return $"es={state.MessageEs}|en={state.MessageEn}";
            return state.MessageEs ?? state.MessageEn ?? string.Empty;
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");
    }
}
