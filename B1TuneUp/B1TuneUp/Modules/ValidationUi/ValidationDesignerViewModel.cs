using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules;
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
        private string _validationUserFilter;
        private string _validationLocalizationFilter;
        private string _validationVariantFilter;
        private string _validationDependencyFilter;
        private string _validationPackageFilter;
        private string _mandatorySearch;
        private string _mandatoryFormFilter;

        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        private readonly ValidationDesignerLaunchOptions _launchOptions;

        public ValidationDesignerViewModel(ValidationDesignerLaunchOptions options = null)
        {
            _launchOptions = options ?? new ValidationDesignerLaunchOptions();

            _validationView = (ListCollectionView)CollectionViewSource.GetDefaultView(_validationEntries);
            _validationView.Filter = FilterValidation;

            _mandatoryView = (ListCollectionView)CollectionViewSource.GetDefaultView(_mandatoryEntries);
            _mandatoryView.Filter = FilterMandatory;

            EventOptions = new[] { "FORM_LOAD", "ITEM_PRESSED", "DATA_ADD_BEFORE", "DATA_UPDATE_BEFORE", "COMBO_SELECT", "EDIT_VALIDATE", "CLICK", "DOUBLE_CLICK", "VALIDATE" };
            SeverityOptions = new[] { "ERROR", "WARNING", "INFO" };

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => _validationEntries.Any() || _mandatoryEntries.Any());
            ExportCommand = new RelayCommand(async () => await ExportAsync(), () => _validationEntries.Any() || _mandatoryEntries.Any());
            ImportCommand = new RelayCommand(async () => await ImportAsync());

            NewValidationCommand = new RelayCommand(NewValidation);
            DuplicateValidationCommand = new RelayCommand(DuplicateValidation, () => SelectedValidation != null);
            SaveValidationCommand = new RelayCommand(async () => await SaveValidationAsync(), () => SelectedValidation != null);
            DeleteValidationCommand = new RelayCommand(async () => await DeleteValidationAsync(), () => SelectedValidation != null);

            NewMandatoryCommand = new RelayCommand(NewMandatory);
            DuplicateMandatoryCommand = new RelayCommand(DuplicateMandatory, () => SelectedMandatory != null);
            SaveMandatoryCommand = new RelayCommand(async () => await SaveMandatoryAsync(), () => SelectedMandatory != null);
            DeleteMandatoryCommand = new RelayCommand(async () => await DeleteMandatoryAsync(), () => SelectedMandatory != null);

            if (!string.IsNullOrWhiteSpace(_launchOptions.FormFilter))
            {
                ValidationFormFilter = _launchOptions.FormFilter;
                MandatoryFormFilter = _launchOptions.FormFilter;
            }
            if (!string.IsNullOrWhiteSpace(_launchOptions.ItemFilter))
            {
                ValidationSearch = _launchOptions.ItemFilter;
                MandatorySearch = _launchOptions.ItemFilter;
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

        public string ValidationUserFilter
        {
            get => _validationUserFilter;
            set
            {
                if (_validationUserFilter == value) return;
                _validationUserFilter = value;
                OnPropertyChanged();
                _validationView.Refresh();
            }
        }

        public string ValidationLocalizationFilter
        {
            get => _validationLocalizationFilter;
            set
            {
                if (_validationLocalizationFilter == value) return;
                _validationLocalizationFilter = value;
                OnPropertyChanged();
                _validationView.Refresh();
            }
        }

        public string ValidationVariantFilter
        {
            get => _validationVariantFilter;
            set
            {
                if (_validationVariantFilter == value) return;
                _validationVariantFilter = value;
                OnPropertyChanged();
                _validationView.Refresh();
            }
        }

        public string ValidationDependencyFilter
        {
            get => _validationDependencyFilter;
            set
            {
                if (_validationDependencyFilter == value) return;
                _validationDependencyFilter = value;
                OnPropertyChanged();
                _validationView.Refresh();
            }
        }

        public string ValidationPackageFilter
        {
            get => _validationPackageFilter;
            set
            {
                if (_validationPackageFilter == value) return;
                _validationPackageFilter = value;
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
        public RelayCommand ExportCommand { get; }
        public RelayCommand ImportCommand { get; }
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
                    ApplyLaunchOptions();
                    StatusMessage = $"{_validationEntries.Count} validaciones · {_mandatoryEntries.Count} campos obligatorios.";
                    ExportCommand.RaiseCanExecuteChanged();
                    SaveAllCommand.RaiseCanExecuteChanged();
                });
            });
        }

        private void NewValidation()
        {
            var entry = new ValidationRuleEntry
            {
                Name = "Nueva validación",
                Severity = "ERROR",
                EventType = "DATA_ADD_BEFORE",
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
                LogValidationAudit($"Validación '{SelectedValidation.Name ?? SelectedValidation.Code}' guardada.", "Validation", SelectedValidation);
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
                LogValidationAudit($"Validación '{toDelete.Name ?? toDelete.Code}' eliminada.", "Deleted", toDelete);
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
                LogValidationAudit($"Campo obligatorio '{SelectedMandatory.Name ?? SelectedMandatory.Code}' guardado.", "Mandatory", null, SelectedMandatory);
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
                LogValidationAudit($"Campo obligatorio '{toDelete.Name ?? toDelete.Code}' eliminado.", "Deleted", null, toDelete);
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
                LogValidationAudit("Guardado masivo de validaciones y campos obligatorios.", "Bulk");
            });
        }

        private async Task ExportAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Validation Package (*.json)|*.json",
                FileName = string.IsNullOrWhiteSpace(ValidationFormFilter) ? "validation-package.json" : $"validation-{ValidationFormFilter}.json"
            };
            if (dialog.ShowDialog() != true) return;

            await RunSafeAsync("Exportando validaciones...", async () =>
            {
                string formType = string.IsNullOrWhiteSpace(ValidationFormFilter) ? null : ValidationFormFilter.Trim();
                await Task.Run(() => ValidationRuleService.ExportPackage(dialog.FileName, formType));
                StatusMessage = "Paquete exportado correctamente.";
            });
        }

        private async Task ImportAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Validation Package (*.json)|*.json"
            };
            if (dialog.ShowDialog() != true) return;

            await RunSafeAsync("Importando validaciones...", async () =>
            {
                await Task.Run(() => ValidationRuleService.ImportPackage(dialog.FileName));
                await LoadAsync();
                StatusMessage = "Paquete importado correctamente.";
            });
        }

        private void ApplyLaunchOptions()
        {
            if (_launchOptions == null) return;
            if (_launchOptions.StartNewValidation)
            {
                _launchOptions.StartNewValidation = false;
                NewValidation();
                SelectedValidation.FormType = _launchOptions.FormFilter ?? SelectedValidation.FormType;
                SelectedValidation.ItemName = ComposeItemFilter(_launchOptions.ItemFilter, _launchOptions.ColumnFilter);
                SelectedValidation.EventType = "ITEM_PRESSED";
                SelectedValidation.Message = $"ValidaciÃ³n creada desde formulario para {ComposeItemFilter(_launchOptions.ItemFilter, _launchOptions.ColumnFilter)}";
            }

            if (_launchOptions.StartNewMandatory)
            {
                _launchOptions.StartNewMandatory = false;
                NewMandatory();
                SelectedMandatory.FormType = _launchOptions.FormFilter ?? SelectedMandatory.FormType;
                SelectedMandatory.ItemId = _launchOptions.ItemFilter ?? SelectedMandatory.ItemId;
                SelectedMandatory.ColumnId = _launchOptions.ColumnFilter ?? SelectedMandatory.ColumnId;
            }
        }

        private static string ComposeItemFilter(string item, string column)
        {
            if (string.IsNullOrWhiteSpace(item)) return string.Empty;
            return string.IsNullOrWhiteSpace(column) ? item : $"{item}.{column}";
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
            if (!string.IsNullOrWhiteSpace(ValidationUserFilter))
            {
                string token = ValidationUserFilter.Trim();
                if (!Contains(entry.AppliesToUser, token) &&
                    !Contains(entry.AppliesToUserGroup, token) &&
                    !Contains(entry.ExcludedUsers, token) &&
                    !Contains(entry.ExcludedUserGroups, token))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(ValidationLocalizationFilter))
            {
                string token = ValidationLocalizationFilter.Trim();
                if (!Contains(entry.ScopeLocalization, token))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(ValidationVariantFilter))
            {
                string token = ValidationVariantFilter.Trim();
                if (!Contains(entry.ScopeVariant, token))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(ValidationDependencyFilter))
            {
                string token = ValidationDependencyFilter.Trim();
                if (!Contains(entry.ScopeDependsOn, token) &&
                    !Contains(entry.ScopeInheritFrom, token) &&
                    !Contains(entry.Notes, token))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(ValidationPackageFilter))
            {
                string token = ValidationPackageFilter.Trim();
                if (!Contains(entry.ScopePackages, token))
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

        private void LogValidationAudit(string message, string status = "Info", ValidationRuleEntry validation = null, MandatoryFieldEntry mandatory = null)
        {
            try
            {
                string user = B1App.Instance?.Company?.UserName ?? Environment.UserName;
                string form = validation?.FormType ?? mandatory?.FormType ?? SelectedValidation?.FormType ?? SelectedMandatory?.FormType ?? string.Empty;
                string item = validation?.ItemName ?? mandatory?.ItemId ?? SelectedValidation?.ItemName ?? SelectedMandatory?.ItemId ?? string.Empty;
                AuditLogManager.LogDetailedAction("ValidationDesigner", message, status, user, string.IsNullOrWhiteSpace(form) ? "GENERAL" : form, string.IsNullOrWhiteSpace(item) ? "Item:*" : $"Item:{item}");
            }
            catch
            {
                // ignore logging errors
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
                SaveAllCommand.RaiseCanExecuteChanged();
                ExportCommand.RaiseCanExecuteChanged();
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
