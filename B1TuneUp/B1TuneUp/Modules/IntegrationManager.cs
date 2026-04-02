using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Xml;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    public static class IntegrationManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Dictionary<string, Timer> _syncTimers = new Dictionary<string, Timer>();
        private static readonly object _syncLock = new object();

        // Simple REST caller (synchronous wrapper)
        public static string CallRest(string url, string method, string body = null, string headersSerialized = null)
        {
            try
            {
                var req = new HttpRequestMessage(new HttpMethod(method ?? "GET"), url);
                if (!string.IsNullOrEmpty(body)) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(headersSerialized))
                {
                    var headers = ParseKeyValueString(headersSerialized);
                    foreach (var kv in headers)
                    {
                        try { req.Headers.Add(kv.Key, kv.Value); } catch { }
                    }
                }

                var resp = _httpClient.SendAsync(req).GetAwaiter().GetResult();
                var content = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return content;
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

        // Simple SOAP caller - wraps body in a SOAP Envelope if needed
        public static string CallSoap(string url, string action, string soapBody)
        {
            try
            {
                var envelope = WrapInSoapEnvelope(soapBody);
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");
                if (!string.IsNullOrEmpty(action)) req.Headers.Add("SOAPAction", action);

                var resp = _httpClient.SendAsync(req).GetAwaiter().GetResult();
                var content = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return content;
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

        private static string WrapInSoapEnvelope(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            if (body.TrimStart().StartsWith("<?xml")) return body; // assume full envelope
            return $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\n  <soap:Body>\n    {body}\n  </soap:Body>\n</soap:Envelope>";
        }

        private static Dictionary<string, string> ParseKeyValueString(string raw)
        {
            var d = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(raw)) return d;
            var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var idx = p.IndexOf('=');
                if (idx > 0)
                {
                    var k = p.Substring(0, idx);
                    var v = p.Substring(idx + 1);
                    d[k] = v;
                }
            }
            return d;
        }

        // Real-time sync using polling - calls a macro when new data is detected (simple change-detection by response content hash)
        public static void StartRealTimeSync(string id, string url, int intervalSeconds, string handlerMacro, string method = "GET", string headersSerialized = null, string body = null)
        {
            StartRealTimeSync(id, () => CallRest(url, method ?? "GET", body, headersSerialized), intervalSeconds, handlerMacro);
        }

        public static void StartRealTimeSync(string id, Func<string> fetcher, int intervalSeconds, string handlerMacro)
        {
            if (string.IsNullOrEmpty(id) || fetcher == null || intervalSeconds <= 0) return;
            lock (_syncLock)
            {
                if (_syncTimers.ContainsKey(id)) return; // already running
                string lastHash = null;
                Timer timer = new Timer(state =>
                {
                    try
                    {
                        var content = fetcher();
                        var hash = content ?? "";
                        if (lastHash == null) lastHash = hash;
                        else if (hash != lastHash)
                        {
                            lastHash = hash;
                            // execute handler macro
                            try
                            {
                                MacroEngine.ExecuteMacro(handlerMacro);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }, null, 0, intervalSeconds * 1000);

                _syncTimers[id] = timer;
            }
        }

        public static void StopRealTimeSync(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (_syncLock)
            {
                if (_syncTimers.TryGetValue(id, out var t))
                {
                    try { t.Dispose(); } catch { }
                    _syncTimers.Remove(id);
                }
            }
        }

        // Import CSV to a B1 form using dynamic mapping: mapping = "ColName=ItemUID|Col2=Item2"
        public static bool ImportCsvToForm(string filePath, string mappingSerialized, SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                if (form == null) return false;
                if (!File.Exists(filePath)) return false;

                var mapping = ParseKeyValueString(mappingSerialized);
                using (var sr = new StreamReader(filePath, Encoding.Default))
                {
                    string headerLine = sr.ReadLine();
                    if (headerLine == null) return false;
                    var headers = SplitCsvLine(headerLine);
                    string line;
                    int rowIndex = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var cols = SplitCsvLine(line);
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var colName = headers[i];
                            if (mapping.ContainsKey(colName))
                            {
                                var itemId = mapping[colName];
                                try
                                {
                                    if (form.Items.Exists(itemId))
                                    {
                                        var it = form.Items.Item(itemId);
                                        var val = i < cols.Length ? cols[i] : "";
                                        if (it.Specific is SAPbouiCOM.EditText et) et.Value = val;
                                        else if (it.Specific is SAPbouiCOM.ComboBox cb) { try { cb.Select(val, BoSearchKey.psk_ByValue); } catch { } }
                                    }
                                }
                                catch { }
                            }
                        }
                        rowIndex++;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error importando CSV: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        // Export a grid to CSV using an optional mapping of columnName->outputHeader
        public static bool ExportGridToCsv(string filePath, SAPbouiCOM.Form form, string gridId, string mappingSerialized)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                if (form == null) return false;
                if (!form.Items.Exists(gridId)) return false;

                var grid = (SAPbouiCOM.Grid)form.Items.Item(gridId).Specific;
                var mapping = ParseKeyValueString(mappingSerialized);

                using (var sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // header
                    var headers = new List<string>();
                    for (int c = 0; c < grid.DataTable.Columns.Count; c++) headers.Add(grid.DataTable.Columns.Item(c).Name);
                    sw.WriteLine(string.Join(",", headers));

                    for (int r = 0; r < grid.DataTable.Rows.Count; r++)
                    {
                        var values = new List<string>();
                        for (int c = 0; c < grid.DataTable.Columns.Count; c++)
                        {
                            var v = grid.DataTable.GetValue(c, r)?.ToString() ?? "";
                            // escape commas
                            if (v.Contains(",") || v.Contains("\n")) v = "\"" + v.Replace("\"", "\"\"") + "\"";
                            values.Add(v);
                        }
                        sw.WriteLine(string.Join(",", values));
                    }
                }

                B1App.Instance.Application.SetStatusBarMessage($"Exportado CSV a {filePath}", BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error exportando CSV: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            if (line == null) return new string[0];
            var parts = new List<string>();
            var cur = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"') { inQuotes = !inQuotes; continue; }
                if (ch == ',' && !inQuotes) { parts.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(ch);
            }
            parts.Add(cur.ToString());
            return parts.ToArray();
        }
    }
}
