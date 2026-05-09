using System;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class AutoRefreshPreferenceService
    {
        private const string KeyPrefix = "AutoRefresh:";

        public static AutoRefreshPreference Load(string context)
        {
            if (string.IsNullOrWhiteSpace(context)) context = "default";
            string enabledKey = BuildKey(context, "enabled");
            string intervalKey = BuildKey(context, "interval");

            var enabledValue = GetGlobalSetting(enabledKey) ?? SettingsManager.GetSetting(enabledKey, "true") ?? "true";
            var intervalRaw = GetGlobalSetting(intervalKey) ?? SettingsManager.GetSetting(intervalKey, "60") ?? "60";

            if (!string.IsNullOrEmpty(enabledValue))
            {
                SettingsManager.SetSetting(enabledKey, enabledValue);
            }
            if (!string.IsNullOrEmpty(intervalRaw))
            {
                SettingsManager.SetSetting(intervalKey, intervalRaw);
            }

            int interval = 60;
            int.TryParse(intervalRaw, out interval);
            interval = Math.Max(5, interval);

            return new AutoRefreshPreference
            {
                Context = context,
                Enabled = !string.Equals(enabledValue, "false", StringComparison.OrdinalIgnoreCase),
                IntervalSeconds = interval
            };
        }

        public static void Save(string context, bool enabled, int intervalSeconds)
        {
            if (string.IsNullOrWhiteSpace(context)) context = "default";
            string enabledKey = BuildKey(context, "enabled");
            string intervalKey = BuildKey(context, "interval");
            string enabledValue = enabled ? "true" : "false";
            string intervalValue = Math.Max(5, intervalSeconds).ToString();

            SettingsManager.SetSetting(enabledKey, enabledValue);
            SettingsManager.SetSetting(intervalKey, intervalValue);
            SaveGlobalSetting(enabledKey, enabledValue);
            SaveGlobalSetting(intervalKey, intervalValue);
        }

        private static string BuildKey(string context, string suffix) => $"{KeyPrefix}{context}:{suffix}";

        private static string GetGlobalSetting(string key)
        {
            try
            {
                var company = B1App.Instance?.Company;
                if (company == null) return null;
                var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                try
                {
                    string safeKey = key.Replace("'", "''");
                    bool isHana = B1App.Instance.IsHana;
                    string sql = isHana
                        ? $"SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = '{safeKey}'"
                        : $"SELECT [U_Value] FROM [@BTUN_TBOX] WHERE [U_Code] = '{safeKey}'";
                    rs.DoQuery(sql);
                    if (rs.RecordCount > 0)
                    {
                        var value = B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, 0);
                        return value?.ToString();
                    }
                }
                finally
                {
                    ComObjectManager.Release(rs);
                }
            }
            catch
            {
            }
            return null;
        }

        private static void SaveGlobalSetting(string key, string value)
        {
            try
            {
                var company = B1App.Instance?.Company;
                if (company == null) return;
                bool isHana = B1App.Instance.IsHana;
                string safeKey = key.Replace("'", "''");
                string safeValue = (value ?? string.Empty).Replace("'", "''");
                var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                try
                {
                    string selectSql = isHana
                        ? $"SELECT \"Code\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = '{safeKey}'"
                        : $"SELECT [Code] FROM [@BTUN_TBOX] WHERE [U_Code] = '{safeKey}'";
                    rs.DoQuery(selectSql);
                    if (rs.RecordCount > 0)
                    {
                        string updateSql = isHana
                            ? $"UPDATE \"@BTUN_TBOX\" SET \"U_Value\" = '{safeValue}' WHERE \"U_Code\" = '{safeKey}'"
                            : $"UPDATE [@BTUN_TBOX] SET [U_Value] = '{safeValue}' WHERE [U_Code] = '{safeKey}'";
                        rs.DoQuery(updateSql);
                    }
                    else
                    {
                        string insertSql = isHana
                            ? $"INSERT INTO \"@BTUN_TBOX\" (\"Code\",\"Name\",\"U_Code\",\"U_Value\") VALUES ('{safeKey}', '{safeKey}', '{safeKey}', '{safeValue}')"
                            : $"INSERT INTO [@BTUN_TBOX] ([Code],[Name],[U_Code],[U_Value]) VALUES ('{safeKey}', '{safeKey}', '{safeKey}', '{safeValue}')";
                        rs.DoQuery(insertSql);
                    }
                }
                finally
                {
                    ComObjectManager.Release(rs);
                }
            }
            catch
            {
                // ignore persistence issues to avoid blocking UI
            }
        }
    }

    public class AutoRefreshPreference
    {
        public string Context { get; set; }
        public bool Enabled { get; set; }
        public int IntervalSeconds { get; set; }
    }
}
