using System;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class AuditLogManager
    {
        public static void LogAction(string actionType, string details, string status = "Success")
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                // Limpiar comillas simples para evitar errores SQL
                details = details.Replace("'", "''");

                string sql;
                if (B1App.Instance.IsHana)
                {
                    sql = $@"INSERT INTO ""@BTUN_LOG"" (""DocEntry"", ""U_Date"", ""U_Type"", ""U_Details"", ""U_Status"", ""U_User"") 
                             VALUES ((SELECT IFNULL(MAX(""DocEntry""), 0) + 1 FROM ""@BTUN_LOG""), CURRENT_TIMESTAMP, '{actionType}', '{details}', '{status}', '{B1App.Instance.Company.UserName}')";
                }
                else
                {
                    sql = $@"INSERT INTO [@BTUN_LOG] (DocEntry, U_Date, U_Type, U_Details, U_Status, U_User) 
                             VALUES ((SELECT ISNULL(MAX(DocEntry), 0) + 1 FROM [@BTUN_LOG]), GETDATE(), '{actionType}', '{details}', '{status}', '{B1App.Instance.Company.UserName}')";
                }

                rs.DoQuery(sql);
            }
            catch { }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void LogDetailedAction(string actionType, string details, string status, string userId, string formType, string additionalInfo = "")
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string fullDetails = $"{details} | FormType: {formType} | Additional: {additionalInfo}".Replace("'", "''");

                string sql;
                if (B1App.Instance.IsHana)
                {
                    sql = $"INSERT INTO \"@BTUN_LOG\" (\"DocEntry\", \"U_Date\", \"U_Type\", \"U_Details\", \"U_Status\", \"U_User\") VALUES ((SELECT IFNULL(MAX(\"DocEntry\"), 0) + 1 FROM \"@BTUN_LOG\"), CURRENT_TIMESTAMP, '{actionType}', '{fullDetails}', '{status}', '{userId}')";
                }
                else
                {
                    sql = $"INSERT INTO [@BTUN_LOG] (DocEntry, U_Date, U_Type, U_Details, U_Status, U_User) VALUES ((SELECT ISNULL(MAX(DocEntry), 0) + 1 FROM [@BTUN_LOG]), GETDATE(), '{actionType}', '{fullDetails}', '{status}', '{userId}')";
                }

                rs.DoQuery(sql);
            }
            catch { }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }
    }
}
