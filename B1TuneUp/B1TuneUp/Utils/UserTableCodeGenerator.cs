using System;
using SAPbobsCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Utils
{
    /// <summary>
    /// Provides sequential numeric codes for user tables that expect Code/Name pairs instead of DocEntry.
    /// Values are persisted in @BTUN_TBOX using the key pattern SEQ_{TABLE} to avoid scanning entire tables.
    /// </summary>
    public static class UserTableCodeGenerator
    {
        private const string SequencePrefix = "SEQ_";

        public static int GetNext(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            var normalized = NormalizeName(tableName);
            var seqKey = SequencePrefix + normalized;
            var company = B1App.Instance?.Company ?? throw new InvalidOperationException("SAP Company connection is not available.");
            bool isHana = B1App.Instance.IsHana;
            Recordset rs = null;
            try
            {
                rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string quotedTable = isHana ? "\"@BTUN_TBOX\"" : "[@BTUN_TBOX]";
                string selectSql = isHana
                    ? $"SELECT \"U_Value\" FROM {quotedTable} WHERE \"U_Code\" = '{seqKey}'"
                    : $"SELECT [U_Value] FROM {quotedTable} WHERE [U_Code] = '{seqKey}'";

                rs.DoQuery(selectSql);
                int nextValue = 1;
                bool exists = rs.RecordCount > 0;
                if (exists)
                {
                    if (int.TryParse(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0), out int current) && current >= 1)
                    {
                        nextValue = current + 1;
                    }
                }

                string sql;
                if (exists)
                {
                    sql = isHana
                        ? $"UPDATE {quotedTable} SET \"U_Value\" = '{nextValue}' WHERE \"U_Code\" = '{seqKey}'"
                        : $"UPDATE {quotedTable} SET [U_Value] = '{nextValue}' WHERE [U_Code] = '{seqKey}'";
                }
                else
                {
                    sql = isHana
                        ? $"INSERT INTO {quotedTable} (\"U_Code\", \"U_Value\") VALUES ('{seqKey}', '{nextValue}')"
                        : $"INSERT INTO {quotedTable} ([U_Code], [U_Value]) VALUES ('{seqKey}', '{nextValue}')";
                }
                rs.DoQuery(sql);
                return nextValue;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string NormalizeName(string name)
        {
            var normalized = name.StartsWith("@", StringComparison.Ordinal) ? name.Substring(1) : name;
            return normalized.ToUpperInvariant();
        }
    }
}
