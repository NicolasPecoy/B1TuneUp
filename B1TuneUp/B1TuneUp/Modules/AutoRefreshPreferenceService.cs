using System;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class AutoRefreshPreferenceService
    {
        private const string KeyPrefix = "AutoRefresh:";

        public static AutoRefreshPreference Load(string context)
        {
            if (string.IsNullOrWhiteSpace(context)) context = "default";
            var enabled = SettingsManager.GetSetting(BuildKey(context, "enabled"), "true");
            var intervalRaw = SettingsManager.GetSetting(BuildKey(context, "interval"), "60");
            int interval = 60;
            int.TryParse(intervalRaw, out interval);
            interval = Math.Max(5, interval);

            return new AutoRefreshPreference
            {
                Context = context,
                Enabled = !string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase),
                IntervalSeconds = interval
            };
        }

        public static void Save(string context, bool enabled, int intervalSeconds)
        {
            if (string.IsNullOrWhiteSpace(context)) context = "default";
            SettingsManager.SetSetting(BuildKey(context, "enabled"), enabled ? "true" : "false");
            SettingsManager.SetSetting(BuildKey(context, "interval"), Math.Max(5, intervalSeconds).ToString());
        }

        private static string BuildKey(string context, string suffix) => $"{KeyPrefix}{context}:{suffix}";
    }

    public class AutoRefreshPreference
    {
        public string Context { get; set; }
        public bool Enabled { get; set; }
        public int IntervalSeconds { get; set; }
    }
}
