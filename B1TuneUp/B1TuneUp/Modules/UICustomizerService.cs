using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class UICustomizerService
    {
        private const string TableName = "@BTUN_UI";

        public static IList<UiCustomizationEntry> GetAll(string formType = null)
        {
            var list = new List<UiCustomizationEntry>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string filter = string.IsNullOrWhiteSpace(formType) ? "" : (B1App.Instance.IsHana ? $" WHERE \"U_FormType\" = '{formType.Replace("'", "''")}'" : $" WHERE [U_FormType] = '{formType.Replace("'", "''")}'");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"Code\",\"Name\",\"U_FormType\",\"U_ItemID\",\"U_Action\",\"U_Top\",\"U_Left\",\"U_Width\",\"U_Height\",\"U_Label\",\"U_FromPane\",\"U_ToPane\",\"U_UserCode\",\"U_UserGroup\",\"U_Condition\",\"U_Priority\",\"U_Localization\",\"U_Variant\",\"U_DependsOn\",\"U_Inherit\" FROM \"{TableName}\"{filter} ORDER BY \"U_FormType\",\"U_Priority\",\"Code\""
                    : $"SELECT [Code],[Name],[U_FormType],[U_ItemID],[U_Action],[U_Top],[U_Left],[U_Width],[U_Height],[U_Label],[U_FromPane],[U_ToPane],[U_UserCode],[U_UserGroup],[U_Condition],[U_Priority],[U_Localization],[U_Variant],[U_DependsOn],[U_Inherit] FROM [{TableName}]{filter} ORDER BY [U_FormType],[U_Priority],[Code]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    list.Add(MapEntry(rs));
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        private static UiCustomizationEntry MapEntry(Recordset rs)
        {
            return new UiCustomizationEntry
            {
                Code = Convert.ToString(rs.Fields.Item(0).Value),
                Name = Convert.ToString(rs.Fields.Item(1).Value),
                FormType = Convert.ToString(rs.Fields.Item(2).Value),
                ItemId = Convert.ToString(rs.Fields.Item(3).Value),
                Action = Convert.ToString(rs.Fields.Item(4).Value),
                Top = ToNullableInt(rs.Fields.Item(5).Value),
                Left = ToNullableInt(rs.Fields.Item(6).Value),
                Width = ToNullableInt(rs.Fields.Item(7).Value),
                Height = ToNullableInt(rs.Fields.Item(8).Value),
                Label = Convert.ToString(rs.Fields.Item(9).Value),
                FromPane = ToNullableInt(rs.Fields.Item(10).Value),
                ToPane = ToNullableInt(rs.Fields.Item(11).Value),
                UpdatedAt = null,
                UserCode = Convert.ToString(rs.Fields.Item(12).Value),
                UserGroup = Convert.ToString(rs.Fields.Item(13).Value),
                Condition = Convert.ToString(rs.Fields.Item(14).Value),
                Priority = ToNullableInt(rs.Fields.Item(15).Value) ?? 10,
                Localization = Convert.ToString(rs.Fields.Item(16).Value),
                Variant = Convert.ToString(rs.Fields.Item(17).Value),
                DependsOn = Convert.ToString(rs.Fields.Item(18).Value),
                InheritFrom = Convert.ToString(rs.Fields.Item(19).Value)
            };
        }

        public static UiCustomizationEntry Save(UiCustomizationEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_UI");
                bool exists = !string.IsNullOrEmpty(entry.Code) && table.GetByKey(entry.Code);
                if (!exists)
                {
                    entry.Code = string.IsNullOrWhiteSpace(entry.Code) ? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant() : entry.Code;
                    table.Code = entry.Code;
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.DisplayName : entry.Name;
                }
                else
                {
                    table.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.DisplayName : entry.Name;
                }

                table.UserFields.Fields.Item("U_FormType").Value = entry.FormType ?? string.Empty;
                table.UserFields.Fields.Item("U_ItemID").Value = entry.ItemId ?? string.Empty;
                table.UserFields.Fields.Item("U_Action").Value = entry.Action ?? "Hide";
                table.UserFields.Fields.Item("U_Top").Value = entry.Top ?? 0;
                table.UserFields.Fields.Item("U_Left").Value = entry.Left ?? 0;
                table.UserFields.Fields.Item("U_Width").Value = entry.Width ?? 0;
                table.UserFields.Fields.Item("U_Height").Value = entry.Height ?? 0;
                table.UserFields.Fields.Item("U_Label").Value = entry.Label ?? string.Empty;
                table.UserFields.Fields.Item("U_FromPane").Value = entry.FromPane ?? 0;
                table.UserFields.Fields.Item("U_ToPane").Value = entry.ToPane ?? 0;
                table.UserFields.Fields.Item("U_UserCode").Value = entry.UserCode ?? string.Empty;
                table.UserFields.Fields.Item("U_UserGroup").Value = entry.UserGroup ?? string.Empty;
                table.UserFields.Fields.Item("U_Condition").Value = entry.Condition ?? string.Empty;
                table.UserFields.Fields.Item("U_Priority").Value = entry.Priority;
                table.UserFields.Fields.Item("U_Localization").Value = entry.Localization ?? string.Empty;
                table.UserFields.Fields.Item("U_Variant").Value = entry.Variant ?? string.Empty;
                table.UserFields.Fields.Item("U_DependsOn").Value = entry.DependsOn ?? string.Empty;
                table.UserFields.Fields.Item("U_Inherit").Value = entry.InheritFrom ?? string.Empty;

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
            return entry;
        }

        public static void Delete(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            UserTable table = null;
            try
            {
                table = B1App.Instance.Company.UserTables.Item("BTUN_UI");
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

        public static IReadOnlyList<string> GetDistinctFormTypes()
        {
            var list = new List<string>();
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? "SELECT DISTINCT \"U_FormType\" FROM \"@BTUN_UI\" ORDER BY \"U_FormType\""
                    : "SELECT DISTINCT [U_FormType] FROM [@BTUN_UI] ORDER BY [U_FormType]";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var ft = Convert.ToString(rs.Fields.Item(0).Value);
                    if (!string.IsNullOrWhiteSpace(ft)) list.Add(ft);
                    rs.MoveNext();
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return list;
        }

        public static void RefreshActiveForm()
        {
            try
            {
                var form = B1App.Instance.Application.Forms.ActiveForm;
                if (form != null)
                {
                    UICustomizer.ApplyCustomization(form);
                    form.Update();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error refrescando formulario: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        public static void OpenItemPlacement()
        {
            try
            {
                var form = B1App.Instance.Application.Forms.ActiveForm;
                ItemPlacementManager.OpenPlacementForm(form);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Item Placement no disponible: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        public static UiCustomizationPackage BuildPackage(string formType)
        {
            if (string.IsNullOrWhiteSpace(formType))
                throw new ArgumentException("FormType is required to export a UI package.", nameof(formType));

            string normalized = formType.Trim();
            var package = new UiCustomizationPackage
            {
                FormType = normalized,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = SafeGetCurrentUser()
            };

            package.UiEntries = new List<UiCustomizationEntry>(GetAll(normalized));

            var validations = ValidationRuleService.GetAll();
            package.ValidationRules = validations
                .Where(v => string.Equals(v.FormType, normalized, StringComparison.OrdinalIgnoreCase))
                .Select(CloneValidationForExport)
                .ToList();

            package.Scopes = BuildScopeDescriptors(package.UiEntries, package.ValidationRules);
            package.Dependencies = BuildDependencies(package.UiEntries, package.ValidationRules);
            package.InheritanceLinks = BuildInheritanceLinks(package.UiEntries, package.ValidationRules);

            var mandatory = MandatoryFieldService.GetAll();
            package.MandatoryRules = mandatory
                .Where(v => string.Equals(v.FormType, normalized, StringComparison.OrdinalIgnoreCase))
                .Select(CloneMandatoryForExport)
                .ToList();

            var pads = ActionPadService.GetAll();
            package.ActionPads = pads
                .Where(p => string.Equals(p.FormType, normalized, StringComparison.OrdinalIgnoreCase))
                .Select(ClonePadForExport)
                .ToList();

            return package;
        }

        public static void ExportPackage(string formType, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Destination file path is required.", nameof(filePath));

            var package = BuildPackage(formType);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, JsonSerializer.Serialize(package, options));
        }

        public static UiCustomizationPackage ImportPackage(string filePath, bool includeLayout = true, bool includeValidations = true, bool includeMandatory = true, bool includeActionPads = true)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("UI package file not found.", filePath);

            var json = File.ReadAllText(filePath);
            var package = JsonSerializer.Deserialize<UiCustomizationPackage>(json) ?? throw new InvalidOperationException("Archivo de paquete inválido.");
            string formType = package.FormType?.Trim();
            if (string.IsNullOrWhiteSpace(formType))
                throw new InvalidOperationException("El paquete no especifica FormType.");

            if (includeLayout)
            {
                var codeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in package.UiEntries ?? Array.Empty<UiCustomizationEntry>())
                {
                    var copy = entry.Clone();
                    var originalCode = copy.Code;
                    copy.Code = null;
                    if (!string.IsNullOrWhiteSpace(copy.InheritFrom) && codeMap.TryGetValue(copy.InheritFrom, out var mapped))
                    {
                        copy.InheritFrom = mapped;
                    }
                    copy.FormType = formType;
                    var saved = UICustomizerService.Save(copy);
                    if (!string.IsNullOrWhiteSpace(originalCode) && !string.IsNullOrWhiteSpace(saved.Code))
                    {
                        codeMap[originalCode] = saved.Code;
                    }
                }
            }

            if (includeValidations)
            {
                foreach (var entry in package.ValidationRules ?? Array.Empty<ValidationRuleEntry>())
                {
                    var copy = entry.Clone();
                    copy.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
                    copy.FormType = formType;
                    ValidationRuleService.Save(copy);
                }
            }

            if (includeMandatory)
            {
                foreach (var entry in package.MandatoryRules ?? Array.Empty<MandatoryFieldEntry>())
                {
                    var copy = entry.Clone();
                    copy.Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
                    copy.FormType = formType;
                    MandatoryFieldService.Save(copy);
                }
            }

            if (includeActionPads)
            {
                foreach (var pad in package.ActionPads ?? Array.Empty<ActionPadEntry>())
                {
                    var copy = pad.Clone();
                    copy.DocEntry = 0;
                    copy.FormType = formType;
                    foreach (var button in copy.Buttons)
                    {
                        button.DocEntry = 0;
                        button.PadEntry = 0;
                    }
                    ActionPadService.Save(copy);
                }
            }

            return package;
        }

        private static string SafeGetCurrentUser()
        {
            try { return B1App.Instance?.Application?.Company?.UserName ?? Environment.UserName; }
            catch { return Environment.UserName; }
        }

        private static ValidationRuleEntry CloneValidationForExport(ValidationRuleEntry entry)
        {
            return new ValidationRuleEntry
            {
                Code = entry.Code,
                Name = entry.Name,
                FormType = entry.FormType,
                ItemName = entry.ItemName,
                EventType = entry.EventType,
                Condition = entry.Condition,
                Action = entry.Action,
                Severity = entry.Severity,
                Active = entry.Active,
                AppliesToUser = entry.AppliesToUser,
                AppliesToUserGroup = entry.AppliesToUserGroup,
                Message = entry.Message,
                BlockAlways = entry.BlockAlways,
                Sequence = entry.Sequence,
                PromptButtons = entry.PromptButtons,
                Notes = entry.Notes,
                ExcludedUsers = entry.ExcludedUsers,
                ExcludedUserGroups = entry.ExcludedUserGroups,
                ScopeLocalization = entry.ScopeLocalization,
                ScopeVariant = entry.ScopeVariant,
                ScopeDependsOn = entry.ScopeDependsOn,
                ScopeInheritFrom = entry.ScopeInheritFrom,
                ScopePackages = entry.ScopePackages
            };
        }

        private static MandatoryFieldEntry CloneMandatoryForExport(MandatoryFieldEntry entry)
        {
            return new MandatoryFieldEntry
            {
                Code = entry.Code,
                Name = entry.Name,
                FormType = entry.FormType,
                ItemId = entry.ItemId,
                ColumnId = entry.ColumnId,
                Condition = entry.Condition,
                ErrorMessage = entry.ErrorMessage
            };
        }

        private static ActionPadEntry ClonePadForExport(ActionPadEntry pad)
        {
            var clone = pad.Clone();
            return clone;
        }

        private static IList<UiScopeDescriptor> BuildScopeDescriptors(IEnumerable<UiCustomizationEntry> entries, IEnumerable<ValidationRuleEntry> validations)
        {
            var map = new Dictionary<(string User, string Group, string Locale, string Variant), int>();

            void AddScope(string user, string group, string locale, string variant)
            {
                string u = NormalizeScopeToken(user);
                string g = NormalizeScopeToken(group);
                string l = NormalizeScopeToken(locale);
                string v = NormalizeScopeToken(variant);
                var key = (u, g, l, v);
                map[key] = map.ContainsKey(key) ? map[key] + 1 : 1;
            }

            foreach (var entry in entries ?? Array.Empty<UiCustomizationEntry>())
            {
                AddScope(entry.UserCode, entry.UserGroup, entry.Localization, entry.Variant);
            }

            foreach (var rule in validations ?? Array.Empty<ValidationRuleEntry>())
            {
                var users = SplitTokens(rule.AppliesToUser).DefaultIfEmpty("*");
                var groups = SplitTokens(rule.AppliesToUserGroup).DefaultIfEmpty("*");
                foreach (var user in users)
                foreach (var group in groups)
                {
                    AddScope(user, group, rule.ScopeLocalization, rule.ScopeVariant);
                }
            }

            return map
                .Select(k => new UiScopeDescriptor
                {
                    UserCode = k.Key.User,
                    UserGroup = k.Key.Group,
                    Localization = k.Key.Locale,
                    Variant = k.Key.Variant,
                    EntryCount = k.Value
                })
                .OrderByDescending(s => s.EntryCount)
                .ToList();

            string NormalizeScopeToken(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "*";
                return value.Trim().ToUpperInvariant();
            }
        }

        private static IList<UiPackageDependency> BuildDependencies(IEnumerable<UiCustomizationEntry> entries, IEnumerable<ValidationRuleEntry> validations)
        {
            var set = new Dictionary<string, UiPackageDependency>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in entries.SelectMany(e => SplitTokens(e.DependsOn)))
            {
                if (set.ContainsKey(token)) continue;
                set[token] = new UiPackageDependency
                {
                    Token = token,
                    Description = token.Contains(":") ? token : $"Requires customization '{token}'",
                    Required = true
                };
            }

            foreach (var token in (validations ?? Array.Empty<ValidationRuleEntry>()).SelectMany(v => SplitTokens(v.ScopeDependsOn)))
            {
                if (set.ContainsKey(token)) continue;
                set[token] = new UiPackageDependency
                {
                    Token = token,
                    Description = $"Validation dependency '{token}'",
                    Required = true
                };
            }

            foreach (var pkg in (validations ?? Array.Empty<ValidationRuleEntry>()).SelectMany(v => v.GetPackageTokens()))
            {
                if (set.ContainsKey(pkg)) continue;
                set[pkg] = new UiPackageDependency
                {
                    Token = pkg,
                    Description = $"Package '{pkg}' requerido por validaciones",
                    Required = true
                };
            }

            return set.Values.ToList();
        }

        private static IList<UiInheritanceLink> BuildInheritanceLinks(IEnumerable<UiCustomizationEntry> entries, IEnumerable<ValidationRuleEntry> validations)
        {
            var links = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.InheritFrom) && !string.IsNullOrWhiteSpace(e.Code))
                .Select(e => new UiInheritanceLink
                {
                    ParentCode = e.InheritFrom,
                    ChildCode = e.Code
                })
                .ToList();

            if (validations != null)
            {
                foreach (var rule in validations.Where(v => !string.IsNullOrWhiteSpace(v.ScopeInheritFrom) && !string.IsNullOrWhiteSpace(v.Code)))
                {
                    links.Add(new UiInheritanceLink
                    {
                        ParentCode = rule.ScopeInheritFrom,
                        ChildCode = rule.Code
                    });
                }
            }

            return links;
        }

        private static IEnumerable<string> SplitTokens(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            var pieces = raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var piece in pieces)
            {
                var token = piece.Trim();
                if (!string.IsNullOrEmpty(token))
                    yield return token;
            }
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null) return null;
            if (int.TryParse(value.ToString(), out int result)) return result;
            return null;
        }

        private static DateTime? ToNullableDate(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out DateTime dt)) return dt;
            return null;
        }
    }
}
