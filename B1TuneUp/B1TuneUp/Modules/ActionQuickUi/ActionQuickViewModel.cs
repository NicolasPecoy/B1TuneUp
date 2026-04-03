using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.ActionQuickUi
{
    public class ActionQuickViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ActionPadEntry> _pads = new ObservableCollection<ActionPadEntry>();
        private readonly ObservableCollection<QuickCopyEntry> _quickCopies = new ObservableCollection<QuickCopyEntry>();
        private readonly ObservableCollection<ItemActionEntry> _itemActions = new ObservableCollection<ItemActionEntry>();

        private readonly ListCollectionView _padsView;
        private readonly ListCollectionView _quickView;
        private readonly ListCollectionView _itemView;

        private ActionPadEntry _selectedPad;
        private ActionPadButtonEntry _selectedPadButton;
        private QuickCopyEntry _selectedQuickCopy;
        private ItemActionEntry _selectedItemAction;

        private string _padSearch;
        private string _quickSearch;
        private string _itemSearch;

        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public ActionQuickViewModel()
        {
            _padsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_pads);
            _padsView.Filter = o => FilterPads(o as ActionPadEntry, _padSearch);

            _quickView = (ListCollectionView)CollectionViewSource.GetDefaultView(_quickCopies);
            _quickView.Filter = o => FilterQuick(o as QuickCopyEntry, _quickSearch);

            _itemView = (ListCollectionView)CollectionViewSource.GetDefaultView(_itemActions);
            _itemView.Filter = o => FilterItems(o as ItemActionEntry, _itemSearch);

            RefreshCommand = new RelayCommand(async () => await LoadAsync());

            NewPadCommand = new RelayCommand(NewPad);
            DuplicatePadCommand = new RelayCommand(DuplicatePad, () => SelectedPad != null);
            SavePadCommand = new RelayCommand(async () => await SavePadAsync(), () => SelectedPad != null);
            DeletePadCommand = new RelayCommand(async () => await DeletePadAsync(), () => SelectedPad != null);
            AddPadButtonCommand = new RelayCommand(AddPadButton, () => SelectedPad != null);
            DeletePadButtonCommand = new RelayCommand(DeletePadButton, () => SelectedPadButton != null && SelectedPad != null);

            NewQuickCopyCommand = new RelayCommand(NewQuick);
            DuplicateQuickCopyCommand = new RelayCommand(DuplicateQuick, () => SelectedQuickCopy != null);
            SaveQuickCopyCommand = new RelayCommand(async () => await SaveQuickAsync(), () => SelectedQuickCopy != null);
            DeleteQuickCopyCommand = new RelayCommand(async () => await DeleteQuickAsync(), () => SelectedQuickCopy != null);

            NewItemActionCommand = new RelayCommand(NewItemAction);
            DuplicateItemActionCommand = new RelayCommand(DuplicateItemAction, () => SelectedItemAction != null);
            SaveItemActionCommand = new RelayCommand(async () => await SaveItemActionAsync(), () => SelectedItemAction != null);
            DeleteItemActionCommand = new RelayCommand(async () => await DeleteItemActionAsync(), () => SelectedItemAction != null);
        }

        public ICollectionView PadsView => _padsView;
        public ICollectionView QuickView => _quickView;
        public ICollectionView ItemView => _itemView;

        public ActionPadEntry SelectedPad
        {
            get => _selectedPad;
            set
            {
                if (_selectedPad == value) return;
                _selectedPad = value;
                OnPropertyChanged();
                SelectedPadButton = _selectedPad?.Buttons.FirstOrDefault();
                DuplicatePadCommand.RaiseCanExecuteChanged();
                SavePadCommand.RaiseCanExecuteChanged();
                DeletePadCommand.RaiseCanExecuteChanged();
                AddPadButtonCommand.RaiseCanExecuteChanged();
                DeletePadButtonCommand.RaiseCanExecuteChanged();
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
                DeletePadButtonCommand.RaiseCanExecuteChanged();
            }
        }

        public QuickCopyEntry SelectedQuickCopy
        {
            get => _selectedQuickCopy;
            set
            {
                if (_selectedQuickCopy == value) return;
                _selectedQuickCopy = value;
                OnPropertyChanged();
                DuplicateQuickCopyCommand.RaiseCanExecuteChanged();
                SaveQuickCopyCommand.RaiseCanExecuteChanged();
                DeleteQuickCopyCommand.RaiseCanExecuteChanged();
            }
        }

        public ItemActionEntry SelectedItemAction
        {
            get => _selectedItemAction;
            set
            {
                if (_selectedItemAction == value) return;
                _selectedItemAction = value;
                OnPropertyChanged();
                DuplicateItemActionCommand.RaiseCanExecuteChanged();
                SaveItemActionCommand.RaiseCanExecuteChanged();
                DeleteItemActionCommand.RaiseCanExecuteChanged();
            }
        }

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

        public string QuickSearch
        {
            get => _quickSearch;
            set
            {
                if (_quickSearch == value) return;
                _quickSearch = value;
                OnPropertyChanged();
                _quickView.Refresh();
            }
        }

        public string ItemSearch
        {
            get => _itemSearch;
            set
            {
                if (_itemSearch == value) return;
                _itemSearch = value;
                OnPropertyChanged();
                _itemView.Refresh();
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

        public RelayCommand RefreshCommand { get; }

        public RelayCommand NewPadCommand { get; }
        public RelayCommand DuplicatePadCommand { get; }
        public RelayCommand SavePadCommand { get; }
        public RelayCommand DeletePadCommand { get; }
        public RelayCommand AddPadButtonCommand { get; }
        public RelayCommand DeletePadButtonCommand { get; }

        public RelayCommand NewQuickCopyCommand { get; }
        public RelayCommand DuplicateQuickCopyCommand { get; }
        public RelayCommand SaveQuickCopyCommand { get; }
        public RelayCommand DeleteQuickCopyCommand { get; }

        public RelayCommand NewItemActionCommand { get; }
        public RelayCommand DuplicateItemActionCommand { get; }
        public RelayCommand SaveItemActionCommand { get; }
        public RelayCommand DeleteItemActionCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Loading Action Pad / Quick Copy / Item Actions...", async () =>
            {
                var padTask = Task.Run(() => ActionPadService.GetAll());
                var quickTask = Task.Run(() => QuickCopyService.GetAll());
                var itemTask = Task.Run(() => ItemActionService.GetAll());

                await Task.WhenAll(padTask, quickTask, itemTask);

                RunOnUi(() =>
                {
                    _pads.Clear();
                    foreach (var pad in padTask.Result) _pads.Add(pad);
                    _padsView.Refresh();
                    SelectedPad = _pads.FirstOrDefault();

                    _quickCopies.Clear();
                    foreach (var quick in quickTask.Result) _quickCopies.Add(quick);
                    _quickView.Refresh();
                    SelectedQuickCopy = _quickCopies.FirstOrDefault();

                    _itemActions.Clear();
                    foreach (var act in itemTask.Result) _itemActions.Add(act);
                    _itemView.Refresh();
                    SelectedItemAction = _itemActions.FirstOrDefault();

                    StatusMessage = $"{_pads.Count} pads · {_quickCopies.Count} quick copies · {_itemActions.Count} item actions.";
                });
            });
        }

        private void NewPad()
        {
            var entry = new ActionPadEntry
            {
                Title = "New Action Pad",
                FormType = "",
                Position = "Right"
            };
            entry.Buttons.Add(new ActionPadButtonEntry { Label = "Action", Action = "Msg('Hello');", Order = 1 });
            _pads.Add(entry);
            SelectedPad = entry;
            _padsView.Refresh();
        }

        private void DuplicatePad()
        {
            if (SelectedPad == null) return;
            var copy = SelectedPad.Clone();
            copy.DocEntry = 0;
            copy.Title = $"{copy.Title} Copy";
            foreach (var btn in copy.Buttons) btn.DocEntry = 0;
            _pads.Add(copy);
            SelectedPad = copy;
            _padsView.Refresh();
        }

        private async Task SavePadAsync()
        {
            if (SelectedPad == null) return;
            await RunSafeAsync("Saving Action Pad...", async () =>
            {
                await Task.Run(() => ActionPadService.Save(SelectedPad));
                StatusMessage = $"Action Pad '{SelectedPad.Title}' saved.";
            });
        }

        private async Task DeletePadAsync()
        {
            if (SelectedPad == null) return;
            var target = SelectedPad;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Deleting Action Pad...", async () =>
                {
                    await Task.Run(() => ActionPadService.Delete(target.DocEntry));
                });
            }
            _pads.Remove(target);
            SelectedPad = _pads.FirstOrDefault();
            _padsView.Refresh();
            StatusMessage = "Action Pad removed.";
        }

        private void AddPadButton()
        {
            if (SelectedPad == null) return;
            int order = SelectedPad.Buttons.Any() ? SelectedPad.Buttons.Max(b => b.Order) + 1 : 1;
            var button = new ActionPadButtonEntry { Label = "New Button", Action = "Msg('Action');", Order = order };
            SelectedPad.Buttons.Add(button);
            SelectedPadButton = button;
        }

        private void DeletePadButton()
        {
            if (SelectedPad == null || SelectedPadButton == null) return;
            SelectedPad.Buttons.Remove(SelectedPadButton);
            SelectedPadButton = SelectedPad.Buttons.FirstOrDefault();
        }

        private void NewQuick()
        {
            var entry = new QuickCopyEntry
            {
                ButtonLabel = "Quick Copy",
                SourceFormType = "",
                SourceObjectType = "Documents",
                TargetObjectType = "Documents",
                Active = true
            };
            _quickCopies.Add(entry);
            SelectedQuickCopy = entry;
            _quickView.Refresh();
        }

        private void DuplicateQuick()
        {
            if (SelectedQuickCopy == null) return;
            var copy = SelectedQuickCopy.Clone();
            copy.DocEntry = 0;
            copy.ButtonLabel = $"{copy.ButtonLabel} Copy";
            _quickCopies.Add(copy);
            SelectedQuickCopy = copy;
            _quickView.Refresh();
        }

        private async Task SaveQuickAsync()
        {
            if (SelectedQuickCopy == null) return;
            await RunSafeAsync("Saving Quick Copy...", async () =>
            {
                await Task.Run(() => QuickCopyService.Save(SelectedQuickCopy));
                StatusMessage = $"Quick Copy '{SelectedQuickCopy.ButtonLabel}' saved.";
            });
        }

        private async Task DeleteQuickAsync()
        {
            if (SelectedQuickCopy == null) return;
            var target = SelectedQuickCopy;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Deleting Quick Copy...", async () =>
                {
                    await Task.Run(() => QuickCopyService.Delete(target.DocEntry));
                });
            }
            _quickCopies.Remove(target);
            SelectedQuickCopy = _quickCopies.FirstOrDefault();
            _quickView.Refresh();
            StatusMessage = "Quick Copy removed.";
        }

        private void NewItemAction()
        {
            var entry = new ItemActionEntry
            {
                FormType = "",
                ItemId = "",
                Event = "Click",
                Action = "Msg('Action executed');"
            };
            _itemActions.Add(entry);
            SelectedItemAction = entry;
            _itemView.Refresh();
        }

        private void DuplicateItemAction()
        {
            if (SelectedItemAction == null) return;
            var copy = SelectedItemAction.Clone();
            copy.DocEntry = 0;
            _itemActions.Add(copy);
            SelectedItemAction = copy;
            _itemView.Refresh();
        }

        private async Task SaveItemActionAsync()
        {
            if (SelectedItemAction == null) return;
            await RunSafeAsync("Saving Item Action...", async () =>
            {
                await Task.Run(() => ItemActionService.Save(SelectedItemAction));
                StatusMessage = $"Item Action '{SelectedItemAction.FormType}/{SelectedItemAction.ItemId}' saved.";
            });
        }

        private async Task DeleteItemActionAsync()
        {
            if (SelectedItemAction == null) return;
            var target = SelectedItemAction;
            if (target.DocEntry > 0)
            {
                await RunSafeAsync("Deleting Item Action...", async () =>
                {
                    await Task.Run(() => ItemActionService.Delete(target.DocEntry));
                });
            }
            _itemActions.Remove(target);
            SelectedItemAction = _itemActions.FirstOrDefault();
            _itemView.Refresh();
            StatusMessage = "Item Action removed.";
        }

        private bool FilterPads(ActionPadEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.Title, term) ||
                   Contains(entry.FormType, term) ||
                   entry.Buttons.Any(b => Contains(b.Label, term));
        }

        private bool FilterQuick(QuickCopyEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.ButtonLabel, term) ||
                   Contains(entry.SourceFormType, term) ||
                   Contains(entry.TargetObjectType, term);
        }

        private bool FilterItems(ItemActionEntry entry, string term)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(term)) return true;
            return Contains(entry.FormType, term) ||
                   Contains(entry.ItemId, term) ||
                   Contains(entry.Event, term);
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
                RaiseCommandStates();
            }
        }

        private void RaiseCommandStates()
        {
            DuplicatePadCommand.RaiseCanExecuteChanged();
            SavePadCommand.RaiseCanExecuteChanged();
            DeletePadCommand.RaiseCanExecuteChanged();
            AddPadButtonCommand.RaiseCanExecuteChanged();
            DeletePadButtonCommand.RaiseCanExecuteChanged();
            DuplicateQuickCopyCommand.RaiseCanExecuteChanged();
            SaveQuickCopyCommand.RaiseCanExecuteChanged();
            DeleteQuickCopyCommand.RaiseCanExecuteChanged();
            DuplicateItemActionCommand.RaiseCanExecuteChanged();
            SaveItemActionCommand.RaiseCanExecuteChanged();
            DeleteItemActionCommand.RaiseCanExecuteChanged();
        }

        private void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
