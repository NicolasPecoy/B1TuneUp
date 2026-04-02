using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using B1TuneUp.Models;

namespace B1TuneUp.Modules.IntegrationUi
{
    public class IntegrationConfiguratorViewModel : INotifyPropertyChanged
    {
        private IntegrationConfig _selectedIntegration;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public ObservableCollection<IntegrationConfig> Integrations { get; } = new ObservableCollection<IntegrationConfig>();
        public IReadOnlyList<string> ChannelOptions { get; } = new[] { "REST", "SOAP" };
        public IReadOnlyList<string> HttpMethodOptions { get; } = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
        public IReadOnlyList<string> AuthModes { get; } = new[] { "None", "Basic", "Bearer", "ApiKey" };

        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand TestCommand { get; }
        public RelayCommand StartRealtimeCommand { get; }
        public RelayCommand StopRealtimeCommand { get; }

        public IntegrationConfiguratorViewModel()
        {
            NewCommand = new RelayCommand(NewIntegration);
            DuplicateCommand = new RelayCommand(DuplicateIntegration, () => SelectedIntegration != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedIntegration != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedIntegration != null);
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            TestCommand = new RelayCommand(async () => await TestAsync(), () => SelectedIntegration != null);
            StartRealtimeCommand = new RelayCommand(StartRealtime, () => CanStartRealtime);
            StopRealtimeCommand = new RelayCommand(StopRealtime, () => SelectedIntegration != null);
        }

        public IntegrationConfig SelectedIntegration
        {
            get => _selectedIntegration;
            set
            {
                if (_selectedIntegration == value) return;
                _selectedIntegration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartRealtime));
                RaiseCommandStates();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (value != _isBusy) { _isBusy = value; OnPropertyChanged(); } }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set { if (value != _busyMessage) { _busyMessage = value; OnPropertyChanged(); } }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (value != _statusMessage) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public bool CanStartRealtime =>
            SelectedIntegration != null &&
            SelectedIntegration.Active &&
            SelectedIntegration.ScheduleMinutes.HasValue &&
            SelectedIntegration.ScheduleMinutes.Value > 0 &&
            !string.IsNullOrWhiteSpace(SelectedIntegration.HandlerMacro);

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando configuraciones...", () =>
            {
                Integrations.Clear();
                foreach (var cfg in IntegrationConfigService.GetAll().OrderBy(c => c.DisplayName))
                {
                    Integrations.Add(cfg);
                }
                SortList();
                if (Integrations.Count == 0)
                {
                    SelectedIntegration = CreateNewIntegration();
                }
                else if (SelectedIntegration == null)
                {
                    SelectedIntegration = Integrations.First();
                }
                return Task.CompletedTask;
            });
        }

        private void NewIntegration() => SelectedIntegration = CreateNewIntegration();

        private void DuplicateIntegration()
        {
            if (SelectedIntegration == null) return;
            var copy = SelectedIntegration.Clone();
            copy.Code = null;
            copy.Name = $"{copy.Name} (Copy)";
            SelectedIntegration = copy;
        }

        private async Task SaveAsync()
        {
            if (SelectedIntegration == null) return;
            await RunSafeAsync("Guardando configuración...", () =>
            {
                IntegrationConfigService.Save(SelectedIntegration);
                var existing = Integrations.FirstOrDefault(x => x.Code == SelectedIntegration.Code);
                if (existing == null)
                {
                    Integrations.Add(SelectedIntegration);
                }
                StatusMessage = $"Configuración '{SelectedIntegration.DisplayName}' guardada.";
                return Task.CompletedTask;
            });
            SortList();
        }

        private async Task DeleteAsync()
        {
            if (SelectedIntegration == null) return;
            var current = SelectedIntegration;
            await RunSafeAsync("Eliminando configuración...", () =>
            {
                if (!string.IsNullOrEmpty(current.Code))
                {
                    IntegrationConfigService.Delete(current.Code);
                }
                return Task.CompletedTask;
            });
            if (Integrations.Contains(current)) Integrations.Remove(current);
            SelectedIntegration = Integrations.FirstOrDefault() ?? CreateNewIntegration();
            StatusMessage = $"Configuración '{current.DisplayName}' eliminada.";
        }

        private async Task TestAsync()
        {
            if (SelectedIntegration == null) return;
            await RunSafeAsync("Ejecutando prueba en vivo...", async () =>
            {
                var result = await IntegrationConfigService.TestConnectionAsync(SelectedIntegration);
                SelectedIntegration.LastTestResult = result;
                StatusMessage = result.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase)
                    ? $"Error: {result}"
                    : "Prueba ejecutada correctamente.";
            });
        }

        private void StartRealtime()
        {
            if (!CanStartRealtime) return;
            try
            {
                IntegrationConfigService.StartRealtimeMonitor(SelectedIntegration);
                StatusMessage = "Monitoreo en tiempo real iniciado.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error iniciando monitoreo: {ex.Message}";
            }
        }

        private void StopRealtime()
        {
            if (SelectedIntegration?.Code == null) return;
            try
            {
                IntegrationConfigService.StopRealtimeMonitor(SelectedIntegration.Code);
                StatusMessage = "Monitoreo detenido.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deteniendo monitoreo: {ex.Message}";
            }
        }

        private IntegrationConfig CreateNewIntegration()
        {
            return new IntegrationConfig
            {
                Name = "Nueva Integración",
                Channel = "REST",
                Method = "GET",
                AuthMode = "None",
                Active = true,
                ScheduleMinutes = 0
            };
        }

        private async Task RunSafeAsync(string busyMessage, Func<Task> work)
        {
            try
            {
                IsBusy = true;
                BusyMessage = busyMessage;
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
                RaiseCommandStates();
            }
        }

        private void SortList()
        {
            var ordered = Integrations.OrderBy(c => c.DisplayName).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var current = ordered[i];
                var existingIndex = Integrations.IndexOf(current);
                if (existingIndex != i && existingIndex >= 0)
                {
                    Integrations.Move(existingIndex, i);
                }
            }
        }

        private void RaiseCommandStates()
        {
            DuplicateCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            TestCommand.RaiseCanExecuteChanged();
            StartRealtimeCommand.RaiseCanExecuteChanged();
            StopRealtimeCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
