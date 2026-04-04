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

namespace B1TuneUp.Modules.ValidationUi
{
    public class ValidationDesignerViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ValidationRuleEntry> _validationEntries = new ObservableCollection<ValidationRuleEntry>();
        private readonly ObservableCollection<MandatoryFieldEntry> _mandatoryEntries = new ObservableCollection<MandatoryFieldEntry>();
        private readonly ListCollectionView _validationView;
        private readonly ListCollectionView _mandatoryView;

        private ValidationRuleEntry _selectedValidation;
        private MandatoryFieldEntry _selectedMandatory;

        private string _validationSearch;
        private string _validationFormFilter;
        private string _mandatorySearch;
        private string _mandatoryFormFilter;

        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        private readonly string _initialValidationFilter;
        private readonly string _initialItemSearch;

        public ValidationDesignerViewModel(string defaultFormFilter = null, string defaultItemSearch = null)
        {
            _initialValidationFilter = defaultFormFilter;
            _initialItemSearch = defaultItemSearch;

            _validationView = (ListCollectionView)CollectionViewSource.GetDefaultView(_validationEntries);
            _validationView.Filter = FilterValidation;

            _mandatoryView = (ListCollectionView)CollectionViewSource.GetDefaultView(_mandatoryEntries);
            _mandatoryView.Filter = FilterMandatory;

            EventOptions = new[] { "FORM_LOAD", "ITEM_PRESSED", "DATA_ADD_BEFORE", "DATA_UPDATE_BEFORE", "COMBO_SELECT", "EDIT_VALIDATE" };
            SeverityOptions = new[] { "ERROR", "WARNING", "INFO" };

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => _validationEntries.Any() || _mandatoryEntries.Any());

            NewValidationCommand = new RelayCommand(NewValidation);
            DuplicateValidationCommand = new RelayCommand(DuplicateValidation, () => SelectedValidation != null);
            SaveValidationCommand = new RelayCommand(async () => await SaveValidationAsync(), () => SelectedValidation != null);
            DeleteValidationCommand = new RelayCommand(async () => await DeleteValidationAsync(), () => SelectedValidation != null);

            NewMandatoryCommand = new RelayCommand(NewMandatory);
            DuplicateMandatoryCommand = new RelayCommand(DuplicateMandatory, () => SelectedMandatory != null);
            SaveMandatoryCommand = new RelayCommand(async () => await SaveMandatoryAsync(), () => SelectedMandatory != null);
            DeleteMandatoryCommand = new RelayCommand(async () => await DeleteMandatoryAsync(), () => SelectedMandatory != null);

