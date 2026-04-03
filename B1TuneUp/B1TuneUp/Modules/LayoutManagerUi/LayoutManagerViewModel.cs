using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using SAPbouiCOM;

namespace B1TuneUp.Modules.LayoutManagerUi
{
    public class LayoutManagerViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<LayoutDefinitionEntry> _layouts = new ObservableCollection<LayoutDefinitionEntry>();
        private readonly Dispatcher _dispatcher;
        private readonly ICollectionView _layoutsView;
        private LayoutDefinitionEntry _selectedLayout;
        private string _searchTerm;
        private string _manualFormType;
        private string _newLayoutName;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public LayoutManagerViewModel()
        {
            _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _layoutsView = CollectionViewSource.GetDefaultView(_layouts);
            _layoutsView.Filter = FilterLayout;

            RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !IsBusy);
            ExportCommand = new RelayCommand(async () => await ExportAsync(), () => SelectedLayout != null);
            ApplyCommand = new RelayCommand(async () => await ApplyAsync(), () => SelectedLayout != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedLayout != null);
            ImportCommand = new RelayCommand(async () => await ImportAsync());
            DetectActiveFormCommand = new RelayCommand(DetectActiveForm);

            DetectActiveForm();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICollectionView LayoutsView => _layoutsView;

        public LayoutDefinitionEntry SelectedLayout
        {
            get => _selectedLayout;
            set
            {
                if (_selectedLayout == value) return;
                _selectedLayout = value;
                OnPropertyChanged(nameof(SelectedLayout));
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
                _layoutsView.Refresh();
            }
        }

        public string ManualFormType
        {
            get => _manualFormType;
            set
            {
                if (_manualFormType == value) return;
                _manualFormType = value;
                OnPropertyChanged(nameof(ManualFormType));
            }
        }

        public string NewLayoutName
        {
            get => _newLayoutName;
            set
            {
                if (_newLayoutName == value) return;
                _newLayoutName = value;
                OnPropertyChanged(nameof(NewLayoutName));
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
        public RelayCommand ExportCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ImportCommand { get; }
        public RelayCommand DetectActiveFormCommand { get; }

        public async Task LoadAsync()
        {
            await RunSafeAsync(async () =>
            {
                var table = await Task.Run(() => ItemPlacementManager.GetLayouts(null));
                await _dispatcher.InvokeAsync(() =>
                {
                    _layouts.Clear();
                    foreach (DataRow row in table.Rows)
                    {
                        _layouts.Add(MapLayout(row));
                    }
                    SelectedLayout = _layouts.FirstOrDefault();
                }, DispatcherPriority.DataBind);
                StatusMessage = $"Layouts cargados: {_layouts.Count}";
            }, "Cargando layouts...");
        }

        private async Task ExportAsync()
        {
            if (SelectedLayout == null) return;
            var sfd = new SaveFileDialog
            {
                Filter = "SRF (*.srf)|*.srf|XML (*.xml)|*.xml|Todos (*.*)|*.*",
                FileName = $"{SelectedLayout.FormType}_{SelectedLayout.LayoutName}.srf"
            };
            if (sfd.ShowDialog() != true) return;

            await RunSafeAsync(async () =>
            {
                bool ok = await Task.Run(() => ItemPlacementManager.RestoreSrfFromLayout(SelectedLayout.LayoutName, SelectedLayout.FormType, sfd.FileName));
                StatusMessage = ok ? $"Layout exportado a {sfd.FileName}" : "No se pudo exportar el layout.";
            }, "Exportando layout...");
        }

        private async Task ApplyAsync()
        {
            if (SelectedLayout == null) return;
            await RunSafeAsync(async () =>
            {
                await Task.Run(() => ItemPlacementManager.LoadLayoutVersion(SelectedLayout.LayoutName, SelectedLayout.FormType));
                StatusMessage = $"Layout aplicado: {SelectedLayout.DisplayName}";
            }, "Aplicando layout en SAP...");
        }

        private async Task DeleteAsync()
        {
            if (SelectedLayout == null) return;
            await RunSafeAsync(async () =>
            {
                await Task.Run(() => ItemPlacementManager.DeleteLayout(SelectedLayout.LayoutName, SelectedLayout.FormType));
                await _dispatcher.InvokeAsync(() =>
                {
                    _layouts.Remove(SelectedLayout);
                    SelectedLayout = _layouts.FirstOrDefault();
                }, DispatcherPriority.DataBind);
                StatusMessage = "Layout eliminado.";
            }, "Eliminando layout...");
        }

        private async Task ImportAsync()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "SRF/XML (*.srf;*.xml)|*.srf;*.xml|Todos (*.*)|*.*"
            };
            if (ofd.ShowDialog() != true) return;

            var targetFormType = string.IsNullOrWhiteSpace(ManualFormType)
                ? B1App.Instance?.Application?.Forms?.ActiveForm?.TypeEx
                : ManualFormType.Trim();

            if (string.IsNullOrWhiteSpace(targetFormType))
            {
                StatusMessage = "Activa un formulario en SAP o ingresa el tipo manualmente.";
                return;
            }

            var layoutName = string.IsNullOrWhiteSpace(NewLayoutName)
                ? System.IO.Path.GetFileNameWithoutExtension(ofd.FileName)
                : NewLayoutName.Trim();

            if (string.IsNullOrWhiteSpace(layoutName))
            {
                StatusMessage = "Ingresa un nombre para el layout.";
                return;
            }

            await RunSafeAsync(async () =>
            {
                bool saved = await Task.Run(() => ItemPlacementManager.SaveSrfToLayout(targetFormType, layoutName, ofd.FileName));
                if (saved)
                {
                    await LoadAsync();
                    StatusMessage = $"Layout importado: {layoutName}";
                    NewLayoutName = string.Empty;
                }
                else
                {
                    StatusMessage = "No se pudo guardar el layout importado.";
                }
            }, "Importando layout...");
        }

        private void DetectActiveForm()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                ManualFormType = form?.TypeEx ?? ManualFormType;
            }
            catch
            {
                if (ManualFormType == null)
                {
                    ManualFormType = string.Empty;
                }
            }
        }

        private LayoutDefinitionEntry MapLayout(DataRow row)
        {
            return new LayoutDefinitionEntry
            {
                LayoutName = row["U_Name"]?.ToString(),
                FormType = row["U_FormType"]?.ToString(),
                Description = row["U_Desc"]?.ToString(),
                FileName = row["U_FileName"]?.ToString(),
                Owner = row["U_Owner"]?.ToString(),
                CreatedAt = TryDate(row["U_CreatedAt"]),
                UpdatedAt = TryDate(row["U_UpdatedAt"]),
                Version = TryInt(row["U_Version"])
            };
        }

        private static DateTime? TryDate(object value)
        {
            if (value == null) return null;
            DateTime dt;
            return DateTime.TryParse(value.ToString(), out dt) ? (DateTime?)dt : null;
        }

        private static int? TryInt(object value)
        {
            if (value == null) return null;
            int number;
            return int.TryParse(value.ToString(), out number) ? (int?)number : null;
        }

        private bool FilterLayout(object obj)
        {
            var entry = obj as LayoutDefinitionEntry;
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(SearchTerm)) return true;
            var term = SearchTerm.Trim();
            return (entry.FormType?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (entry.LayoutName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (entry.Description?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
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
                B1App.Instance?.Application?.SetStatusBarMessage($"LayoutManager: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
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
            ExportCommand.RaiseCanExecuteChanged();
            ApplyCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
