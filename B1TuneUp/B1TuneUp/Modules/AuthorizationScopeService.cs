using System;
using System.Collections.Generic;
using System.Linq;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class AuthorizationScopeService
    {
        private static readonly object SyncRoot = new object();
        private static UserScopeContext _cachedContext;

        public static void Invalidate()
        {
            lock (SyncRoot)
            {
                _cachedContext = null;
            }
        }

        public static bool IsModuleAvailable(string moduleKey)
        {
            var config = ModuleActivationService.GetModule(moduleKey);
            return config != null && MatchesModule(config);
        }

        public static bool MatchesModule(ModuleConfigurationEntry config)
        {
            if (config == null || !config.Enabled)
            {
                return false;
            }

            return MatchesScope(config.AllowedUsers, config.AllowedGroups, config.DeniedUsers, config.DeniedGroups);
        }

        public static bool MatchesValidation(ValidationRuleEntry entry)
        {
            if (entry == null) return false;
            return MatchesScope(
                entry.AppliesToUser,
                entry.AppliesToUserGroup,
                entry.ExcludedUsers,
                entry.ExcludedUserGroups);
        }

        public static bool MatchesScope(string allowedUsers, string allowedGroups, string deniedUsers, string deniedGroups)
        {
            var context = GetCurrentContext();
            if (context.IsSuperUser)
            {
                return true;
            }

            if (MatchesAny(deniedUsers, context.UserCode, context.UserName))
            {
                return false;
            }

            if (MatchesAny(deniedGroups, context.GroupCodes.ToArray()))
            {
                return false;
            }

            if (!AllowListContains(allowedUsers, context.UserCode, context.UserName))
            {
                return false;
            }

            if (!AllowListContains(allowedGroups, context.GroupCodes.ToArray()))
            {
                return false;
            }

            return true;
        }

        public static IReadOnlyList<string> GetCurrentGroups()
        {
            return GetCurrentContext().GroupCodes;
        }

        public static string GetCurrentUserCode()
        {
            return GetCurrentContext().UserCode;
        }

        public static IEnumerable<string> SplitTokens(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                yield break;
            }

            foreach (var token in raw.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    yield return trimmed;
                }
            }
        }

        private static bool AllowListContains(string raw, params string[] candidates)
        {
            var tokens = SplitTokens(raw)
                .Where(token => !token.StartsWith("!", StringComparison.Ordinal))
                .ToList();
            if (tokens.Count == 0)
            {
                return true;
            }

            return MatchesAny(tokens, candidates);
        }

        private static bool MatchesAny(string raw, params string[] candidates)
        {
            return MatchesAny(SplitTokens(raw), candidates);
        }

        private static bool MatchesAny(IEnumerable<string> tokens, params string[] candidates)
        {
            var cleanedCandidates = (candidates ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToArray();
            if (cleanedCandidates.Length == 0)
            {
                return false;
            }

            foreach (var token in tokens ?? Array.Empty<string>())
            {
                var normalized = token.Trim();
                if (normalized == "*")
                {
                    return true;
                }

                if (normalized.StartsWith("!", StringComparison.Ordinal))
                {
                    normalized = normalized.Substring(1).Trim();
                }

                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (cleanedCandidates.Any(candidate => string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private static UserScopeContext GetCurrentContext()
        {
            if (_cachedContext != null) return _cachedContext;
            lock (SyncRoot)
            {
                if (_cachedContext != null) return _cachedContext;

                var context = new UserScopeContext
                {
                    UserCode = SafeString(() => B1App.Instance.Company.UserName),
                    UserName = SafeString(() => B1App.Instance.Application.Company.UserName)
                };

                Recordset rs = null;
                try
                {
                    rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string safeUserCode = (context.UserCode ?? string.Empty).Replace("'", "''");
                    string sql = B1App.Instance.IsHana
                        ? $"SELECT UG.\"GroupCode\" FROM OUSR U INNER JOIN USR6 UG ON U.\"USERID\" = UG.\"USERID\" WHERE U.\"USER_CODE\" = '{safeUserCode}'"
                        : $"SELECT UG.GroupCode FROM OUSR U WITH (NOLOCK) INNER JOIN USR6 UG ON U.USERID = UG.USERID WHERE U.USER_CODE = '{safeUserCode}'";
                    rs.DoQuery(sql);
                    while (!rs.EoF)
                    {
                        string group = SafeString(() => B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0));
                        if (!string.IsNullOrWhiteSpace(group) && !context.GroupCodes.Any(existing => string.Equals(existing, group, StringComparison.OrdinalIgnoreCase)))
                        {
                            context.GroupCodes.Add(group);
                        }
                        rs.MoveNext();
                    }

                    foreach (var group in AuthorizationAdminService.GetGroupsForUser(context.UserCode, context.UserName))
                    {
                        if (!context.GroupCodes.Any(existing => string.Equals(existing, group, StringComparison.OrdinalIgnoreCase)))
                        {
                            context.GroupCodes.Add(group);
                        }
                    }

                    context.IsSuperUser = AuthorizationAdminService.IsSuperUser(context.UserCode, context.UserName);
                }
                catch (Exception ex)
                {
                    ExceptionLogger.LogHandled(ex, "AuthorizationScopeService.GetCurrentContext");
                }
                finally
                {
                    ComObjectManager.Release(rs);
                }

                _cachedContext = context;
                return context;
            }
        }

        private static string SafeString(Func<string> getter)
        {
            try { return getter() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private sealed class UserScopeContext
        {
            public string UserCode { get; set; }
            public string UserName { get; set; }
            public bool IsSuperUser { get; set; }
            public List<string> GroupCodes { get; } = new List<string>();
        }
    }
}
