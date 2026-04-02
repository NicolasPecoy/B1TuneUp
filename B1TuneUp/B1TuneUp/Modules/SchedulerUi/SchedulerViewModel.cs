using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.SchedulerUi
{
    public class SchedulerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SchedulerEntry> Jobs { get; } = new ObservableCollection<SchedulerEntry>();

        private SchedulerEntry _selectedJob;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private string _searchFilter;

        public SchedulerViewModel()
        {
            NewCommand = new RelayCommand(NewJob);
            DuplicateCommand = new RelayCommand(DuplicateJob, () => SelectedJob != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedJob != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedJob != null);
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            RunNowCommand = new RelayCommand(RunNow, () => SelectedJob != null);
            ToggleActiveCommand = new RelayCommand(ToggleActive, () => SelectedJob != null);
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand RunNowCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }

        public SchedulerEntry SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (_selectedJob == value) return;
                _selectedJob = value;
                OnPropertyChanged();
                RaiseCommandStates();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando Scheduler...", async () =>
            {
                var jobs = await Task.Run(() => SchedulerService.GetAll());
                Jobs.Clear();
                foreach (var job in jobs.OrderBy(j => j.Name))
                {
                    Jobs.Add(job);
                }
                if (Jobs.Count == 0)
                {
                    SelectedJob = CreateDefaultJob();
                }
                else if (SelectedJob == null)
                {
                    SelectedJob = Jobs.First();
                }
                ApplyFilter();
            });
        }

        private SchedulerEntry CreateDefaultJob()
        {
            return new SchedulerEntry
            {
                Name = "New Task",
                IntervalMinutes = 60,
                Active = true
            };
        }

        private void NewJob()
        {
            SelectedJob = CreateDefaultJob();
        }

        private void DuplicateJob()
        {
            if (SelectedJob == null) return;
            var clone = SelectedJob.Clone();
            clone.Code = null;
            clone.Name = $"{clone.Name} (copy)";
            Jobs.Add(clone);
            SelectedJob = clone;
        }

        private async Task SaveAsync()
        {
            if (SelectedJob == null) return;
            await RunSafeAsync("Guardando tarea...", async () =>
            {
                await Task.Run(() => SchedulerService.Save(SelectedJob));
                if (!Jobs.Contains(SelectedJob)) Jobs.Add(SelectedJob);
                StatusMessage = $"Tarea {SelectedJob.DisplayName} guardada.";
                ApplyFilter();
            });
        }

        private async Task DeleteAsync()
        {
            if (SelectedJob == null) return;
            var job = SelectedJob;
            await RunSafeAsync("Eliminando tarea...", async () =>
            {
                await Task.Run(() => SchedulerService.Delete(job.Code));
                Jobs.Remove(job);
                SelectedJob = Jobs.FirstOrDefault() ?? CreateDefaultJob();
                StatusMessage = "Tarea eliminada.";
            });
        }

        private void RunNow()
        {
            if (SelectedJob == null) return;
            SchedulerService.RunNow(SelectedJob);
            StatusMessage = $"Tarea {SelectedJob.DisplayName} ejecutada manualmente.";
        }

        private void ToggleActive()
        {
            if (SelectedJob == null) return;
            SelectedJob.Active = !SelectedJob.Active;
            SchedulerService.Save(SelectedJob);
            StatusMessage = SelectedJob.Active ? "Tarea activada." : "Tarea desactivada.";
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchFilter))
            {
                return;
            }

            var match = Jobs.FirstOrDefault(j =>
                (j.Name ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (j.Action ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) SelectedJob = match;
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
            DuplicateCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            RunNowCommand.RaiseCanExecuteChanged();
            ToggleActiveCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
