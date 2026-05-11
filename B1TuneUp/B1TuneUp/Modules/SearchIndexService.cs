using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class SearchIndexService
    {
        private const string Prefix = "SEARCHIDX_";
        private const string WatermarkPrefix = "SEARCHWM_";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IList<SearchIndexEntry> GetAll(string searchCode = null)
        {
            return ToolboxSettingService.GetAll()
                .Where(s => (s.Code ?? string.Empty).StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                .Select(ReadEntry)
                .Where(e => e != null && e.Active)
                .Where(e => string.IsNullOrWhiteSpace(searchCode) || string.Equals(e.SearchCode, searchCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static int RebuildAll(int maxRowsPerConfig = 5000)
        {
            int count = 0;
            foreach (var config in SearchConfigService.GetAll().Where(c => c.Active))
            {
                count += Rebuild(config, maxRowsPerConfig);
            }
            AuditLogManager.LogAction("B1SearchIndex", $"RebuildAll indexed {count} documents.", "Index");
            return count;
        }

        public static int Rebuild(SearchConfigEntry config, int maxRows = 5000)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.Query)) return 0;
            int count = 0;
            Recordset rs = null;
            try
            {
                string sql = SearchSqlSecurityService.ApplyServerPaging(config.Query, 0, Math.Max(1, maxRows), B1App.Instance.IsHana)
                    .Replace("{search}", string.Empty)
                    .Replace("%search%", string.Empty)
                    .Replace("{offset}", "0")
                    .Replace("{limit}", Math.Max(1, maxRows).ToString());
                SearchSqlSecurityService.ValidateSelectOnly(sql);

                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var values = ReadRow(rs);
                    var entry = BuildEntry(config, values);
                    Save(entry);
                    count++;
                    rs.MoveNext();
                }

                SetWatermark(config.Code, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"SearchIndexService.Rebuild:{config.Code}");
                throw;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return count;
        }

        public static IList<AdvancedSearchResult> Search(string text, int take = 50)
        {
            string normalized = Normalize(text);
            var history = SearchProductService.GetHistory(SafeUser()).GroupBy(h => h.ResultKey ?? string.Empty).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            var favorites = new HashSet<string>(SearchProductService.GetFavorites(SafeUser()).Select(f => f.ResultKey ?? string.Empty), StringComparer.OrdinalIgnoreCase);

            return GetAll()
                .Where(CanUse)
                .Select(e => new { Entry = e, Score = Score(e, normalized, history, favorites) })
                .Where(x => string.IsNullOrWhiteSpace(normalized) || x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Entry.Title)
                .Take(Math.Max(1, take))
                .Select(x => ToResult(x.Entry, x.Score))
                .ToList();
        }

        public static void Save(SearchIndexEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code)) entry.Code = BuildCode(entry.SearchCode, entry.ObjectKey);
            entry.SourceHash = ComputeHash((entry.Title ?? string.Empty) + "|" + (entry.Subtitle ?? string.Empty) + "|" + (entry.Keywords ?? string.Empty) + "|" + (entry.DataJson ?? string.Empty));
            entry.IndexedAtUtc = DateTime.UtcNow;
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = Prefix + entry.Code,
                Category = "B1SearchIndex",
                Description = entry.Title ?? entry.Code,
                Value = JsonSerializer.Serialize(entry, JsonOptions)
            });
        }

        public static DateTime? GetWatermark(string searchCode)
        {
            var setting = ToolboxSettingService.GetByCode(WatermarkPrefix + NormalizeCode(searchCode));
            if (setting == null || string.IsNullOrWhiteSpace(setting.Value)) return null;
            return DateTime.TryParse(setting.Value, out var value) ? value : (DateTime?)null;
        }

        private static void SetWatermark(string searchCode, DateTime value)
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = WatermarkPrefix + NormalizeCode(searchCode),
                Category = "B1SearchIndex",
                Description = "Last successful B1 Search indexing run",
                Value = value.ToString("O")
            });
        }

        private static SearchIndexEntry BuildEntry(SearchConfigEntry config, Dictionary<string, string> values)
        {
            string title = First(values, "Title", "Name", "CardName", "ItemName", "DocNum", "Code");
            string key = First(values, "Key", "DocEntry", "CardCode", "ItemCode", "Code");
            string subtitle = First(values, "Subtitle", "Description", "Comments", "CardCode", "ItemCode");
            string keywords = string.Join(" ", values.Values.Where(v => !string.IsNullOrWhiteSpace(v)));
            return new SearchIndexEntry
            {
                SearchCode = config.Code,
                SearchName = config.Name,
                ObjectType = string.IsNullOrWhiteSpace(config.FormType) ? config.Category : config.FormType,
                ObjectKey = key,
                Title = title,
                Subtitle = subtitle,
                Keywords = keywords,
                DataJson = JsonSerializer.Serialize(values, JsonOptions),
                Action = config.Action,
                BaseRank = config.Favorite ? 20 : 10,
                Active = config.Active
            };
        }

        private static int Score(SearchIndexEntry entry, string text, IDictionary<string, int> history, ISet<string> favorites)
        {
            int score = entry.BaseRank;
            string key = entry.ObjectKey ?? string.Empty;
            if (favorites.Contains(key)) score += 40;
            if (history.TryGetValue(key, out var usage)) score += Math.Min(30, usage * 3);
            if (string.IsNullOrWhiteSpace(text)) return score;

            string title = Normalize(entry.Title);
            string subtitle = Normalize(entry.Subtitle);
            string keywords = Normalize(entry.Keywords);
            if (title.Equals(text, StringComparison.OrdinalIgnoreCase)) score += 120;
            else if (title.StartsWith(text, StringComparison.OrdinalIgnoreCase)) score += 90;
            else if (title.Contains(text)) score += 70;
            if (subtitle.Contains(text)) score += 35;
            if (keywords.Contains(text)) score += 20;

            foreach (var token in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (title.Contains(token)) score += 12;
                else if (keywords.Contains(token)) score += 5;
            }
            return score;
        }

        private static AdvancedSearchResult ToResult(SearchIndexEntry entry, int score)
        {
            return new AdvancedSearchResult
            {
                SearchCode = entry.SearchCode,
                SearchName = entry.SearchName,
                Key = entry.ObjectKey,
                Title = entry.Title,
                Subtitle = entry.Subtitle,
                Action = entry.Action,
                Rank = score,
                DataJson = entry.DataJson
            };
        }

        private static bool CanUse(SearchIndexEntry entry)
        {
            var config = SearchConfigService.GetAll().FirstOrDefault(c => string.Equals(c.Code, entry.SearchCode, StringComparison.OrdinalIgnoreCase));
            return config == null || AuthorizationScopeService.MatchesScope(config.AllowedUsers, config.AllowedGroups, config.DeniedUsers, config.DeniedGroups);
        }

        private static SearchIndexEntry ReadEntry(ToolboxSettingEntry setting)
        {
            try
            {
                var entry = JsonSerializer.Deserialize<SearchIndexEntry>(setting.Value ?? string.Empty, JsonOptions);
                if (entry != null && string.IsNullOrWhiteSpace(entry.Code)) entry.Code = setting.Code?.Substring(Prefix.Length);
                return entry;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, $"SearchIndexService.ReadEntry:{setting.Code}");
                return null;
            }
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

        private static string First(Dictionary<string, string> values, params string[] names)
        {
            foreach (var name in names)
            {
                if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
            }
            return values.Values.FirstOrDefault() ?? string.Empty;
        }

        private static string BuildCode(string searchCode, string key) => NormalizeCode(searchCode) + "_" + ComputeHash(key ?? Guid.NewGuid().ToString("N")).Substring(0, 16);
        private static string NormalizeCode(string value) => (value ?? "GLOBAL").Trim().Replace(" ", "_").ToUpperInvariant();
        private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();
        private static string SafeUser() { try { return B1App.Instance.Company.UserName; } catch { return string.Empty; } }

        private static string ComputeHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty))).Replace("-", string.Empty);
            }
        }
    }
}
