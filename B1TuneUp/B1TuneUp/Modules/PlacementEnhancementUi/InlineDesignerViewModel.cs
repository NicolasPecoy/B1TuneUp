using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules.ItemActionsUi;
using B1TuneUp.Modules.ValidationUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class InlineDesignerViewModel : INotifyPropertyChanged
    {
        private readonly InlineDesignerSession _session;
        private InlineDesignerItem _selectedItem;
        private bool _snapToGrid = true;
        private double _gridSize = 5;
        private string _statusMessage;
        private UiCustomizationScope _defaultScope;
        private int _interactionDepth;
        private IList<ValidationRuleEntry> _validationCache;
        private IList<ItemActionEntry> _actionCache;
        private int _linkedValidationCount;
        private int _linkedActionCount;
        private InlineDesignerScopeOption _selectedScopeOption;

        public InlineDesignerViewModel(InlineDesignerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = session.Items;
            _defaultScope = LoadScopeDefaults();
            if (!string.IsNullOrWhiteSpace(session.DefaultItemId))
            {
                SelectedItem = Items.FirstOrDefault(i => i.ItemId == session.DefaultItemId);
            }
            if (SelectedItem == null && Items.Count > 0)
            {
                SelectedItem = Items[0];
            }

            ApplyCommand = new RelayCommand(_ => Apply(false));
            PersistCommand = new RelayCommand(_ => Apply(true));
            CloseCommand = new RelayCommand(window => (window as Window)?.Close());
            OpenValidationCommand = new RelayCommand(_ => OpenValidation(), _ => SelectedItem != null);
            OpenActionCommand = new RelayCommand(_ => OpenActions(), _ => SelectedItem != null);
            ExportCommand = new RelayCommand(_ => ExportPackage());
            ImportCommand = new RelayCommand(_ => ImportPackage());
        }

        public ObservableCollection<InlineDesignerItem> Items { get; }
        public double FormWidth => _session.FormWidth;
        public double FormHeight => _session.FormHeight;
        public string FormTitle => _session.FormTitle;
        public string FormType => _session.FormType;
        public InlineDesignerSession Session => _session;
        public ObservableCollection<InlineDesignerScopeOption> ScopeOptions { get; } = new ObservableCollection<InlineDesignerScopeOption>();

        public InlineDesignerItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                if (_selectedItem != null) _selectedItem.IsSelected = false;
                _selectedItem = value;
                if (_selectedItem != null)
                {
                    _selectedItem.IsSelected = true;
                    EnsureScopeInitialized(_selectedItem);
                }
                UpdateScopeOptions();
                RefreshLinkedArtifacts();
                OnPropertyChanged();
                OpenValidationCommand.RaiseCanExecuteChanged();
                OpenActionCommand.RaiseCanExecuteChanged();
            }
        }

        public InlineDesignerScopeOption SelectedScopeOption
        {
            get => _selectedScopeOption;
            set
            {
                if (_selectedScopeOption == value) return;
                _selectedScopeOption = value;
                OnPropertyChanged();
                ApplyScopeOption(value);
            }
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set
            {
                if (_snapToGrid == value) return;
                _snapToGrid = value;
                OnPropertyChanged();
            }
        }

        public double GridSize
        {
            get => _gridSize;
            set
            {
                if (Math.Abs(_gridSize - value) < 0.1) return;
                _gridSize = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int LinkedValidationCount
        {
            get => _linkedValidationCount;
            private set
            {
                if (_linkedValidationCount == value) return;
                _linkedValidationCount = value;
                OnPropertyChanged();
            }
        }

        public int LinkedActionCount
        {
            get => _linkedActionCount;
            private set
            {
                if (_linkedActionCount == value) return;
                _linkedActionCount = value;
                OnPropertyChanged();
            }
        }

        public bool IsUserInteracting => _interactionDepth > 0;

        public RelayCommand ApplyCommand { get; }
        public RelayCommand PersistCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand OpenValidationCommand { get; }
        public RelayCommand OpenActionCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand ImportCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void MoveItem(InlineDesignerItem item, double deltaX, double deltaY)
        {
            if (item == null) return;
            double nx = item.Left + deltaX;
            double ny = item.Top + deltaY;
            if (SnapToGrid)
            {
                nx = Math.Round(nx / GridSize) * GridSize;
                ny = Math.Round(ny / GridSize) * GridSize;
            }
            item.Left = Math.Max(0, nx);
            item.Top = Math.Max(0, ny);
            _session.PushLiveItem(item);
        }

        public void ResizeItem(InlineDesignerItem item, double deltaX, double deltaY)
        {
            if (item == null) return;
            double nw = Math.Max(10, item.Width + deltaX);
            double nh = Math.Max(6, item.Height + deltaY);
            if (SnapToGrid)
            {
                nw = Math.Round(nw / GridSize) * GridSize;
                nh = Math.Round(nh / GridSize) * GridSize;
            }
            item.Width = nw;
            item.Height = nh;
            _session.PushLiveItem(item);
        }

        public void BeginInteraction() => _interactionDepth++;

        public void EndInteraction()
        {
            if (_interactionDepth > 0) _interactionDepth--;
        }

        private void Apply(bool persist)
        {
            try
            {
                _session.ApplyLayout(persist);
                StatusMessage = persist ? "Diseño guardado en BTUN_UI y aplicado." : "Diseño aplicado temporalmente.";
                if (persist)
                {
                    PersistScopeDefaults(SelectedItem);
                    UpdateScopeOptions();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void OpenValidation()
        {
            ValidationDesignerLauncher.Show(FormType, SelectedItem?.ItemId);
            StatusMessage = "Abriendo diseñador de validaciones...";
        }

        private void OpenActions()
        {
            ItemActionsLauncher.Show(FormType, SelectedItem?.ItemId);
            StatusMessage = "Abriendo acciones vinculadas...";
        }

        private void ExportPackage()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "B1TuneUp UI Package (*.b1pkg)|*.b1pkg|JSON (*.json)|*.json",
                FileName = $"{FormType}_UiPackage.b1pkg"
            };
            if (dialog.ShowDialog() == true)
            {
                UICustomizerService.ExportPackage(FormType, dialog.FileName);
                StatusMessage = $"Paquete exportado: {Path.GetFileName(dialog.FileName)}";
            }
        }

        private void ImportPackage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "B1TuneUp UI Package (*.b1pkg;*.json)|*.b1pkg;*.json",
                Title = "Importar paquete UI"
            };
            if (dialog.ShowDialog() == true)
            {
                UICustomizerService.ImportPackage(dialog.FileName);
                StatusMessage = $"Paquete importado desde {Path.GetFileName(dialog.FileName)}";
            }
        }

        private UiCustomizationScope LoadScopeDefaults()
        {
            return new UiCustomizationScope
            {
                UserCode = SettingsManager.GetSetting("InlineDesigner.Scope.UserCode", string.Empty),
                UserGroup = SettingsManager.GetSetting("InlineDesigner.Scope.UserGroup", string.Empty),
                Localization = SettingsManager.GetSetting("InlineDesigner.Scope.Localization", string.Empty),
                Variant = SettingsManager.GetSetting("InlineDesigner.Scope.Variant", string.Empty),
                DependsOn = SettingsManager.GetSetting("InlineDesigner.Scope.DependsOn", string.Empty),
                InheritFrom = SettingsManager.GetSetting("InlineDesigner.Scope.InheritFrom", string.Empty),
                Priority = ParsePriority(SettingsManager.GetSetting("InlineDesigner.Scope.Priority", "10"))
            };
        }

        private void PersistScopeDefaults(InlineDesignerItem item)
        {
            if (item == null) return;
            SettingsManager.SetSetting("InlineDesigner.Scope.UserCode", item.ScopeUserCode ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.UserGroup", item.ScopeUserGroup ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.Localization", item.ScopeLocalization ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.Variant", item.ScopeVariant ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.DependsOn", item.ScopeDependsOn ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.InheritFrom", item.ScopeInheritFrom ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.Priority", item.ScopePriority.ToString());
            _defaultScope = item.ToScope();
        }

        private static int ParsePriority(string raw)
            => int.TryParse(raw, out var value) && value > 0 ? value : 10;

        private void EnsureScopeInitialized(InlineDesignerItem item)
        {
            if (item == null || item.ScopeInitialized) return;
            item.ScopeUserCode = _defaultScope.UserCode;
            item.ScopeUserGroup = _defaultScope.UserGroup;
            item.ScopeLocalization = _defaultScope.Localization;
            item.ScopeVariant = _defaultScope.Variant;
            item.ScopeDependsOn = _defaultScope.DependsOn;
            item.ScopeInheritFrom = _defaultScope.InheritFrom;
            item.ScopePriority = _defaultScope.Priority <= 0 ? 10 : _defaultScope.Priority;
            item.ScopeInitialized = true;
        }

        private void ApplyScopeOption(InlineDesignerScopeOption option)
        {
            if (option == null || SelectedItem == null) return;
            if (option.IsNew)
            {
                SelectedItem.EntryCode = null;
                SelectedItem.ScopeUserCode = _defaultScope.UserCode;
                SelectedItem.ScopeUserGroup = _defaultScope.UserGroup;
                SelectedItem.ScopeLocalization = _defaultScope.Localization;
                SelectedItem.ScopeVariant = _defaultScope.Variant;
                SelectedItem.ScopeDependsOn = _defaultScope.DependsOn;
                SelectedItem.ScopeInheritFrom = _defaultScope.InheritFrom;
                SelectedItem.ScopePriority = _defaultScope.Priority <= 0 ? 10 : _defaultScope.Priority;
                SelectedItem.ScopeInitialized = true;
            }
            else if (option.Entry != null)
            {
                SelectedItem.EntryCode = option.Entry.Code;
                SelectedItem.ScopeUserCode = option.Entry.UserCode;
                SelectedItem.ScopeUserGroup = option.Entry.UserGroup;
                SelectedItem.ScopeLocalization = option.Entry.Localization;
                SelectedItem.ScopeVariant = option.Entry.Variant;
                SelectedItem.ScopeDependsOn = option.Entry.DependsOn;
                SelectedItem.ScopeInheritFrom = option.Entry.InheritFrom;
                SelectedItem.ScopePriority = option.Entry.Priority <= 0 ? 10 : option.Entry.Priority;
                SelectedItem.CustomLabel = option.Entry.Label;
                SelectedItem.ActionType = option.Entry.Action ?? SelectedItem.ActionType;
                SelectedItem.Condition = option.Entry.Condition;
                SelectedItem.ScopeInitialized = true;
            }
        }

        private void UpdateScopeOptions()
        {
            ScopeOptions.Clear();
            if (SelectedItem == null)
            {
                SelectedScopeOption = null;
                return;
            }

            ScopeOptions.Add(InlineDesignerScopeOption.CreateNew());
            var entries = _session.GetEntriesForItem(SelectedItem.ItemId);
            foreach (var entry in entries)
            {
                ScopeOptions.Add(InlineDesignerScopeOption.FromEntry(entry));
            }

            var match = ScopeOptions.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.Code) &&
                string.Equals(o.Code, SelectedItem.EntryCode, StringComparison.OrdinalIgnoreCase));
            SelectedScopeOption = match ?? ScopeOptions.FirstOrDefault();
        }

        private void RefreshLinkedArtifacts()
        {
            if (SelectedItem == null)
            {
                LinkedValidationCount = 0;
                LinkedActionCount = 0;
                return;
            }

            if (_validationCache == null)
            {
                _validationCache = ValidationRuleService.GetAll();
            }

            if (_actionCache == null)
            {
                _actionCache = ItemActionService.GetAll();
            }

            LinkedValidationCount = _validationCache.Count(v =>
                string.Equals(v.FormType, FormType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(v.ItemName, SelectedItem.ItemId, StringComparison.OrdinalIgnoreCase));

            LinkedActionCount = _actionCache.Count(a =>
                string.Equals(a.FormType, FormType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.ItemId, SelectedItem.ItemId, StringComparison.OrdinalIgnoreCase));
        }

        public void NotifySurfaceChanged()
        {
            OnPropertyChanged(nameof(FormWidth));
            OnPropertyChanged(nameof(FormHeight));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class InlineDesignerScopeOption
    {
        private InlineDesignerScopeOption(string display, UiCustomizationEntry entry, bool isNew)
        {
            Display = display;
            Entry = entry;
            Code = entry?.Code;
            IsNew = isNew;
        }

        public string Display { get; }
        public string Code { get; }
        public bool IsNew { get; }
        public UiCustomizationEntry Entry { get; }

        public static InlineDesignerScopeOption CreateNew()
            => new InlineDesignerScopeOption("Nuevo alcance (hereda defaults)", null, true);

        public static InlineDesignerScopeOption FromEntry(UiCustomizationEntry entry)
        {
            string scope = DescribeScope(entry);
            string label = string.IsNullOrWhiteSpace(entry.Code)
                ? $"Alcance {scope}"
                : $"#{entry.Code} · {scope}";
            return new InlineDesignerScopeOption(label, entry.Clone(), false);
        }

        private static string DescribeScope(UiCustomizationEntry entry)
        {
            string user = string.IsNullOrWhiteSpace(entry.UserCode) ? "*" : entry.UserCode;
            string group = string.IsNullOrWhiteSpace(entry.UserGroup) ? "*" : entry.UserGroup;
            string locale = string.IsNullOrWhiteSpace(entry.Localization) ? "*" : entry.Localization;
            string variant = string.IsNullOrWhiteSpace(entry.Variant) ? "*" : entry.Variant;
            return $"U:{user} · G:{group} · L:{locale} · V:{variant}";
        }

        public override string ToString() => Display;
    }
}