            if (!string.IsNullOrWhiteSpace(defaultFormFilter))
            {
                ValidationFormFilter = defaultFormFilter;
                MandatoryFormFilter = defaultFormFilter;
            }
            if (!string.IsNullOrWhiteSpace(defaultItemSearch))
            {
                ValidationSearch = defaultItemSearch;
                MandatorySearch = defaultItemSearch;
            }
        }

        public ICollectionView ValidationView => _validationView;
        public ICollectionView MandatoryView => _mandatoryView;

        public string[] EventOptions { get; }
        public string[] SeverityOptions { get; }

        public ValidationRuleEntry SelectedValidation
        {
            get => _selectedValidation;
            set
            {
                if (_selectedValidation == value) return;
                _selectedValidation = value;
                OnPropertyChanged();
                SaveValidationCommand.RaiseCanExecuteChanged();
                DuplicateValidationCommand.RaiseCanExecuteChanged();
                DeleteValidationCommand.RaiseCanExecuteChanged();
            }
        }

        public MandatoryFieldEntry SelectedMandatory
        {
            get => _selectedMandatory;
            set
            {
                if (_selectedMandatory == value) return;
                _selectedMandatory = value;
                OnPropertyChanged();
                SaveMandatoryCommand.RaiseCanExecuteChanged();
                DuplicateMandatoryCommand.RaiseCanExecuteChanged();
                DeleteMandatoryCommand.RaiseCanExecuteChanged();
            }
        }

        public string ValidationSearch
        {
            get => _validationSearch;
            set
            {
                if (_validationSearch == value) return;
                _validationSearch = value;
                OnPropertyChanged();
                _validationView.Refresh();
            }
        }

        public string ValidationFormFilter
        {
            get => _validationFormFilter;
            set
            {
                if (_validationFormFilter == value) return;
                _validationFormFilter = value;
                OnPropertyChanged();
                _validationView.Refresh();
            }
        }

        public string MandatorySearch
        {
            get => _mandatorySearch;
            set
            {
                if (_mandatorySearch == value) return;
                _mandatorySearch = value;
                OnPropertyChanged();
                _mandatoryView.Refresh();
            }
        }

        public string MandatoryFormFilter
        {
            get => _mandatoryFormFilter;
            set
            {
                if (_mandatoryFormFilter == value) return;
                _mandatoryFormFilter = value;
                OnPropertyChanged();
                _mandatoryView.Refresh();
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
        public RelayCommand SaveAllCommand { get; }
        public RelayCommand NewValidationCommand { get; }
        public RelayCommand DuplicateValidationCommand { get; }
        public RelayCommand SaveValidationCommand { get; }
        public RelayCommand DeleteValidationCommand { get; }
        public RelayCommand NewMandatoryCommand { get; }
        public RelayCommand DuplicateMandatoryCommand { get; }
        public RelayCommand SaveMandatoryCommand { get; }
        public RelayCommand DeleteMandatoryCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando validaciones y campos obligatorios...", async () =>
            {
                var validations = await Task.Run(() => ValidationRuleService.GetAll());
                var mandatory = await Task.Run(() => MandatoryFieldService.GetAll());

                RunOnUi(() =>
                {
                    _validationEntries.Clear();
                    foreach (var item in validations.OrderBy(v => v.Code))
                    {
                        _validationEntries.Add(item);
                    }
                    _validationView.Refresh();
                    SelectedValidation = _validationEntries.FirstOrDefault();

                    _mandatoryEntries.Clear();
                    foreach (var item in mandatory.OrderBy(m => m.Code))
                    {
                        _mandatoryEntries.Add(item);
                    }
                    _mandatoryView.Refresh();
                    SelectedMandatory = _mandatoryEntries.FirstOrDefault();

                    StatusMessage = $"{_validationEntries.Count} validaciones · {_mandatoryEntries.Count} campos obligatorios.";
                });
            });
        }

        private void NewValidation()
        {
            var entry = new ValidationRuleEntry
            {
                Name = "Nueva validación",
                Severity = "ERROR",
                EventType = "FORM_LOAD",
                Active = true
            };
            _validationEntries.Add(entry);
            SelectedValidation = entry;
            _validationView.Refresh();
        }

        private void DuplicateValidation()
        {
            if (SelectedValidation == null) return;
            var copy = SelectedValidation.Clone();
            copy.Code = null;
            copy.Name = $"{copy.Name} (Copy)";
            _validationEntries.Add(copy);
            SelectedValidation = copy;
            _validationView.Refresh();
        }

        private async Task SaveValidationAsync()
        {
            if (SelectedValidation == null) return;
            await RunSafeAsync("Guardando validación...", async () =>
            {
                await Task.Run(() => ValidationRuleService.Save(SelectedValidation));
                StatusMessage = $"Validación '{SelectedValidation.Name ?? SelectedValidation.Code}' guardada.";
            });
        }

        private async Task DeleteValidationAsync()
        {
            if (SelectedValidation == null) return;
            if (MessageBox.Show($"¿Eliminar la validación '{SelectedValidation.Name ?? SelectedValidation.Code}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            var toDelete = SelectedValidation;
            await RunSafeAsync("Eliminando validación...", async () =>
            {
                await Task.Run(() => ValidationRuleService.Delete(toDelete.Code));
                RunOnUi(() =>
                {
                    _validationEntries.Remove(toDelete);
                    SelectedValidation = _validationEntries.FirstOrDefault();
                    _validationView.Refresh();
                });
                StatusMessage = "Validación eliminada.";
            });
        }

        private void NewMandatory()
        {
            var entry = new MandatoryFieldEntry
            {
                Name = "Campo obligatorio",
                ErrorMessage = "Este campo es obligatorio."
            };
            _mandatoryEntries.Add(entry);
            SelectedMandatory = entry;
            _mandatoryView.Refresh();
        }

        private void DuplicateMandatory()
        {
            if (SelectedMandatory == null) return;
            var copy = SelectedMandatory.Clone();
            copy.Code = null;
            copy.Name = $"{copy.Name} (Copy)";
            _mandatoryEntries.Add(copy);
            SelectedMandatory = copy;
            _mandatoryView.Refresh();
        }

        private async Task SaveMandatoryAsync()
        {
            if (SelectedMandatory == null) return;
            await RunSafeAsync("Guardando campo obligatorio...", async () =>
            {
                await Task.Run(() => MandatoryFieldService.Save(SelectedMandatory));
                StatusMessage = $"Campo obligatorio '{SelectedMandatory.Name ?? SelectedMandatory.Code}' guardado.";
            });
        }

        private async Task DeleteMandatoryAsync()
        {
            if (SelectedMandatory == null) return;
            if (MessageBox.Show($"¿Eliminar el campo '{SelectedMandatory.Name ?? SelectedMandatory.Code}'?", "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            var toDelete = SelectedMandatory;
            await RunSafeAsync("Eliminando campo...", async () =>
            {
                await Task.Run(() => MandatoryFieldService.Delete(toDelete.Code));
                RunOnUi(() =>
                {
                    _mandatoryEntries.Remove(toDelete);
                    SelectedMandatory = _mandatoryEntries.FirstOrDefault();
                    _mandatoryView.Refresh();
                });
                StatusMessage = "Campo obligatorio eliminado.";
            });
        }

        private async Task SaveAllAsync()
        {
            await RunSafeAsync("Guardando todos los cambios...", async () =>
            {
                await Task.Run(() =>
                {
                    foreach (var val in _validationEntries)
                    {
                        ValidationRuleService.Save(val);
                    }
                    foreach (var mand in _mandatoryEntries)
                    {
                        MandatoryFieldService.Save(mand);
                    }
                });
                StatusMessage = "Todos los cambios fueron guardados.";
            });
        }

        private bool FilterValidation(object obj)
        {
            var entry = obj as ValidationRuleEntry;
            if (entry == null) return false;
            if (!string.IsNullOrWhiteSpace(ValidationSearch))
            {
                string term = ValidationSearch.Trim();
                if (!Contains(entry.FormType, term) &&
                    !Contains(entry.ItemName, term) &&
                    !Contains(entry.Condition, term) &&
                    !Contains(entry.Action, term))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(ValidationFormFilter))
            {
                if (!Contains(entry.FormType, ValidationFormFilter))
                {
                    return false;
                }
            }
            return true;
        }

        private bool FilterMandatory(object obj)
        {
            var entry = obj as MandatoryFieldEntry;
            if (entry == null) return false;
            if (!string.IsNullOrWhiteSpace(MandatorySearch))
            {
                string term = MandatorySearch.Trim();
                if (!Contains(entry.FormType, term) &&
                    !Contains(entry.ItemId, term) &&
                    !Contains(entry.ErrorMessage, term))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(MandatoryFormFilter))
            {
                if (!Contains(entry.FormType, MandatoryFormFilter))
                {
                    return false;
                }
            }
            return true;
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
                SaveAllCommand.RaiseCanExecuteChanged();
            }
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
