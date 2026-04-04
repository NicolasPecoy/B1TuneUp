using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using B1TuneUp.Models;
using B1TuneUp.Modules;
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
        private readonly ObservableCollection<ActionPadEntry> _pads = new ObservableCollection<ActionPadEntry>();
        private readonly ListCollectionView _padsView;
        private ActionPadEntry _selectedPad;
        private ActionPadButtonEntry _selectedPadButton;
        private string _padSearch;

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
            _padsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_pads);
            _padsView.Filter = FilterPads;

            NewCommand = new RelayCommand(NewEntry);
            DuplicateCommand = new RelayCommand(DuplicateEntry, () => SelectedEntry != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedEntry != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedEntry != null);
            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            LaunchPlacementCommand = new RelayCommand(() => UICustomizerService.OpenItemPlacement());
            RefreshActiveFormCommand = new RelayCommand(() => UICustomizerService.RefreshActiveForm());

            PadNewCommand = new RelayCommand(NewPad);
            PadDuplicateCommand = new RelayCommand(DuplicatePad, () => SelectedPad != null);
            PadSaveCommand = new RelayCommand(async () => await SavePadAsync(), () => SelectedPad != null);
            PadDeleteCommand = new RelayCommand(async () => await DeletePadAsync(), () => SelectedPad != null);
            PadRefreshCommand = new RelayCommand(async () => await RefreshPadsAsync());
            PadAddButtonCommand = new RelayCommand(AddPadButton, () => SelectedPad != null);
            PadDuplicateButtonCommand = new RelayCommand(DuplicatePadButton, () => SelectedPadButton != null);
            PadRemoveButtonCommand = new RelayCommand(RemovePadButton, () => SelectedPadButton != null);
            PadMoveButtonUpCommand = new RelayCommand(() => MovePadButton(-1), () => CanMovePadButton(-1));
            PadMoveButtonDownCommand = new RelayCommand(() => MovePadButton(1), () => CanMovePadButton(1));
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand LaunchPlacementCommand { get; }
        public RelayCommand RefreshActiveFormCommand { get; }
        public RelayCommand PadNewCommand { get; }
        public RelayCommand PadDuplicateCommand { get; }
        public RelayCommand PadSaveCommand { get; }
        public RelayCommand PadDeleteCommand { get; }
        public RelayCommand PadRefreshCommand { get; }
        public RelayCommand PadAddButtonCommand { get; }
        public RelayCommand PadDuplicateButtonCommand { get; }
        public RelayCommand PadRemoveButtonCommand { get; }
        public RelayCommand PadMoveButtonUpCommand { get; }
        public RelayCommand PadMoveButtonDownCommand { get; }

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

        public ICollectionView PadsView => _padsView;

        public string PadSearch
        {
            get => _padSearch;
            set
            {
                if (_padSearch == value) return;
                _padSearch = value;
                OnPropertyChanged();
                _padsView.Refresh();
            }
        }

        public ActionPadEntry SelectedPad
        {
            get => _selectedPad;
            set
            {
                if (_selectedPad == value) return;
                _selectedPad = value;
                OnPropertyChanged();
                SelectedPadButton = _selectedPad?.Buttons?.FirstOrDefault();
                RaiseCommandStates();
            }
        }

        public ActionPadButtonEntry SelectedPadButton
        {
            get => _selectedPadButton;
            set
            {
                if (_selectedPadButton == value) return;
                _selectedPadButton = value;
                OnPropertyChanged();
                RaiseCommandStates();
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
                var entriesTask = Task.Run(() => UICustomizerService.GetAll());
                var formTypesTask = Task.Run(() => UICustomizerService.GetDistinctFormTypes());
                var padsTask = Task.Run(() => ActionPadService.GetAll());

                await Task.WhenAll(entriesTask, formTypesTask, padsTask);
                var entries = entriesTask.Result;
                var formTypes = formTypesTask.Result;
                var pads = padsTask.Result;

                _entries.Clear();
                foreach (var entry in entries.OrderBy(e => e.FormType).ThenBy(e => e.ItemId))
                {
                    _entries.Add(entry);
                }
                FormTypes = formTypes;
                ApplyPadResults(pads);

                if (_entries.Count == 0)
                {
                    SelectedEntry = CreateDefaultEntry();
                }
                else if (SelectedEntry == null)
                {
                    SelectedEntry = _entries.FirstOrDefault();
                }

                if (_pads.Count == 0 && SelectedPad == null)
                {
                    SelectedPad = CreateDefaultPad();
                }

                StatusMessage = $"{_entries.Count} personalizaciones UI · {_pads.Count} Action Pads disponibles.";
            });
        }

        private async Task RefreshPadsAsync()
        {
            await RunSafeAsync("Actualizando Action Pads...", async () =>
            {
                var pads = await Task.Run(() => ActionPadService.GetAll());
                ApplyPadResults(pads);
                if (_pads.Count == 0)
                {
                    SelectedPad = CreateDefaultPad();
                }
                StatusMessage = $"{_pads.Count} Action Pads sincronizados.";
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
                StatusMessage = $"PersonalizaciÃ³n {SelectedEntry.DisplayName} guardada.";
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
                StatusMessage = "PersonalizaciÃ³n eliminada.";
            });
        }

        private void NewPad()
        {
            var pad = CreateDefaultPad();
            _pads.Add(pad);
            SelectedPad = pad;
            _padsView.Refresh();
        }

        private void DuplicatePad()
        {
            if (SelectedPad == null) return;
            var copy = SelectedPad.Clone();
            copy.DocEntry = 0;
            copy.Title = $"{copy.Title} (copy)";
            foreach (var button in copy.Buttons)
            {
                button.DocEntry = 0;
            }
            _pads.Add(copy);
            SelectedPad = copy;
            _padsView.Refresh();
        }

        private async Task SavePadAsync()
        {
            if (SelectedPad == null) return;
            await RunSafeAsync("Guardando Action Pad...", async () =>
            {
                await Task.Run(() => ActionPadService.Save(SelectedPad));
                if (!_pads.Contains(SelectedPad)) _pads.Add(SelectedPad);
                _padsView.Refresh();
                StatusMessage = $"Action Pad '{SelectedPad.Title}' guardado.";
            });
        }

        private async Task DeletePadAsync()
        {
            if (SelectedPad == null) return;
            var target = SelectedPad;
            await RunSafeAsync("Eliminando Action Pad...", async () =>
            {
                if (target.DocEntry > 0)
                {
                    await Task.Run(() => ActionPadService.Delete(target.DocEntry));
                }
                _pads.Remove(target);
                SelectedPad = _pads.FirstOrDefault() ?? CreateDefaultPad();
                StatusMessage = "Action Pad eliminado.";
            });
        }

        private void AddPadButton()
        {
            if (SelectedPad == null)
            {
                SelectedPad = CreateDefaultPad();
                _pads.Add(SelectedPad);
            }
            var button = CreateDefaultButton(GetNextButtonOrder(SelectedPad));
            SelectedPad.Buttons.Add(button);
            SelectedPadButton = button;
            RecalculateButtonOrders(SelectedPad);
        }

        private void DuplicatePadButton()
        {
            if (SelectedPadButton == null || SelectedPad == null) return;
            var copy = SelectedPadButton.Clone();
            copy.DocEntry = 0;
            copy.Order = GetNextButtonOrder(SelectedPad);
            SelectedPad.Buttons.Add(copy);
            SelectedPadButton = copy;
            RecalculateButtonOrders(SelectedPad);
        }

        private void RemovePadButton()
        {
            if (SelectedPadButton == null || SelectedPad == null) return;
            var buttons = SelectedPad.Buttons;
            buttons.Remove(SelectedPadButton);
            if (buttons.Count == 0)
            {
                var fallback = CreateDefaultButton(10);
                buttons.Add(fallback);
                SelectedPadButton = fallback;
            }
            else
            {
                SelectedPadButton = buttons.FirstOrDefault();
            }
            RecalculateButtonOrders(SelectedPad);
        }

        private void MovePadButton(int delta)
        {
            if (!CanMovePadButton(delta) || SelectedPad == null || SelectedPadButton == null) return;
            var buttons = SelectedPad.Buttons;
            int index = buttons.IndexOf(SelectedPadButton);
            int newIndex = index + delta;
            buttons.Move(index, newIndex);
            SelectedPadButton = buttons[newIndex];
            RecalculateButtonOrders(SelectedPad);
        }

        private bool CanMovePadButton(int delta)
        {
            if (SelectedPad == null || SelectedPadButton == null) return false;
            var buttons = SelectedPad.Buttons;
            int index = buttons.IndexOf(SelectedPadButton);
            int newIndex = index + delta;
            return newIndex >= 0 && newIndex < buttons.Count;
        }

        private int GetNextButtonOrder(ActionPadEntry pad)
        {
            if (pad?.Buttons == null || pad.Buttons.Count == 0) return 10;
            int maxOrder = pad.Buttons.Max(b => b.Order);
            return maxOrder + 10;
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

        private ActionPadEntry CreateDefaultPad()
        {
            var pad = new ActionPadEntry
            {
                FormType = FormFilter,
                Title = "Nuevo Action Pad",
                Position = "Right",
                Columns = 2,
                ButtonWidth = 140,
                ButtonHeight = 28,
                DockMode = "Floating",
                FollowForm = true
            };
            pad.Buttons.Add(CreateDefaultButton(10));
            return pad;
        }

        private ActionPadButtonEntry CreateDefaultButton(int order)
        {
            return new ActionPadButtonEntry
            {
                Label = "Botón rápido",
                Action = "Msg('Configura tu acción');",
                Tooltip = "Editar para ejecutar macros o transacciones",
                Order = order,
                GridRow = -1,
                GridCol = -1,
                ColSpan = 1,
                RowSpan = 1,
                Color = "#2E75B6"
            };
        }

        private void ApplyPadResults(IEnumerable<ActionPadEntry> pads)
        {
            _pads.Clear();
            foreach (var pad in pads.OrderBy(p => p.FormType).ThenBy(p => p.Title))
            {
                if (pad.Buttons == null)
                {
                    pad.Buttons = new ObservableCollection<ActionPadButtonEntry>();
                }
                RecalculateButtonOrders(pad);
                _pads.Add(pad);
            }
            _padsView.Refresh();
            if (_pads.Count > 0)
            {
                if (SelectedPad == null || !_pads.Contains(SelectedPad))
                {
                    SelectedPad = _pads.FirstOrDefault();
                }
                else
                {
                    SelectedPadButton = SelectedPad?.Buttons?.FirstOrDefault();
                }
            }
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

        private bool FilterPads(object obj)
        {
            var pad = obj as ActionPadEntry;
            if (pad == null) return false;
            if (string.IsNullOrWhiteSpace(PadSearch)) return true;
            return (pad.Title ?? string.Empty).IndexOf(PadSearch, StringComparison.OrdinalIgnoreCase) >= 0
                || (pad.FormType ?? string.Empty).IndexOf(PadSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RecalculateButtonOrders(ActionPadEntry pad)
        {
            if (pad?.Buttons == null) return;
            int order = 10;
            foreach (var button in pad.Buttons)
            {
                button.Order = order;
                order += 10;
            }
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
            PadDuplicateCommand.RaiseCanExecuteChanged();
            PadSaveCommand.RaiseCanExecuteChanged();
            PadDeleteCommand.RaiseCanExecuteChanged();
            PadAddButtonCommand.RaiseCanExecuteChanged();
            PadDuplicateButtonCommand.RaiseCanExecuteChanged();
            PadRemoveButtonCommand.RaiseCanExecuteChanged();
            PadMoveButtonUpCommand.RaiseCanExecuteChanged();
            PadMoveButtonDownCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
