using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.ProcessDesigner
{
    public class ProcessDesignerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ProcessDefinition> Processes { get; } = new ObservableCollection<ProcessDefinition>();

        private ProcessDefinition _selectedProcess;
        private ProcessStepDefinition _selectedStep;
        private string _formFilter;
        private string _searchFilter;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public ProcessDesignerViewModel()
        {
            NewProcessCommand = new RelayCommand(NewProcess);
            DuplicateProcessCommand = new RelayCommand(DuplicateProcess, () => SelectedProcess != null);
            SaveProcessCommand = new RelayCommand(async () => await SaveProcessAsync(), () => SelectedProcess != null);
            DeleteProcessCommand = new RelayCommand(async () => await DeleteProcessAsync(), () => SelectedProcess != null);
            RefreshCommand = new RelayCommand(async () => await LoadAsync());

            AddStepCommand = new RelayCommand(AddStep, () => SelectedProcess != null);
            DuplicateStepCommand = new RelayCommand(DuplicateStep, () => SelectedStep != null);
            DeleteStepCommand = new RelayCommand(DeleteStep, () => SelectedStep != null);
        }

        public RelayCommand NewProcessCommand { get; }
        public RelayCommand DuplicateProcessCommand { get; }
        public RelayCommand SaveProcessCommand { get; }
        public RelayCommand DeleteProcessCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddStepCommand { get; }
        public RelayCommand DuplicateStepCommand { get; }
        public RelayCommand DeleteStepCommand { get; }

        public ProcessDefinition SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (_selectedProcess == value) return;
                _selectedProcess = value;
                SelectedStep = value != null && value.Steps.Count > 0 ? value.Steps[0] : null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSteps));
                RaiseCommandStates();
            }
        }

        public ObservableCollection<ProcessStepDefinition> SelectedSteps => SelectedProcess?.Steps;

        public ProcessStepDefinition SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (_selectedStep == value) return;
                _selectedStep = value;
                OnPropertyChanged();
                RaiseCommandStates();
            }
        }

        public string FormFilter
        {
            get => _formFilter;
            set
            {
                if (_formFilter == value) return;
                _formFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter == value) return;
                _searchFilter = value;
                OnPropertyChanged();
                ApplyFilter();
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

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando procesos...", async () =>
            {
                var items = await Task.Run(() => ProcessDesignerService.GetAll());
                Processes.Clear();
                foreach (var process in items)
                {
                    Processes.Add(process);
                }
                SelectedProcess = Processes.FirstOrDefault();
            });
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrEmpty(FormFilter) && string.IsNullOrEmpty(SearchFilter))
            {
                return;
            }

            var match = Processes.FirstOrDefault(p =>
                (string.IsNullOrEmpty(FormFilter) || (p.FormType ?? "").IndexOf(FormFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (string.IsNullOrEmpty(SearchFilter) || (p.Name ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0));

            if (match != null) SelectedProcess = match;
        }

        private void NewProcess()
        {
            var process = new ProcessDefinition
            {
                Name = "Nuevo Proceso",
                FormType = FormFilter,
                Active = true
            };
            process.Steps.Add(CreateDefaultStep(1));
            Processes.Add(process);
            SelectedProcess = process;
        }

        private ProcessStepDefinition CreateDefaultStep(int order)
        {
            return new ProcessStepDefinition
            {
                Order = order,
                Name = "Paso",
                Mandatory = false
            };
        }

        private void DuplicateProcess()
        {
            if (SelectedProcess == null) return;
            var clone = new ProcessDefinition
            {
                Name = SelectedProcess.Name + " (copy)",
                Description = SelectedProcess.Description,
                FormType = SelectedProcess.FormType,
                Active = SelectedProcess.Active,
                AutoShow = SelectedProcess.AutoShow
            };
            foreach (var step in SelectedProcess.Steps)
            {
                clone.Steps.Add(new ProcessStepDefinition
                {
                    Order = step.Order,
                    Name = step.Name,
                    Description = step.Description,
                    DoneCondition = step.DoneCondition,
                    Action = step.Action,
                    Mandatory = step.Mandatory
                });
            }
            Processes.Add(clone);
            SelectedProcess = clone;
        }

        private async Task SaveProcessAsync()
        {
            if (SelectedProcess == null) return;
            await RunSafeAsync("Guardando proceso...", async () =>
            {
                await Task.Run(() => ProcessDesignerService.Save(SelectedProcess));
                StatusMessage = "Proceso guardado correctamente.";
            });
        }

        private async Task DeleteProcessAsync()
        {
            if (SelectedProcess == null) return;
            var target = SelectedProcess;
            await RunSafeAsync("Eliminando proceso...", async () =>
            {
                await Task.Run(() => ProcessDesignerService.Delete(target.DocEntry ?? target.Code));
                Processes.Remove(target);
                SelectedProcess = Processes.FirstOrDefault();
                StatusMessage = "Proceso eliminado.";
            });
        }

        private void AddStep()
        {
            if (SelectedProcess == null) return;
            int order = SelectedProcess.Steps.Count > 0 ? SelectedProcess.Steps.Max(s => s.Order) + 1 : 1;
            var step = CreateDefaultStep(order);
            SelectedProcess.Steps.Add(step);
            SelectedStep = step;
        }

        private void DuplicateStep()
        {
            if (SelectedProcess == null || SelectedStep == null) return;
            var clone = new ProcessStepDefinition
            {
                Order = SelectedStep.Order + 1,
                Name = SelectedStep.Name + " (copy)",
                Description = SelectedStep.Description,
                DoneCondition = SelectedStep.DoneCondition,
                Action = SelectedStep.Action,
                Mandatory = SelectedStep.Mandatory
            };
            SelectedProcess.Steps.Add(clone);
            SelectedStep = clone;
        }

        private void DeleteStep()
        {
            if (SelectedProcess == null || SelectedStep == null) return;
            var idx = SelectedProcess.Steps.IndexOf(SelectedStep);
            SelectedProcess.Steps.Remove(SelectedStep);
            SelectedStep = SelectedProcess.Steps.Count > 0
                ? SelectedProcess.Steps[Math.Max(0, Math.Min(idx, SelectedProcess.Steps.Count - 1))]
                : null;
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
                RaiseCommandStates();
            }
        }

        private void RaiseCommandStates()
        {
            DuplicateProcessCommand.RaiseCanExecuteChanged();
            SaveProcessCommand.RaiseCanExecuteChanged();
            DeleteProcessCommand.RaiseCanExecuteChanged();
            AddStepCommand.RaiseCanExecuteChanged();
            DuplicateStepCommand.RaiseCanExecuteChanged();
            DeleteStepCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
