using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace B1TuneUp.Models
{
    public class ValidationScopeMetadata
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public string Note { get; set; }
        public string Localization { get; set; }
        public string Variant { get; set; }
        public string DependsOn { get; set; }
        public string InheritFrom { get; set; }
        public string[] Packages { get; set; } = Array.Empty<string>();
        public string ExcludedUsers { get; set; }
        public string ExcludedUserGroups { get; set; }

        private const string Separator = "\n---B1TUNEUP::META---\n";

        public static ValidationScopeMetadata Parse(string raw)
        {
            var metadata = new ValidationScopeMetadata();
            if (string.IsNullOrWhiteSpace(raw))
                return metadata;

            string payload = null;
            if (raw?.IndexOf(Separator, StringComparison.Ordinal) >= 0)
            {
                var parts = raw.Split(new[] { Separator }, 2, StringSplitOptions.None);
                metadata.Note = parts[0];
                payload = parts.Length > 1 ? parts[1] : null;
            }
            else if (raw.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                payload = raw;
            }
            else
            {
                metadata.Note = raw;
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<ValidationScopeMetadata>(payload, SerializerOptions);
                    if (parsed != null)
                    {
                        metadata.Localization = parsed.Localization;
                        metadata.Variant = parsed.Variant;
                        metadata.DependsOn = parsed.DependsOn;
                        metadata.InheritFrom = parsed.InheritFrom;
                        metadata.Packages = parsed.Packages ?? Array.Empty<string>();
                        metadata.ExcludedUsers = parsed.ExcludedUsers;
                        metadata.ExcludedUserGroups = parsed.ExcludedUserGroups;
                    }
                }
                catch
                {
                    // ignore invalid payloads
                }
            }

            return metadata;
        }

        public string Serialize()
        {
            bool hasMetadata =
                !string.IsNullOrWhiteSpace(Localization) ||
                !string.IsNullOrWhiteSpace(Variant) ||
                !string.IsNullOrWhiteSpace(DependsOn) ||
                !string.IsNullOrWhiteSpace(InheritFrom) ||
                !string.IsNullOrWhiteSpace(ExcludedUsers) ||
                !string.IsNullOrWhiteSpace(ExcludedUserGroups) ||
                (Packages != null && Packages.Any(p => !string.IsNullOrWhiteSpace(p)));

            if (!hasMetadata)
                return Note ?? string.Empty;

            var payload = new ValidationScopeMetadata
            {
                Localization = string.IsNullOrWhiteSpace(Localization) ? null : Localization,
                Variant = string.IsNullOrWhiteSpace(Variant) ? null : Variant,
                DependsOn = string.IsNullOrWhiteSpace(DependsOn) ? null : DependsOn,
                InheritFrom = string.IsNullOrWhiteSpace(InheritFrom) ? null : InheritFrom,
                ExcludedUsers = string.IsNullOrWhiteSpace(ExcludedUsers) ? null : ExcludedUsers,
                ExcludedUserGroups = string.IsNullOrWhiteSpace(ExcludedUserGroups) ? null : ExcludedUserGroups,
                Packages = (Packages ?? Array.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray()
            };

            string json = JsonSerializer.Serialize(payload, SerializerOptions);
            if (string.IsNullOrWhiteSpace(Note))
                return $"{Separator}{json}";
            return $"{Note}{Separator}{json}";
        }

        public ValidationScopeMetadata Clone()
        {
            return new ValidationScopeMetadata
            {
                Note = Note,
                Localization = Localization,
                Variant = Variant,
                DependsOn = DependsOn,
                InheritFrom = InheritFrom,
                ExcludedUsers = ExcludedUsers,
                ExcludedUserGroups = ExcludedUserGroups,
                Packages = Packages?.ToArray() ?? Array.Empty<string>()
            };
        }

        public void UpdatePackagesFromRaw(string raw)
        {
            if (raw == null)
            {
                Packages = Array.Empty<string>();
                return;
            }
            var tokens = raw
                .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Packages = tokens;
        }

        public string PackagesToRaw()
        {
            return Packages == null ? string.Empty : string.Join(", ", Packages.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}
