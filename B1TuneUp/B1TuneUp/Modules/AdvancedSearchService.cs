using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class AdvancedSearchService
    {
        private static readonly Dictionary<string, Tuple<DateTime, IList<AdvancedSearchResult>>> Cache =
            new Dictionary<string, Tuple<DateTime, IList<AdvancedSearchResult>>>(StringComparer.OrdinalIgnoreCase);

        public static IList<AdvancedSearchResult> Search(string text, int page = 0, int pageSize = 50)
        {
            text = text ?? string.Empty;
            string cacheKey = $"{text}|{page}|{pageSize}|{SafeUser()}";
            if (Cache.TryGetValue(cacheKey, out var cached) && cached.Item1 > DateTime.Now) return cached.Item2;

            var results = new List<AdvancedSearchResult>();
            foreach (var config in SearchConfigService.GetAll().Where(c => c.Active))
            {
                if (!AuthorizationScopeService.MatchesScope(config.AllowedUsers, config.AllowedGroups, config.DeniedUsers, config.DeniedGroups)) continue;
                results.AddRange(ExecuteConfig(config, text, page, pageSize));
            }

            var ordered = results
                .OrderByDescending(r => r.Rank)
                .ThenBy(r => r.SearchName)
                .Take(pageSize)
                .ToList();
            int ttl = SearchConfigService.GetAll().Where(c => c.Active).Select(c => c.CacheSeconds).DefaultIfEmpty(30).Max();
            Cache[cacheKey] = Tuple.Create(DateTime.Now.AddSeconds(Math.Max(0, ttl)), (IList<AdvancedSearchResult>)ordered);
            return ordered;
        }

        public static IList<string> Autocomplete(string prefix, int take = 10)
        {
            prefix = prefix ?? string.Empty;
            return Search(prefix, 0, take)
                .Select(r => r.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(take)
                .ToList();
        }

        public static void ExecuteAction(AdvancedSearchResult result, SAPbouiCOM.Form form = null)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Action)) return;
            string action = result.Action;
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(result.DataJson ?? "{}") ?? new Dictionary<string, string>();
                foreach (var pair in values)
                {
                    action = action.Replace("$[" + pair.Key + "]", pair.Value ?? string.Empty);
                }
            }
            catch (Exception ex) { ExceptionLogger.LogHandled(ex, "AdvancedSearchService.ExecuteAction.Deserialize"); }
            MacroEngine.ExecuteMacro(action, form);
            AuditLogManager.LogAction("AdvancedSearch", $"Action executed for {result.SearchCode}:{result.Key}", "Action");
        }

        private static IList<AdvancedSearchResult> ExecuteConfig(SearchConfigEntry config, string text, int page, int pageSize)
        {
            var list = new List<AdvancedSearchResult>();
            if (string.IsNullOrWhiteSpace(config.Query)) return list;
            string sql = PrepareSql(config.Query, text, page, pageSize);
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var values = ReadRow(rs);
                    string title = First(values, "Title", "Name", "CardName", "ItemName", "DocNum", "Code");
                    string key = First(values, "Key", "DocEntry", "CardCode", "ItemCode", "Code");
                    list.Add(new AdvancedSearchResult
                    {
                        SearchCode = config.Code,
                        SearchName = config.Name,
                        Key = key,
                        Title = title,
                        Subtitle = First(values, "Subtitle", "Description", "Comments", "CardCode", "ItemCode"),
                        Rank = Rank(text, title, values),
                        Action = config.Action,
                        DataJson = JsonSerializer.Serialize(values)
                    });
                    SearchProductService.TrackHistory(config.Code, text, key);
                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"AdvancedSearchService.ExecuteConfig:{config.Code}");
                throw;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        private static string PrepareSql(string query, string text, int page, int pageSize)
        {
            SearchSqlSecurityService.ValidateSelectOnly(query);
            query = SearchSqlSecurityService.ApplyServerPaging(query, page, pageSize, B1App.Instance.IsHana);
            string safeText = Escape(text);
            return query
                .Replace("%search%", safeText)
                .Replace("{search}", safeText)
                .Replace("{offset}", Math.Max(0, page * pageSize).ToString())
                .Replace("{limit}", Math.Max(1, pageSize).ToString());
        }

        private static Dictionary<string, string> ReadRow(Recordset rs)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rs.Fields.Count; i++)
            {
                string name = SapUiSafe.SafeFieldName(rs, i);
                values[string.IsNullOrWhiteSpace(name) ? "Col" + i : name] = SapUiSafe.SafeField(rs, i);
            }
            return values;
        }

        private static int Rank(string text, string title, Dictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(text)) return 10;
            if (!string.IsNullOrWhiteSpace(title) && title.Equals(text, StringComparison.OrdinalIgnoreCase)) return 100;
            if (!string.IsNullOrWhiteSpace(title) && title.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) return 80;
            return values.Values.Any(v => (v ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ? 50 : 1;
        }

        private static string First(Dictionary<string, string> values, params string[] names)
        {
            foreach (var name in names)
            {
                if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
            }
            return values.Values.FirstOrDefault() ?? string.Empty;
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("'", "''");

        private static string SafeUser()
        {
            try { return B1App.Instance.Company.UserName; }
            catch (Exception ex) { ExceptionLogger.LogHandled(ex, "AdvancedSearchService.SafeUser"); return string.Empty; }
        }
    }
}
