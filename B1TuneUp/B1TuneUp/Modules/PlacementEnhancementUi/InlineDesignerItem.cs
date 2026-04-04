using System.ComponentModel;
using System.Runtime.CompilerServices;
using B1TuneUp.Models;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class InlineDesignerItem : INotifyPropertyChanged
    {
        private string _itemId;
        private string _caption;
        private double _left;
        private double _top;
        private double _width;
        private double _height;
        private string _dataBind;
        private bool _isSelected;
        private string _entryCode;
        private string _scopeUserCode;
        private string _scopeUserGroup;
        private string _scopeLocalization;
        private string _scopeVariant;
        private string _scopeDependsOn;
        private string _scopeInheritFrom;
        private int _scopePriority = 10;
        private string _condition;
        private string _customLabel;
        private string _actionType = "Move";
        private bool _scopeInitialized;

        public string ItemId
        {
            get => _itemId;
            set => SetProperty(ref _itemId, value);
        }

        public string Caption
        {
            get => _caption;
            set => SetProperty(ref _caption, value);
        }

        public double Left
        {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        public double Top
        {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public string DataBind
        {
            get => _dataBind;
            set => SetProperty(ref _dataBind, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string EntryCode
        {
            get => _entryCode;
            set => SetProperty(ref _entryCode, value);
        }

        public string ScopeUserCode
        {
            get => _scopeUserCode;
            set => SetProperty(ref _scopeUserCode, value);
        }

        public string ScopeUserGroup
        {
            get => _scopeUserGroup;
            set => SetProperty(ref _scopeUserGroup, value);
        }

        public string ScopeLocalization
        {
            get => _scopeLocalization;
            set => SetProperty(ref _scopeLocalization, value);
        }

        public string ScopeVariant
        {
            get => _scopeVariant;
            set => SetProperty(ref _scopeVariant, value);
        }

        public string ScopeDependsOn
        {
            get => _scopeDependsOn;
            set => SetProperty(ref _scopeDependsOn, value);
        }

        public string ScopeInheritFrom
        {
            get => _scopeInheritFrom;
            set => SetProperty(ref _scopeInheritFrom, value);
        }

        public int ScopePriority
        {
            get => _scopePriority;
            set => SetProperty(ref _scopePriority, value <= 0 ? 10 : value);
        }

        public string Condition
        {
            get => _condition;
            set => SetProperty(ref _condition, value);
        }

        public string CustomLabel
        {
            get => _customLabel;
            set => SetProperty(ref _customLabel, value);
        }

        public string ActionType
        {
            get => _actionType;
            set => SetProperty(ref _actionType, string.IsNullOrWhiteSpace(value) ? "Move" : value);
        }

        public bool ScopeInitialized
        {
            get => _scopeInitialized;
            set => SetProperty(ref _scopeInitialized, value);
        }

        public UiCustomizationScope ToScope()
        {
            return new UiCustomizationScope
            {
                UserCode = ScopeUserCode?.Trim(),
                UserGroup = ScopeUserGroup?.Trim(),
                Localization = ScopeLocalization?.Trim(),
                Variant = ScopeVariant?.Trim(),
                DependsOn = ScopeDependsOn?.Trim(),
                InheritFrom = ScopeInheritFrom?.Trim(),
                Priority = ScopePriority
            };
        }

        public void ApplyEntry(UiCustomizationEntry entry)
        {
            if (entry == null) return;
            EntryCode = entry.Code;
            ScopeUserCode = entry.UserCode;
            ScopeUserGroup = entry.UserGroup;
            ScopeLocalization = entry.Localization;
            ScopeVariant = entry.Variant;
            ScopeDependsOn = entry.DependsOn;
            ScopeInheritFrom = entry.InheritFrom;
            ScopePriority = entry.Priority;
            Condition = entry.Condition;
            CustomLabel = entry.Label;
            ActionType = entry.Action ?? "Move";
            ScopeInitialized = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
