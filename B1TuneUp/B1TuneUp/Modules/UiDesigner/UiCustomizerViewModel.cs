using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.UiDesigner
{
    public class UiCustomizerViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<UiCustomizationEntry> _entries = new ObservableCollection<UiCustomizationEntry>();
        private readonly ICollectionView _entriesView;
        private UiCustomizationEntry _selectedEntry;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private string _formFilter;
        private string _searchFilter;
        private IReadOnlyList<string> _formTypes = Array.Empty<string>();

        public ObservableCollection<UiCustomizationEntry> Entries => _entries;
        public ICollectionView FilteredEntries => _entriesView;

        public IReadOnlyList<string> FormTypes
        {
            get => _formTypes;
            private set { _formTypes = value; OnPropertyChanged(); }
        }

        public IReadOnlyList<string> ActionOptions { get; } = new[]
        {
            "Hide","Move","Resize","ChangeLabel","Enable","Disable","AddButton","AddFolder","AddEditText"
        };

        public UiCustomizerViewModel()
        {
            _entriesView = CollectionViewSource.GetDefaultView(_entries);
            _entriesView.Filter = FilterEntries;

            NewCommand = new RelayCommand(NewEntry);
            DuplicateCommand = new RelayCommand(DuplicateEntry, () => SelectedEntry != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedEntry != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedEntry != null);
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            LaunchPlacementCommand = new RelayCommand(() => UICustomizerService.OpenItemPlacement());
            RefreshActiveFormCommand = new RelayCommand(() => UICustomizerService.RefreshActiveForm());
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand LaunchPlacementCommand { get; }
        public RelayCommand RefreshActiveFormCommand { get; }

        public UiCustomizationEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry == value) return;
                _selectedEntry = value;
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
                _entriesView.Refresh();
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
                _entriesView.Refresh();
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
            await RunSafeAsync("Cargando personalizaciones...", async () =>
            {
                var entries = await Task.Run(() => UICustomizerService.GetAll());
                var formTypes = await Task.Run(() => UICustomizerService.GetDistinctFormTypes());

                _entries.Clear();
                foreach (var entry in entries.OrderBy(e => e.FormType).ThenBy(e => e.ItemId))
                {
                    _entries.Add(entry);
                }
                FormTypes = formTypes;

                if (_entries.Count == 0)
                {
                    SelectedEntry = CreateDefaultEntry();
                }
                else if (SelectedEntry == null)
                {
                    SelectedEntry = _entries.FirstOrDefault();
                }
            });
        }

        private void NewEntry()
        {
            SelectedEntry = CreateDefaultEntry();
        }

        private void DuplicateEntry()
        {
            if (SelectedEntry == null) return;
            var copy = SelectedEntry.Clone();
            copy.Code = null;
            copy.Name = $"{copy.DisplayName} (copy)";
            _entries.Add(copy);
            SelectedEntry = copy;
        }

        private async Task SaveAsync()
        {
            if (SelectedEntry == null) return;
            await RunSafeAsync("Guardando cambios...", async () =>
            {
                await Task.Run(() => UICustomizerService.Save(SelectedEntry));
                StatusMessage = $"Personalización {SelectedEntry.DisplayName} guardada.";
                if (!_entries.Contains(SelectedEntry)) _entries.Add(SelectedEntry);
                _entriesView.Refresh();
            });
        }

        private async Task DeleteAsync()
        {
            if (SelectedEntry == null) return;
            var entry = SelectedEntry;
            await RunSafeAsync("Eliminando...", async () =>
            {
                await Task.Run(() => UICustomizerService.Delete(entry.Code));
                _entries.Remove(entry);
                SelectedEntry = _entries.FirstOrDefault() ?? CreateDefaultEntry();
                StatusMessage = "Personalización eliminada.";
            });
        }

        private UiCustomizationEntry CreateDefaultEntry()
        {
            return new UiCustomizationEntry
            {
                FormType = FormFilter,
                Action = "Hide",
                Name = "New customization"
            };
        }

        private bool FilterEntries(object obj)
        {
            var entry = obj as UiCustomizationEntry;
            if (entry == null) return false;
            bool matchForm = string.IsNullOrWhiteSpace(FormFilter) || (entry.FormType?.IndexOf(FormFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!matchForm) return false;
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            return (entry.ItemId ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (entry.Action ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (entry.Label ?? "").IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
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
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
