using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using Microsoft.Win32;

namespace B1TuneUp.Modules.ConfigCenter
{
    public class ConfigCenterViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ModuleConfigurationEntry> _modules = new ObservableCollection<ModuleConfigurationEntry>();
        private readonly ObservableCollection<ConfigurationDiagnosticEntry> _diagnostics = new ObservableCollection<ConfigurationDiagnosticEntry>();
        private readonly ObservableCollection<UniversalFunctionEntry> _functions = new ObservableCollection<UniversalFunctionEntry>();
        private readonly ObservableCollection<AuthorizationGroupEntry> _groups = new ObservableCollection<AuthorizationGroupEntry>();
        private ModuleConfigurationEntry _selectedModule;
        private UniversalFunctionEntry _selectedFunction;
        private AuthorizationGroupEntry _selectedGroup;
        private string _superUsers;
        private string _statusMessage;
        private bool _isBusy;

        public ConfigCenterViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            SaveModulesCommand = new RelayCommand(async () => await SaveModulesAsync());
            RepairMetadataCommand = new RelayCommand(async () => await RepairMetadataAsync());
            ExportCommand = new RelayCommand(async () => await ExportAsync());
            ImportCommand = new RelayCommand(async () => await ImportAsync());
            NewFunctionCommand = new RelayCommand(NewFunction);
            SaveFunctionCommand = new RelayCommand(async () => await SaveFunctionAsync(), () => SelectedFunction != null);
            DeleteFunctionCommand = new RelayCommand(async () => await DeleteFunctionAsync(), () => SelectedFunction != null);
            TestFunctionCommand = new RelayCommand(async () => await TestFunctionAsync(), () => SelectedFunction != null);
            NewGroupCommand = new RelayCommand(NewGroup);
            SaveGroupCommand = new RelayCommand(async () => await SaveGroupAsync(), () => SelectedGroup != null);
            DeleteGroupCommand = new RelayCommand(async () => await DeleteGroupAsync(), () => SelectedGroup != null);
            SaveSuperUsersCommand = new RelayCommand(async () => await SaveSuperUsersAsync());
        }

        public ObservableCollection<ModuleConfigurationEntry> Modules => _modules;
        public ObservableCollection<ConfigurationDiagnosticEntry> Diagnostics => _diagnostics;
        public ObservableCollection<UniversalFunctionEntry> Functions => _functions;
        public ObservableCollection<AuthorizationGroupEntry> Groups => _groups;
        public string[] FunctionTypes => UniversalFunctionService.SupportedTypes;

        public ModuleConfigurationEntry SelectedModule
        {
            get => _selectedModule;
            set { if (_selectedModule == value) return; _selectedModule = value; OnPropertyChanged(); }
        }

        public UniversalFunctionEntry SelectedFunction
        {
            get => _selectedFunction;
            set
            {
                if (_selectedFunction == value) return;
                _selectedFunction = value;
                OnPropertyChanged();
                SaveFunctionCommand.RaiseCanExecuteChanged();
                DeleteFunctionCommand.RaiseCanExecuteChanged();
                TestFunctionCommand.RaiseCanExecuteChanged();
            }
        }

        public AuthorizationGroupEntry SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup == value) return;
                _selectedGroup = value;
                OnPropertyChanged();
                SaveGroupCommand.RaiseCanExecuteChanged();
                DeleteGroupCommand.RaiseCanExecuteChanged();
            }
        }

        public string SuperUsers
        {
            get => _superUsers;
            set { if (_superUsers == value) return; _superUsers = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage == value) return; _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand SaveModulesCommand { get; }
        public RelayCommand RepairMetadataCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand ImportCommand { get; }
        public RelayCommand NewFunctionCommand { get; }
        public RelayCommand SaveFunctionCommand { get; }
        public RelayCommand DeleteFunctionCommand { get; }
        public RelayCommand TestFunctionCommand { get; }
        public RelayCommand NewGroupCommand { get; }
        public RelayCommand SaveGroupCommand { get; }
        public RelayCommand DeleteGroupCommand { get; }
        public RelayCommand SaveSuperUsersCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunAsync("Cargando centro de configuracion...", () =>
            {
                Replace(_modules, ModuleActivationService.GetAll().Select(m => m.Clone()));
                Replace(_diagnostics, ConfigurationCenterService.RunDiagnostics());
                Replace(_functions, UniversalFunctionService.GetAll().Select(f => f.Clone()));
                Replace(_groups, AuthorizationAdminService.GetGroups().Select(g => g.Clone()));
                SuperUsers = AuthorizationAdminService.GetSuperUsers();
                SelectedModule = Modules.FirstOrDefault();
                SelectedFunction = Functions.FirstOrDefault();
                SelectedGroup = Groups.FirstOrDefault();
                StatusMessage = "Configuracion cargada.";
            });
        }

        private async Task SaveModulesAsync()
        {
            await RunAsync("Guardando modulos...", () =>
            {
                foreach (var module in Modules) ModuleActivationService.Save(module);
                StatusMessage = "Modulos guardados.";
            });
        }

        private async Task RepairMetadataAsync()
        {
            await RunAsync("Reparando metadata...", () =>
            {
                ConfigurationCenterService.RepairMetadata();
                Replace(_diagnostics, ConfigurationCenterService.RunDiagnostics());
                StatusMessage = "Metadata verificada y reparada.";
            });
        }

        private async Task ExportAsync()
        {
            var dialog = new SaveFileDialog { Filter = "B1TuneUp package (*.json)|*.json", FileName = "b1tuneup-config-package.json" };
            if (dialog.ShowDialog() != true) return;
            await RunAsync("Exportando paquete...", () =>
            {
                ConfigurationCenterService.ExportPackage(dialog.FileName);
                StatusMessage = $"Paquete exportado: {dialog.FileName}";
            });
        }

        private async Task ImportAsync()
        {
            var dialog = new OpenFileDialog { Filter = "B1TuneUp package (*.json)|*.json" };
            if (dialog.ShowDialog() != true) return;
            await RunAsync("Importando paquete...", () =>
            {
                ConfigurationCenterService.ImportPackage(dialog.FileName);
                StatusMessage = $"Paquete importado: {dialog.FileName}";
            });
            await LoadAsync();
        }

        private void NewFunction()
        {
            var entry = new UniversalFunctionEntry { Code = "NEW_UF", Name = "New Universal Function", Type = "Macro" };
            Functions.Add(entry);
            SelectedFunction = entry;
        }

        private async Task SaveFunctionAsync()
        {
            if (SelectedFunction == null) return;
            await RunAsync("Guardando Universal Function...", () =>
            {
                UniversalFunctionService.Save(SelectedFunction);
                StatusMessage = $"Universal Function '{SelectedFunction.Code}' guardada.";
            });
        }

        private async Task DeleteFunctionAsync()
        {
            if (SelectedFunction == null) return;
            if (MessageBox.Show($"Eliminar Universal Function '{SelectedFunction.Code}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var entry = SelectedFunction;
            await RunAsync("Eliminando Universal Function...", () =>
            {
                UniversalFunctionService.Delete(entry.Code);
                Functions.Remove(entry);
                SelectedFunction = Functions.FirstOrDefault();
                StatusMessage = "Universal Function eliminada.";
            });
        }

        private async Task TestFunctionAsync()
        {
            if (SelectedFunction == null) return;
            await RunAsync("Ejecutando Universal Function...", () =>
            {
                UniversalFunctionService.Save(SelectedFunction);
                string result = UniversalFunctionService.Execute(SelectedFunction.Code);
                StatusMessage = string.IsNullOrWhiteSpace(result) ? "Universal Function ejecutada." : result;
            });
        }

        private void NewGroup()
        {
            var entry = new AuthorizationGroupEntry { Code = "NEW_GROUP", Name = "New Group" };
            Groups.Add(entry);
            SelectedGroup = entry;
        }

        private async Task SaveGroupAsync()
        {
            if (SelectedGroup == null) return;
            await RunAsync("Guardando grupo...", () =>
            {
                AuthorizationAdminService.SaveGroup(SelectedGroup);
                StatusMessage = $"Grupo '{SelectedGroup.Code}' guardado.";
            });
        }

        private async Task DeleteGroupAsync()
        {
            if (SelectedGroup == null) return;
            if (MessageBox.Show($"Eliminar grupo '{SelectedGroup.Code}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var group = SelectedGroup;
            await RunAsync("Eliminando grupo...", () =>
            {
                AuthorizationAdminService.DeleteGroup(group.Code);
                Groups.Remove(group);
                SelectedGroup = Groups.FirstOrDefault();
                StatusMessage = "Grupo eliminado.";
            });
        }

        private async Task SaveSuperUsersAsync()
        {
            await RunAsync("Guardando superusuarios...", () =>
            {
                AuthorizationAdminService.SaveSuperUsers(SuperUsers);
                StatusMessage = "Superusuarios actualizados.";
            });
        }

        private async Task RunAsync(string busyMessage, Action action)
        {
            try
            {
                IsBusy = true;
                StatusMessage = busyMessage;
                await Task.Run(action);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                MessageBox.Show(ex.Message, "B1TuneUp", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> values)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                target.Clear();
                foreach (var value in values) target.Add(value);
            });
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
