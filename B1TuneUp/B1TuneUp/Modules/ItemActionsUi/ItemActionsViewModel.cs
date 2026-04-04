using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using ModulesRoot = B1TuneUp.Modules;
using SAPbouiCOM;

namespace B1TuneUp.Modules.ItemActionsUi
{
    public class ItemActionsViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ItemActionEntry> _entries = new ObservableCollection<ItemActionEntry>();
        private readonly Dispatcher _dispatcher;
        private readonly ICollectionView _entriesView;
        private ItemActionEntry _selectedEntry;
        private string _searchTerm;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        private readonly string _initialFormFilter;
        private readonly string _initialItemId;

        public ItemActionsViewModel(string formFilter = null, string itemId = null)
        {
            _initialFormFilter = formFilter;
            _initialItemId = itemId;

            _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _entriesView = CollectionViewSource.GetDefaultView(_entries);
            _entriesView.Filter = FilterEntry;

            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !IsBusy);
            NewCommand = new RelayCommand(NewEntry);
            DuplicateCommand = new RelayCommand(DuplicateEntry, () => SelectedEntry != null);
            SaveCommand = new RelayCommand(async () => await SaveSelectedAsync(), () => SelectedEntry != null);
            DeleteCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedEntry != null && SelectedEntry.DocEntry > 0);

            EventOptions = new List<string> { "Change", "ItemPressed", "DoubleClick", "LostFocus", "GotFocus", "FormLoad" };

            if (!string.IsNullOrWhiteSpace(formFilter) || !string.IsNullOrWhiteSpace(itemId))
            {
                SearchTerm = $"{formFilter} {itemId}".Trim();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICollectionView EntriesView => _entriesView;

        public IList<string> EventOptions { get; }

        public ItemActionEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry == value) return;
                _selectedEntry = value;
                OnPropertyChanged(nameof(SelectedEntry));
                RaiseCommandStates();
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm == value) return;
                _searchTerm = value;
                OnPropertyChanged(nameof(SearchTerm));
                _entriesView.Refresh();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                RaiseCommandStates();
            }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set
            {
                if (_busyMessage == value) return;
                _busyMessage = value;
                OnPropertyChanged(nameof(BusyMessage));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public async Task LoadAsync()
        {
            await RunSafeAsync(async () =>
            {
                var list = await Task.Run(ModulesRoot.ItemActionService.GetAll);
                await _dispatcher.InvokeAsync(() =>
                {
                    _entries.Clear();
                    foreach (var entry in list)
                    {
                        _entries.Add(entry);
                    }
                    if (_entries.Count == 0)
                    {
                        NewEntry();
                    }
                    else
                    {
                        SelectedEntry = TrySelectInitialEntry();
                    }
                }, DispatcherPriority.DataBind);
                StatusMessage = $"Total acciones: {_entries.Count}";
            }, "Cargando acciones...");
        }

        private ItemActionEntry TrySelectInitialEntry()
        {
            if (!string.IsNullOrWhiteSpace(_initialItemId))
            {
                var match = _entries.FirstOrDefault(e => string.Equals(e.ItemId, _initialItemId, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            if (!string.IsNullOrWhiteSpace(_initialFormFilter))
            {
                var match = _entries.FirstOrDefault(e => string.Equals(e.FormType, _initialFormFilter, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return _entries.FirstOrDefault();
        }

        private void NewEntry()
        {
            var activeFormType = B1App.Instance?.Application?.Forms?.ActiveForm?.TypeEx ?? string.Empty;
            var entry = new ItemActionEntry
            {
                FormType = activeFormType,
                Event = EventOptions.FirstOrDefault() ?? "Change"
            };
            _entries.Add(entry);
            SelectedEntry = entry;
        }

        private void DuplicateEntry()
        {
            if (SelectedEntry == null) return;
            var clone = SelectedEntry.Clone();
            clone.DocEntry = 0;
            _entries.Add(clone);
            SelectedEntry = clone;
            StatusMessage = "Acción duplicada. Ajusta los datos y guarda.";
        }

        private async Task SaveSelectedAsync()
        {
            if (SelectedEntry == null) return;
            await RunSafeAsync(async () =>
            {
                await Task.Run(() => ModulesRoot.ItemActionService.Save(SelectedEntry));
                StatusMessage = $"Acción para {SelectedEntry.ItemId} guardada.";
            }, "Guardando acción...");
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedEntry == null || SelectedEntry.DocEntry <= 0) return;
            await RunSafeAsync(async () =>
            {
                var docEntry = SelectedEntry.DocEntry;
                await Task.Run(() => ModulesRoot.ItemActionService.Delete(docEntry));
                await _dispatcher.InvokeAsync(() =>
                {
                    _entries.Remove(SelectedEntry);
                    SelectedEntry = _entries.FirstOrDefault();
                    StatusMessage = "Acción eliminada.";
                }, DispatcherPriority.DataBind);
            }, "Eliminando acción...");
        }

        private bool FilterEntry(object obj)
        {
            var entry = obj as ItemActionEntry;
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(SearchTerm)) return true;
            var term = SearchTerm.Trim();
            return (entry.FormType?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (entry.ItemId?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (entry.Action?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private async Task RunSafeAsync(Func<Task> work, string busyMessage)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                BusyMessage = busyMessage;
                await work();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                B1App.Instance?.Application?.SetStatusBarMessage($"ItemActions: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
            finally
            {
                BusyMessage = string.Empty;
                IsBusy = false;
            }
        }

        private void RaiseCommandStates()
        {
            RefreshCommand.RaiseCanExecuteChanged();
            DuplicateCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
