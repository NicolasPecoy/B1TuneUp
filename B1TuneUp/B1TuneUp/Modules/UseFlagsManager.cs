using System;
using System.Collections.Generic;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    public static class UseFlagsManager
    {
        private const string BusinessPartnerFormType = "134";
        private const string FlagItemId = "BTUN_FLG";

        private static readonly string[] CardCodeItemCandidates = { "4", "5", "54", "14", "6" };
        private static readonly string[] MarketingDocumentTables =
        {
            // A/R
            "OQUT", "ORDR", "ODLN", "OINV", "ORIN", "ODPI", "ORDN",
            // A/P
            "OPQT", "OPOR", "OPDN", "OPCH", "ORPC", "ODPO", "ORPD"
        };

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, UseFlagContext> ContextByFormUid = new Dictionary<string, UseFlagContext>(StringComparer.OrdinalIgnoreCase);

        public static void HandleItemEvent(Form form, ItemEvent pVal)
        {
            if (form == null) return;

            string eventName = pVal.EventType.ToString();
            if (string.Equals(eventName, "et_FORM_CLOSE", StringComparison.OrdinalIgnoreCase) && pVal.BeforeAction)
            {
                ClearContext(form.UniqueID);
                return;
            }

            if (string.Equals(eventName, "et_CLICK", StringComparison.OrdinalIgnoreCase) &&
                !pVal.BeforeAction &&
                string.Equals(pVal.ItemUID, FlagItemId, StringComparison.OrdinalIgnoreCase))
            {
                ShowCountryInStatusBar(form.UniqueID);
                return;
            }

            if (!TryResolveScope(form, out var scope))
            {
                return;
            }

            bool enabled = scope == UseFlagScope.BusinessPartner
                ? ToolboxManager.IsSettingEnabled(ToolboxManager.UseFlagsSettingCode, true)
                : ToolboxManager.IsSettingEnabled(ToolboxManager.UseFlagsDocumentsSettingCode, true);

            if (!enabled)
            {
                HideFlag(form);
                return;
            }

            if (pVal.BeforeAction) return;
            if (!ShouldRefresh(eventName)) return;

            RefreshFlag(form, scope);
        }

        private static bool TryResolveScope(Form form, out UseFlagScope scope)
        {
            scope = UseFlagScope.None;
            try
            {
                if (string.Equals(form.TypeEx, BusinessPartnerFormType, StringComparison.OrdinalIgnoreCase))
                {
                    scope = UseFlagScope.BusinessPartner;
                    return true;
                }

                if (TryGetDocumentTable(form, out _))
                {
                    scope = UseFlagScope.MarketingDocument;
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool ShouldRefresh(string eventName)
        {
            return string.Equals(eventName, "et_FORM_LOAD", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_FORM_ACTIVATE", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_FORM_RESIZE", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_FORM_VISIBLE", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_FORM_DATA_LOAD", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_ITEM_PRESSED", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_VALIDATE", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_COMBO_SELECT", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(eventName, "et_CLICK", StringComparison.OrdinalIgnoreCase);
        }

        private static void RefreshFlag(Form form, UseFlagScope scope)
        {
            try
            {
                var context = BuildFormContext(form, scope);
                if (context == null || string.IsNullOrWhiteSpace(context.CardCode))
                {
                    HideFlag(form);
                    UpdateContext(form.UniqueID, string.Empty, string.Empty, string.Empty);
                    return;
                }

                var current = GetContext(form.UniqueID);
                if (current != null && string.Equals(current.CacheToken, context.CacheToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(current.CountryCode))
                    {
                        HideFlag(form);
                    }
                    else
                    {
                        ApplyFlagToForm(form, current.CountryCode, current.CountryName, context.AnchorItemId);
                    }
                    return;
                }

                var info = ResolveBillToCountry(context.CardCode, context.BillToCode);
                if (string.IsNullOrWhiteSpace(info.CountryCode))
                {
                    HideFlag(form);
                    UpdateContext(form.UniqueID, context.CacheToken, string.Empty, string.Empty);
                    return;
                }

                ApplyFlagToForm(form, info.CountryCode, info.CountryName, context.AnchorItemId);
                UpdateContext(form.UniqueID, context.CacheToken, info.CountryCode, info.CountryName);
            }
            catch
            {
                // El indicador visual no debe bloquear el flujo del usuario.
            }
        }

        private static FormContext BuildFormContext(Form form, UseFlagScope scope)
        {
            if (scope == UseFlagScope.BusinessPartner)
            {
                string anchor = string.Empty;
                string cardCode = GetCardCode(form, "OCRD", out anchor);
                string normalizedCard = NormalizeSapValue(cardCode);
                string cacheToken = $"BP|{normalizedCard}";
                return new FormContext(normalizedCard, string.Empty, cacheToken, string.IsNullOrWhiteSpace(anchor) ? "5" : anchor);
            }

            if (!TryGetDocumentTable(form, out var table)) return null;

            string documentAnchor = string.Empty;
            string documentCardCode = GetCardCode(form, table, out documentAnchor);
            string payToCode = ReadFromDbDataSource(form, table, "PayToCode");
            string docEntryRaw = ReadFromDbDataSource(form, table, "DocEntry");
            int.TryParse(docEntryRaw, out int docEntry);

            string normalizedCardCode = NormalizeSapValue(documentCardCode);
            string normalizedPayTo = NormalizeSapValue(payToCode);
            string cacheTokenDoc = $"DOC|{table}|{docEntry}|{normalizedCardCode}|{normalizedPayTo}";

            return new FormContext(
                normalizedCardCode,
                normalizedPayTo,
                cacheTokenDoc,
                string.IsNullOrWhiteSpace(documentAnchor) ? "4" : documentAnchor);
        }

        private static string GetCardCode(Form form, string preferredTable, out string anchorItemId)
        {
            anchorItemId = string.Empty;

            if (!string.IsNullOrWhiteSpace(preferredTable))
            {
                string fromTable = ReadFromDbDataSource(form, preferredTable, "CardCode");
                if (!string.IsNullOrWhiteSpace(fromTable))
                {
                    // Intentamos ubicar el item visual más probable del CardCode.
                    foreach (var candidate in CardCodeItemCandidates)
                    {
                        if (!string.IsNullOrWhiteSpace(ReadFromItem(form, candidate)))
                        {
                            anchorItemId = candidate;
                            break;
                        }
                    }
                    return fromTable;
                }
            }

            string fromOcrd = ReadFromDbDataSource(form, "OCRD", "CardCode");
            if (!string.IsNullOrWhiteSpace(fromOcrd))
            {
                anchorItemId = "5";
                return fromOcrd;
            }

            foreach (var itemId in CardCodeItemCandidates)
            {
                string value = ReadFromItem(form, itemId);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    anchorItemId = itemId;
                    return value;
                }
            }

            return string.Empty;
        }

        private static bool TryGetDocumentTable(Form form, out string tableName)
        {
            tableName = string.Empty;
            if (form == null) return false;

            foreach (var table in MarketingDocumentTables)
            {
                if (HasDbDataSource(form, table))
                {
                    tableName = table;
                    return true;
                }
            }
            return false;
        }

        private static bool HasDbDataSource(Form form, string tableName)
        {
            try
            {
                var ds = form.DataSources?.DBDataSources?.Item(tableName);
                return ds != null;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyFlagToForm(Form form, string countryCode, string countryName, string anchorItemId)
        {
            string iconPath = FlagIconProvider.GetIconPath(countryCode);
            if (string.IsNullOrWhiteSpace(iconPath))
            {
                HideFlag(form);
                return;
            }

            var flagItem = EnsureFlagItem(form);
            if (flagItem == null) return;

            PositionFlagItem(form, flagItem, anchorItemId);
            SetPicture(flagItem, iconPath);
            flagItem.Visible = true;

            try
            {
                flagItem.Description = string.IsNullOrWhiteSpace(countryName) ? countryCode : $"{countryName} ({countryCode})";
            }
            catch { }
        }

        private static Item EnsureFlagItem(Form form)
        {
            Item item = SapUiSafe.TryGetItem(form, FlagItemId);
            if (item != null) return item;

            try
            {
                item = form.Items.Add(FlagItemId, BoFormItemTypes.it_PICTURE);
                item.Width = 30;
                item.Height = 20;
                item.Enabled = true;
                item.Visible = true;
                try { item.AffectsFormMode = false; } catch { }
            }
            catch
            {
                return null;
            }
            return item;
        }

        private static void PositionFlagItem(Form form, Item flagItem, string anchorItemId)
        {
            try
            {
                Item anchor = TryGetItem(form, anchorItemId);
                if (anchor == null)
                {
                    foreach (var candidate in CardCodeItemCandidates)
                    {
                        anchor = TryGetItem(form, candidate);
                        if (anchor != null) break;
                    }
                }

                if (anchor != null)
                {
                    flagItem.Left = anchor.Left + anchor.Width + 6;
                    flagItem.Top = anchor.Top + Math.Max(0, (anchor.Height - flagItem.Height) / 2);
                    try
                    {
                        flagItem.FromPane = anchor.FromPane;
                        flagItem.ToPane = anchor.ToPane;
                    }
                    catch { }
                }
                else
                {
                    flagItem.Left = Math.Max(6, form.ClientWidth - flagItem.Width - 8);
                    flagItem.Top = 42;
                }

                if (flagItem.Left + flagItem.Width > form.ClientWidth - 4)
                    flagItem.Left = Math.Max(6, form.ClientWidth - flagItem.Width - 8);

                if (flagItem.Top + flagItem.Height > form.ClientHeight - 4)
                    flagItem.Top = Math.Max(6, form.ClientHeight - flagItem.Height - 8);
            }
            catch { }
        }

        private static void SetPicture(Item flagItem, string filePath)
        {
            try
            {
                object specific = SapUiSafe.TryGetSpecificObject(flagItem);
                if (specific == null) return;

                var type = specific.GetType();
                var prop = type.GetProperty("Picture") ?? type.GetProperty("PictureFileName");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(specific, filePath, null);
                    return;
                }

                var method = type.GetMethod("SetPicture");
                if (method != null)
                {
                    method.Invoke(specific, new object[] { filePath });
                }
            }
            catch
            {
                // Variaciones del SDK: no interrumpimos el flujo.
            }
        }

        private static void HideFlag(Form form)
        {
            try
            {
                var item = SapUiSafe.TryGetItem(form, FlagItemId);
                if (item == null) return;
                item.Visible = false;
            }
            catch { }
        }

        private static Item TryGetItem(Form form, string itemId)
        {
            return SapUiSafe.TryGetItem(form, itemId);
        }

        private static string ReadFromDbDataSource(Form form, string table, string field)
        {
            if (form == null || string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(field)) return string.Empty;
            try
            {
                var ds = form.DataSources?.DBDataSources?.Item(table);
                if (ds == null) return string.Empty;
                return NormalizeSapValue(ds.GetValue(field, 0));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ReadFromItem(Form form, string itemId)
        {
            var item = TryGetItem(form, itemId);
            if (item == null) return string.Empty;
            try
            {
                if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_EXTEDIT)
                {
                    return NormalizeSapValue(SapUiSafe.TryGetSpecific<EditText>(item)?.Value);
                }
                if (item.Type == BoFormItemTypes.it_COMBO_BOX)
                {
                    return NormalizeSapValue(SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(item)));
                }
            }
            catch { }
            return string.Empty;
        }

        private static CountryInfo ResolveBillToCountry(string cardCode, string preferredBillToCode)
        {
            string normalizedCardCode = EscapeSql(cardCode);
            string billToDef = string.Empty;
            string bpCountry = string.Empty;

            ReadBusinessPartnerDefaults(normalizedCardCode, ref billToDef, ref bpCountry);

            string country = string.Empty;
            string preferred = NormalizeSapValue(preferredBillToCode);
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                country = ReadBillToCountryByAddress(normalizedCardCode, preferred);
            }

            if (string.IsNullOrWhiteSpace(country) && !string.IsNullOrWhiteSpace(billToDef))
            {
                country = ReadBillToCountryByAddress(normalizedCardCode, billToDef);
            }
            if (string.IsNullOrWhiteSpace(country))
            {
                country = ReadFirstBillToCountry(normalizedCardCode);
            }
            if (string.IsNullOrWhiteSpace(country))
            {
                country = bpCountry;
            }

            country = NormalizeSapValue(country).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(country))
            {
                return CountryInfo.Empty;
            }

            string countryName = ReadCountryName(country);
            if (string.IsNullOrWhiteSpace(countryName))
            {
                countryName = country;
            }

            return new CountryInfo(country, countryName);
        }

        private static void ReadBusinessPartnerDefaults(string safeCardCode, ref string billToDef, ref string bpCountry)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"BillToDef\",\"Country\" FROM \"OCRD\" WHERE \"CardCode\" = '{safeCardCode}' LIMIT 1"
                    : $"SELECT TOP 1 BillToDef, Country FROM OCRD WHERE CardCode = '{safeCardCode}'";
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    billToDef = NormalizeSapValue(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0));
                    bpCountry = NormalizeSapValue(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 1));
                }
            }
            catch { }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string ReadBillToCountryByAddress(string safeCardCode, string billToDef)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string safeAddress = EscapeSql(billToDef);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"Country\" FROM \"CRD1\" WHERE \"CardCode\" = '{safeCardCode}' AND \"AdresType\" = 'B' AND \"Address\" = '{safeAddress}' LIMIT 1"
                    : $"SELECT TOP 1 Country FROM CRD1 WHERE CardCode = '{safeCardCode}' AND AdresType = 'B' AND Address = '{safeAddress}'";
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return NormalizeSapValue(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0));
                }
            }
            catch { }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return string.Empty;
        }

        private static string ReadFirstBillToCountry(string safeCardCode)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"Country\" FROM \"CRD1\" WHERE \"CardCode\" = '{safeCardCode}' AND \"AdresType\" = 'B' ORDER BY \"LineNum\" LIMIT 1"
                    : $"SELECT TOP 1 Country FROM CRD1 WHERE CardCode = '{safeCardCode}' AND AdresType = 'B' ORDER BY LineNum";
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return NormalizeSapValue(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0));
                }
            }
            catch { }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return string.Empty;
        }

        private static string ReadCountryName(string safeCountryCode)
        {
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"Name\" FROM \"OCRY\" WHERE \"Code\" = '{EscapeSql(safeCountryCode)}' LIMIT 1"
                    : $"SELECT TOP 1 Name FROM OCRY WHERE Code = '{EscapeSql(safeCountryCode)}'";
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return NormalizeSapValue(B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0));
                }
            }
            catch { }
            finally
            {
                ComObjectManager.Release(rs);
            }
            return string.Empty;
        }

        private static string NormalizeSapValue(string raw)
        {
            return (raw ?? string.Empty).Replace("\0", string.Empty).Trim();
        }

        private static string EscapeSql(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private static void UpdateContext(string formUid, string cacheToken, string countryCode, string countryName)
        {
            if (string.IsNullOrWhiteSpace(formUid)) return;
            lock (SyncRoot)
            {
                ContextByFormUid[formUid] = new UseFlagContext
                {
                    CacheToken = cacheToken ?? string.Empty,
                    CountryCode = countryCode ?? string.Empty,
                    CountryName = countryName ?? string.Empty
                };
            }
        }

        private static UseFlagContext GetContext(string formUid)
        {
            if (string.IsNullOrWhiteSpace(formUid)) return null;
            lock (SyncRoot)
            {
                if (ContextByFormUid.TryGetValue(formUid, out var ctx))
                {
                    return ctx;
                }
            }
            return null;
        }

        private static void ClearContext(string formUid)
        {
            if (string.IsNullOrWhiteSpace(formUid)) return;
            lock (SyncRoot)
            {
                ContextByFormUid.Remove(formUid);
            }
        }

        private static void ShowCountryInStatusBar(string formUid)
        {
            var ctx = GetContext(formUid);
            if (ctx == null || string.IsNullOrWhiteSpace(ctx.CountryCode)) return;

            string name = string.IsNullOrWhiteSpace(ctx.CountryName) ? ctx.CountryCode : ctx.CountryName;
            B1App.Instance.Application.SetStatusBarMessage($"País: {name} ({ctx.CountryCode})", BoMessageTime.bmt_Short, false);
        }

        private enum UseFlagScope
        {
            None,
            BusinessPartner,
            MarketingDocument
        }

        private sealed class FormContext
        {
            public FormContext(string cardCode, string billToCode, string cacheToken, string anchorItemId)
            {
                CardCode = cardCode ?? string.Empty;
                BillToCode = billToCode ?? string.Empty;
                CacheToken = cacheToken ?? string.Empty;
                AnchorItemId = anchorItemId ?? string.Empty;
            }

            public string CardCode { get; }
            public string BillToCode { get; }
            public string CacheToken { get; }
            public string AnchorItemId { get; }
        }

        private sealed class UseFlagContext
        {
            public string CacheToken { get; set; } = string.Empty;
            public string CountryCode { get; set; } = string.Empty;
            public string CountryName { get; set; } = string.Empty;
        }

        private readonly struct CountryInfo
        {
            public static CountryInfo Empty => new CountryInfo(string.Empty, string.Empty);

            public CountryInfo(string countryCode, string countryName)
            {
                CountryCode = countryCode ?? string.Empty;
                CountryName = countryName ?? string.Empty;
            }

            public string CountryCode { get; }
            public string CountryName { get; }
        }
    }
}
