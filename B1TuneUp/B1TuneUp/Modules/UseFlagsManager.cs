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

            if (!IsBusinessPartnerForm(form)) return;

            if (string.Equals(eventName, "et_CLICK", StringComparison.OrdinalIgnoreCase) &&
                !pVal.BeforeAction &&
                string.Equals(pVal.ItemUID, FlagItemId, StringComparison.OrdinalIgnoreCase))
            {
                ShowCountryInStatusBar(form.UniqueID);
                return;
            }

            if (!ToolboxManager.IsSettingEnabled(ToolboxManager.UseFlagsSettingCode, true))
            {
                HideFlag(form);
                return;
            }

            if (pVal.BeforeAction) return;
            if (!ShouldRefresh(eventName)) return;

            RefreshFlag(form);
        }

        private static bool IsBusinessPartnerForm(Form form)
        {
            try
            {
                return string.Equals(form.TypeEx, BusinessPartnerFormType, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
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

        private static void RefreshFlag(Form form)
        {
            try
            {
                string cardCode = GetCardCode(form);
                if (string.IsNullOrWhiteSpace(cardCode))
                {
                    HideFlag(form);
                    UpdateContext(form.UniqueID, string.Empty, string.Empty, string.Empty);
                    return;
                }

                var current = GetContext(form.UniqueID);
                if (current != null &&
                    string.Equals(current.CardCode, cardCode, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(current.CountryCode))
                    {
                        HideFlag(form);
                    }
                    else
                    {
                        ApplyFlagToForm(form, current.CountryCode, current.CountryName);
                    }
                    return;
                }

                var info = ResolveBillToCountry(cardCode);
                if (string.IsNullOrWhiteSpace(info.CountryCode))
                {
                    HideFlag(form);
                    UpdateContext(form.UniqueID, cardCode, string.Empty, string.Empty);
                    return;
                }

                ApplyFlagToForm(form, info.CountryCode, info.CountryName);
                UpdateContext(form.UniqueID, cardCode, info.CountryCode, info.CountryName);
            }
            catch
            {
                // No interrumpimos la interacción del usuario por fallos visuales del indicador.
            }
        }

        private static void ApplyFlagToForm(Form form, string countryCode, string countryName)
        {
            string iconPath = FlagIconProvider.GetIconPath(countryCode);
            if (string.IsNullOrWhiteSpace(iconPath))
            {
                HideFlag(form);
                return;
            }

            var flagItem = EnsureFlagItem(form);
            if (flagItem == null) return;

            PositionFlagItem(form, flagItem);
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
            Item item = null;
            try
            {
                item = form.Items.Item(FlagItemId);
            }
            catch
            {
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
            }
            return item;
        }

        private static void PositionFlagItem(Form form, Item flagItem)
        {
            try
            {
                Item anchor = TryGetItem(form, "5") ?? TryGetItem(form, "4");
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
                object specific = flagItem?.Specific;
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
                // Ignoramos errores de rendering por variaciones del SDK.
            }
        }

        private static void HideFlag(Form form)
        {
            try
            {
                var item = form.Items.Item(FlagItemId);
                item.Visible = false;
            }
            catch { }
        }

        private static Item TryGetItem(Form form, string itemId)
        {
            if (form == null || string.IsNullOrWhiteSpace(itemId)) return null;
            try { return form.Items.Item(itemId); } catch { return null; }
        }

        private static string GetCardCode(Form form)
        {
            string value = ReadFromDbDataSource(form, "OCRD", "CardCode");
            if (!string.IsNullOrWhiteSpace(value)) return value;

            value = ReadFromItem(form, "5");
            if (!string.IsNullOrWhiteSpace(value)) return value;

            value = ReadFromItem(form, "4");
            return value ?? string.Empty;
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
                    return NormalizeSapValue(((EditText)item.Specific).Value);
                }
                if (item.Type == BoFormItemTypes.it_COMBO_BOX)
                {
                    return NormalizeSapValue(((ComboBox)item.Specific).Selected?.Value);
                }
            }
            catch { }
            return string.Empty;
        }

        private static CountryInfo ResolveBillToCountry(string cardCode)
        {
            string normalizedCardCode = EscapeSql(cardCode);
            string billToDef = string.Empty;
            string bpCountry = string.Empty;

            ReadBusinessPartnerDefaults(normalizedCardCode, ref billToDef, ref bpCountry);

            string country = string.Empty;
            if (!string.IsNullOrWhiteSpace(billToDef))
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
                    billToDef = NormalizeSapValue(rs.Fields.Item(0).Value?.ToString());
                    bpCountry = NormalizeSapValue(rs.Fields.Item(1).Value?.ToString());
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
                    return NormalizeSapValue(rs.Fields.Item(0).Value?.ToString());
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
                    return NormalizeSapValue(rs.Fields.Item(0).Value?.ToString());
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
                    return NormalizeSapValue(rs.Fields.Item(0).Value?.ToString());
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

        private static void UpdateContext(string formUid, string cardCode, string countryCode, string countryName)
        {
            if (string.IsNullOrWhiteSpace(formUid)) return;
            lock (SyncRoot)
            {
                ContextByFormUid[formUid] = new UseFlagContext
                {
                    CardCode = cardCode ?? string.Empty,
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

        private sealed class UseFlagContext
        {
            public string CardCode { get; set; } = string.Empty;
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
