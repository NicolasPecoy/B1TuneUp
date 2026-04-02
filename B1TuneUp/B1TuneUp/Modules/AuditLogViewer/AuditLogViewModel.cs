using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.AuditLogViewer
{
    public class AuditLogViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AuditLogEntry> Entries { get; } = new ObservableCollection<AuditLogEntry>();

        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _typeFilter;
        private string _statusFilter;
        private string _userFilter;
        private string _searchText;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public AuditLogViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            CopyDetailsCommand = new RelayCommand(CopyDetails, () => SelectedEntry != null);
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand CopyDetailsCommand { get; }

        private AuditLogEntry _selectedEntry;
        public AuditLogEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry == value) return;
                _selectedEntry = value;
                OnPropertyChanged();
                CopyDetailsCommand.RaiseCanExecuteChanged();
            }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set { if (_fromDate == value) return; _fromDate = value; OnPropertyChanged(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { if (_toDate == value) return; _toDate = value; OnPropertyChanged(); }
        }

        public string TypeFilter
        {
            get => _typeFilter;
            set { if (_typeFilter == value) return; _typeFilter = value; OnPropertyChanged(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { if (_statusFilter == value) return; _statusFilter = value; OnPropertyChanged(); }
        }

        public string UserFilter
        {
            get => _userFilter;
            set { if (_userFilter == value) return; _userFilter = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { if (_searchText == value) return; _searchText = value; OnPropertyChanged(); }
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
            await RunSafeAsync("Cargando logs...", async () =>
            {
                var list = await Task.Run(() => AuditLogService.GetEntries(FromDate, ToDate, TypeFilter, StatusFilter, UserFilter));
                var filtered = string.IsNullOrEmpty(SearchText)
                    ? list
                    : list.Where(e => (e.Details ?? "").IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      (e.Type ?? "").IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
                Entries.Clear();
                foreach (var entry in filtered)
                {
                    Entries.Add(entry);
                }
                SelectedEntry = Entries.FirstOrDefault();
                StatusMessage = $"{Entries.Count} registros.";
            });
        }

        private void CopyDetails()
        {
            try
            {
                if (SelectedEntry == null) return;
                System.Windows.Clipboard.SetText(SelectedEntry.Details ?? string.Empty);
                StatusMessage = "Detalles copiados al portapapeles.";
            }
            catch { }
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
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
