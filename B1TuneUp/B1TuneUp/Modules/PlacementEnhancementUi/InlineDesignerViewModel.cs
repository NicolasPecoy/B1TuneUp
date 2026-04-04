using System;
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
        private string _scopeUserCode;
        private string _scopeUserGroup;
        private string _scopeLocalization;
        private string _scopeVariant;
        private string _scopeDependsOn;
        private string _scopeInheritFrom;
        private string _scopePriority = "10";

        public InlineDesignerViewModel(InlineDesignerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = session.Items;
            if (!string.IsNullOrWhiteSpace(session.DefaultItemId))
            {
                SelectedItem = Items.FirstOrDefault(i => i.ItemId == session.DefaultItemId);
            }
            if (SelectedItem == null && Items.Count > 0)
            {
                SelectedItem = Items[0];
            }

            LoadScopeDefaults();

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

        public InlineDesignerItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                if (_selectedItem != null) _selectedItem.IsSelected = false;
                _selectedItem = value;
                if (_selectedItem != null) _selectedItem.IsSelected = true;
                OnPropertyChanged();
                OpenValidationCommand.RaiseCanExecuteChanged();
                OpenActionCommand.RaiseCanExecuteChanged();
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

        public string ScopeUserCode
        {
            get => _scopeUserCode;
            set { if (_scopeUserCode == value) return; _scopeUserCode = value; OnPropertyChanged(); }
        }

        public string ScopeUserGroup
        {
            get => _scopeUserGroup;
            set { if (_scopeUserGroup == value) return; _scopeUserGroup = value; OnPropertyChanged(); }
        }

        public string ScopeLocalization
        {
            get => _scopeLocalization;
            set { if (_scopeLocalization == value) return; _scopeLocalization = value; OnPropertyChanged(); }
        }

        public string ScopeVariant
        {
            get => _scopeVariant;
            set { if (_scopeVariant == value) return; _scopeVariant = value; OnPropertyChanged(); }
        }

        public string ScopeDependsOn
        {
            get => _scopeDependsOn;
            set { if (_scopeDependsOn == value) return; _scopeDependsOn = value; OnPropertyChanged(); }
        }

        public string ScopeInheritFrom
        {
            get => _scopeInheritFrom;
            set { if (_scopeInheritFrom == value) return; _scopeInheritFrom = value; OnPropertyChanged(); }
        }

        public string ScopePriority
        {
            get => _scopePriority;
            set { if (_scopePriority == value) return; _scopePriority = value; OnPropertyChanged(); }
        }

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
        }

        private void Apply(bool persist)
        {
            try
            {
                var scope = BuildScope();
                _session.ApplyLayout(persist, scope);
                StatusMessage = persist ? "Diseno guardado en BTUN_UI y aplicado." : "Diseno aplicado temporalmente.";
                if (persist)
                {
                    PersistScopeDefaults(scope);
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
        }

        private void OpenActions()
        {
            ItemActionsLauncher.Show(FormType, SelectedItem?.ItemId);
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

        private void LoadScopeDefaults()
        {
            ScopeUserCode = SettingsManager.GetSetting("InlineDesigner.Scope.UserCode", string.Empty);
            ScopeUserGroup = SettingsManager.GetSetting("InlineDesigner.Scope.UserGroup", string.Empty);
            ScopeLocalization = SettingsManager.GetSetting("InlineDesigner.Scope.Localization", string.Empty);
            ScopeVariant = SettingsManager.GetSetting("InlineDesigner.Scope.Variant", string.Empty);
            ScopeDependsOn = SettingsManager.GetSetting("InlineDesigner.Scope.DependsOn", string.Empty);
            ScopeInheritFrom = SettingsManager.GetSetting("InlineDesigner.Scope.InheritFrom", string.Empty);
            ScopePriority = SettingsManager.GetSetting("InlineDesigner.Scope.Priority", "10");
        }

        private void PersistScopeDefaults(UiCustomizationScope scope)
        {
            SettingsManager.SetSetting("InlineDesigner.Scope.UserCode", scope.UserCode ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.UserGroup", scope.UserGroup ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.Localization", scope.Localization ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.Variant", scope.Variant ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.DependsOn", scope.DependsOn ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.InheritFrom", scope.InheritFrom ?? string.Empty);
            SettingsManager.SetSetting("InlineDesigner.Scope.Priority", scope.Priority.ToString());
        }

        private UiCustomizationScope BuildScope()
        {
            return new UiCustomizationScope
            {
                UserCode = ScopeUserCode?.Trim(),
                UserGroup = ScopeUserGroup?.Trim(),
                Localization = ScopeLocalization?.Trim(),
                Variant = ScopeVariant?.Trim(),
                DependsOn = ScopeDependsOn?.Trim(),
                InheritFrom = ScopeInheritFrom?.Trim(),
                Priority = ParsePriority()
            };
        }

        private int ParsePriority()
        {
            return int.TryParse(ScopePriority, out var value) && value > 0 ? value : 10;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
