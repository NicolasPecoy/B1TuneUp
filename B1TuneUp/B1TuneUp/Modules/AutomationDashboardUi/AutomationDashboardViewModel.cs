using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.AutomationDashboardUi
{
    public class AutomationDashboardViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<MenuConfigEntry> _menus = new ObservableCollection<MenuConfigEntry>();
        private readonly ObservableCollection<MacroScriptEntry> _macros = new ObservableCollection<MacroScriptEntry>();

        private readonly ListCollectionView _menusView;
        private readonly ListCollectionView _macrosView;

        private MenuConfigEntry _selectedMenu;
        private MacroScriptEntry _selectedMacro;

        private string _menuSearch;
        private string _macroSearch;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private string _testOutput;

        private string _deployTarget = "QA";
        private string _deployNotes;

        public AutomationDashboardViewModel()
        {
            _menusView = (ListCollectionView)CollectionViewSource.GetDefaultView(_menus);
            _menusView.Filter = o => FilterMenu(o as MenuConfigEntry, _menuSearch);

            _macrosView = (ListCollectionView)CollectionViewSource.GetDefaultView(_macros);
            _macrosView.Filter = o => FilterMacro(o as MacroScriptEntry, _macroSearch);

            RefreshCommand = new RelayCommand(async () => await LoadAsync());

            NewMenuCommand = new RelayCommand(NewMenu);
            DuplicateMenuCommand = new RelayCommand(DuplicateMenu, () => SelectedMenu != null);
            SaveMenuCommand = new RelayCommand(async () => await SaveMenuAsync(), () => SelectedMenu != null);
            DeleteMenuCommand = new RelayCommand(async () => await DeleteMenuAsync(), () => SelectedMenu != null);
            TestMenuCommand = new RelayCommand(TestMenu, () => SelectedMenu != null && !string.IsNullOrEmpty(SelectedMenu.Action));

            NewMacroCommand = new RelayCommand(NewMacro);
            DuplicateMacroCommand = new RelayCommand(DuplicateMacro, () => SelectedMacro != null);
            SaveMacroCommand = new RelayCommand(async () => await SaveMacroAsync(), () => SelectedMacro != null);
            DeleteMacroCommand = new RelayCommand(async () => await DeleteMacroAsync(), () => SelectedMacro != null);
            RunMacroCommand = new RelayCommand(RunMacro, () => SelectedMacro != null);
            CopyMacroCommand = new RelayCommand(CopyMacro, () => SelectedMacro != null && !string.IsNullOrEmpty(SelectedMacro.Source));

            DeployCommand = new RelayCommand(DeployChanges);
        }

        public ICollectionView MenusView => _menusView;
        public ICollectionView MacrosView => _macrosView;

        public MenuConfigEntry SelectedMenu
        {
            get => _selectedMenu;
            set
            {
                if (_selectedMenu == value) return;
                _selectedMenu = value;
                OnPropertyChanged();
                DuplicateMenuCommand.RaiseCanExecuteChanged();
                SaveMenuCommand.RaiseCanExecuteChanged();
                DeleteMenuCommand.RaiseCanExecuteChanged();
                TestMenuCommand.RaiseCanExecuteChanged();
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
                DuplicateMacroCommand.RaiseCanExecuteChanged();
                SaveMacroCommand.RaiseCanExecuteChanged();
                DeleteMacroCommand.RaiseCanExecuteChanged();
                RunMacroCommand.RaiseCanExecuteChanged();
                CopyMacroCommand.RaiseCanExecuteChanged();
                TestOutput = string.Empty;
            }
        }

        public string MenuSearch
        {
            get => _menuSearch;
            set
            {
                if (_menuSearch == value) return;
                _menuSearch = value;
                OnPropertyChanged();
                _menusView.Refresh();
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
                _macrosView.Refresh();
            }
        }

        public string DeployTarget
        {
            get => _deployTarget;
            set { if (_deployTarget == value) return; _deployTarget = value; OnPropertyChanged(); }
        }

        public string DeployNotes
        {
            get => _deployNotes;
            set { if (_deployNotes == value) return; _deployNotes = value; OnPropertyChanged(); }
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

        public string TestOutput
        {
            get => _testOutput;
            private set { if (_testOutput == value) return; _testOutput = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewMenuCommand { get; }
        public RelayCommand DuplicateMenuCommand { get; }
        public RelayCommand SaveMenuCommand { get; }
        public RelayCommand DeleteMenuCommand { get; }
        public RelayCommand TestMenuCommand { get; }

        public RelayCommand NewMacroCommand { get; }
        public RelayCommand DuplicateMacroCommand { get; }
        public RelayCommand SaveMacroCommand { get; }
        public RelayCommand DeleteMacroCommand { get; }
        public RelayCommand RunMacroCommand { get; }
        public RelayCommand CopyMacroCommand { get; }

        public RelayCommand DeployCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando Automation Dashboard...", async () =>
            {
                var menusTask = Task.Run(() => MenuConfigService.GetAll());
                var macrosTask = Task.Run(() => MacroScriptService.GetAll());

                await Task.WhenAll(menusTask, macrosTask);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _menus.Clear();
                    foreach (var entry in menusTask.Result) _menus.Add(entry);
                    _menusView.Refresh();
                    SelectedMenu = _menus.FirstOrDefault();

                    _macros.Clear();
                    foreach (var entry in macrosTask.Result) _macros.Add(entry);
                    _macrosView.Refresh();
                    SelectedMacro = _macros.FirstOrDefault();

                    StatusMessage = $"{_menus.Count} menus personalizados · {_macros.Count} macros disponibles.";
                });
            });
        }

        private void NewMenu()
        {
            var entry = new MenuConfigEntry
            {
                ParentId = "43520",
                MenuId = "BTUN_",
                Caption = "Nuevo menu",
                Position = 9000,
                Action = "Msg('Hola');"
            };
            _menus.Add(entry);
            SelectedMenu = entry;
            _menusView.Refresh();
        }

        private void DuplicateMenu()
        {
            if (SelectedMenu == null) return;
            var copy = SelectedMenu.Clone();
            copy.DocEntry = 0;
            copy.MenuId = SelectedMenu.MenuId + "_copy";
            _menus.Add(copy);
            SelectedMenu = copy;
            _menusView.Refresh();
        }

        private async Task SaveMenuAsync()
        {
            if (SelectedMenu == null) return;
            await RunSafeAsync("Guardando menu...", async () =>
            {
                await Task.Run(() => MenuConfigService.Save(SelectedMenu));
                StatusMessage = $"Menu {SelectedMenu.MenuId} guardado.";
            });
        }

        private async Task DeleteMenuAsync()
        {
            if (SelectedMenu == null) return;
            var target = SelectedMenu;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Eliminando menu...", async () =>
                {
                    await Task.Run(() => MenuConfigService.Delete(target.DocEntry));
                });
            }
            _menus.Remove(target);
            SelectedMenu = _menus.FirstOrDefault();
            _menusView.Refresh();
            StatusMessage = "Menu eliminado.";
        }

        private void TestMenu()
        {
            if (SelectedMenu == null || string.IsNullOrWhiteSpace(SelectedMenu.Action)) return;
            try
            {
                MacroEngine.ExecuteMacro(SelectedMenu.Action);
                TestOutput = $"Accion del menu {SelectedMenu.MenuId} ejecutada.";
            }
            catch (Exception ex)
            {
                TestOutput = ex.Message;
            }
        }

        private void NewMacro()
        {
            var entry = new MacroScriptEntry { Name = "Nueva macro", Description = "", Source = "Msg('Hola');" };
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

        private async Task SaveMacroAsync()
        {
            if (SelectedMacro == null) return;
            await RunSafeAsync("Guardando macro...", async () =>
            {
                await Task.Run(() => MacroScriptService.Save(SelectedMacro));
                StatusMessage = $"Macro {SelectedMacro.Name} guardada.";
            });
        }

        private async Task DeleteMacroAsync()
        {
            if (SelectedMacro == null) return;
            var target = SelectedMacro;
            if (!string.IsNullOrWhiteSpace(target.Code))
            {
                await RunSafeAsync("Eliminando macro...", async () =>
                {
                    await Task.Run(() => MacroScriptService.Delete(target.Code));
                });
            }
            _macros.Remove(target);
            SelectedMacro = _macros.FirstOrDefault();
            _macrosView.Refresh();
            StatusMessage = "Macro eliminada.";
        }

        private void RunMacro()
        {
            if (SelectedMacro == null) return;
            try
            {
                MacroEngine.ExecuteMacro(SelectedMacro.Source ?? string.Empty);
                TestOutput = $"Macro {SelectedMacro.Name} ejecutada.";
            }
            catch (Exception ex)
            {
                TestOutput = ex.Message;
            }
        }

        private void CopyMacro()
        {
            try
            {
                Clipboard.SetText(SelectedMacro?.Source ?? string.Empty);
                StatusMessage = "Codigo copiado.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void DeployChanges()
        {
            string target = string.IsNullOrWhiteSpace(DeployTarget) ? "QA" : DeployTarget.Trim();
            string notes = string.IsNullOrWhiteSpace(DeployNotes) ? "Sin notas" : DeployNotes.Trim();
            StatusMessage = $"Despliegue simulado hacia {target}: {notes}";
            TestOutput = $"Checklist completado para {target} a las {DateTime.Now:HH:mm}.";
        }

        private bool FilterMenu(MenuConfigEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.MenuId, term) || Contains(entry.Caption, term) || Contains(entry.Action, term);
        }

        private bool FilterMacro(MacroScriptEntry entry, string term)
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
                RaiseCanExecute();
            }
        }

        private void RaiseCanExecute()
        {
            DuplicateMenuCommand.RaiseCanExecuteChanged();
            SaveMenuCommand.RaiseCanExecuteChanged();
            DeleteMenuCommand.RaiseCanExecuteChanged();
            TestMenuCommand.RaiseCanExecuteChanged();
            DuplicateMacroCommand.RaiseCanExecuteChanged();
            SaveMacroCommand.RaiseCanExecuteChanged();
            DeleteMacroCommand.RaiseCanExecuteChanged();
            RunMacroCommand.RaiseCanExecuteChanged();
            CopyMacroCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
