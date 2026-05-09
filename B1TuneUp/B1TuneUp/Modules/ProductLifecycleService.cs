using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class ProductLifecycleService
    {
        private const string LicenseKeyCode = "PRODUCT_LICENSE_KEY";
        private const string OwnerSigningSecretCode = "PRODUCT_LICENSE_OWNER_SECRET";
        private const string TrialStartCode = "PRODUCT_TRIAL_START";
        private const string MinimumSapVersion = "10.0";
        private const string LicensePrefix = "B1TL1";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static ProductLifecycleInfo GetInfo()
        {
            EnsureTrialStarted();
            string trialStartRaw = ToolboxSettingService.GetByCode(TrialStartCode)?.Value;
            DateTime trialStart;
            if (!DateTime.TryParse(trialStartRaw, out trialStart)) trialStart = DateTime.Today;
            DateTime trialEnd = trialStart.AddDays(30);
            string licenseKey = ToolboxSettingService.GetByCode(LicenseKeyCode)?.Value ?? string.Empty;
            ProductLicensePayload payload;
            string validationDetail;
            string licenseStatus = ValidateLicense(licenseKey, trialEnd, out payload, out validationDetail);
            string sapVersion = SafeSapVersion();
            string compatibility = IsCompatibleSap(sapVersion) ? "OK" : "Warning";

            return new ProductLifecycleInfo
            {
                InstalledVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0",
                MetadataVersion = ToolboxSettingService.GetByCode(ConfigurationCenterService.MetadataVersionCode)?.Value ?? string.Empty,
                SapVersion = sapVersion,
                CompatibilityStatus = compatibility,
                LicenseStatus = licenseStatus,
                LicenseEdition = payload?.Edition ?? "Trial",
                LicenseExpiresOn = payload?.ExpiresOn ?? trialEnd.ToString("yyyy-MM-dd"),
                LicensedCustomer = payload?.Customer ?? SafeCompanyName(),
                CompanyDb = SafeCompanyDb(),
                InstallationNumber = SafeInstallationNumber(),
                LicenseKey = MaskLicense(licenseKey),
                TrialExpiresOn = trialEnd.ToString("yyyy-MM-dd"),
                Detail = validationDetail
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

        public static string GenerateOwnerPremiumLicense(int months = 120, string customer = null)
        {
            var payload = new ProductLicensePayload
            {
                LicenseId = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                Customer = string.IsNullOrWhiteSpace(customer) ? SafeCompanyName() : customer,
                Edition = "Premium",
                CompanyDb = SafeCompanyDb(),
                InstallationNumber = SafeInstallationNumber(),
                HardwareKey = SafeHardwareKey(),
                IssuedOn = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ExpiresOn = DateTime.UtcNow.AddMonths(months <= 0 ? 120 : months).ToString("yyyy-MM-dd"),
                Modules = ModuleActivationService.GetAll().Select(m => m.Key).OrderBy(k => k).ToList(),
                MaxUsers = 999,
                Notes = "Owner premium license generated locally."
            };
            string token = Sign(payload);
            SaveLicense(token);
            AuditLogManager.LogAction("ProductLifecycle", $"Licencia owner premium generada para {payload.Customer}.", "License");
            return token;
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

        private static string ValidateLicense(string key, DateTime trialEnd, out ProductLicensePayload payload, out string detail)
        {
            payload = null;
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (TryReadSignedLicense(key, out payload, out detail))
                {
                    DateTime expires;
                    if (DateTime.TryParse(payload.ExpiresOn, out expires) && DateTime.Today > expires.Date)
                    {
                        detail = $"Licencia expirada el {expires:yyyy-MM-dd}.";
                        return "Expired";
                    }
                    if (!MatchesCurrentCompany(payload))
                    {
                        detail = "Licencia firmada valida, pero no corresponde a esta compania/instalacion.";
                        return "InvalidScope";
                    }
                    detail = $"{payload.Edition} valida para {payload.Customer} hasta {payload.ExpiresOn}.";
                    return string.Equals(payload.Edition, "Premium", StringComparison.OrdinalIgnoreCase) ? "LicensedPremium" : "Licensed";
                }
                return "Invalid";
            }

            detail = DateTime.Today <= trialEnd ? $"Trial hasta {trialEnd:yyyy-MM-dd}." : $"Trial expirado el {trialEnd:yyyy-MM-dd}.";
            return DateTime.Today <= trialEnd ? "Trial" : "Expired";
        }

        private static string Sign(ProductLicensePayload payload)
        {
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            string encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
            string signature = Base64UrlEncode(SignBytes(encodedPayload));
            return $"{LicensePrefix}.{encodedPayload}.{signature}";
        }

        private static bool TryReadSignedLicense(string token, out ProductLicensePayload payload, out string detail)
        {
            payload = null;
            detail = string.Empty;
            try
            {
                string[] parts = (token ?? string.Empty).Trim().Split('.');
                if (parts.Length != 3 || !string.Equals(parts[0], LicensePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    detail = "Formato de licencia invalido.";
                    return false;
                }
                string expected = Base64UrlEncode(SignBytes(parts[1]));
                if (!FixedTimeEquals(expected, parts[2]))
                {
                    detail = "Firma de licencia invalida.";
                    return false;
                }
                string json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                payload = JsonSerializer.Deserialize<ProductLicensePayload>(json, JsonOptions);
                if (payload == null || !string.Equals(payload.Product, "B1TuneUp", StringComparison.OrdinalIgnoreCase))
                {
                    detail = "Payload de licencia invalido.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                detail = ex.Message;
                return false;
            }
        }

        private static byte[] SignBytes(string encodedPayload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(GetOwnerSigningSecret())))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPayload));
            }
        }

        private static string GetOwnerSigningSecret()
        {
            string env = Environment.GetEnvironmentVariable("B1TUNEUP_LICENSE_SECRET");
            if (!string.IsNullOrWhiteSpace(env)) return env;

            var setting = ToolboxSettingService.GetByCode(OwnerSigningSecretCode);
            if (!string.IsNullOrWhiteSpace(setting?.Value)) return setting.Value;

            string secret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = OwnerSigningSecretCode,
                Category = "Product",
                Description = "Owner license signing secret",
                Value = secret
            });
            return secret;
        }

        private static bool MatchesCurrentCompany(ProductLicensePayload payload)
        {
            if (payload == null) return false;
            return MatchesOptional(payload.CompanyDb, SafeCompanyDb())
                   && MatchesOptional(payload.InstallationNumber, SafeInstallationNumber())
                   && MatchesOptional(payload.HardwareKey, SafeHardwareKey());
        }

        private static bool MatchesOptional(string licensedValue, string actualValue)
        {
            return string.IsNullOrWhiteSpace(licensedValue)
                   || string.Equals(licensedValue, actualValue, StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeSapVersion()
        {
            try { return B1App.Instance.Company.Version.ToString(); }
            catch { return string.Empty; }
        }

        private static string SafeCompanyName()
        {
            try { return B1App.Instance.Company.CompanyName ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string SafeCompanyDb()
        {
            try { return B1App.Instance.Company.CompanyDB ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string SafeInstallationNumber()
        {
            try
            {
                var company = B1App.Instance.Company;
                var type = company.GetType();
                foreach (var name in new[] { "InstallationId", "InstallationID", "InstallNumber", "InstallationNumber" })
                {
                    var prop = type.GetProperty(name);
                    if (prop != null) return prop.GetValue(company, null)?.ToString() ?? string.Empty;
                }
            }
            catch { return string.Empty; }
            return string.Empty;
        }

        private static string SafeHardwareKey()
        {
            return $"{SafeCompanyDb()}|{SafeInstallationNumber()}|{SafeSapVersion()}";
        }

        private static bool IsCompatibleSap(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return false;
            return version.IndexOf(MinimumSapVersion, StringComparison.OrdinalIgnoreCase) >= 0 || version.StartsWith("10", StringComparison.OrdinalIgnoreCase);
        }

        private static string MaskLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return key.Length <= 18 ? "****************" : key.Substring(0, 8) + "..." + key.Substring(key.Length - 8);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            byte[] a = Encoding.UTF8.GetBytes(left ?? string.Empty);
            byte[] b = Encoding.UTF8.GetBytes(right ?? string.Empty);
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
