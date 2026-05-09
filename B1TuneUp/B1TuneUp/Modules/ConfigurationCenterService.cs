using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class ConfigurationCenterService
    {
        public const string MetadataVersionCode = "CONFIG_METADATA_VERSION";
        public const string CurrentMetadataVersion = "2026.05.09";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IList<ConfigurationDiagnosticEntry> RunDiagnostics()
        {
            var items = new List<ConfigurationDiagnosticEntry>();
            AddTableDiagnostic(items, "BTUN_TBOX", "Toolbox / Config store");
            AddTableDiagnostic(items, "BTUN_CODE", "Macro / code snippets");
            AddTableDiagnostic(items, "BTUN_RULES", "Rules");
            AddTableDiagnostic(items, "BTUN_MENUS", "Menus");
            AddTableDiagnostic(items, "BTUN_LOG", "Audit log");

            string version = ToolboxSettingService.GetByCode(MetadataVersionCode)?.Value ?? string.Empty;
            items.Add(new ConfigurationDiagnosticEntry
            {
                Area = "Metadata",
                Name = "Version",
                Status = string.Equals(version, CurrentMetadataVersion, StringComparison.OrdinalIgnoreCase) ? "OK" : "Warning",
                IsOk = string.Equals(version, CurrentMetadataVersion, StringComparison.OrdinalIgnoreCase),
                Detail = string.IsNullOrWhiteSpace(version)
                    ? "Metadata version not initialized."
                    : $"Installed {version}; expected {CurrentMetadataVersion}."
            });

            foreach (var module in ModuleActivationService.GetAll())
            {
                items.Add(new ConfigurationDiagnosticEntry
                {
                    Area = "Module",
                    Name = module.Name,
                    Status = module.Enabled ? "Enabled" : "Disabled",
                    IsOk = true,
                    Detail = module.Key
                });
            }

            return items;
        }

        public static void RepairMetadata()
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = MetadataVersionCode,
                Category = "System",
                Description = "B1TuneUp metadata version",
                Value = CurrentMetadataVersion
            });

            foreach (var module in ModuleActivationService.GetAll())
            {
                ModuleActivationService.Save(module);
            }
        }

        public static ConfigurationPackage BuildPackage(string moduleKey = null)
        {
            var package = new ConfigurationPackage
            {
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Company = SafeCompanyName()
            };

            package.Modules.AddRange(ModuleActivationService.GetAll()
                .Where(m => string.IsNullOrWhiteSpace(moduleKey) || string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Clone()));

            package.Settings.AddRange(ToolboxSettingService.GetAll()
                .Where(s => string.IsNullOrWhiteSpace(moduleKey) || BelongsToModule(s.Code, moduleKey)));

            package.UniversalFunctions.AddRange(UniversalFunctionService.GetAll()
                .Where(f => string.IsNullOrWhiteSpace(moduleKey) || string.Equals(moduleKey, "UniversalFunctions", StringComparison.OrdinalIgnoreCase)));

            package.AuthorizationGroups.AddRange(AuthorizationAdminService.GetGroups()
                .Where(g => string.IsNullOrWhiteSpace(moduleKey) || string.Equals(moduleKey, "Authorization", StringComparison.OrdinalIgnoreCase)));

            return package;
        }

        public static void ExportPackage(string path, string moduleKey = null)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(BuildPackage(moduleKey), JsonOptions));
        }

        public static ConfigurationPackage ImportPackage(string path)
        {
            var package = JsonSerializer.Deserialize<ConfigurationPackage>(File.ReadAllText(path), JsonOptions)
                          ?? new ConfigurationPackage();

            foreach (var module in package.Modules ?? new List<ModuleConfigurationEntry>())
            {
                ModuleActivationService.Save(module);
            }

            foreach (var setting in package.Settings ?? new List<ToolboxSettingEntry>())
            {
                ToolboxSettingService.Save(setting);
            }

            foreach (var function in package.UniversalFunctions ?? new List<UniversalFunctionEntry>())
            {
                UniversalFunctionService.Save(function);
            }

            foreach (var group in package.AuthorizationGroups ?? new List<AuthorizationGroupEntry>())
            {
                AuthorizationAdminService.SaveGroup(group);
            }

            AuthorizationScopeService.Invalidate();
            return package;
        }

        private static void AddTableDiagnostic(ICollection<ConfigurationDiagnosticEntry> items, string tableName, string displayName)
        {
            bool exists = TableExists(tableName);
            items.Add(new ConfigurationDiagnosticEntry
            {
                Area = "Database",
                Name = displayName,
                Status = exists ? "OK" : "Missing",
                IsOk = exists,
                Detail = tableName
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
                return Convert.ToInt32(SapUiSafe.SafeFieldValue(rs, 0) ?? 0) > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static bool BelongsToModule(string code, string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey)) return true;
            if (string.IsNullOrWhiteSpace(code)) return false;
            return code.IndexOf(moduleKey, StringComparison.OrdinalIgnoreCase) >= 0
                   || code.StartsWith(ModuleActivationService.BuildSettingCode(moduleKey), StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeCompanyName()
        {
            try { return B1App.Instance.Company.CompanyName ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
