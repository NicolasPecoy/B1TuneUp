using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

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
            AuditLogManager.LogAction("Authorization", $"Grupo {entry.Code} guardado.", "Info");
            AuthorizationScopeService.Invalidate();
            return entry;
        }

        public static void DeleteGroup(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            ToolboxSettingService.Delete(GroupPrefix + NormalizeGroupCode(code));
            AuditLogManager.LogAction("Authorization", $"Grupo {code} eliminado.", "Deleted");
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
            AuditLogManager.LogAction("Authorization", "Superusuarios actualizados.", "Info");
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

        public static IList<SapUserEntry> GetSapUsers()
        {
            var users = new List<SapUserEntry>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"USER_CODE\", \"U_NAME\", \"GROUPS\", \"SUPERUSER\" FROM OUSR ORDER BY \"USER_CODE\""
                    : "SELECT USER_CODE, U_NAME, GROUPS, SUPERUSER FROM OUSR WITH (NOLOCK) ORDER BY USER_CODE";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    users.Add(new SapUserEntry
                    {
                        UserCode = SapUiSafe.SafeField(rs, "USER_CODE"),
                        UserName = SapUiSafe.SafeField(rs, "U_NAME"),
                        GroupCode = SapUiSafe.SafeField(rs, "GROUPS"),
                        SuperUser = string.Equals(SapUiSafe.SafeField(rs, "SUPERUSER"), "Y", StringComparison.OrdinalIgnoreCase)
                    });
                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "AuthorizationAdminService.GetSapUsers");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return users;
        }

        public static AuthSimulationResult Simulate(string userCode, string objectType, string objectCode, string allowedUsers, string allowedGroups, string deniedUsers, string deniedGroups)
        {
            var user = GetSapUsers().FirstOrDefault(u => string.Equals(u.UserCode, userCode, StringComparison.OrdinalIgnoreCase));
            bool super = IsSuperUser(userCode, user?.UserName) || (user?.SuperUser ?? false);
            bool deniedUser = AuthorizationScopeService.SplitTokens(deniedUsers).Any(t => t == "*" || string.Equals(t, userCode, StringComparison.OrdinalIgnoreCase));
            var userGroups = GetGroupsForUser(userCode, user?.UserName).ToList();
            if (!string.IsNullOrWhiteSpace(user?.GroupCode)) userGroups.Add(user.GroupCode);
            bool deniedGroup = AuthorizationScopeService.SplitTokens(deniedGroups).Any(g => userGroups.Any(ug => string.Equals(ug, g, StringComparison.OrdinalIgnoreCase)));
            bool hasAllowList = AuthorizationScopeService.SplitTokens(allowedUsers).Any() || AuthorizationScopeService.SplitTokens(allowedGroups).Any();
            bool allowedUser = AuthorizationScopeService.SplitTokens(allowedUsers).Any(t => t == "*" || string.Equals(t, userCode, StringComparison.OrdinalIgnoreCase));
            bool allowedGroup = AuthorizationScopeService.SplitTokens(allowedGroups).Any(g => userGroups.Any(ug => string.Equals(ug, g, StringComparison.OrdinalIgnoreCase)));
            bool allowed = super || (!deniedUser && !deniedGroup && (!hasAllowList || allowedUser || allowedGroup));
            return new AuthSimulationResult
            {
                UserCode = userCode,
                ObjectCode = objectCode,
                ObjectType = objectType,
                Allowed = allowed,
                Detail = super ? "Super user." : deniedUser || deniedGroup ? "Bloqueado por denegacion." : hasAllowList ? "Evaluado contra lista allow." : "Sin restricciones explicitas."
            };
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
