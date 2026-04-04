using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.DashboardSearchMacroUi
{
    public class DashboardSearchMacroViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<DashboardWidgetEntry> _widgets = new ObservableCollection<DashboardWidgetEntry>();
        private readonly ObservableCollection<SearchConfigEntry> _searchConfigs = new ObservableCollection<SearchConfigEntry>();
        private readonly ObservableCollection<MacroScriptEntry> _macroScripts = new ObservableCollection<MacroScriptEntry>();

        private readonly ListCollectionView _widgetsView;
        private readonly ListCollectionView _searchView;
        private readonly ListCollectionView _macroView;

        private DashboardWidgetEntry _selectedWidget;
        private SearchConfigEntry _selectedSearch;
        private MacroScriptEntry _selectedMacro;

        private string _widgetSearch;
        private string _searchSearch;
        private string _macroSearch;

        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private readonly IReadOnlyList<int> _autoRefreshOptions = new[] { 30, 60, 120, 300 };
        private bool _autoRefreshEnabled = true;
        private int _autoRefreshIntervalSeconds = 120;
        private DateTime? _lastRefreshAt;
        private DateTime? _nextRefreshAt;

        public DashboardSearchMacroViewModel()
        {
            _widgetsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_widgets);
            _widgetsView.Filter = o => FilterEntry(o as DashboardWidgetEntry, _widgetSearch);

            _searchView = (ListCollectionView)CollectionViewSource.GetDefaultView(_searchConfigs);
            _searchView.Filter = o => FilterEntry(o as SearchConfigEntry, _searchSearch);

            _macroView = (ListCollectionView)CollectionViewSource.GetDefaultView(_macroScripts);
            _macroView.Filter = o => FilterEntry(o as MacroScriptEntry, _macroSearch);

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => _widgets.Any() || _searchConfigs.Any() || _macroScripts.Any());

            NewWidgetCommand = new RelayCommand(NewWidget);
            DuplicateWidgetCommand = new RelayCommand(DuplicateWidget, () => SelectedWidget != null);
            SaveWidgetCommand = new RelayCommand(async () => await SaveWidgetAsync(), () => SelectedWidget != null);
            DeleteWidgetCommand = new RelayCommand(async () => await DeleteWidgetAsync(), () => SelectedWidget != null);

            NewSearchCommand = new RelayCommand(NewSearch);
            DuplicateSearchCommand = new RelayCommand(DuplicateSearch, () => SelectedSearch != null);
            SaveSearchCommand = new RelayCommand(async () => await SaveSearchAsync(), () => SelectedSearch != null);
            DeleteSearchCommand = new RelayCommand(async () => await DeleteSearchAsync(), () => SelectedSearch != null);

            NewMacroCommand = new RelayCommand(NewMacro);
            DuplicateMacroCommand = new RelayCommand(DuplicateMacro, () => SelectedMacro != null);
            SaveMacroCommand = new RelayCommand(async () => await SaveMacroAsync(), () => SelectedMacro != null);
            DeleteMacroCommand = new RelayCommand(async () => await DeleteMacroAsync(), () => SelectedMacro != null);
        }

        public ICollectionView WidgetsView => _widgetsView;
        public ICollectionView SearchView => _searchView;
        public ICollectionView MacroView => _macroView;

        public DashboardWidgetEntry SelectedWidget
        {
            get => _selectedWidget;
            set
            {
                if (_selectedWidget == value) return;
                _selectedWidget = value;
                OnPropertyChanged();
                SaveWidgetCommand.RaiseCanExecuteChanged();
                DuplicateWidgetCommand.RaiseCanExecuteChanged();
                DeleteWidgetCommand.RaiseCanExecuteChanged();
            }
        }

        public SearchConfigEntry SelectedSearch
        {
            get => _selectedSearch;
            set
            {
                if (_selectedSearch == value) return;
                _selectedSearch = value;
                OnPropertyChanged();
                SaveSearchCommand.RaiseCanExecuteChanged();
                DuplicateSearchCommand.RaiseCanExecuteChanged();
                DeleteSearchCommand.RaiseCanExecuteChanged();
            }
        }

        public MacroScriptEntry SelectedMacro
        {
            get => _selectedMacro;
            set
            {
                if (_selectedMacro == value) return;
                _selectedMacro = value;
                OnPropertyChanged();
                SaveMacroCommand.RaiseCanExecuteChanged();
                DuplicateMacroCommand.RaiseCanExecuteChanged();
                DeleteMacroCommand.RaiseCanExecuteChanged();
            }
        }

        public string WidgetSearch
        {
            get => _widgetSearch;
            set
            {
                if (_widgetSearch == value) return;
                _widgetSearch = value;
                OnPropertyChanged();
                _widgetsView.Refresh();
            }
        }

        public string SearchSearch
        {
            get => _searchSearch;
            set
            {
                if (_searchSearch == value) return;
                _searchSearch = value;
                OnPropertyChanged();
                _searchView.Refresh();
            }
        }

        public string MacroSearch
        {
            get => _macroSearch;
            set
            {
                if (_macroSearch == value) return;
                _macroSearch = value;
                OnPropertyChanged();
                _macroView.Refresh();
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

        public IReadOnlyList<int> AutoRefreshOptions => _autoRefreshOptions;

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                if (_autoRefreshEnabled == value) return;
                _autoRefreshEnabled = value;
                OnPropertyChanged();
                UpdateAutoRefreshSchedule();
            }
        }

        public int AutoRefreshIntervalSeconds
        {
            get => _autoRefreshIntervalSeconds;
            set
            {
                int normalized = Math.Max(30, value);
                if (_autoRefreshIntervalSeconds == normalized) return;
                _autoRefreshIntervalSeconds = normalized;
                OnPropertyChanged();
                UpdateAutoRefreshSchedule();
            }
        }

        public DateTime? LastRefreshAt
        {
            get => _lastRefreshAt;
            private set
            {
                if (_lastRefreshAt == value) return;
                _lastRefreshAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoRefreshStatus));
            }
        }

        public DateTime? NextRefreshAt
        {
            get => _nextRefreshAt;
            private set
            {
                if (_nextRefreshAt == value) return;
                _nextRefreshAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoRefreshStatus));
            }
        }

        public string AutoRefreshStatus
        {
            get
            {
                if (!AutoRefreshEnabled) return "Auto-refresh desactivado";
                string next = NextRefreshAt.HasValue ? NextRefreshAt.Value.ToString("HH:mm:ss") : "calculando";
                string last = LastRefreshAt.HasValue ? LastRefreshAt.Value.ToString("HH:mm:ss") : "n/d";
                return $"Próximo: {next} · Último: {last}";
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand SaveAllCommand { get; }

        public RelayCommand NewWidgetCommand { get; }
        public RelayCommand DuplicateWidgetCommand { get; }
        public RelayCommand SaveWidgetCommand { get; }
        public RelayCommand DeleteWidgetCommand { get; }

        public RelayCommand NewSearchCommand { get; }
        public RelayCommand DuplicateSearchCommand { get; }
        public RelayCommand SaveSearchCommand { get; }
        public RelayCommand DeleteSearchCommand { get; }

        public RelayCommand NewMacroCommand { get; }
        public RelayCommand DuplicateMacroCommand { get; }
        public RelayCommand SaveMacroCommand { get; }
        public RelayCommand DeleteMacroCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync(bool triggeredByAutoRefresh = false)
        {
            string busyText = triggeredByAutoRefresh ? "Actualizando Dashboard/Search/Macros..." : "Cargando Dashboard/Search/Macros...";
            await RunSafeAsync(busyText, async () =>
            {
                var widgetTask = Task.Run(() => DashboardWidgetService.GetAll());
                var searchTask = Task.Run(() => SearchConfigService.GetAll());
                var macroTask = Task.Run(() => MacroScriptService.GetAll());

                await Task.WhenAll(widgetTask, searchTask, macroTask);

                RunOnUi(() =>
                {
                    _widgets.Clear();
                    foreach (var item in widgetTask.Result) _widgets.Add(item);
                    _widgetsView.Refresh();
                    SelectedWidget = _widgets.FirstOrDefault();

                    _searchConfigs.Clear();
                    foreach (var item in searchTask.Result) _searchConfigs.Add(item);
                    _searchView.Refresh();
                    SelectedSearch = _searchConfigs.FirstOrDefault();

                    _macroScripts.Clear();
                    foreach (var item in macroTask.Result) _macroScripts.Add(item);
                    _macroView.Refresh();
                    SelectedMacro = _macroScripts.FirstOrDefault();

                    StatusMessage = $"{_widgets.Count} widgets · {_searchConfigs.Count} búsquedas · {_macroScripts.Count} macros.";
                    LastRefreshAt = DateTime.Now;
                    UpdateAutoRefreshSchedule();
                });
            });
        }

        public async Task TryAutoRefreshAsync()
        {
            if (!AutoRefreshEnabled || IsBusy) return;
            if (NextRefreshAt.HasValue && DateTime.Now < NextRefreshAt.Value) return;
            await LoadAsync(true);
        }

        private void NewWidget()
        {
            var entry = new DashboardWidgetEntry
            {
                Name = "Widget",
                Title = "Nuevo widget",
                WidgetType = "Stats",
                Width = 320,
                Height = 200
            };
            _widgets.Add(entry);
            SelectedWidget = entry;
            _widgetsView.Refresh();
        }

        private void DuplicateWidget()
        {
            if (SelectedWidget == null) return;
            var copy = SelectedWidget.Clone();
            copy.Code = null;
            copy.Name = $"{copy.Name} (Copy)";
            _widgets.Add(copy);
            SelectedWidget = copy;
            _widgetsView.Refresh();
        }

        private async Task SaveWidgetAsync()
        {
            if (SelectedWidget == null) return;
            await RunSafeAsync("Guardando widget...", async () =>
            {
                await Task.Run(() => DashboardWidgetService.Save(SelectedWidget));
                StatusMessage = $"Widget '{SelectedWidget.Title}' guardado.";
            });
        }

        private async Task DeleteWidgetAsync()
        {
            if (SelectedWidget == null) return;
            if (MessageBox.Show($"Â¿Eliminar el widget '{SelectedWidget.Title}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            var toDelete = SelectedWidget;
            await RunSafeAsync("Eliminando widget...", async () =>
            {
                await Task.Run(() => DashboardWidgetService.Delete(toDelete.Code));
                RunOnUi(() =>
                {
                    _widgets.Remove(toDelete);
                    SelectedWidget = _widgets.FirstOrDefault();
                    _widgetsView.Refresh();
                });
                StatusMessage = "Widget eliminado.";
            });
        }

        private void NewSearch()
        {
            var entry = new SearchConfigEntry
            {
                Name = "Nueva bÃºsqueda",
                Query = "SELECT TOP 10 * FROM OCRD WHERE CardName LIKE '%search%'",
                Action = "Msg('Abrir registro $[CardCode]')"
            };
            _searchConfigs.Add(entry);
            SelectedSearch = entry;
            _searchView.Refresh();
        }

        private void DuplicateSearch()
        {
            if (SelectedSearch == null) return;
            var copy = SelectedSearch.Clone();
            copy.Code = null;
            copy.Name = $"{copy.Name} (Copy)";
            _searchConfigs.Add(copy);
            SelectedSearch = copy;
            _searchView.Refresh();
        }

        private async Task SaveSearchAsync()
        {
            if (SelectedSearch == null) return;
            await RunSafeAsync("Guardando bÃºsqueda...", async () =>
            {
                await Task.Run(() => SearchConfigService.Save(SelectedSearch));
                StatusMessage = $"BÃºsqueda '{SelectedSearch.Name}' guardada.";
            });
        }

        private async Task DeleteSearchAsync()
        {
            if (SelectedSearch == null) return;
            if (MessageBox.Show($"Â¿Eliminar la bÃºsqueda '{SelectedSearch.Name}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            var toDelete = SelectedSearch;
            await RunSafeAsync("Eliminando bÃºsqueda...", async () =>
            {
                await Task.Run(() => SearchConfigService.Delete(toDelete.Code));
                RunOnUi(() =>
                {
                    _searchConfigs.Remove(toDelete);
                    SelectedSearch = _searchConfigs.FirstOrDefault();
                    _searchView.Refresh();
                });
                StatusMessage = "BÃºsqueda eliminada.";
            });
        }

        private void NewMacro()
        {
            var entry = new MacroScriptEntry
            {
                Name = "Nueva macro",
                Source = "Msg('Hola SAP');"
            };
            _macroScripts.Add(entry);
            SelectedMacro = entry;
            _macroView.Refresh();
        }

        private void DuplicateMacro()
        {
            if (SelectedMacro == null) return;
            var copy = SelectedMacro.Clone();
            copy.Code = null;
            copy.Name = $"{copy.Name} (Copy)";
            _macroScripts.Add(copy);
            SelectedMacro = copy;
            _macroView.Refresh();
        }

        private async Task SaveMacroAsync()
        {
            if (SelectedMacro == null) return;
            await RunSafeAsync("Guardando macro...", async () =>
            {
                await Task.Run(() => MacroScriptService.Save(SelectedMacro));
                StatusMessage = $"Macro '{SelectedMacro.Name}' guardada.";
            });
        }

        private async Task DeleteMacroAsync()
        {
            if (SelectedMacro == null) return;
            if (MessageBox.Show($"Â¿Eliminar la macro '{SelectedMacro.Name}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            var toDelete = SelectedMacro;
            await RunSafeAsync("Eliminando macro...", async () =>
            {
                await Task.Run(() => MacroScriptService.Delete(toDelete.Code));
                RunOnUi(() =>
                {
                    _macroScripts.Remove(toDelete);
                    SelectedMacro = _macroScripts.FirstOrDefault();
                    _macroView.Refresh();
                });
                StatusMessage = "Macro eliminada.";
            });
        }

        private async Task SaveAllAsync()
        {
            await RunSafeAsync("Guardando todo...", async () =>
            {
                await Task.Run(() =>
                {
                    foreach (var widget in _widgets) DashboardWidgetService.Save(widget);
                    foreach (var search in _searchConfigs) SearchConfigService.Save(search);
                    foreach (var macro in _macroScripts) MacroScriptService.Save(macro);
                });
                StatusMessage = "Todos los registros fueron guardados.";
            });
        }

        private bool FilterEntry(DashboardWidgetEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.Title, term) || Contains(entry.WidgetType, term) || Contains(entry.Query, term);
        }

        private bool FilterEntry(SearchConfigEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.Name, term) || Contains(entry.Query, term) || Contains(entry.Action, term);
        }

        private bool FilterEntry(MacroScriptEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.Name, term) || Contains(entry.Description, term) || Contains(entry.Source, term);
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
                SaveAllCommand.RaiseCanExecuteChanged();
            }
        }

        private void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        private void UpdateAutoRefreshSchedule()
        {
            if (AutoRefreshEnabled)
            {
                NextRefreshAt = DateTime.Now.AddSeconds(AutoRefreshIntervalSeconds);
            }
            else
            {
                NextRefreshAt = null;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
