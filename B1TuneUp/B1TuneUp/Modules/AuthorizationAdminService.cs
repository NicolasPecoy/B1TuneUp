using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class AuthorizationAdminService
    {
        private const string GroupPrefix = "AUTHGROUP_";
        private const string SuperUsersCode = "AUTH_SUPERUSERS";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IList<AuthorizationGroupEntry> GetGroups()
        {
            return ToolboxSettingService.GetAll()
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && s.Code.StartsWith(GroupPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(ReadGroup)
                .Where(g => g != null)
                .OrderBy(g => g.Code)
                .ToList();
        }

        public static AuthorizationGroupEntry SaveGroup(AuthorizationGroupEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Code)) throw new InvalidOperationException("El codigo del grupo es obligatorio.");

            entry.Code = NormalizeGroupCode(entry.Code);
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = GroupPrefix + entry.Code,
                Category = "Authorization",
                Description = entry.Name ?? entry.Code,
                Value = JsonSerializer.Serialize(entry, JsonOptions)
            });
            AuthorizationScopeService.Invalidate();
            return entry;
        }

        public static void DeleteGroup(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            ToolboxSettingService.Delete(GroupPrefix + NormalizeGroupCode(code));
            AuthorizationScopeService.Invalidate();
        }

        public static string GetSuperUsers()
        {
            return ToolboxSettingService.GetByCode(SuperUsersCode)?.Value ?? string.Empty;
        }

        public static void SaveSuperUsers(string users)
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = SuperUsersCode,
                Category = "Authorization",
                Description = "B1TuneUp super users",
                Value = users ?? string.Empty
            });
            AuthorizationScopeService.Invalidate();
        }

        public static bool IsSuperUser(string userCode, string userName = null)
        {
            return AuthorizationScopeService.SplitTokens(GetSuperUsers())
                .Any(token => string.Equals(token, userCode, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(token, userName, StringComparison.OrdinalIgnoreCase)
                              || token == "*");
        }

        public static IEnumerable<string> GetGroupsForUser(string userCode, string userName = null)
        {
            foreach (var group in GetGroups())
            {
                if (AuthorizationScopeService.SplitTokens(group.Users)
                    .Any(token => token == "*"
                                  || string.Equals(token, userCode, StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(token, userName, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return group.Code;
                }
            }
        }

        private static AuthorizationGroupEntry ReadGroup(ToolboxSettingEntry setting)
        {
            try
            {
                var group = JsonSerializer.Deserialize<AuthorizationGroupEntry>(setting.Value ?? string.Empty, JsonOptions);
                if (group == null) return null;
                group.Code = NormalizeGroupCode(group.Code);
                return group;
            }
            catch
            {
                return new AuthorizationGroupEntry
                {
                    Code = NormalizeGroupCode(setting.Code?.Substring(GroupPrefix.Length)),
                    Name = setting.Description,
                    Users = setting.Value
                };
            }
        }

        private static string NormalizeGroupCode(string code)
        {
            return (code ?? string.Empty).Trim().Replace(" ", "_").ToUpperInvariant();
        }
    }
}
