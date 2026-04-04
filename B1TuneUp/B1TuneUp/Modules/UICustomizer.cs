using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public class UICustomizer
    {
        public static void ApplyCustomization(SAPbouiCOM.Form oForm)
        {
            try
            {
                var entries = UICustomizerService.GetAll(oForm.TypeEx)
                    .OrderBy(e => e.Priority)
                    .ToList();
                if (entries.Count == 0) return;

                var entryMap = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Code))
                    .ToDictionary(e => e.Code, e => e, StringComparer.OrdinalIgnoreCase);
                var appliedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var localization = GetLocalizationContext();
                var variantTag = GetVariantTag();

                foreach (var entry in entries)
                {
                    if (!MatchesCurrentUser(entry.UserCode, entry.UserGroup))
                        continue;
                    if (!MatchesLocalization(entry.Localization, localization))
                        continue;
                    if (!MatchesVariant(entry.Variant, variantTag))
                        continue;
                    if (!ConditionMatches(entry.Condition, oForm))
                        continue;
                    if (!DependenciesSatisfied(entry, appliedCodes))
                        continue;

                    var effective = ResolveInheritance(entry, entryMap);
                    try
                    {
                        ApplyEntry(oForm, effective);
                        if (!string.IsNullOrWhiteSpace(entry.Code))
                        {
                            appliedCodes.Add(entry.Code);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando UI Customization: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static readonly object _contextLock = new object();
        private static UserContext _userContext;

        private static bool MatchesCurrentUser(string userFilter, string groupFilter)
        {
            if (string.IsNullOrWhiteSpace(userFilter) && string.IsNullOrWhiteSpace(groupFilter))
                return true;

            var context = GetUserContext();
            if (!AllowListContains(userFilter, context.UserCode, context.UserName))
                return false;
            if (!AllowListContains(groupFilter, context.GroupCodes.ToArray()))
                return false;
            return true;
        }

        private static bool MatchesLocalization(string localizationFilter, LocalizationContext context)
        {
            if (string.IsNullOrWhiteSpace(localizationFilter)) return true;
            foreach (var token in SplitTokens(localizationFilter))
            {
                if (token == "*") return true;
                if (string.Equals(token, context.Country, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(token, context.Region, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(token, context.Language, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(token, context.LanguageTag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool MatchesVariant(string variantFilter, string variantTag)
        {
            if (string.IsNullOrWhiteSpace(variantFilter)) return true;
            if (string.IsNullOrWhiteSpace(variantTag)) return false;
            foreach (var token in SplitTokens(variantFilter))
            {
                if (token == "*" || token.Equals(variantTag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool ConditionMatches(string conditionSql, SAPbouiCOM.Form oForm)
        {
            if (string.IsNullOrWhiteSpace(conditionSql)) return true;
            return MacroEngine.CheckCondition(conditionSql, oForm);
        }

        private static bool DependenciesSatisfied(UiCustomizationEntry entry, HashSet<string> appliedCodes)
        {
            foreach (var token in SplitTokens(entry.DependsOn))
            {
                if (token.StartsWith("feature:", StringComparison.OrdinalIgnoreCase))
                {
                    // for now assume feature dependencies are satisfied
                    continue;
                }

                if (!appliedCodes.Contains(token))
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Omitiendo {entry.Code ?? entry.ItemId}: depende de {token}.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                    return false;
                }
            }
            return true;
        }

        private static UiCustomizationEntry ResolveInheritance(UiCustomizationEntry entry, IDictionary<string, UiCustomizationEntry> map)
        {
            if (string.IsNullOrWhiteSpace(entry.InheritFrom) || map == null || !map.TryGetValue(entry.InheritFrom, out var parent))
                return entry;
            var resolved = ResolveInheritanceInternal(entry, map, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return resolved;
        }

        private static UiCustomizationEntry ResolveInheritanceInternal(UiCustomizationEntry entry, IDictionary<string, UiCustomizationEntry> map, HashSet<string> trail)
        {
            if (string.IsNullOrWhiteSpace(entry.InheritFrom) || !map.TryGetValue(entry.InheritFrom, out var parent))
                return entry;
            var key = entry.Code ?? Guid.NewGuid().ToString("N");
            if (!trail.Add(key)) return entry;
            var resolvedParent = ResolveInheritanceInternal(parent, map, trail);
            trail.Remove(key);
            return MergeEntries(resolvedParent, entry);
        }

        private static UiCustomizationEntry MergeEntries(UiCustomizationEntry parent, UiCustomizationEntry child)
        {
            var merged = parent.Clone();
            merged.Code = child.Code;
            merged.Name = string.IsNullOrWhiteSpace(child.Name) ? merged.Name : child.Name;
            merged.FormType = string.IsNullOrWhiteSpace(child.FormType) ? merged.FormType : child.FormType;
            merged.ItemId = string.IsNullOrWhiteSpace(child.ItemId) ? merged.ItemId : child.ItemId;
            merged.Action = string.IsNullOrWhiteSpace(child.Action) ? merged.Action : child.Action;
            merged.Top = child.Top ?? merged.Top;
            merged.Left = child.Left ?? merged.Left;
            merged.Width = child.Width ?? merged.Width;
            merged.Height = child.Height ?? merged.Height;
            merged.Label = string.IsNullOrWhiteSpace(child.Label) ? merged.Label : child.Label;
            merged.FromPane = child.FromPane ?? merged.FromPane;
            merged.ToPane = child.ToPane ?? merged.ToPane;
            merged.UserCode = string.IsNullOrWhiteSpace(child.UserCode) ? merged.UserCode : child.UserCode;
            merged.UserGroup = string.IsNullOrWhiteSpace(child.UserGroup) ? merged.UserGroup : child.UserGroup;
            merged.Condition = string.IsNullOrWhiteSpace(child.Condition) ? merged.Condition : child.Condition;
            merged.Priority = child.Priority > 0 ? child.Priority : merged.Priority;
            merged.Localization = string.IsNullOrWhiteSpace(child.Localization) ? merged.Localization : child.Localization;
            merged.Variant = string.IsNullOrWhiteSpace(child.Variant) ? merged.Variant : child.Variant;
            merged.DependsOn = string.IsNullOrWhiteSpace(child.DependsOn) ? merged.DependsOn : child.DependsOn;
            merged.InheritFrom = child.InheritFrom;
            return merged;
        }

        private static void ApplyEntry(SAPbouiCOM.Form oForm, UiCustomizationEntry entry)
        {
            string action = entry.Action ?? "Hide";
            string itemId = entry.ItemId ?? string.Empty;
            switch (action)
            {
                case "Hide":
                    oForm.Items.Item(itemId).Visible = false;
                    break;
                case "Move":
                    {
                        Item item = oForm.Items.Item(itemId);
                        if (entry.Top.HasValue) item.Top = entry.Top.Value;
                        if (entry.Left.HasValue) item.Left = entry.Left.Value;
                        if (entry.Width.HasValue) item.Width = entry.Width.Value;
                        if (entry.Height.HasValue) item.Height = entry.Height.Value;
                    }
                    break;
                case "Resize":
                    {
                        Item item = oForm.Items.Item(itemId);
                        if (entry.Width.HasValue) item.Width = entry.Width.Value;
                        if (entry.Height.HasValue) item.Height = entry.Height.Value;
                    }
                    break;
                case "ChangeLabel":
                    {
                        Item item = oForm.Items.Item(itemId);
                        string text = entry.Label ?? string.Empty;
                        if (item.Specific is StaticText lbl)
                        {
                            lbl.Caption = text;
                        }
                        else if (item.Specific is EditText txt)
                        {
                            txt.Value = text;
                        }
                        else if (item.Specific is SAPbouiCOM.Button btn)
                        {
                            btn.Caption = text;
                        }
                    }
                    break;
                case "Enable":
                    oForm.Items.Item(itemId).Enabled = true;
                    break;
                case "Disable":
                    oForm.Items.Item(itemId).Enabled = false;
                    break;
                case "AddButton":
                    AddButton(oForm,
                        $"btn_{Guid.NewGuid().ToString().Substring(0, 5)}",
                        entry.Label ?? string.Empty,
                        entry.Left ?? 0,
                        entry.Top ?? 0,
                        entry.Width ?? 80,
                        entry.Height ?? 20,
                        itemId);
                    break;
                case "AddFolder":
                    AddFolder(oForm,
                        $"fld_{Guid.NewGuid().ToString().Substring(0, 5)}",
                        entry.Label ?? string.Empty,
                        entry.Left ?? 0,
                        entry.Top ?? 0,
                        entry.Width ?? 80,
                        entry.Height ?? 20);
                    break;
                case "AddEditText":
                    AddEditText(oForm,
                        $"txt_{Guid.NewGuid().ToString().Substring(0, 5)}",
                        entry.Label ?? string.Empty,
                        entry.Left ?? 0,
                        entry.Top ?? 0,
                        entry.Width ?? 80,
                        entry.Height ?? 20);
                    break;
            }
        }

        private static UserContext GetUserContext()
        {
            if (_userContext != null) return _userContext;
            lock (_contextLock)
            {
                if (_userContext != null) return _userContext;
                var ctx = new UserContext
                {
                    UserCode = SafeString(() => B1App.Instance.Company.UserName),
                    UserName = SafeString(() => B1App.Instance.Company.UserName)
                };

                try
                {
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string sql = B1App.Instance.IsHana
                        ? $"SELECT G.\"GroupCode\" FROM OUSR U INNER JOIN USR6 UG ON U.\"USERID\" = UG.\"USERID\" INNER JOIN OUGR G ON UG.\"GroupCode\" = G.\"GroupCode\" WHERE U.\"USER_CODE\" = '{ctx.UserCode}'"
                        : $"SELECT G.GroupCode FROM OUSR U WITH (NOLOCK) INNER JOIN USR6 UG ON U.USERID = UG.USERID INNER JOIN OUGR G ON UG.GroupCode = G.GroupCode WHERE U.USER_CODE = '{ctx.UserCode}'";
                    rs.DoQuery(sql);
                    while (!rs.EoF)
                    {
                        string code = rs.Fields.Item(0).Value?.ToString();
                        if (!string.IsNullOrEmpty(code)) ctx.GroupCodes.Add(code);
                        rs.MoveNext();
                    }
                    ComObjectManager.Release(rs);
                }
                catch { }

                _userContext = ctx;
                return ctx;
            }
        }

        private static bool AllowListContains(string filter, params string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            if (candidates == null) candidates = Array.Empty<string>();

            var tokens = filter.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string value = token.Trim();
                if (value == "*") return true;
                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrEmpty(candidate) && value.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static string SafeString(object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private static string SafeString(Func<string> getter)
        {
            try { return getter() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static LocalizationContext _localizationContext;
        private static readonly object _locLock = new object();

        private static LocalizationContext GetLocalizationContext()
        {
            if (_localizationContext != null) return _localizationContext;
            lock (_locLock)
            {
                if (_localizationContext != null) return _localizationContext;
                var ctx = new LocalizationContext();
                try
                {
                    var company = B1App.Instance?.Company;
                    if (company != null)
                    {
                        ctx.Language = ResolveLanguage();
                        ctx.CompanyDb = SafeString(() => company.CompanyDB);
                        var adminInfo = company.GetCompanyService()?.GetAdminInfo();
                        if (adminInfo != null)
                        {
                            ctx.Country = adminInfo.Country ?? string.Empty;
                            ctx.Region = adminInfo.State ?? string.Empty;
                        }
                    }
                }
                catch { }

                ctx.LanguageTag = string.IsNullOrWhiteSpace(ctx.Country)
                    ? ctx.Language
                    : $"{ctx.Language}-{ctx.Country}";
                _localizationContext = ctx;
                return ctx;
            }
        }

        private static string ResolveLanguage()
        {
            var stored = SettingsManager.GetSetting("Language", string.Empty);
            if (!string.IsNullOrWhiteSpace(stored)) return stored;

            var runtime = SafeString(() =>
            {
                var app = B1App.Instance?.Application;
                var prop = app?.GetType().GetProperty("Language");
                return prop?.GetValue(app)?.ToString();
            });

            return string.IsNullOrWhiteSpace(runtime) ? "en-US" : runtime;
        }
        private static string _variantTag;
        private static string GetVariantTag()
        {
            if (!string.IsNullOrWhiteSpace(_variantTag)) return _variantTag;
            _variantTag = SettingsManager.GetSetting("UICustomizer.Variant", "GLOBAL");
            return _variantTag;
        }

        private static IEnumerable<string> SplitTokens(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            var tokens = raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var cleaned = token.Trim();
                if (!string.IsNullOrEmpty(cleaned))
                    yield return cleaned;
            }
        }

        private class UserContext
        {
            public string UserCode { get; set; }
            public string UserName { get; set; }
            public List<string> GroupCodes { get; } = new List<string>();
        }

        private class LocalizationContext
        {
            public string Language { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string Region { get; set; } = string.Empty;
            public string CompanyDb { get; set; } = string.Empty;
            public string LanguageTag { get; set; } = string.Empty;
        }

        public static void AddButton(SAPbouiCOM.Form oForm, string itemId, string caption, int left, int top, int width, int height, string fromItemId = "")
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(itemId, BoFormItemTypes.it_BUTTON);
                oItem.Left = left;
                oItem.Top = top;
                oItem.Width = width;
                oItem.Height = height;

                SAPbouiCOM.Button oBtn = (SAPbouiCOM.Button)oItem.Specific;
                oBtn.Caption = caption;

                if (!string.IsNullOrEmpty(fromItemId))
                {
                    Item fromItem = oForm.Items.Item(fromItemId);
                    oItem.Top = fromItem.Top;
                    oItem.Height = fromItem.Height;
                    oItem.Left = fromItem.Left + fromItem.Width + 5;
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir botón: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }

        public static void HideItem(SAPbouiCOM.Form oForm, string itemId)
        {
            try
            {
                oForm.Items.Item(itemId).Visible = false;
            }
            catch { }
        }

        public static void AddTab(SAPbouiCOM.Form oForm, string tabId, string caption, string afterTabId)
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(tabId, BoFormItemTypes.it_FOLDER);
                Folder oFolder = (Folder)oItem.Specific;
                oFolder.Caption = caption;
                oFolder.GroupWith(afterTabId);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir pestaña: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }

        public static void MoveItem(SAPbouiCOM.Form oForm, string itemId, int left, int top)
        {
            try
            {
                Item oItem = oForm.Items.Item(itemId);
                oItem.Left = left;
                oItem.Top = top;
            }
            catch { }
        }

        public static void AddFolder(SAPbouiCOM.Form oForm, string itemId, string caption, int left, int top, int width, int height)
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(itemId, BoFormItemTypes.it_FOLDER);
                oItem.Left = left;
                oItem.Top = top;
                oItem.Width = width;
                oItem.Height = height;

                Folder oFolder = (Folder)oItem.Specific;
                oFolder.Caption = caption;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir carpeta: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }

        public static void AddEditText(SAPbouiCOM.Form oForm, string itemId, string value, int left, int top, int width, int height)
        {
            Item oItem = null;
            try
            {
                oItem = oForm.Items.Add(itemId, BoFormItemTypes.it_EDIT);
                oItem.Left = left;
                oItem.Top = top;
                oItem.Width = width;
                oItem.Height = height;

                EditText oEdit = (EditText)oItem.Specific;
                oEdit.Value = value;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error al añadir campo de texto: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(oItem);
            }
        }
    }
}



