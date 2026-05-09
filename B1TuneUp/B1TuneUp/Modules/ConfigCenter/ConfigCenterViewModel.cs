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
        private readonly ObservableCollection<MetadataDefinitionEntry> _metadata = new ObservableCollection<MetadataDefinitionEntry>();
        private readonly ObservableCollection<PackagePreviewEntry> _packagePreview = new ObservableCollection<PackagePreviewEntry>();
        private readonly ObservableCollection<UniversalFunctionEntry> _functions = new ObservableCollection<UniversalFunctionEntry>();
        private readonly ObservableCollection<AuthorizationGroupEntry> _groups = new ObservableCollection<AuthorizationGroupEntry>();
        private readonly ObservableCollection<UnifiedTriggerEntry> _triggers = new ObservableCollection<UnifiedTriggerEntry>();
        private readonly ObservableCollection<SapUserEntry> _sapUsers = new ObservableCollection<SapUserEntry>();
        private ModuleConfigurationEntry _selectedModule;
        private UniversalFunctionEntry _selectedFunction;
        private AuthorizationGroupEntry _selectedGroup;
        private UnifiedTriggerEntry _selectedTrigger;
        private SapUserEntry _selectedSapUser;
        private string _simulationResult;
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
            PreviewImportCommand = new RelayCommand(async () => await PreviewImportAsync());
            NewFunctionCommand = new RelayCommand(NewFunction);
            SaveFunctionCommand = new RelayCommand(async () => await SaveFunctionAsync(), () => SelectedFunction != null);
            DeleteFunctionCommand = new RelayCommand(async () => await DeleteFunctionAsync(), () => SelectedFunction != null);
            TestFunctionCommand = new RelayCommand(async () => await TestFunctionAsync(), () => SelectedFunction != null);
            NewGroupCommand = new RelayCommand(NewGroup);
            SaveGroupCommand = new RelayCommand(async () => await SaveGroupAsync(), () => SelectedGroup != null);
            DeleteGroupCommand = new RelayCommand(async () => await DeleteGroupAsync(), () => SelectedGroup != null);
            SaveSuperUsersCommand = new RelayCommand(async () => await SaveSuperUsersAsync());
            NewTriggerCommand = new RelayCommand(NewTrigger);
            SaveTriggerCommand = new RelayCommand(async () => await SaveTriggerAsync(), () => SelectedTrigger != null);
            DeleteTriggerCommand = new RelayCommand(async () => await DeleteTriggerAsync(), () => SelectedTrigger != null);
            SimulateAuthorizationCommand = new RelayCommand(SimulateAuthorization, () => SelectedSapUser != null);
        }

        public ObservableCollection<ModuleConfigurationEntry> Modules => _modules;
        public ObservableCollection<ConfigurationDiagnosticEntry> Diagnostics => _diagnostics;
        public ObservableCollection<MetadataDefinitionEntry> Metadata => _metadata;
        public ObservableCollection<PackagePreviewEntry> PackagePreview => _packagePreview;
        public ObservableCollection<UniversalFunctionEntry> Functions => _functions;
        public ObservableCollection<AuthorizationGroupEntry> Groups => _groups;
        public ObservableCollection<UnifiedTriggerEntry> Triggers => _triggers;
        public ObservableCollection<SapUserEntry> SapUsers => _sapUsers;
        public string[] FunctionTypes => UniversalFunctionService.SupportedTypes;
        public string[] TriggerEvents => UnifiedTriggerService.SupportedEvents;

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

        public UnifiedTriggerEntry SelectedTrigger
        {
            get => _selectedTrigger;
            set
            {
                if (_selectedTrigger == value) return;
                _selectedTrigger = value;
                OnPropertyChanged();
                SaveTriggerCommand.RaiseCanExecuteChanged();
                DeleteTriggerCommand.RaiseCanExecuteChanged();
            }
        }

        public SapUserEntry SelectedSapUser
        {
            get => _selectedSapUser;
            set
            {
                if (_selectedSapUser == value) return;
                _selectedSapUser = value;
                OnPropertyChanged();
                SimulateAuthorizationCommand.RaiseCanExecuteChanged();
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

        public string SimulationResult
        {
            get => _simulationResult;
            set { if (_simulationResult == value) return; _simulationResult = value; OnPropertyChanged(); }
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
        public RelayCommand PreviewImportCommand { get; }
        public RelayCommand NewFunctionCommand { get; }
        public RelayCommand SaveFunctionCommand { get; }
        public RelayCommand DeleteFunctionCommand { get; }
        public RelayCommand TestFunctionCommand { get; }
        public RelayCommand NewGroupCommand { get; }
        public RelayCommand SaveGroupCommand { get; }
        public RelayCommand DeleteGroupCommand { get; }
        public RelayCommand SaveSuperUsersCommand { get; }
        public RelayCommand NewTriggerCommand { get; }
        public RelayCommand SaveTriggerCommand { get; }
        public RelayCommand DeleteTriggerCommand { get; }
        public RelayCommand SimulateAuthorizationCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunAsync("Cargando centro de configuracion...", () =>
            {
                Replace(_modules, ModuleActivationService.GetAll().Select(m => m.Clone()));
                Replace(_diagnostics, ConfigurationCenterService.RunDiagnostics());
                Replace(_metadata, MetadataRegistryService.Validate());
                Replace(_packagePreview, Enumerable.Empty<PackagePreviewEntry>());
                Replace(_functions, UniversalFunctionService.GetAll().Select(f => f.Clone()));
                Replace(_groups, AuthorizationAdminService.GetGroups().Select(g => g.Clone()));
                Replace(_triggers, UnifiedTriggerService.GetAll().Select(t => t.Clone()));
                Replace(_sapUsers, AuthorizationAdminService.GetSapUsers());
                SuperUsers = AuthorizationAdminService.GetSuperUsers();
                SelectedModule = Modules.FirstOrDefault();
                SelectedFunction = Functions.FirstOrDefault();
                SelectedGroup = Groups.FirstOrDefault();
                SelectedTrigger = Triggers.FirstOrDefault();
                SelectedSapUser = SapUsers.FirstOrDefault();
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
                Replace(_metadata, MetadataRegistryService.Validate());
                StatusMessage = "Metadata verificada y reparada.";
            });
        }

        private async Task PreviewImportAsync()
        {
            var dialog = new OpenFileDialog { Filter = "B1TuneUp package (*.json)|*.json" };
            if (dialog.ShowDialog() != true) return;
            await RunAsync("Analizando paquete...", () =>
            {
                Replace(_packagePreview, ConfigurationCenterService.PreviewImport(dialog.FileName));
                StatusMessage = $"Preview listo: {PackagePreview.Count} cambios detectados.";
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

        private void NewTrigger()
        {
            var entry = new UnifiedTriggerEntry { Code = "NEW_TRIGGER", Name = "New Trigger", EventType = "FORM_LOAD" };
            Triggers.Add(entry);
            SelectedTrigger = entry;
        }

        private async Task SaveTriggerAsync()
        {
            if (SelectedTrigger == null) return;
            await RunAsync("Guardando trigger...", () =>
            {
                UnifiedTriggerService.Save(SelectedTrigger);
                StatusMessage = $"Trigger '{SelectedTrigger.Code}' guardado.";
            });
        }

        private async Task DeleteTriggerAsync()
        {
            if (SelectedTrigger == null) return;
            if (MessageBox.Show($"Eliminar trigger '{SelectedTrigger.Code}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var trigger = SelectedTrigger;
            await RunAsync("Eliminando trigger...", () =>
            {
                UnifiedTriggerService.Delete(trigger.Code);
                Triggers.Remove(trigger);
                SelectedTrigger = Triggers.FirstOrDefault();
                StatusMessage = "Trigger eliminado.";
            });
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

        private void SimulateAuthorization()
        {
            if (SelectedSapUser == null) return;
            var fn = SelectedFunction;
            var result = AuthorizationAdminService.Simulate(
                SelectedSapUser.UserCode,
                fn == null ? "Module" : "UniversalFunction",
                fn == null ? SelectedModule?.Key : fn.Code,
                fn == null ? SelectedModule?.AllowedUsers : fn.AllowedUsers,
                fn == null ? SelectedModule?.AllowedGroups : fn.AllowedGroups,
                fn == null ? SelectedModule?.DeniedUsers : fn.DeniedUsers,
                fn == null ? SelectedModule?.DeniedGroups : fn.DeniedGroups);
            SimulationResult = $"{result.UserCode}: {(result.Allowed ? "Allowed" : "Denied")} - {result.Detail}";
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
