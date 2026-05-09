using System;
using System.Reflection;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class ProductLifecycleService
    {
        private const string LicenseKeyCode = "PRODUCT_LICENSE_KEY";
        private const string TrialStartCode = "PRODUCT_TRIAL_START";
        private const string MinimumSapVersion = "10.0";

        public static ProductLifecycleInfo GetInfo()
        {
            EnsureTrialStarted();
            string trialStartRaw = ToolboxSettingService.GetByCode(TrialStartCode)?.Value;
            DateTime trialStart;
            if (!DateTime.TryParse(trialStartRaw, out trialStart)) trialStart = DateTime.Today;
            DateTime trialEnd = trialStart.AddDays(30);
            string licenseKey = ToolboxSettingService.GetByCode(LicenseKeyCode)?.Value ?? string.Empty;
            string licenseStatus = ValidateLicense(licenseKey, trialEnd);
            string sapVersion = SafeSapVersion();
            string compatibility = IsCompatibleSap(sapVersion) ? "OK" : "Warning";

            return new ProductLifecycleInfo
            {
                InstalledVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0",
                MetadataVersion = ToolboxSettingService.GetByCode(ConfigurationCenterService.MetadataVersionCode)?.Value ?? string.Empty,
                SapVersion = sapVersion,
                CompatibilityStatus = compatibility,
                LicenseStatus = licenseStatus,
                LicenseKey = MaskLicense(licenseKey),
                TrialExpiresOn = trialEnd.ToString("yyyy-MM-dd"),
                Detail = licenseStatus == "Licensed" ? "Licencia valida." : $"Trial hasta {trialEnd:yyyy-MM-dd}."
            };
        }

        public static void SaveLicense(string licenseKey)
        {
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = LicenseKeyCode,
                Category = "Product",
                Description = "B1TuneUp license key",
                Value = licenseKey ?? string.Empty
            });
            AuditLogManager.LogAction("ProductLifecycle", "Licencia actualizada.", "License");
        }

        public static void RunGuidedUpgrade()
        {
            OperationalDiagnosticsService.Measure("Upgrade", "MetadataRepair", ConfigurationCenterService.RepairMetadata);
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = "PRODUCT_LAST_UPGRADE",
                Category = "Product",
                Description = "Last guided upgrade",
                Value = DateTime.UtcNow.ToString("O")
            });
            AuditLogManager.LogAction("ProductLifecycle", "Upgrade guiado ejecutado.", "Upgrade");
        }

        public static string GetFunctionalDocumentation()
        {
            return "B1TuneUp Config Center: Modules, Metadata, Universal Functions, Event Triggers, Authorization, Support, Lifecycle and Samples.";
        }

        private static void EnsureTrialStarted()
        {
            if (ToolboxSettingService.GetByCode(TrialStartCode) != null) return;
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = TrialStartCode,
                Category = "Product",
                Description = "Trial start date",
                Value = DateTime.Today.ToString("yyyy-MM-dd")
            });
        }

        private static string ValidateLicense(string key, DateTime trialEnd)
        {
            if (!string.IsNullOrWhiteSpace(key) && key.Trim().StartsWith("B1TU-", StringComparison.OrdinalIgnoreCase)) return "Licensed";
            return DateTime.Today <= trialEnd ? "Trial" : "Expired";
        }

        private static string SafeSapVersion()
        {
            try { return B1App.Instance.Company.Version.ToString(); }
            catch { return string.Empty; }
        }

        private static bool IsCompatibleSap(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return false;
            return version.IndexOf(MinimumSapVersion, StringComparison.OrdinalIgnoreCase) >= 0 || version.StartsWith("10", StringComparison.OrdinalIgnoreCase);
        }

        private static string MaskLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return key.Length <= 8 ? "********" : key.Substring(0, 4) + "..." + key.Substring(key.Length - 4);
        }
    }
}
