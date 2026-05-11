using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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
        private static readonly Dictionary<string, string> Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            "DotNetSnippet",
            "ContentCreator",
            "SQLReport",
            "FileExporter",
            "FileImporter",
            "CreateActivity",
            "Dashboard",
            "InternalMessage",
            "DataLauncher"
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
            if (!MacroEngine.CheckCondition(entry.Condition, form)) return string.Empty;

            int attempts = Math.Max(0, entry.RetryCount) + 1;
            Exception lastError = null;
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    string result = ExecuteCore(entry, form, rowOverride);
                    StoreVariable(entry.ResultVariable, result);
                    ExecuteChain(entry.OnSuccessFunction, form, rowOverride);
                    AuditLogManager.LogAction("UniversalFunction", $"{entry.Code} ejecutada.", "Success");
                    return result;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    ExceptionLogger.LogHandled(ex, $"UniversalFunctionService.Execute:{entry.Code}:attempt:{attempt}");
                    if (attempt < attempts && entry.RetryDelayMs > 0) Thread.Sleep(entry.RetryDelayMs);
                }
            }

            ExecuteChain(entry.OnErrorFunction, form, rowOverride);
            if (entry.ContinueOnError) return lastError?.Message ?? string.Empty;
            throw lastError ?? new InvalidOperationException($"Universal Function '{entry.Code}' fallo.");
        }

        private static string ExecuteCore(UniversalFunctionEntry entry, Form form, int rowOverride)
        {
            string payload = ResolveVariables(MacroEngine.ProcessSqlVariables(entry.Payload ?? string.Empty, form, rowOverride));
            string parameters = ResolveVariables(MacroEngine.ProcessSqlVariables(entry.Parameters ?? string.Empty, form, rowOverride));
            string type = entry.Type ?? "Macro";

            if (type.Equals("SQL", StringComparison.OrdinalIgnoreCase)) return ExecuteSql(payload);
            if (type.Equals("Macro", StringComparison.OrdinalIgnoreCase)) { MacroEngine.ExecuteMacro(payload, form, rowOverride); return string.Empty; }
            if (type.Equals("Message", StringComparison.OrdinalIgnoreCase)) { B1App.Instance.Application.MessageBox(payload); return payload; }
            if (type.Equals("ExternalApp", StringComparison.OrdinalIgnoreCase)) return ExecuteExternal(payload, parameters, form, rowOverride);
            if (type.Equals("CrystalReport", StringComparison.OrdinalIgnoreCase)) return ExecuteReport(payload, parameters);
            if (type.Equals("LineLoop", StringComparison.OrdinalIgnoreCase)) return ExecuteLineLoop(entry, form);
            if (type.Equals("HTTP", StringComparison.OrdinalIgnoreCase)) return ExecuteHttp(payload, parameters);
            if (type.Equals("Email", StringComparison.OrdinalIgnoreCase)) { EmailManager.SendEmail(payload); return payload; }
            if (type.Equals("File", StringComparison.OrdinalIgnoreCase)) return ExecuteFile(payload, parameters);
            if (type.Equals("DIObject", StringComparison.OrdinalIgnoreCase)) return ExecuteDiObject(payload);
            if (type.Equals("DotNetSnippet", StringComparison.OrdinalIgnoreCase)) return ExecuteSnippet(entry, form);
            if (type.Equals("ContentCreator", StringComparison.OrdinalIgnoreCase)) return ExecuteContentCreator(payload, parameters, form, rowOverride);
            if (type.Equals("SQLReport", StringComparison.OrdinalIgnoreCase)) return ExecuteSqlReport(payload, parameters);
            if (type.Equals("FileExporter", StringComparison.OrdinalIgnoreCase)) return ExecuteFileExporter(payload, parameters);
            if (type.Equals("FileImporter", StringComparison.OrdinalIgnoreCase)) return ExecuteFileImporter(payload, parameters, form);
            if (type.Equals("CreateActivity", StringComparison.OrdinalIgnoreCase)) return ExecuteCreateActivity(payload, parameters);
            if (type.Equals("Dashboard", StringComparison.OrdinalIgnoreCase)) { DashboardManager.ShowDashboard(); return payload; }
            if (type.Equals("InternalMessage", StringComparison.OrdinalIgnoreCase)) { B1App.Instance.Application.SetStatusBarMessage(payload, BoMessageTime.bmt_Short, false); return payload; }
            if (type.Equals("DataLauncher", StringComparison.OrdinalIgnoreCase)) return ExecuteDataLauncher(payload, parameters, form, rowOverride);

            throw new InvalidOperationException($"Tipo de Universal Function no soportado: {entry.Type}");
        }

        public static UniversalFunctionTestResult Test(string code, Form form = null, int rowOverride = -1)
        {
            var started = DateTime.UtcNow;
            var entry = GetByCode(code);
            var result = new UniversalFunctionTestResult
            {
                FunctionCode = code,
                FunctionType = entry?.Type,
                StartedAtUtc = started
            };
            try
            {
                if (entry == null) throw new InvalidOperationException($"Universal Function '{code}' no existe.");
                result.Chain = SplitParameters(entry.OnSuccessFunction).Concat(SplitParameters(entry.OnErrorFunction)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                result.Result = Execute(entry, form, rowOverride);
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                ExceptionLogger.LogHandled(ex, $"UniversalFunctionService.Test:{code}");
                return result;
            }
            finally
            {
                result.FinishedAtUtc = DateTime.UtcNow;
                AuditLogManager.LogAction("UniversalFunctionTest", $"{code}: {(result.Success ? "OK" : result.Error)}", result.Success ? "Success" : "Error");
            }
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

        private static string ExecuteReport(string reportCode, string parametersJson)
        {
            var parameters = ReadStringDictionary(parametersJson) ?? ReportManager.GetReportParameters(reportCode);
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
            var timeoutSeconds = 100;
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var authMode = string.Empty;
            var authUser = string.Empty;
            var authSecret = string.Empty;
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                var options = ReadStringDictionary(parameters);
                if (options != null)
                {
                    if (options.TryGetValue("method", out var configuredMethod)) method = configuredMethod;
                    if (options.TryGetValue("body", out var configuredBody)) body = configuredBody;
                    if (options.TryGetValue("timeoutSeconds", out var timeoutRaw)) int.TryParse(timeoutRaw, out timeoutSeconds);
                    if (options.TryGetValue("authMode", out var configuredAuth)) authMode = configuredAuth;
                    if (options.TryGetValue("authUser", out var configuredUser)) authUser = configuredUser;
                    if (options.TryGetValue("authSecret", out var configuredSecret)) authSecret = configuredSecret;
                    if (options.TryGetValue("headers", out var rawHeaders)) headers = ParseHeaders(rawHeaders);
                }
            }

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds <= 0 ? 100 : timeoutSeconds);
                foreach (var header in headers) client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                ApplyAuth(client, authMode, authUser, authSecret);

                if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    return client.GetStringAsync(url).GetAwaiter().GetResult();
                }

                var request = new HttpRequestMessage(new HttpMethod(method), url)
                {
                    Content = new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json")
                };
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return content;
            }
        }

        private static string ExecuteFile(string path, string parameters)
        {
            var options = ReadStringDictionary(parameters);
            string action = options != null && options.TryGetValue("action", out var configuredAction) ? configuredAction : (parameters ?? "Open");
            string content = options != null && options.TryGetValue("content", out var configuredContent) ? configuredContent : string.Empty;
            string target = options != null && options.TryGetValue("target", out var configuredTarget) ? configuredTarget : string.Empty;
            if (action.Equals("Read", StringComparison.OrdinalIgnoreCase)) return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            if (action.Equals("Write", StringComparison.OrdinalIgnoreCase)) { File.WriteAllText(path, content ?? string.Empty); return path; }
            if (action.Equals("Append", StringComparison.OrdinalIgnoreCase)) { File.AppendAllText(path, content ?? string.Empty); return path; }
            if (action.Equals("Copy", StringComparison.OrdinalIgnoreCase)) { File.Copy(path, target, true); return target; }
            if (action.Equals("Move", StringComparison.OrdinalIgnoreCase)) { File.Move(path, target); return target; }
            if (action.Equals("Exists", StringComparison.OrdinalIgnoreCase)) return (File.Exists(path) || Directory.Exists(path)).ToString();
            if (action.Equals("List", StringComparison.OrdinalIgnoreCase)) return Directory.Exists(path) ? string.Join(Environment.NewLine, Directory.GetFileSystemEntries(path)) : string.Empty;
            if (action.Equals("CreateFolder", StringComparison.OrdinalIgnoreCase)) { Directory.CreateDirectory(path); return path; }
            if (action.Equals("Delete", StringComparison.OrdinalIgnoreCase)) { if (File.Exists(path)) File.Delete(path); else if (Directory.Exists(path)) Directory.Delete(path, false); return path; }
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            return path;
        }

        private static string ExecuteDiObject(string payload)
        {
            var options = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload ?? "{}", JsonOptions) ?? new Dictionary<string, JsonElement>();
            string objectType = GetJsonString(options, "objectType");
            string mode = GetJsonString(options, "mode") ?? "Add";
            string key = GetJsonString(options, "key");
            if (string.IsNullOrWhiteSpace(objectType)) throw new InvalidOperationException("DIObject requiere objectType.");
            var boType = ParseObjectType(objectType);
            object sapObject = B1App.Instance.Company.GetBusinessObject(boType);
            try
            {
                if (mode.Equals("Update", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(key))
                {
                    InvokeIfExists(sapObject, "GetByKey", key);
                }

                if (options.TryGetValue("fields", out var fields) && fields.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in fields.EnumerateObject()) SetProperty(sapObject, field.Name, field.Value);
                }

                if (options.TryGetValue("userFields", out var userFields) && userFields.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in userFields.EnumerateObject()) SetUserField(sapObject, field.Name, field.Value);
                }

                int result = mode.Equals("Update", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt32(InvokeIfExists(sapObject, "Update"))
                    : Convert.ToInt32(InvokeIfExists(sapObject, "Add"));
                if (result != 0) throw new InvalidOperationException(B1App.Instance.Company.GetLastErrorDescription());
                string newKey;
                B1App.Instance.Company.GetNewObjectCode(out newKey);
                return string.IsNullOrWhiteSpace(newKey) ? objectType : newKey;
            }
            finally
            {
                ComObjectManager.Release(sapObject);
            }
        }

        private static string ExecuteSnippet(UniversalFunctionEntry entry, Form form)
        {
            string codeName = entry.Payload;
            if (string.IsNullOrWhiteSpace(codeName)) codeName = entry.Code;
            DynamicCodeEngine.RunCode(codeName, form);
            return codeName;
        }

        private static string ExecuteContentCreator(string template, string parameters, Form form, int rowOverride)
        {
            var values = ReadStringDictionary(parameters) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string result = template ?? string.Empty;
            foreach (var pair in values)
            {
                result = result.Replace("{{" + pair.Key + "}}", ResolveVariables(MacroEngine.ProcessSqlVariables(pair.Value ?? string.Empty, form, rowOverride)));
            }
            result = ResolveVariables(MacroEngine.ProcessSqlVariables(result, form, rowOverride));
            if (values.TryGetValue("targetFile", out var targetFile) && !string.IsNullOrWhiteSpace(targetFile))
            {
                File.WriteAllText(targetFile, result, Encoding.UTF8);
                return targetFile;
            }
            return result;
        }

        private static string ExecuteSqlReport(string sql, string parameters)
        {
            var options = ReadStringDictionary(parameters) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string delimiter = options.TryGetValue("delimiter", out var configuredDelimiter) ? configuredDelimiter : ",";
            string targetFile = options.TryGetValue("targetFile", out var configuredTarget) ? configuredTarget : string.Empty;
            var csv = new StringBuilder();
            Recordset rs = null;
            try
            {
                SearchSqlSecurityService.ValidateSelectOnly(sql);
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                for (int i = 0; i < rs.Fields.Count; i++)
                {
                    if (i > 0) csv.Append(delimiter);
                    csv.Append(EscapeCsv(SapUiSafe.SafeFieldName(rs, i), delimiter));
                }
                csv.AppendLine();
                while (!rs.EoF)
                {
                    for (int i = 0; i < rs.Fields.Count; i++)
                    {
                        if (i > 0) csv.Append(delimiter);
                        csv.Append(EscapeCsv(SapUiSafe.SafeField(rs, i), delimiter));
                    }
                    csv.AppendLine();
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            if (!string.IsNullOrWhiteSpace(targetFile))
            {
                File.WriteAllText(targetFile, csv.ToString(), Encoding.UTF8);
                return targetFile;
            }
            return csv.ToString();
        }

        private static string ExecuteFileExporter(string payload, string parameters)
        {
            var options = ReadStringDictionary(parameters) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string targetFile = options.TryGetValue("targetFile", out var target) ? target : payload;
            string sql = options.TryGetValue("sql", out var configuredSql) ? configuredSql : payload;
            return ExecuteSqlReport(sql, JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["targetFile"] = targetFile,
                ["delimiter"] = options.TryGetValue("delimiter", out var delimiter) ? delimiter : ","
            }, JsonOptions));
        }

        private static string ExecuteFileImporter(string path, string parameters, Form form)
        {
            var options = ReadStringDictionary(parameters) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string mode = options.TryGetValue("mode", out var configuredMode) ? configuredMode : "MacroPerLine";
            if (!File.Exists(path)) throw new FileNotFoundException("Archivo de importacion no encontrado.", path);
            if (mode.Equals("MacroPerLine", StringComparison.OrdinalIgnoreCase))
            {
                string macro = options.TryGetValue("macro", out var configuredMacro) ? configuredMacro : string.Empty;
                int count = 0;
                foreach (var line in File.ReadAllLines(path))
                {
                    Variables["line"] = line ?? string.Empty;
                    MacroEngine.ExecuteMacro(ResolveVariables(macro), form);
                    count++;
                }
                return count.ToString();
            }
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string ExecuteCreateActivity(string payload, string parameters)
        {
            var fields = ReadStringDictionary(string.IsNullOrWhiteSpace(parameters) ? payload : parameters)
                         ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            object activity = B1App.Instance.Company.GetBusinessObject(BoObjectTypes.oContacts);
            try
            {
                foreach (var field in fields)
                {
                    try { activity.GetType().InvokeMember(field.Key, BindingFlags.SetProperty, null, activity, new object[] { field.Value }); }
                    catch (Exception ex) { ExceptionLogger.LogHandled(ex, $"UniversalFunctionService.CreateActivity.Set:{field.Key}"); }
                }
                int result = Convert.ToInt32(activity.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, activity, Array.Empty<object>()));
                if (result != 0) throw new InvalidOperationException(B1App.Instance.Company.GetLastErrorDescription());
                string newKey;
                B1App.Instance.Company.GetNewObjectCode(out newKey);
                return newKey;
            }
            finally
            {
                ComObjectManager.Release(activity);
            }
        }

        private static string ExecuteDataLauncher(string payload, string parameters, Form form, int rowOverride)
        {
            var options = ReadStringDictionary(parameters) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (options.TryGetValue("macro", out var macro) && !string.IsNullOrWhiteSpace(macro))
            {
                MacroEngine.ExecuteMacro(macro, form, rowOverride);
                return macro;
            }
            if (options.TryGetValue("formType", out var formType) && !string.IsNullOrWhiteSpace(formType))
            {
                B1App.Instance.Application.ActivateMenuItem(formType);
                return formType;
            }
            if (!string.IsNullOrWhiteSpace(payload))
            {
                MacroEngine.ExecuteMacro(payload, form, rowOverride);
                return payload;
            }
            return string.Empty;
        }

        private static string EscapeCsv(string value, string delimiter)
        {
            value = value ?? string.Empty;
            if (value.Contains("\"") || value.Contains("\r") || value.Contains("\n") || value.Contains(delimiter))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
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

        private static void ExecuteChain(string codes, Form form, int rowOverride)
        {
            foreach (var code in SplitParameters(codes))
            {
                Execute(code, form, rowOverride);
            }
        }

        private static void StoreVariable(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            lock (Variables) Variables[name.Trim()] = value ?? string.Empty;
        }

        private static string ResolveVariables(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            lock (Variables)
            {
                foreach (var variable in Variables)
                {
                    value = value.Replace("${" + variable.Key + "}", variable.Value ?? string.Empty);
                }
            }
            return value;
        }

        private static Dictionary<string, string> ReadStringDictionary(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, string> ParseHeaders(string raw)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return headers;
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(raw, JsonOptions);
                if (parsed != null) return parsed;
            }
            catch { }

            foreach (var part in raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split(new[] { '=' }, 2);
                if (pieces.Length == 2) headers[pieces[0].Trim()] = pieces[1].Trim();
            }
            return headers;
        }

        private static void ApplyAuth(HttpClient client, string mode, string user, string secret)
        {
            if (string.IsNullOrWhiteSpace(mode) || mode.Equals("None", StringComparison.OrdinalIgnoreCase)) return;
            if (mode.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
            }
            else if (mode.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes((user ?? string.Empty) + ":" + (secret ?? string.Empty)));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
            }
            else if (mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(string.IsNullOrWhiteSpace(user) ? "X-API-Key" : user, secret);
            }
        }

        private static BoObjectTypes ParseObjectType(string value)
        {
            if (int.TryParse(value, out var numeric)) return (BoObjectTypes)numeric;
            return (BoObjectTypes)Enum.Parse(typeof(BoObjectTypes), value, true);
        }

        private static string GetJsonString(Dictionary<string, JsonElement> values, string name)
        {
            return values.TryGetValue(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.ToString() : null;
        }

        private static object InvokeIfExists(object target, string method, params object[] args)
        {
            return target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args);
        }

        private static void SetProperty(object target, string name, JsonElement value)
        {
            object converted = ConvertJson(value);
            target.GetType().InvokeMember(name, BindingFlags.SetProperty, null, target, new[] { converted });
        }

        private static void SetUserField(object target, string name, JsonElement value)
        {
            try
            {
                dynamic dyn = target;
                dyn.UserFields.Fields.Item(name).Value = ConvertJson(value);
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"UniversalFunctionService.SetUserField:{name}");
            }
        }

        private static object ConvertJson(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number)
            {
                if (value.TryGetInt32(out var i)) return i;
                if (value.TryGetDouble(out var d)) return d;
            }
            if (value.ValueKind == JsonValueKind.True) return true;
            if (value.ValueKind == JsonValueKind.False) return false;
            return value.ToString();
        }
    }
}
