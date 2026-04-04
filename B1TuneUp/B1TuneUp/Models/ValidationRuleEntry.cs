using System;
using System.Linq;

namespace B1TuneUp.Models
{
    public class ValidationRuleEntry
    {
        private ValidationScopeMetadata _metadata = new ValidationScopeMetadata();

        public string Code { get; set; }
        public string Name { get; set; }
        public string FormType { get; set; }
        public string ItemName { get; set; }
        public string EventType { get; set; }
        public string Condition { get; set; }
        public string Action { get; set; }
        public string Severity { get; set; } = "ERROR";
        public bool Active { get; set; } = true;
        public string AppliesToUser { get; set; }
        public string AppliesToUserGroup { get; set; }
        public string Message { get; set; }
        public bool BlockAlways { get; set; } = true;
        public int Sequence { get; set; } = 10;
        public string PromptButtons { get; set; }
        public string Notes
        {
            get => _metadata?.Note;
            set
            {
                EnsureMetadata();
                _metadata.Note = value;
            }
        }
        public string ScopeLocalization
        {
            get => _metadata?.Localization;
            set
            {
                EnsureMetadata();
                _metadata.Localization = value;
            }
        }
        public string ScopeVariant
        {
            get => _metadata?.Variant;
            set
            {
                EnsureMetadata();
                _metadata.Variant = value;
            }
        }
        public string ScopeDependsOn
        {
            get => _metadata?.DependsOn;
            set
            {
                EnsureMetadata();
                _metadata.DependsOn = value;
            }
        }
        public string ScopeInheritFrom
        {
            get => _metadata?.InheritFrom;
            set
            {
                EnsureMetadata();
                _metadata.InheritFrom = value;
            }
        }
        public string ScopePackages
        {
            get => _metadata == null ? string.Empty : _metadata.PackagesToRaw();
            set
            {
                EnsureMetadata();
                _metadata.UpdatePackagesFromRaw(value);
            }
        }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ValidationRuleEntry Clone()
        {
            var clone = (ValidationRuleEntry)MemberwiseClone();
            clone._metadata = _metadata?.Clone() ?? new ValidationScopeMetadata();
            return clone;
        }

        public string ScopeSummary
        {
            get
            {
                string users = string.IsNullOrWhiteSpace(AppliesToUser) ? "*" : AppliesToUser;
                string groups = string.IsNullOrWhiteSpace(AppliesToUserGroup) ? "*" : AppliesToUserGroup;
                string locale = string.IsNullOrWhiteSpace(ScopeLocalization) ? "*" : ScopeLocalization;
                string variant = string.IsNullOrWhiteSpace(ScopeVariant) ? "*" : ScopeVariant;
                return $"U:{users} · G:{groups} · L:{locale} · V:{variant}";
            }
        }

        public string DependencySummary
        {
            get
            {
                string depends = string.IsNullOrWhiteSpace(ScopeDependsOn) ? "-" : ScopeDependsOn;
                string inherit = string.IsNullOrWhiteSpace(ScopeInheritFrom) ? "-" : ScopeInheritFrom;
                string packages = string.IsNullOrWhiteSpace(ScopePackages) ? "-" : ScopePackages;
                return $"Dep:{depends} · Inherit:{inherit} · Pack:{packages}";
            }
        }

        internal void LoadMetadata(string rawNotes)
        {
            _metadata = ValidationScopeMetadata.Parse(rawNotes);
        }

        internal string SerializeMetadata()
        {
            return _metadata?.Serialize() ?? string.Empty;
        }

        internal string[] GetPackageTokens()
        {
            return _metadata?.Packages ?? Array.Empty<string>();
        }

        private void EnsureMetadata()
        {
            if (_metadata == null)
            {
                _metadata = new ValidationScopeMetadata();
            }
        }
    }
}
