using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    public static class UniversalFunctionService
    {
        private const string Prefix = "UF_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static readonly string[] SupportedTypes =
        {
            "SQL",
            "Macro",
            "Message",
            "ExternalApp",
            "CrystalReport",
            "LineLoop",
            "HTTP",
            "Email",
            "File",
            "DIObject",
            "DotNetSnippet"
        };

        public static IList<UniversalFunctionEntry> GetAll()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && s.Code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(ReadEntry)
                .Where(e => e != null)
                .OrderBy(e => e.Code)
                .ToList();
        }

        public static UniversalFunctionEntry GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var setting = ToolboxSettingService.GetByCode(Prefix + NormalizeCode(code));
            return setting == null ? null : ReadEntry(setting);
        }

        public static UniversalFunctionEntry Save(UniversalFunctionEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code)) throw new InvalidOperationException("El codigo de Universal Function es obligatorio.");

            entry.Code = NormalizeCode(entry.Code);
            if (!SupportedTypes.Any(t => string.Equals(t, entry.Type, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Tipo de Universal Function no soportado: {entry.Type}");
            }

            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = Prefix + entry.Code,
                Category = "UniversalFunctions",
                Description = entry.Name ?? entry.Code,
                Value = JsonSerializer.Serialize(entry, JsonOptions)
            });
            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            ToolboxSettingService.Delete(Prefix + NormalizeCode(code));
        }

        public static string Execute(string code, Form form = null, int rowOverride = -1)
        {
            var entry = GetByCode(code);
            if (entry == null) throw new InvalidOperationException($"Universal Function '{code}' no existe.");
            return Execute(entry, form, rowOverride);
        }

        public static string Execute(UniversalFunctionEntry entry, Form form = null, int rowOverride = -1)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (!entry.Active) return string.Empty;
            if (!AuthorizationScopeService.MatchesScope(entry.AllowedUsers, entry.AllowedGroups, entry.DeniedUsers, entry.DeniedGroups))
            {
                throw new UnauthorizedAccessException($"No autorizado para ejecutar Universal Function '{entry.Code}'.");
            }

            form = form ?? SapUiSafe.TryGetActiveForm();
            string payload = MacroEngine.ProcessSqlVariables(entry.Payload ?? string.Empty, form, rowOverride);
            string type = entry.Type ?? "Macro";

            if (type.Equals("SQL", StringComparison.OrdinalIgnoreCase)) return ExecuteSql(payload);
            if (type.Equals("Macro", StringComparison.OrdinalIgnoreCase)) { MacroEngine.ExecuteMacro(payload, form, rowOverride); return string.Empty; }
            if (type.Equals("Message", StringComparison.OrdinalIgnoreCase)) { B1App.Instance.Application.MessageBox(payload); return payload; }
            if (type.Equals("ExternalApp", StringComparison.OrdinalIgnoreCase)) return ExecuteExternal(payload, entry.Parameters, form, rowOverride);
            if (type.Equals("CrystalReport", StringComparison.OrdinalIgnoreCase)) return ExecuteReport(payload);
            if (type.Equals("LineLoop", StringComparison.OrdinalIgnoreCase)) return ExecuteLineLoop(entry, form);
            if (type.Equals("HTTP", StringComparison.OrdinalIgnoreCase)) return ExecuteHttp(payload, entry.Parameters);
            if (type.Equals("Email", StringComparison.OrdinalIgnoreCase)) { EmailManager.SendEmail(payload); return payload; }
            if (type.Equals("File", StringComparison.OrdinalIgnoreCase)) return ExecuteFile(payload, entry.Parameters);
            if (type.Equals("DIObject", StringComparison.OrdinalIgnoreCase)) return ExecuteDiObject(payload);
            if (type.Equals("DotNetSnippet", StringComparison.OrdinalIgnoreCase)) return ExecuteSnippet(entry, form);

            throw new InvalidOperationException($"Tipo de Universal Function no soportado: {entry.Type}");
        }

        private static string ExecuteSql(string sql)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                if (rs.EoF) return string.Empty;
                return SapUiSafe.SafeField(rs, 0);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string ExecuteExternal(string path, string args, Form form, int rowOverride)
        {
            var info = new ProcessStartInfo
            {
                FileName = path,
                Arguments = MacroEngine.ProcessSqlVariables(args ?? string.Empty, form, rowOverride),
                UseShellExecute = true
            };
            Process.Start(info);
            return path;
        }

        private static string ExecuteReport(string reportCode)
        {
            var parameters = ReportManager.GetReportParameters(reportCode);
            ReportManager.ShowAdvancedPrintPreview(reportCode, parameters);
            return reportCode;
        }

        private static string ExecuteLineLoop(UniversalFunctionEntry entry, Form form)
        {
            var parts = SplitParameters(entry.Parameters);
            if (parts.Length == 0) throw new InvalidOperationException("LineLoop requiere matrixId en Parameters.");
            var matrixId = parts[0];
            var matrix = SapUiSafe.TryGetSpecific<Matrix>(form, matrixId);
            if (matrix == null) return string.Empty;

            for (int row = 1; row <= matrix.RowCount; row++)
            {
                MacroEngine.ExecuteMacro(entry.Payload, form, row);
            }

            return matrix.RowCount.ToString();
        }

        private static string ExecuteHttp(string url, string parameters)
        {
            var method = "GET";
            var body = string.Empty;
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                var options = JsonSerializer.Deserialize<Dictionary<string, string>>(parameters);
                if (options != null)
                {
                    if (options.TryGetValue("method", out var configuredMethod)) method = configuredMethod;
                    if (options.TryGetValue("body", out var configuredBody)) body = configuredBody;
                }
            }

            using (var client = new HttpClient())
            {
                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    return client.PostAsync(url, new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json"))
                        .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }

                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
        }

        private static string ExecuteFile(string path, string parameters)
        {
            string action = parameters ?? "Open";
            if (action.Equals("Read", StringComparison.OrdinalIgnoreCase)) return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            if (action.Equals("CreateFolder", StringComparison.OrdinalIgnoreCase)) { Directory.CreateDirectory(path); return path; }
            if (action.Equals("Delete", StringComparison.OrdinalIgnoreCase)) { if (File.Exists(path)) File.Delete(path); return path; }
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            return path;
        }

        private static string ExecuteDiObject(string payload)
        {
            B1App.Instance.Application.SetStatusBarMessage($"DI Object action queued: {payload}", BoMessageTime.bmt_Short, false);
            return payload;
        }

        private static string ExecuteSnippet(UniversalFunctionEntry entry, Form form)
        {
            string codeName = entry.Payload;
            if (string.IsNullOrWhiteSpace(codeName)) codeName = entry.Code;
            DynamicCodeEngine.RunCode(codeName, form);
            return codeName;
        }

        private static UniversalFunctionEntry ReadEntry(ToolboxSettingEntry setting)
        {
            try
            {
                var entry = JsonSerializer.Deserialize<UniversalFunctionEntry>(setting.Value ?? string.Empty, JsonOptions);
                if (entry == null) return null;
                entry.Code = NormalizeCode(entry.Code);
                return entry;
            }
            catch
            {
                return new UniversalFunctionEntry
                {
                    Code = NormalizeCode(setting.Code?.Substring(Prefix.Length)),
                    Name = setting.Description,
                    Type = "Macro",
                    Payload = setting.Value
                };
            }
        }

        private static string NormalizeCode(string code)
        {
            code = (code ?? string.Empty).Trim();
            if (code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) code = code.Substring(Prefix.Length);
            return code.Replace(" ", "_").ToUpperInvariant();
        }

        private static string[] SplitParameters(string raw)
        {
            return (raw ?? string.Empty)
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
    }
}
