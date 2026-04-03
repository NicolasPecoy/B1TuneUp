using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.FormEnhancementUi
{
    public class FormEnhancementViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<FormSettingEntry> _formSettings = new ObservableCollection<FormSettingEntry>();
        private readonly ObservableCollection<DefaultValueEntry> _defaultValues = new ObservableCollection<DefaultValueEntry>();
        private readonly ObservableCollection<LockFieldEntry> _lockRules = new ObservableCollection<LockFieldEntry>();

        private readonly ListCollectionView _formSettingsView;
        private readonly ListCollectionView _defaultValuesView;
        private readonly ListCollectionView _lockRulesView;

        private FormSettingEntry _selectedFormSetting;
        private DefaultValueEntry _selectedDefaultValue;
        private LockFieldEntry _selectedLockRule;

        private string _formSettingsSearch;
        private string _defaultValuesSearch;
        private string _lockSearch;

        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public FormEnhancementViewModel()
        {
            _formSettingsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_formSettings);
            _formSettingsView.Filter = o => FilterFormSetting(o as FormSettingEntry, _formSettingsSearch);

            _defaultValuesView = (ListCollectionView)CollectionViewSource.GetDefaultView(_defaultValues);
            _defaultValuesView.Filter = o => FilterDefaultValue(o as DefaultValueEntry, _defaultValuesSearch);

            _lockRulesView = (ListCollectionView)CollectionViewSource.GetDefaultView(_lockRules);
            _lockRulesView.Filter = o => FilterLockRule(o as LockFieldEntry, _lockSearch);

            RefreshCommand = new RelayCommand(async () => await LoadAsync());

            NewFormSettingCommand = new RelayCommand(NewFormSetting);
            DuplicateFormSettingCommand = new RelayCommand(DuplicateFormSetting, () => SelectedFormSetting != null);
            SaveFormSettingCommand = new RelayCommand(async () => await SaveFormSettingAsync(), () => SelectedFormSetting != null);
            DeleteFormSettingCommand = new RelayCommand(async () => await DeleteFormSettingAsync(), () => SelectedFormSetting != null);

            NewDefaultValueCommand = new RelayCommand(NewDefaultValue);
            DuplicateDefaultValueCommand = new RelayCommand(DuplicateDefaultValue, () => SelectedDefaultValue != null);
            SaveDefaultValueCommand = new RelayCommand(async () => await SaveDefaultValueAsync(), () => SelectedDefaultValue != null);
            DeleteDefaultValueCommand = new RelayCommand(async () => await DeleteDefaultValueAsync(), () => SelectedDefaultValue != null);

            NewLockRuleCommand = new RelayCommand(NewLockRule);
            DuplicateLockRuleCommand = new RelayCommand(DuplicateLockRule, () => SelectedLockRule != null);
            SaveLockRuleCommand = new RelayCommand(async () => await SaveLockRuleAsync(), () => SelectedLockRule != null);
            DeleteLockRuleCommand = new RelayCommand(async () => await DeleteLockRuleAsync(), () => SelectedLockRule != null);
        }

        public ICollectionView FormSettingsView => _formSettingsView;
        public ICollectionView DefaultValuesView => _defaultValuesView;
        public ICollectionView LockRulesView => _lockRulesView;

        public FormSettingEntry SelectedFormSetting
        {
            get => _selectedFormSetting;
            set
            {
                if (_selectedFormSetting == value) return;
                _selectedFormSetting = value;
                OnPropertyChanged();
                DuplicateFormSettingCommand.RaiseCanExecuteChanged();
                SaveFormSettingCommand.RaiseCanExecuteChanged();
                DeleteFormSettingCommand.RaiseCanExecuteChanged();
            }
        }

        public DefaultValueEntry SelectedDefaultValue
        {
            get => _selectedDefaultValue;
            set
            {
                if (_selectedDefaultValue == value) return;
                _selectedDefaultValue = value;
                OnPropertyChanged();
                DuplicateDefaultValueCommand.RaiseCanExecuteChanged();
                SaveDefaultValueCommand.RaiseCanExecuteChanged();
                DeleteDefaultValueCommand.RaiseCanExecuteChanged();
            }
        }

        public LockFieldEntry SelectedLockRule
        {
            get => _selectedLockRule;
            set
            {
                if (_selectedLockRule == value) return;
                _selectedLockRule = value;
                OnPropertyChanged();
                DuplicateLockRuleCommand.RaiseCanExecuteChanged();
                SaveLockRuleCommand.RaiseCanExecuteChanged();
                DeleteLockRuleCommand.RaiseCanExecuteChanged();
            }
        }

        public string FormSettingsSearch
        {
            get => _formSettingsSearch;
            set
            {
                if (_formSettingsSearch == value) return;
                _formSettingsSearch = value;
                OnPropertyChanged();
                _formSettingsView.Refresh();
            }
        }

        public string DefaultValuesSearch
        {
            get => _defaultValuesSearch;
            set
            {
                if (_defaultValuesSearch == value) return;
                _defaultValuesSearch = value;
                OnPropertyChanged();
                _defaultValuesView.Refresh();
            }
        }

        public string LockSearch
        {
            get => _lockSearch;
            set
            {
                if (_lockSearch == value) return;
                _lockSearch = value;
                OnPropertyChanged();
                _lockRulesView.Refresh();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set { if (_busyMessage == value) return; _busyMessage = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (_statusMessage == value) return; _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshCommand { get; }

        public RelayCommand NewFormSettingCommand { get; }
        public RelayCommand DuplicateFormSettingCommand { get; }
        public RelayCommand SaveFormSettingCommand { get; }
        public RelayCommand DeleteFormSettingCommand { get; }

        public RelayCommand NewDefaultValueCommand { get; }
        public RelayCommand DuplicateDefaultValueCommand { get; }
        public RelayCommand SaveDefaultValueCommand { get; }
        public RelayCommand DeleteDefaultValueCommand { get; }

        public RelayCommand NewLockRuleCommand { get; }
        public RelayCommand DuplicateLockRuleCommand { get; }
        public RelayCommand SaveLockRuleCommand { get; }
        public RelayCommand DeleteLockRuleCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Loading form enhancements...", async () =>
            {
                var settingsTask = Task.Run(() => FormSettingsService.GetAll());
                var defaultsTask = Task.Run(() => DefaultValueService.GetAll());
                var locksTask = Task.Run(() => LockFieldService.GetAll());

                await Task.WhenAll(settingsTask, defaultsTask, locksTask);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _formSettings.Clear();
                    foreach (var entry in settingsTask.Result) _formSettings.Add(entry);
                    _formSettingsView.Refresh();
                    SelectedFormSetting = _formSettings.FirstOrDefault();

                    _defaultValues.Clear();
                    foreach (var entry in defaultsTask.Result) _defaultValues.Add(entry);
                    _defaultValuesView.Refresh();
                    SelectedDefaultValue = _defaultValues.FirstOrDefault();

                    _lockRules.Clear();
                    foreach (var entry in locksTask.Result) _lockRules.Add(entry);
                    _lockRulesView.Refresh();
                    SelectedLockRule = _lockRules.FirstOrDefault();

                    StatusMessage = $"{_formSettings.Count} form settings · {_defaultValues.Count} default rules · {_lockRules.Count} lock rules.";
                });
            });
        }

        private void NewFormSetting()
        {
            var entry = new FormSettingEntry { FormType = "", UserCode = B1App.Instance?.Company?.UserName ?? "", Data = "W=500;H=320" };
            _formSettings.Add(entry);
            SelectedFormSetting = entry;
            _formSettingsView.Refresh();
        }

        private void DuplicateFormSetting()
        {
            if (SelectedFormSetting == null) return;
            var copy = SelectedFormSetting.Clone();
            copy.DocEntry = 0;
            _formSettings.Add(copy);
            SelectedFormSetting = copy;
            _formSettingsView.Refresh();
        }

        private async Task SaveFormSettingAsync()
        {
            if (SelectedFormSetting == null) return;
            await RunSafeAsync("Saving form setting...", async () =>
            {
                await Task.Run(() => FormSettingsService.Save(SelectedFormSetting));
                StatusMessage = $"Settings for {SelectedFormSetting.FormType}/{SelectedFormSetting.UserCode} saved.";
            });
        }

        private async Task DeleteFormSettingAsync()
        {
            if (SelectedFormSetting == null) return;
            var target = SelectedFormSetting;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Deleting settings...", async () =>
                {
                    await Task.Run(() => FormSettingsService.Delete(target.DocEntry));
                });
            }
            _formSettings.Remove(target);
            SelectedFormSetting = _formSettings.FirstOrDefault();
            _formSettingsView.Refresh();
            StatusMessage = "Form settings removed.";
        }

        private void NewDefaultValue()
        {
            var entry = new DefaultValueEntry { FormType = "", ItemId = "", ColumnId = "", OnEvent = "Load", Query = "SELECT 'VALUE'" };
            _defaultValues.Add(entry);
            SelectedDefaultValue = entry;
            _defaultValuesView.Refresh();
        }

        private void DuplicateDefaultValue()
        {
            if (SelectedDefaultValue == null) return;
            var copy = SelectedDefaultValue.Clone();
            copy.DocEntry = 0;
            _defaultValues.Add(copy);
            SelectedDefaultValue = copy;
            _defaultValuesView.Refresh();
        }

        private async Task SaveDefaultValueAsync()
        {
            if (SelectedDefaultValue == null) return;
            await RunSafeAsync("Saving default rule...", async () =>
            {
                await Task.Run(() => DefaultValueService.Save(SelectedDefaultValue));
                StatusMessage = "Default rule saved.";
            });
        }

        private async Task DeleteDefaultValueAsync()
        {
            if (SelectedDefaultValue == null) return;
            var target = SelectedDefaultValue;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Deleting default rule...", async () =>
                {
                    await Task.Run(() => DefaultValueService.Delete(target.DocEntry));
                });
            }
            _defaultValues.Remove(target);
            SelectedDefaultValue = _defaultValues.FirstOrDefault();
            _defaultValuesView.Refresh();
            StatusMessage = "Default rule removed.";
        }

        private void NewLockRule()
        {
            var entry = new LockFieldEntry { FormType = "", ItemId = "", ColumnId = "", LockType = "ReadOnly", OnEvent = "Load", Condition = string.Empty };
            _lockRules.Add(entry);
            SelectedLockRule = entry;
            _lockRulesView.Refresh();
        }

        private void DuplicateLockRule()
        {
            if (SelectedLockRule == null) return;
            var copy = SelectedLockRule.Clone();
            copy.DocEntry = 0;
            _lockRules.Add(copy);
            SelectedLockRule = copy;
            _lockRulesView.Refresh();
        }

        private async Task SaveLockRuleAsync()
        {
            if (SelectedLockRule == null) return;
            await RunSafeAsync("Saving lock rule...", async () =>
            {
                await Task.Run(() => LockFieldService.Save(SelectedLockRule));
                StatusMessage = "Lock rule saved.";
            });
        }

        private async Task DeleteLockRuleAsync()
        {
            if (SelectedLockRule == null) return;
            var target = SelectedLockRule;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Deleting lock rule...", async () =>
                {
                    await Task.Run(() => LockFieldService.Delete(target.DocEntry));
                });
            }
            _lockRules.Remove(target);
            SelectedLockRule = _lockRules.FirstOrDefault();
            _lockRulesView.Refresh();
            StatusMessage = "Lock rule removed.";
        }

        private bool FilterFormSetting(FormSettingEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.FormType, term) || Contains(entry.UserCode, term) || Contains(entry.Data, term);
        }

        private bool FilterDefaultValue(DefaultValueEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.FormType, term) || Contains(entry.ItemId, term) || Contains(entry.Query, term);
        }

        private bool FilterLockRule(LockFieldEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.FormType, term) || Contains(entry.ItemId, term) || Contains(entry.LockType, term) || Contains(entry.Condition, term);
        }

        private static bool Contains(string source, string term)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(term)) return false;
            return source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task RunSafeAsync(string message, Func<Task> work)
        {
            try
            {
                IsBusy = true;
                BusyMessage = message;
                await work();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
                RaiseCanExecute();
            }
        }

        private void RaiseCanExecute()
        {
            DuplicateFormSettingCommand.RaiseCanExecuteChanged();
            SaveFormSettingCommand.RaiseCanExecuteChanged();
            DeleteFormSettingCommand.RaiseCanExecuteChanged();
            DuplicateDefaultValueCommand.RaiseCanExecuteChanged();
            SaveDefaultValueCommand.RaiseCanExecuteChanged();
            DeleteDefaultValueCommand.RaiseCanExecuteChanged();
            DuplicateLockRuleCommand.RaiseCanExecuteChanged();
            SaveLockRuleCommand.RaiseCanExecuteChanged();
            DeleteLockRuleCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
