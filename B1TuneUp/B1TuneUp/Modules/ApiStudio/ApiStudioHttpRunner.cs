using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Text.Json;

namespace B1TuneUp.Modules.ApiStudio
{
    public static class ApiStudioHttpRunner
    {
        private static readonly CookieContainer Cookies = new CookieContainer();
        private static readonly HttpClient Client = CreateClient();

        public static async Task<ApiStudioResponse> SendAsync(ApiRequest request, ApiEnvironment environment)
        {
            var response = new ApiStudioResponse();
            var debug = new StringBuilder();
            var watch = Stopwatch.StartNew();

            try
            {
                var variables = BuildVariables(environment);
                var url = ResolveVariables(request.Url, variables);
                var body = ResolveVariables(request.Body, variables);
                var headers = ParseHeaders(ResolveVariables(request.Headers, variables));
                var method = string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method.Trim().ToUpperInvariant();

                debug.AppendLine($"[{DateTime.Now:O}] Preparing request");
                debug.AppendLine($"{method} {url}");
                debug.AppendLine($"Environment: {(environment == null ? "(none)" : environment.DisplayName)}");
                debug.AppendLine("Resolved variables: " + string.Join(", ", variables.Keys.OrderBy(k => k)));

                using (var message = new HttpRequestMessage(new HttpMethod(method), url))
                {
                    foreach (var header in headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            debug.AppendLine($"Header skipped by HttpClient: {header.Key}");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(body) && method != "GET" && method != "HEAD")
                    {
                        var contentType = headers.ContainsKey("Content-Type") ? headers["Content-Type"] : request.ContentType;
                        if (string.IsNullOrWhiteSpace(contentType)) contentType = "application/json";
                        message.Content = new StringContent(body, Encoding.UTF8, contentType);
                    }

                    debug.AppendLine("Request headers:");
                    foreach (var h in message.Headers) debug.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                    if (message.Content != null)
                    {
                        foreach (var h in message.Content.Headers) debug.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                        debug.AppendLine("Request body:");
                        debug.AppendLine(body);
                    }

                    using (var httpResponse = await Client.SendAsync(message).ConfigureAwait(false))
                    {
                        watch.Stop();
                        var responseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                        response.StatusCode = (int)httpResponse.StatusCode;
                        response.StatusDescription = httpResponse.ReasonPhrase;
                        response.DurationMs = watch.ElapsedMilliseconds;
                        response.ResponseBytes = Encoding.UTF8.GetByteCount(responseBody ?? string.Empty);
                        response.Body = responseBody;
                        response.Headers = FormatResponseHeaders(httpResponse);
                        response.FinalUrl = url;
                        response.IsError = !httpResponse.IsSuccessStatusCode;

                        debug.AppendLine("Response:");
                        debug.AppendLine($"{(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase} in {watch.ElapsedMilliseconds} ms");
                        debug.AppendLine("Response headers:");
                        debug.AppendLine(response.Headers);
                        debug.AppendLine("Cookies:");
                        AppendCookies(debug, url);
                    }
                }
            }
            catch (Exception ex)
            {
                watch.Stop();
                response.IsError = true;
                response.StatusDescription = ex.Message;
                response.DurationMs = watch.ElapsedMilliseconds;
                response.Body = "ERROR: " + ex;
                debug.AppendLine("Exception:");
                debug.AppendLine(ex.ToString());
            }

            response.DebugLog = debug.ToString();
            return response;
        }

        public static string ResolveVariables(string text, ApiEnvironment environment)
        {
            return ResolveVariables(text, BuildVariables(environment));
        }

        public static string BeautifyJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            using (var doc = JsonDocument.Parse(text))
            {
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        public static string BeautifyXml(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(text);
            var builder = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = Environment.NewLine, NewLineHandling = NewLineHandling.Replace };
            using (var writer = XmlWriter.Create(builder, settings))
            {
                doc.Save(writer);
            }
            return builder.ToString();
        }

        public static Dictionary<string, string> ParseHeaders(string raw)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return headers;
            var lines = raw.Split(new[] { '\r', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var idx = line.IndexOf('=');
                if (idx < 0) idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();
                if (!string.IsNullOrWhiteSpace(key)) headers[key] = value;
            }
            return headers;
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            var handler = new HttpClientHandler
            {
                CookieContainer = Cookies,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        }

        private static Dictionary<string, string> BuildVariables(ApiEnvironment environment)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (environment?.Variables == null) return dict;
            foreach (var variable in environment.Variables.Where(v => v.Enabled && !string.IsNullOrWhiteSpace(v.Key)))
            {
                dict[variable.Key.Trim()] = variable.Value ?? string.Empty;
            }
            return dict;
        }

        private static string ResolveVariables(string text, IDictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(text) || variables == null) return text;
            return Regex.Replace(text, "\\{\\{([^}]+)\\}\\}", match =>
            {
                var key = match.Groups[1].Value.Trim();
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });
        }

        private static string FormatResponseHeaders(HttpResponseMessage response)
        {
            var sb = new StringBuilder();
            foreach (var header in response.Headers) sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
            foreach (var header in response.Content.Headers) sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
            return sb.ToString();
        }

        private static void AppendCookies(StringBuilder debug, string url)
        {
            try
            {
                var uri = new Uri(url);
                foreach (Cookie cookie in Cookies.GetCookies(uri))
                {
                    debug.AppendLine($"{cookie.Name}={cookie.Value}; domain={cookie.Domain}; path={cookie.Path}");
                }
            }
            catch { }
        }
    }
}
