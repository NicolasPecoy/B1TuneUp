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

namespace B1TuneUp.Modules.MacroEngineUi
{
    public class MacroEngineViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<MacroScriptEntry> _macros = new ObservableCollection<MacroScriptEntry>();
        private readonly ListCollectionView _macrosView;

        private MacroScriptEntry _selectedMacro;
        private string _searchText;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private string _testResult;

        public MacroEngineViewModel()
        {
            _macrosView = (ListCollectionView)CollectionViewSource.GetDefaultView(_macros);
            _macrosView.Filter = o => FilterMacro(o as MacroScriptEntry, _searchText);

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            NewCommand = new RelayCommand(NewMacro);
            DuplicateCommand = new RelayCommand(DuplicateMacro, () => SelectedMacro != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedMacro != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedMacro != null);
            RunCommand = new RelayCommand(RunMacro, () => SelectedMacro != null);
            CopyCommand = new RelayCommand(CopySource, () => SelectedMacro != null && !string.IsNullOrEmpty(SelectedMacro.Source));
        }

        public ICollectionView MacrosView => _macrosView;

        public MacroScriptEntry SelectedMacro
        {
            get => _selectedMacro;
            set
            {
                if (_selectedMacro == value) return;
                _selectedMacro = value;
                OnPropertyChanged();
                DuplicateCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                RunCommand.RaiseCanExecuteChanged();
                CopyCommand.RaiseCanExecuteChanged();
                TestResult = string.Empty;
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                _macrosView.Refresh();
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

        public string TestResult
        {
            get => _testResult;
            private set { if (_testResult == value) return; _testResult = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RunCommand { get; }
        public RelayCommand CopyCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Loading macros...", async () =>
            {
                var list = await Task.Run(() => MacroScriptService.GetAll());
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _macros.Clear();
                    foreach (var item in list) _macros.Add(item);
                    _macrosView.Refresh();
                    SelectedMacro = _macros.FirstOrDefault();
                    StatusMessage = $"{_macros.Count} macro scripts loaded.";
                });
            });
        }

        private void NewMacro()
        {
            var entry = new MacroScriptEntry
            {
                Name = "New Macro",
                Description = "",
                Source = "Msg('Hello SAP');"
            };
            _macros.Add(entry);
            SelectedMacro = entry;
            _macrosView.Refresh();
        }

        private void DuplicateMacro()
        {
            if (SelectedMacro == null) return;
            var copy = SelectedMacro.Clone();
            copy.Code = null;
            copy.Name = SelectedMacro.Name + " Copy";
            _macros.Add(copy);
            SelectedMacro = copy;
            _macrosView.Refresh();
        }

        private async Task SaveAsync()
        {
            if (SelectedMacro == null) return;
            await RunSafeAsync("Saving macro...", async () =>
            {
                await Task.Run(() => MacroScriptService.Save(SelectedMacro));
                StatusMessage = $"Macro '{SelectedMacro.Name}' saved.";
            });
        }

        private async Task DeleteAsync()
        {
            if (SelectedMacro == null) return;
            var target = SelectedMacro;
            if (!string.IsNullOrWhiteSpace(target.Code))
            {
                await RunSafeAsync("Deleting macro...", async () =>
                {
                    await Task.Run(() => MacroScriptService.Delete(target.Code));
                });
            }
            _macros.Remove(target);
            SelectedMacro = _macros.FirstOrDefault();
            _macrosView.Refresh();
            StatusMessage = "Macro removed.";
        }

        private void RunMacro()
        {
            if (SelectedMacro == null) return;
            try
            {
                MacroEngine.ExecuteMacro(SelectedMacro.Source ?? string.Empty);
                TestResult = "Macro executed successfully.";
                StatusMessage = $"Macro '{SelectedMacro.Name}' executed.";
            }
            catch (Exception ex)
            {
                TestResult = ex.Message;
                StatusMessage = "Macro execution error.";
            }
        }

        private void CopySource()
        {
            try
            {
                Clipboard.SetText(SelectedMacro?.Source ?? string.Empty);
                StatusMessage = "Macro source copied to clipboard.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private bool FilterMacro(MacroScriptEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.Name, term) ||
                   Contains(entry.Description, term) ||
                   Contains(entry.Source, term);
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
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
