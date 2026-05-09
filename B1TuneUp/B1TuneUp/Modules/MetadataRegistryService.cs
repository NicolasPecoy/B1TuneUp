using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class MetadataRegistryService
    {
        public static IList<MetadataDefinitionEntry> GetDefinitions()
        {
            var entries = new List<MetadataDefinitionEntry>();
            AddTable(entries, "BTUN_UF", "B1TuneUp Universal Functions");
            AddField(entries, "BTUN_UF", "FuncCode", "Function Code", BoFieldTypes.db_Alpha, 50);
            AddField(entries, "BTUN_UF", "FuncType", "Function Type", BoFieldTypes.db_Alpha, 30);
            AddField(entries, "BTUN_UF", "Payload", "Payload", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_UF", "Params", "Parameters", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_UF", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            AddField(entries, "BTUN_UF", "Scope", "Authorization Scope", BoFieldTypes.db_Memo);

            AddTable(entries, "BTUN_TRIG", "B1TuneUp Unified Triggers");
            AddField(entries, "BTUN_TRIG", "TrigCode", "Trigger Code", BoFieldTypes.db_Alpha, 50);
            AddField(entries, "BTUN_TRIG", "FormType", "Form Type", BoFieldTypes.db_Alpha, 40);
            AddField(entries, "BTUN_TRIG", "ItemID", "Item ID", BoFieldTypes.db_Alpha, 100);
            AddField(entries, "BTUN_TRIG", "ColID", "Column ID", BoFieldTypes.db_Alpha, 100);
            AddField(entries, "BTUN_TRIG", "EventType", "Event Type", BoFieldTypes.db_Alpha, 40);
            AddField(entries, "BTUN_TRIG", "Before", "Before Action", BoFieldTypes.db_Alpha, 1, "N", "Y:Yes;N:No");
            AddField(entries, "BTUN_TRIG", "Condition", "Condition SQL", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_TRIG", "UFCode", "Universal Function Code", BoFieldTypes.db_Alpha, 50);
            AddField(entries, "BTUN_TRIG", "Macro", "Fallback Macro", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_TRIG", "Trace", "Trace Enabled", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            AddField(entries, "BTUN_TRIG", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");

            AddTable(entries, "BTUN_AUTH", "B1TuneUp Authorization Matrix");
            AddField(entries, "BTUN_AUTH", "ObjType", "Object Type", BoFieldTypes.db_Alpha, 40);
            AddField(entries, "BTUN_AUTH", "ObjCode", "Object Code", BoFieldTypes.db_Alpha, 80);
            AddField(entries, "BTUN_AUTH", "AllowUsers", "Allowed Users", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_AUTH", "AllowGrps", "Allowed Groups", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_AUTH", "DenyUsers", "Denied Users", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_AUTH", "DenyGrps", "Denied Groups", BoFieldTypes.db_Memo);

            AddTable(entries, "BTUN_PKG", "B1TuneUp Package History");
            AddField(entries, "BTUN_PKG", "PkgName", "Package Name", BoFieldTypes.db_Alpha, 120);
            AddField(entries, "BTUN_PKG", "PkgVer", "Package Version", BoFieldTypes.db_Alpha, 30);
            AddField(entries, "BTUN_PKG", "DryRun", "Dry Run", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            AddField(entries, "BTUN_PKG", "Summary", "Summary", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_PKG", "Backup", "Backup Path", BoFieldTypes.db_Alpha, 254);

            AddTable(entries, "BTUN_WJOB", "B1TuneUp Worker Jobs");
            AddField(entries, "BTUN_WJOB", "JobType", "Job Type", BoFieldTypes.db_Alpha, 40);
            AddField(entries, "BTUN_WJOB", "Payload", "Payload", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_WJOB", "Params", "Parameters", BoFieldTypes.db_Memo);
            AddField(entries, "BTUN_WJOB", "Status", "Status", BoFieldTypes.db_Alpha, 20);
            AddField(entries, "BTUN_WJOB", "Retries", "Retry Count", BoFieldTypes.db_Numeric, 5);
            AddField(entries, "BTUN_WJOB", "LastError", "Last Error", BoFieldTypes.db_Memo);

            AddTable(entries, "BTUN_PDQUE", "B1TuneUp Print Delivery Queue");
            AddField(entries, "BTUN_PDQUE", "DocType", "Document Type", BoFieldTypes.db_Alpha, 30);
            AddField(entries, "BTUN_PDQUE", "DocEntry", "Document Entry", BoFieldTypes.db_Alpha, 30);
            AddField(entries, "BTUN_PDQUE", "Channel", "Channel", BoFieldTypes.db_Alpha, 20);
            AddField(entries, "BTUN_PDQUE", "Report", "Report Code", BoFieldTypes.db_Alpha, 100);
            AddField(entries, "BTUN_PDQUE", "Status", "Status", BoFieldTypes.db_Alpha, 20);
            AddField(entries, "BTUN_PDQUE", "Output", "Output File", BoFieldTypes.db_Alpha, 254);
            AddField(entries, "BTUN_PDQUE", "LastError", "Last Error", BoFieldTypes.db_Memo);
            return entries;
        }

        public static IList<MetadataDefinitionEntry> Validate()
        {
            var definitions = GetDefinitions();
            foreach (var item in definitions)
            {
                item.Exists = item.IsTable ? TableExists(item.TableName) : FieldExists(item.TableName, item.FieldName);
            }
            return definitions;
        }

        public static string BackupState()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "metadata-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(Validate(), new JsonSerializerOptions { WriteIndented = true }));
            return path;
        }

        public static void Repair()
        {
            BackupState();
            foreach (var table in GetDefinitions().Where(d => d.IsTable))
            {
                MetadataManager.CreateUDT(table.TableName, table.TableDescription, BoUTBTableType.bott_NoObject);
            }

            foreach (var field in GetDefinitions().Where(d => !d.IsTable))
            {
                MetadataManager.CreateUDF("@" + field.TableName, field.FieldName, field.FieldDescription, field.FieldType, field.Size, field.DefaultValue ?? string.Empty, field.ValidValues ?? string.Empty);
            }
        }

        private static void AddTable(ICollection<MetadataDefinitionEntry> list, string name, string description)
        {
            list.Add(new MetadataDefinitionEntry { TableName = name, TableDescription = description, IsTable = true });
        }

        private static void AddField(ICollection<MetadataDefinitionEntry> list, string table, string name, string description, BoFieldTypes type, int size = 0, string defaultValue = "", string validValues = "")
        {
            list.Add(new MetadataDefinitionEntry
            {
                TableName = table,
                FieldName = name,
                FieldDescription = description,
                FieldType = type,
                Size = size,
                DefaultValue = defaultValue,
                ValidValues = validValues
            });
        }

        private static bool TableExists(string tableName)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string safe = tableName.Replace("'", "''");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT COUNT(*) FROM OUTB WHERE \"TableName\" = '{safe}'"
                    : $"SELECT COUNT(*) FROM OUTB WITH (NOLOCK) WHERE TableName = '{safe}'";
                rs.DoQuery(sql);
                return Convert.ToInt32(SapUiSafe.SafeFieldValue(rs, 0, 0)) > 0;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"MetadataRegistryService.TableExists:{tableName}");
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static bool FieldExists(string tableName, string fieldName)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string tableId = "@" + tableName.Replace("'", "''");
                string alias = fieldName.Replace("'", "''");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT COUNT(*) FROM CUFD WHERE \"TableID\" = '{tableId}' AND \"AliasID\" = '{alias}'"
                    : $"SELECT COUNT(*) FROM CUFD WITH (NOLOCK) WHERE TableID = '{tableId}' AND AliasID = '{alias}'";
                rs.DoQuery(sql);
                return Convert.ToInt32(SapUiSafe.SafeFieldValue(rs, 0, 0)) > 0;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"MetadataRegistryService.FieldExists:{tableName}.{fieldName}");
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }
    }
}
