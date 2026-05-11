using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class SearchProductService
    {
        private const string HistoryPrefix = "SEARCHHIST_";
        private const string FavoritePrefix = "SEARCHFAV_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static IList<SearchUsageEntry> GetHistory(string userCode = null)
        {
            return Read(HistoryPrefix)
                .Where(x => string.IsNullOrWhiteSpace(userCode) || string.Equals(x.UserCode, userCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToList();
        }

        public static IList<SearchUsageEntry> GetFavorites(string userCode = null)
        {
            return Read(FavoritePrefix)
                .Where(x => string.IsNullOrWhiteSpace(userCode) || string.Equals(x.UserCode, userCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public static void TrackHistory(string searchCode, string searchText, string resultKey = null)
        {
            Save(HistoryPrefix, new SearchUsageEntry
            {
                Code = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                UserCode = SafeUser(),
                SearchCode = searchCode,
                SearchText = searchText,
                ResultKey = resultKey,
                CreatedAt = DateTime.UtcNow
            });
        }

        public static void ToggleFavorite(string searchCode, string searchText, string resultKey = null)
        {
            Save(FavoritePrefix, new SearchUsageEntry
            {
                Code = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                UserCode = SafeUser(),
                SearchCode = searchCode,
                SearchText = searchText,
                ResultKey = resultKey,
                Favorite = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        public static string BuildAutocompleteSql(SearchConfigEntry config, string prefix, int take = 10)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            string field = string.IsNullOrWhiteSpace(config.AutocompleteField) ? "Title" : config.AutocompleteField;
            string sql = config.Query ?? string.Empty;
            SearchSqlSecurityService.ValidateSelectOnly(sql);
            string safePrefix = (prefix ?? string.Empty).Replace("'", "''");
            return sql.Replace("{autocompleteField}", field)
                      .Replace("{search}", safePrefix)
                      .Replace("%search%", safePrefix)
                      .Replace("{limit}", Math.Max(1, take).ToString())
                      .Replace("{offset}", "0");
        }

        private static IList<SearchUsageEntry> Read(string prefix)
        {
            var list = new List<SearchUsageEntry>();
            try
            {
                foreach (var setting in ToolboxSettingService.GetAll().Where(s => (s.Code ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        var entry = JsonSerializer.Deserialize<SearchUsageEntry>(setting.Value ?? string.Empty, JsonOptions);
                        if (entry != null) list.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        ExceptionLogger.LogHandled(ex, "SearchProductService.Read:" + setting.Code);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "SearchProductService.ReadAll");
            }
            return list;
        }

        private static void Save(string prefix, SearchUsageEntry entry)
        {
            try
            {
                ToolboxSettingService.Save(new ToolboxSettingEntry
                {
                    Code = prefix + entry.Code,
                    Category = "B1Search",
                    Description = "B1 Search usage",
                    Value = JsonSerializer.Serialize(entry, JsonOptions)
                });
                AuditLogManager.LogAction("B1Search", $"{prefix}{entry.SearchCode}:{entry.SearchText}", "Usage");
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "SearchProductService.Save");
            }
        }

        private static string SafeUser()
        {
            try { return B1App.Instance.Company.UserName ?? string.Empty; }
            catch (Exception ex) { ExceptionLogger.LogHandled(ex, "SearchProductService.SafeUser"); return string.Empty; }
        }
    }
}
