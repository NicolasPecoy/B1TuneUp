using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using B1TuneUp.Models;
using B1TuneUp.Modules;
using B1TuneUp.Modules.ActionPadInlineDesigner;
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
        private UiCustomizationScope _defaultScope;
        private int _interactionDepth;
        private readonly ObservableCollection<ValidationRuleEntry> _validationRules = new ObservableCollection<ValidationRuleEntry>();
        private readonly ObservableCollection<ItemActionEntry> _linkedActions = new ObservableCollection<ItemActionEntry>();
        private int _linkedValidationCount;
        private int _linkedActionCount;
        private InlineDesignerScopeOption _selectedScopeOption;
        private ValidationRuleEntry _selectedValidation;
        private readonly ObservableCollection<ValidationRuleEntry> _formValidationMatrix = new ObservableCollection<ValidationRuleEntry>();
        private readonly ListCollectionView _formValidationView;
        private ValidationRuleEntry _selectedMatrixRule;
        private string _matrixSearch;
        private string _matrixEventFilter = "Todos";
        private string _matrixSeverityFilter = "Todas";
        private bool _matrixOnlyActive = true;
        private bool _matrixOnlyBlocking;
        private bool _matrixOnlyCurrentItem;
        private string _matrixUserFilter;
        private string _matrixDependencyFilter;
        private string _matrixLocalizationFilter;
        private string _matrixVariantFilter;
        private string _matrixPackageFilter;
        private int _linkedPadButtonCount;

        public InlineDesignerViewModel(InlineDesignerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = session.Items;

            _formValidationView = (ListCollectionView)CollectionViewSource.GetDefaultView(_formValidationMatrix);
            _formValidationView.Filter = FilterMatrixRule;
            _formValidationView.SortDescriptions.Add(new SortDescription(nameof(ValidationRuleEntry.EventType), ListSortDirection.Ascending));
            _formValidationView.SortDescriptions.Add(new SortDescription(nameof(ValidationRuleEntry.Sequence), ListSortDirection.Ascending));

            _defaultScope = LoadScopeDefaults();
            ScopeUserCode = _defaultScope.UserCode;
            ScopeUserGroup = _defaultScope.UserGroup;
            ScopeLocalization = _defaultScope.Localization;
            ScopeVariant = _defaultScope.Variant;
            ScopeDependsOn = _defaultScope.DependsOn;
            ScopeInheritFrom = _defaultScope.InheritFrom;
            ScopePriority = _defaultScope.Priority.ToString();

            ApplyCommand = new RelayCommand(() => Apply(false));
            PersistCommand = new RelayCommand(() => Apply(true));
            CloseCommand = new RelayCommand(window => (window as Window)?.Close());
            OpenValidationCommand = new RelayCommand(OpenValidation, () => SelectedItem != null);
            OpenActionCommand = new RelayCommand(OpenActions, () => SelectedItem != null);
            ExportCommand = new RelayCommand(ExportPackage);
            ImportCommand = new RelayCommand(ImportPackage);
            NewValidationCommand = new RelayCommand(CreateInlineValidation, () => SelectedItem != null);
            SaveValidationCommand = new RelayCommand(SaveSelectedValidation, () => SelectedValidation != null);
            PreviewValidationCommand = new RelayCommand(PreviewSelectedValidation, () => SelectedValidation != null);
            OpenPadDesignerCommand = new RelayCommand(OpenPadDesigner);

            MatrixRefreshCommand = new RelayCommand(ReloadMatrixData);
            MatrixSaveCommand = new RelayCommand(SaveSelectedMatrixRule, () => SelectedMatrixRule != null);
            MatrixToggleActiveCommand = new RelayCommand(ToggleMatrixRuleActive, () => SelectedMatrixRule != null);
            MatrixToggleBlockCommand = new RelayCommand(ToggleMatrixRuleBlocking, () => SelectedMatrixRule != null);
            MatrixLinkToItemCommand = new RelayCommand(LinkMatrixRuleToSelectedItem, () => SelectedMatrixRule != null && SelectedItem != null);
            MatrixPreviewCommand = new RelayCommand(() => PreviewValidationRule(SelectedMatrixRule), () => SelectedMatrixRule != null);
            MatrixOpenDesignerCommand = new RelayCommand(OpenMatrixInFullDesigner, () => SelectedMatrixRule != null);

            ReloadMatrixData();
            RefreshLinkedPads();

            if (!string.IsNullOrWhiteSpace(session.DefaultItemId))
            {
                SelectedItem = Items.FirstOrDefault(i => i.ItemId == session.DefaultItemId);
            }
            if (SelectedItem == null && Items.Count > 0)
            {
                SelectedItem = Items[0];
            }
            RefreshMatrixCommandStates();
        }

        public ObservableCollection<InlineDesignerItem> Items { get; }
        public double FormWidth => _session.FormWidth;
        public double FormHeight => _session.FormHeight;
        public string FormTitle => _session.FormTitle;
        public string FormType => _session.FormType;
        public InlineDesignerSession Session => _session;
        public ObservableCollection<InlineDesignerScopeOption> ScopeOptions { get; } = new ObservableCollection<InlineDesignerScopeOption>();
        public ObservableCollection<ValidationRuleEntry> ValidationRules => _validationRules;
        public ObservableCollection<ItemActionEntry> LinkedActions => _linkedActions;
        public ICollectionView FormValidationMatrixView => _formValidationView;
        public IReadOnlyList<string> MatrixEventOptions { get; } = new[]
        {
            "Todos","FORM_LOAD","ITEM_PRESSED","DATA_ADD_BEFORE","DATA_UPDATE_BEFORE","ITEM_CLICK","FORM_DATA_ADD","FORM_DATA_UPDATE","FORM_DATA_DELETION","COMBO_SELECT","EDIT_VALIDATE","MENU_CLICK","VALIDATE"
        };
        public IReadOnlyList<string> MatrixSeverityOptions { get; } = new[] { "Todas", "ERROR", "WARNING", "INFO" };
        public IReadOnlyList<string> SeverityOptions { get; } = new[] { "INFO", "WARNING", "ERROR" };
        public IReadOnlyList<string> EventOptions { get; } = new[] { "ITEM_PRESSED", "ITEM_CLICK", "FORM_DATA_ADD", "FORM_DATA_UPDATE", "FORM_DATA_DELETION", "MENU_CLICK", "VALIDATE" };

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
                RefreshMatrixCommandStates();
                RefreshMatrixView();
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

        public ValidationRuleEntry SelectedValidation
        {
            get => _selectedValidation;
            set
            {
                if (_selectedValidation == value) return;
                _selectedValidation = value;
                OnPropertyChanged();
                SaveValidationCommand.RaiseCanExecuteChanged();
                PreviewValidationCommand.RaiseCanExecuteChanged();
            }
        }

        public ValidationRuleEntry SelectedMatrixRule
        {
            get => _selectedMatrixRule;
            set
            {
                if (_selectedMatrixRule == value) return;
                _selectedMatrixRule = value;
                OnPropertyChanged();
                RefreshMatrixCommandStates();
            }
        }

        public string MatrixSearch
        {
            get => _matrixSearch;
            set
            {
                if (_matrixSearch == value) return;
                _matrixSearch = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixEventFilter
        {
            get => _matrixEventFilter;
            set
            {
                if (_matrixEventFilter == value || string.IsNullOrEmpty(value)) return;
                _matrixEventFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixSeverityFilter
        {
            get => _matrixSeverityFilter;
            set
            {
                if (_matrixSeverityFilter == value || string.IsNullOrEmpty(value)) return;
                _matrixSeverityFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public bool MatrixOnlyActive
        {
            get => _matrixOnlyActive;
            set
            {
                if (_matrixOnlyActive == value) return;
                _matrixOnlyActive = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public bool MatrixOnlyBlocking
        {
            get => _matrixOnlyBlocking;
            set
            {
                if (_matrixOnlyBlocking == value) return;
                _matrixOnlyBlocking = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public bool MatrixOnlyCurrentItem
        {
            get => _matrixOnlyCurrentItem;
            set
            {
                if (_matrixOnlyCurrentItem == value) return;
                _matrixOnlyCurrentItem = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixUserFilter
        {
            get => _matrixUserFilter;
            set
            {
                if (_matrixUserFilter == value) return;
                _matrixUserFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixDependencyFilter
        {
            get => _matrixDependencyFilter;
            set
            {
                if (_matrixDependencyFilter == value) return;
                _matrixDependencyFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixLocalizationFilter
        {
            get => _matrixLocalizationFilter;
            set
            {
                if (_matrixLocalizationFilter == value) return;
                _matrixLocalizationFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixVariantFilter
        {
            get => _matrixVariantFilter;
            set
            {
                if (_matrixVariantFilter == value) return;
                _matrixVariantFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
            }
        }

        public string MatrixPackageFilter
        {
            get => _matrixPackageFilter;
            set
            {
                if (_matrixPackageFilter == value) return;
                _matrixPackageFilter = value;
                OnPropertyChanged();
                RefreshMatrixView();
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

        public int LinkedPadButtonCount
        {
            get => _linkedPadButtonCount;
            private set
            {
                if (_linkedPadButtonCount == value) return;
                _linkedPadButtonCount = value;
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
        public RelayCommand NewValidationCommand { get; }
        public RelayCommand SaveValidationCommand { get; }
        public RelayCommand PreviewValidationCommand { get; }
        public RelayCommand MatrixRefreshCommand { get; }
        public RelayCommand MatrixSaveCommand { get; }
        public RelayCommand MatrixToggleActiveCommand { get; }
        public RelayCommand MatrixToggleBlockCommand { get; }
        public RelayCommand MatrixLinkToItemCommand { get; }
        public RelayCommand MatrixPreviewCommand { get; }
        public RelayCommand MatrixOpenDesignerCommand { get; }
        public RelayCommand OpenPadDesignerCommand { get; }

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
                StatusMessage = persist ? "DiseÃ±o guardado en BTUN_UI y aplicado." : "DiseÃ±o aplicado temporalmente.";
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
            StatusMessage = "Abriendo diseÃ±ador de validaciones...";
        }

        private void OpenActions()
        {
            ItemActionsLauncher.Show(FormType, SelectedItem?.ItemId);
            StatusMessage = "Abriendo acciones vinculadas...";
        }

        public void OpenActionEntry(ItemActionEntry entry)
        {
            if (entry == null) return;
            string formType = string.IsNullOrWhiteSpace(entry.FormType) ? FormType : entry.FormType;
            string itemId = string.IsNullOrWhiteSpace(entry.ItemId) ? SelectedItem?.ItemId : entry.ItemId;
            ItemActionsLauncher.Show(formType, itemId);
            StatusMessage = $"Abriendo Function Buttons para {itemId ?? "(sin item)"}...";
        }

        private void OpenPadDesigner()
        {
            try
            {
                ActionPadInlineDesignerManager.ShowOverlayForForm(_session.Form);
                StatusMessage = "Abriendo Action Pad inline...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo abrir Action Pad: {ex.Message}";
            }
            finally
            {
                RefreshLinkedPads();
            }
        }

        private void CreateInlineValidation()
        {
            if (SelectedItem == null) return;
            var entry = new ValidationRuleEntry
            {
                Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(),
                Name = $"VAL_{SelectedItem.ItemId}_{DateTime.Now:HHmmss}",
                FormType = FormType,
                ItemName = SelectedItem.ItemId,
                EventType = "ITEM_PRESSED",
                Severity = "ERROR",
                Active = true,
                BlockAlways = true,
                Message = $"Validación para {SelectedItem.ItemId}",
                Condition = string.Empty,
                PromptButtons = "OK"
            };
            _validationRules.Add(entry);
            _formValidationMatrix.Add(entry);
            _formValidationView.Refresh();
            SelectedMatrixRule = entry;
            SelectedValidation = entry;
            StatusMessage = "Nueva validación creada. Completa condición/mensaje y guarda.";
        }

        private void SaveSelectedValidation()
        {
            if (SelectedValidation == null) return;
            try
            {
                var saved = ValidationRuleService.Save(SelectedValidation);
                ReplaceValidationEntry(saved);
                StatusMessage = $"Validación {saved.Code} guardada.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error guardando validación: {ex.Message}";
            }
        }

        private void SaveSelectedMatrixRule()
        {
            if (SelectedMatrixRule == null) return;
            try
            {
                var saved = ValidationRuleService.Save(SelectedMatrixRule);
                ReplaceValidationEntry(saved);
                StatusMessage = $"Regla {saved.Code} guardada desde la matriz.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error guardando la regla: {ex.Message}";
            }
        }

        private void ToggleMatrixRuleActive()
        {
            if (SelectedMatrixRule == null) return;
            SelectedMatrixRule.Active = !SelectedMatrixRule.Active;
            RefreshMatrixView();
            SaveSelectedMatrixRule();
        }

        private void ToggleMatrixRuleBlocking()
        {
            if (SelectedMatrixRule == null) return;
            SelectedMatrixRule.BlockAlways = !SelectedMatrixRule.BlockAlways;
            RefreshMatrixView();
            SaveSelectedMatrixRule();
        }

        private void LinkMatrixRuleToSelectedItem()
        {
            if (SelectedMatrixRule == null || SelectedItem == null) return;
            SelectedMatrixRule.ItemName = SelectedItem.ItemId;
            RefreshMatrixView();
            SaveSelectedMatrixRule();
        }

        private void OpenMatrixInFullDesigner()
        {
            if (SelectedMatrixRule == null) return;
            ValidationDesignerLauncher.Show(SelectedMatrixRule.FormType, SelectedMatrixRule.ItemName);
            StatusMessage = $"Abriendo diseñador para {SelectedMatrixRule.Code}...";
        }

        private void ReplaceValidationEntry(ValidationRuleEntry updated)
        {
            if (updated == null) return;
            bool existsInMatrix = _formValidationMatrix.Any(v =>
                string.Equals(v.Code, updated.Code, StringComparison.OrdinalIgnoreCase));
            if (!existsInMatrix)
            {
                _formValidationMatrix.Add(updated);
            }

            _formValidationView.Refresh();
            RefreshItemValidationList();

            SelectedMatrixRule = _formValidationMatrix.FirstOrDefault(v =>
                string.Equals(v.Code, updated.Code, StringComparison.OrdinalIgnoreCase));

            var inlineSelection = _validationRules.FirstOrDefault(v =>
                string.Equals(v.Code, updated.Code, StringComparison.OrdinalIgnoreCase));
            if (inlineSelection != null)
            {
                SelectedValidation = inlineSelection;
            }
        }

        private void PreviewSelectedValidation()
            => PreviewValidationRule(SelectedValidation);

        private void PreviewValidationRule(ValidationRuleEntry rule)
        {
            if (rule == null) return;
            try
            {
                var form = _session.Form;
                bool result = string.IsNullOrWhiteSpace(rule.Condition)
                    || MacroEngine.CheckCondition(rule.Condition, form);
                if (!result)
                {
                    StatusMessage = $"La condición de {rule.Code ?? rule.Name} no se cumple; la validación no se activaría.";
                    return;
                }

                string severity = rule.Severity ?? "INFO";
                string behavior = rule.BlockAlways ? "Bloquearía" : "Avisaría";
                StatusMessage = $"{behavior} ({severity}) · {rule.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo evaluar la condición: {ex.Message}";
            }
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
            RefreshItemValidationList();
            RefreshLinkedActions();
            RefreshLinkedPads();
        }

        private void RefreshItemValidationList()
        {
            _validationRules.Clear();

            if (SelectedItem == null)
            {
                LinkedValidationCount = 0;
                SelectedValidation = null;
                return;
            }

            if (_formValidationMatrix.Count == 0)
            {
                ReloadMatrixData();
                if (_formValidationMatrix.Count == 0)
                {
                    LinkedValidationCount = 0;
                    SelectedValidation = null;
                    return;
                }
            }

            foreach (var rule in _formValidationMatrix
                         .Where(v => string.Equals(v.FormType, FormType, StringComparison.OrdinalIgnoreCase)
                             && (string.IsNullOrWhiteSpace(v.ItemName) ||
                                 string.Equals(v.ItemName, SelectedItem.ItemId, StringComparison.OrdinalIgnoreCase)))
                         .OrderBy(v => v.Sequence))
            {
                _validationRules.Add(rule);
            }

            LinkedValidationCount = _validationRules.Count;
            SelectedValidation = _validationRules.FirstOrDefault();
        }

        private void RefreshLinkedActions()
        {
            _linkedActions.Clear();

            if (SelectedItem == null)
            {
                LinkedActionCount = 0;
                return;
            }

            foreach (var action in ItemActionService.GetAll()
                         .Where(a => string.Equals(a.FormType, FormType, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(a.ItemId, SelectedItem.ItemId, StringComparison.OrdinalIgnoreCase)))
            {
                _linkedActions.Add(action.Clone());
            }
            LinkedActionCount = _linkedActions.Count;
        }

        private void RefreshLinkedPads()
        {
            try
            {
                var pad = ActionPadService.GetAll()
                    .FirstOrDefault(p => string.Equals(p.FormType, FormType, StringComparison.OrdinalIgnoreCase));
                LinkedPadButtonCount = pad?.Buttons?.Count ?? 0;
            }
            catch
            {
                LinkedPadButtonCount = 0;
            }
        }

        private void ReloadMatrixData()
        {
            try
            {
                var rules = ValidationRuleService.GetAll()
                    .Where(v => string.Equals(v.FormType, FormType, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(v => v.Sequence)
                    .ThenBy(v => v.ItemName)
                    .ToList();

                _formValidationMatrix.Clear();
                foreach (var rule in rules)
                {
                    _formValidationMatrix.Add(rule);
                }

                _formValidationView.Refresh();
                SelectedMatrixRule = _formValidationMatrix.FirstOrDefault();
                RefreshItemValidationList();
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo cargar la matriz de validaciones: {ex.Message}";
            }
        }

        private void RefreshMatrixCommandStates()
        {
            MatrixSaveCommand?.RaiseCanExecuteChanged();
            MatrixToggleActiveCommand?.RaiseCanExecuteChanged();
            MatrixToggleBlockCommand?.RaiseCanExecuteChanged();
            MatrixLinkToItemCommand?.RaiseCanExecuteChanged();
            MatrixPreviewCommand?.RaiseCanExecuteChanged();
            MatrixOpenDesignerCommand?.RaiseCanExecuteChanged();
        }

        private void RefreshMatrixView()
        {
            _formValidationView?.Refresh();
        }

        private bool FilterMatrixRule(object obj)
        {
            if (!(obj is ValidationRuleEntry entry))
            {
                return false;
            }

            if (!string.Equals(entry.FormType, FormType, StringComparison.OrdinalIgnoreCase))
                return false;

            if (MatrixOnlyActive && !entry.Active)
                return false;

            if (MatrixOnlyBlocking && !entry.BlockAlways)
                return false;

            if (MatrixOnlyCurrentItem && SelectedItem != null)
            {
                if (!string.Equals(entry.ItemName ?? string.Empty, SelectedItem.ItemId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(MatrixEventFilter) && MatrixEventFilter != "Todos"
                && !string.Equals(entry.EventType, MatrixEventFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(MatrixSeverityFilter) && MatrixSeverityFilter != "Todas"
                && !string.Equals(entry.Severity, MatrixSeverityFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(MatrixLocalizationFilter)
                && !ContainsText(entry.ScopeLocalization, MatrixLocalizationFilter.Trim()))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(MatrixVariantFilter)
                && !ContainsText(entry.ScopeVariant, MatrixVariantFilter.Trim()))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(MatrixSearch))
            {
                string term = MatrixSearch.Trim();
                if (!ContainsText(entry.Name, term) &&
                    !ContainsText(entry.ItemName, term) &&
                    !ContainsText(entry.Message, term) &&
                    !ContainsText(entry.Condition, term))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MatrixUserFilter))
            {
                string token = MatrixUserFilter.Trim();
                if (!ContainsText(entry.AppliesToUser, token) && !ContainsText(entry.AppliesToUserGroup, token))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MatrixDependencyFilter))
            {
                string token = MatrixDependencyFilter.Trim();
                if (!ContainsText(entry.ScopeDependsOn, token) &&
                    !ContainsText(entry.ScopePackages, token) &&
                    !ContainsText(entry.Notes, token))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MatrixPackageFilter))
            {
                string token = MatrixPackageFilter.Trim();
                if (!ContainsText(entry.ScopePackages, token))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsText(string source, string term)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(term)) return false;
            return source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
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
                : $"#{entry.Code} Â· {scope}";
            return new InlineDesignerScopeOption(label, entry.Clone(), false);
        }

        private static string DescribeScope(UiCustomizationEntry entry)
        {
            string user = string.IsNullOrWhiteSpace(entry.UserCode) ? "*" : entry.UserCode;
            string group = string.IsNullOrWhiteSpace(entry.UserGroup) ? "*" : entry.UserGroup;
            string locale = string.IsNullOrWhiteSpace(entry.Localization) ? "*" : entry.Localization;
            string variant = string.IsNullOrWhiteSpace(entry.Variant) ? "*" : entry.Variant;
            return $"U:{user} Â· G:{group} Â· L:{locale} Â· V:{variant}";
        }

        public override string ToString() => Display;
    }
}
