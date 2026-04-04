using System;
using System.Globalization;
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

                bool isHana = B1App.Instance.IsHana;
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_LOG");
                string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = $"LOG_{codeValue}";
                string sql = isHana
                    ? $@"INSERT INTO ""@BTUN_LOG"" (""Code"", ""Name"", ""U_Date"", ""U_Type"", ""U_Details"", ""U_Status"", ""U_User"") 
                             VALUES ('{codeValue}', '{nameValue}', CURRENT_TIMESTAMP, '{actionType}', '{details}', '{status}', '{B1App.Instance.Company.UserName}')"
                    : $@"INSERT INTO [@BTUN_LOG] ([Code], [Name], U_Date, U_Type, U_Details, U_Status, U_User) 
                             VALUES ('{codeValue}', '{nameValue}', GETDATE(), '{actionType}', '{details}', '{status}', '{B1App.Instance.Company.UserName}')";
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

                bool isHana = B1App.Instance.IsHana;
                int nextCode = UserTableCodeGenerator.GetNext("@BTUN_LOG");
                string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                string nameValue = $"LOG_{codeValue}";
                string sql = isHana
                    ? $"INSERT INTO \"@BTUN_LOG\" (\"Code\", \"Name\", \"U_Date\", \"U_Type\", \"U_Details\", \"U_Status\", \"U_User\") VALUES ('{codeValue}', '{nameValue}', CURRENT_TIMESTAMP, '{actionType}', '{fullDetails}', '{status}', '{userId}')"
                    : $"INSERT INTO [@BTUN_LOG] ([Code], [Name], U_Date, U_Type, U_Details, U_Status, U_User) VALUES ('{codeValue}', '{nameValue}', GETDATE(), '{actionType}', '{fullDetails}', '{status}', '{userId}')";
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
