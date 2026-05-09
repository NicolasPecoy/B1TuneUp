using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class IntegrationConfigService
    {
        private const string TableName = "@BTUN_INTCFG";

        public static IList<IntegrationConfig> GetAll()
        {
            var configs = new List<IntegrationConfig>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"Code\",\"Name\",\"U_Channel\",\"U_Method\",\"U_Endpoint\",\"U_Headers\",\"U_Body\",\"U_AuthMode\",\"U_AuthUser\",\"U_AuthSecret\",\"U_Schedule\",\"U_Handler\",\"U_Notes\",\"U_Active\" FROM \"@BTUN_INTCFG\" ORDER BY \"Name\""
                    : "SELECT [Code],[Name],[U_Channel],[U_Method],[U_Endpoint],[U_Headers],[U_Body],[U_AuthMode],[U_AuthUser],[U_AuthSecret],[U_Schedule],[U_Handler],[U_Notes],[U_Active] FROM [@BTUN_INTCFG] ORDER BY [Name]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    configs.Add(new IntegrationConfig
                    {
                        Code = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0),
                        Name = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1),
                        Channel = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 2),
                        Method = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 3),
                        Endpoint = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 4),
                        Headers = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 5),
                        Body = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 6),
                        AuthMode = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 7),
                        AuthUser = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 8),
                        AuthSecret = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 9),
                        ScheduleMinutes = ConvertToNullableInt(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 10)),
                        HandlerMacro = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 11),
                        Notes = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 12),
                        Active = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 13) != "N"
                    });
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return configs;
        }

        public static IntegrationConfig Save(IntegrationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_INTCFG");
                bool exists = !string.IsNullOrEmpty(config.Code) && table.GetByKey(config.Code);
                if (!exists)
                {
                    config.Code = string.IsNullOrEmpty(config.Code) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : config.Code;
                    table.Code = config.Code;
                    table.Name = string.IsNullOrWhiteSpace(config.Name) ? config.Code : config.Name;
                }
                else
                {
                    table.Name = string.IsNullOrWhiteSpace(config.Name) ? config.Code : config.Name;
                }

                table.UserFields.Fields.Item("U_Channel").Value = config.Channel ?? "REST";
                table.UserFields.Fields.Item("U_Method").Value = config.Method ?? "GET";
                table.UserFields.Fields.Item("U_Endpoint").Value = config.Endpoint ?? string.Empty;
                table.UserFields.Fields.Item("U_Headers").Value = NormalizeHeaderString(config.Headers);
                table.UserFields.Fields.Item("U_Body").Value = config.Body ?? string.Empty;
                table.UserFields.Fields.Item("U_AuthMode").Value = config.AuthMode ?? "None";
                table.UserFields.Fields.Item("U_AuthUser").Value = config.AuthUser ?? string.Empty;
                table.UserFields.Fields.Item("U_AuthSecret").Value = config.AuthSecret ?? string.Empty;
                table.UserFields.Fields.Item("U_Schedule").Value = config.ScheduleMinutes ?? 0;
                table.UserFields.Fields.Item("U_Handler").Value = config.HandlerMacro ?? string.Empty;
                table.UserFields.Fields.Item("U_Notes").Value = config.Notes ?? string.Empty;
                table.UserFields.Fields.Item("U_Active").Value = config.Active ? "Y" : "N";

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

            return config;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_INTCFG");
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

        public static Task<string> TestConnectionAsync(IntegrationConfig config)
        {
            return Task.Run(() => ExecuteIntegration(config));
        }

        public static string ExecuteIntegration(IntegrationConfig config, string overrideBody = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            string body = overrideBody ?? config.Body ?? string.Empty;
            string preparedHeaders = BuildHeadersForExecution(config);

            if (string.Equals(config.Channel, "SOAP", StringComparison.OrdinalIgnoreCase))
            {
                return IntegrationManager.CallSoap(config.Endpoint, config.Method, body);
            }

            string method = string.IsNullOrWhiteSpace(config.Method) ? "GET" : config.Method;
            var payload = string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ? null : body;
            return IntegrationManager.CallRest(config.Endpoint, method, payload, preparedHeaders);
        }

        public static void StartRealtimeMonitor(IntegrationConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.Code)) return;
            if (!config.Active || !config.ScheduleMinutes.HasValue || config.ScheduleMinutes.Value <= 0) return;
            if (string.IsNullOrWhiteSpace(config.HandlerMacro)) return;
            int intervalSeconds = Math.Max(5, config.ScheduleMinutes.Value) * 60;
            if (string.Equals(config.Channel, "SOAP", StringComparison.OrdinalIgnoreCase))
            {
                IntegrationManager.StartRealTimeSync(
                    config.Code,
                    () => IntegrationManager.CallSoap(config.Endpoint, config.Method, config.Body ?? string.Empty),
                    intervalSeconds,
                    config.HandlerMacro);
            }
            else
            {
                var method = string.IsNullOrWhiteSpace(config.Method) ? "GET" : config.Method;
                var headers = BuildHeadersForExecution(config);
                var body = string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ? null : config.Body;
                IntegrationManager.StartRealTimeSync(config.Code, config.Endpoint, intervalSeconds, config.HandlerMacro, method, headers, body);
            }
        }

        public static void StopRealtimeMonitor(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            IntegrationManager.StopRealTimeSync(code);
        }

        private static int? ConvertToNullableInt(object value)
        {
            try
            {
                if (value == null) return null;
                if (int.TryParse(value.ToString(), out int parsed)) return parsed;
            }
            catch { }
            return null;
        }

        private static string NormalizeHeaderString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var parts = SplitHeaders(raw);
            return string.Join("|", parts.Select(p => $"{p.Key}={p.Value}"));
        }

        private static string BuildHeadersForExecution(IntegrationConfig config)
        {
            var headers = SplitHeaders(config.Headers);
            ApplyAuthHeaders(config, headers);
            if (headers.Count == 0) return null;
            return string.Join("|", headers.Select(p => $"{p.Key}={p.Value}"));
        }

        private static Dictionary<string, string> SplitHeaders(string raw)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return dict;
            var tokens = raw.Split(new[] { '\r', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var pair = token.Split(new[] { '=' }, 2);
                if (pair.Length == 2)
                {
                    var key = pair[0].Trim();
                    var value = pair[1].Trim();
                    if (!string.IsNullOrEmpty(key)) dict[key] = value;
                }
            }
            return dict;
        }

        private static void ApplyAuthHeaders(IntegrationConfig config, IDictionary<string, string> headers)
        {
            if (config == null || headers == null) return;
            switch ((config.AuthMode ?? "None").Trim())
            {
                case "Basic":
                    var user = config.AuthUser ?? string.Empty;
                    var pass = config.AuthSecret ?? string.Empty;
                    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
                    headers["Authorization"] = $"Basic {encoded}";
                    break;
                case "Bearer":
                    headers["Authorization"] = $"Bearer {config.AuthSecret}";
                    break;
                case "ApiKey":
                    var headerName = string.IsNullOrWhiteSpace(config.AuthUser) ? "X-API-KEY" : config.AuthUser.Trim();
                    headers[headerName] = config.AuthSecret ?? string.Empty;
                    break;
            }
        }
    }
}
