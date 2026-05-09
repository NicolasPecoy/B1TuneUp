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

namespace B1TuneUp.Modules.ToolboxUi
{
    public class ToolboxDesignerViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ToolboxSettingEntry> _settings = new ObservableCollection<ToolboxSettingEntry>();
        private readonly ObservableCollection<ModuleConfigurationEntry> _modules = new ObservableCollection<ModuleConfigurationEntry>();
        private readonly ListCollectionView _view;

        private ToolboxSettingEntry _selected;
        private ModuleConfigurationEntry _selectedModule;
        private string _searchText;
        private string _categoryFilter = "Todos";
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public ToolboxDesignerViewModel(string initialCategory = null)
        {
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_settings);
            _view.Filter = FilterSettings;

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            SaveCommand = new RelayCommand(async () => await SaveSelectedAsync(), () => SelectedSetting != null);
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => _settings.Any());
            NewCommand = new RelayCommand(NewSetting);
            DeleteCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedSetting != null);
            SaveModulesCommand = new RelayCommand(async () => await SaveModulesAsync(), () => _modules.Any());

            CategoryOptions = new ObservableCollection<string> { "Todos", "Modules", "General", "Sistema", "Email / SMTP", "Exchange Rates", "Scheduler", "Integraciones", "Notificaciones", "Personalizado" };
            if (!string.IsNullOrWhiteSpace(initialCategory))
            {
                _categoryFilter = initialCategory;
            }
        }

        public ICollectionView SettingsView => _view;
        public ObservableCollection<ModuleConfigurationEntry> Modules => _modules;

        public ObservableCollection<string> CategoryOptions { get; }

        public ToolboxSettingEntry SelectedSetting
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                OnPropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public ModuleConfigurationEntry SelectedModule
        {
            get => _selectedModule;
            set
            {
                if (_selectedModule == value) return;
                _selectedModule = value;
                OnPropertyChanged();
                SaveModulesCommand.RaiseCanExecuteChanged();
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
                _view.Refresh();
            }
        }

        public string CategoryFilter
        {
            get => _categoryFilter;
            set
            {
                if (_categoryFilter == value) return;
                _categoryFilter = value;
                OnPropertyChanged();
                _view.Refresh();
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
        public RelayCommand SaveCommand { get; }
        public RelayCommand SaveAllCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveModulesCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando Toolbox...", async () =>
            {
                var list = await Task.Run(() => ToolboxSettingService.GetAll());
                RunOnUi(() =>
                {
                    _settings.Clear();
                    foreach (var item in list.OrderBy(s => s.Code))
                    {
                        _settings.Add(item);
                    }
                    _modules.Clear();
                    foreach (var module in ModuleActivationService.GetAll())
                    {
                        _modules.Add(module.Clone());
                    }
                    _view.Refresh();
                    SelectedSetting = _settings.FirstOrDefault();
                    SelectedModule = _modules.FirstOrDefault();
                    StatusMessage = $"{_settings.Count} configuraciones cargadas.";
                });
            });
        }

        private async Task SaveSelectedAsync()
        {
            if (SelectedSetting == null) return;
            await RunSafeAsync("Guardando configuración...", async () =>
            {
                await Task.Run(() => ToolboxSettingService.Save(SelectedSetting));
                StatusMessage = $"Configuración '{SelectedSetting.Code}' guardada.";
            });
        }

        private async Task SaveAllAsync()
        {
            await RunSafeAsync("Guardando todas las configuraciones...", async () =>
            {
                await Task.Run(() =>
                {
                    foreach (var setting in _settings)
                    {
                        ToolboxSettingService.Save(setting);
                    }
                    foreach (var module in _modules)
                    {
                        ModuleActivationService.Save(module);
                    }
                });
                StatusMessage = "Todas las configuraciones se guardaron correctamente.";
            });
        }

        private async Task SaveModulesAsync()
        {
            await RunSafeAsync("Guardando mÃ³dulos...", async () =>
            {
                await Task.Run(() =>
                {
                    foreach (var module in _modules)
                    {
                        ModuleActivationService.Save(module);
                    }
                });
                StatusMessage = "ConfiguraciÃ³n de mÃ³dulos actualizada.";
            });
        }

        private void NewSetting()
        {
            var entry = new ToolboxSettingEntry
            {
                Code = "NEW_SETTING",
                Value = string.Empty,
                Category = "Personalizado",
                Description = "Nueva configuración personalizada."
            };
            _settings.Add(entry);
            SelectedSetting = entry;
            _view.Refresh();
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedSetting == null) return;
            if (MessageBox.Show($"¿Eliminar la configuración '{SelectedSetting.Code}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            var toDelete = SelectedSetting;
            await RunSafeAsync("Eliminando configuración...", async () =>
            {
                await Task.Run(() => ToolboxSettingService.Delete(toDelete.Code));
                RunOnUi(() =>
                {
                    _settings.Remove(toDelete);
                    SelectedSetting = _settings.FirstOrDefault();
                    _view.Refresh();
                });
                StatusMessage = $"Configuración '{toDelete.Code}' eliminada.";
            });
        }

        private bool FilterSettings(object obj)
        {
            var entry = obj as ToolboxSettingEntry;
            if (entry == null) return false;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                if (!ContainsInsensitive(entry.Code, term) &&
                    !ContainsInsensitive(entry.Value, term) &&
                    !ContainsInsensitive(entry.Description, term))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(CategoryFilter) && !string.Equals(CategoryFilter, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(entry.Category, CategoryFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ContainsInsensitive(string source, string term)
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
