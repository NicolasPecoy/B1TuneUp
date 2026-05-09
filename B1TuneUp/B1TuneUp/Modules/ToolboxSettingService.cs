using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ToolboxSettingService
    {
        public static IList<ToolboxSettingEntry> GetAll()
        {
            var list = new List<ToolboxSettingEntry>();
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string sql = isHana
                    ? "SELECT \"Code\", \"Name\", \"U_Code\", \"U_Value\" FROM \"@BTUN_TBOX\" ORDER BY \"U_Code\""
                    : "SELECT Code, Name, U_Code, U_Value FROM [@BTUN_TBOX] ORDER BY [U_Code]";

                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string code = ReadString(rs, "U_Code");
                    list.Add(new ToolboxSettingEntry
                    {
                        Code = code,
                        Value = ReadString(rs, "U_Value"),
                        Category = ToolboxSettingMetadata.DetermineCategory(code),
                        Description = ToolboxSettingMetadata.Describe(code)
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

        public static ToolboxSettingEntry GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            Recordset rs = null;
            try
            {
                bool isHana = B1App.Instance.IsHana;
                string safeCode = code.Replace("'", "''");
                string sql = isHana
                    ? $"SELECT \"U_Code\", \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = '{safeCode}'"
                    : $"SELECT U_Code, U_Value FROM [@BTUN_TBOX] WHERE U_Code = '{safeCode}'";

                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                if (rs.EoF) return null;

                string actualCode = ReadString(rs, "U_Code");
                return new ToolboxSettingEntry
                {
                    Code = actualCode,
                    Value = ReadString(rs, "U_Value"),
                    Category = ToolboxSettingMetadata.DetermineCategory(actualCode),
                    Description = ToolboxSettingMetadata.Describe(actualCode)
                };
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static ToolboxSettingEntry Save(ToolboxSettingEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code)) throw new InvalidOperationException("El código es obligatorio.");

            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_TBOX");
                if (table.GetByKey(entry.Code))
                {
                    table.Name = entry.Code;
                    SetField(table, "U_Code", entry.Code);
                    SetField(table, "U_Value", entry.Value ?? string.Empty);
                    int updateRes = table.Update();
                    if (updateRes != 0)
                    {
                        string err = B1App.Instance.Company.GetLastErrorDescription();
                        throw new InvalidOperationException($"SAP SDK error: {err}");
                    }
                }
                else
                {
                    table.Code = entry.Code;
                    table.Name = entry.Code;
                    SetField(table, "U_Code", entry.Code);
                    SetField(table, "U_Value", entry.Value ?? string.Empty);
                    int addRes = table.Add();
                    if (addRes != 0)
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
            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_TBOX");
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

        private static string ReadString(Recordset rs, string field)
        {
            try { return B1TuneUp.Utils.SapUiSafe.SafeField(rs, field); }
            catch { return string.Empty; }
        }

        private static void SetField(UserTable table, string field, string value)
        {
            try { table.UserFields.Fields.Item(field).Value = value ?? string.Empty; }
            catch { }
        }
    }
}
