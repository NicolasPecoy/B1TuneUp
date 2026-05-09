using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class PlacementEnhancementViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<LayoutDefinitionEntry> _layouts = new ObservableCollection<LayoutDefinitionEntry>();
        private readonly ICollectionView _layoutsView;
        private readonly Dispatcher _dispatcher;

        private LayoutDefinitionEntry _selectedLayout;
        private string _layoutSearch;
        private string _layoutFormFilter;
        private string _newLayoutName;
        private string _newLayoutDescription;
        private string _manualFormType;
        private string _activeFormInfo;
        private string _richTextItemId;
        private string _pivotGridId;
        private string _barcodeTargetId;
        private string _dashboardQuery;
        private string _activeItemInfo;
        private string _placementItemId;
        private string _placementAction = "Move";
        private string _placementLabel;
        private int? _placementLeft;
        private int? _placementTop;
        private int? _placementWidth;
        private int? _placementHeight;
        private string _placementUserCode;
        private string _placementCondition;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public PlacementEnhancementViewModel()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _layoutsView = CollectionViewSource.GetDefaultView(_layouts);
            _layoutsView.Filter = obj => FilterLayout(obj as LayoutDefinitionEntry);

            RefreshCommand = new RelayCommand(async () => await LoadLayoutsAsync());
            SaveLayoutCommand = new RelayCommand(async () => await SaveLayoutAsync(), () => !string.IsNullOrWhiteSpace(NewLayoutName));
            ApplyLayoutCommand = new RelayCommand(async () => await ApplyLayoutAsync(), () => SelectedLayout != null);
            DeleteLayoutCommand = new RelayCommand(async () => await DeleteLayoutAsync(), () => SelectedLayout != null);
            ExportLayoutCommand = new RelayCommand(async () => await ExportLayoutAsync(), () => SelectedLayout != null);
            ImportLayoutCommand = new RelayCommand(async () => await ImportLayoutAsync());

            DetectActiveFormCommand = new RelayCommand(UpdateActiveFormInfo);
            OpenPlacementCommand = new RelayCommand(() => ItemPlacementManager.OpenPlacementForm(null));
            LaunchDragDropCommand = new RelayCommand(() => UIEnhancementsManager.EnableDragAndDrop(null));
            OpenRichTextCommand = new RelayCommand(() => UIEnhancementsManager.OpenRichTextEditor(RichTextItemId, null), () => !string.IsNullOrWhiteSpace(RichTextItemId));
            OpenPivotCommand = new RelayCommand(() => UIEnhancementsManager.EnhanceGridWithPivot(null, PivotGridId), () => !string.IsNullOrWhiteSpace(PivotGridId));
            OpenBarcodeCommand = new RelayCommand(() => UIEnhancementsManager.ScanBarcode(BarcodeTargetId, null));
            OpenDashboardCommand = new RelayCommand(() => UIEnhancementsManager.ShowAdvancedDashboard(DashboardQuery));
            OpenDesignerCommand = new RelayCommand(() => UIEnhancementsManager.OpenVisualDesigner(null));
            CaptureActiveItemCommand = new RelayCommand(CaptureActiveItem);
            SavePlacementRuleCommand = new RelayCommand(async () => await SavePlacementRuleAsync(), () => !string.IsNullOrWhiteSpace(PlacementItemId) && !string.IsNullOrWhiteSpace(PlacementAction));
            ExportPackageCommand = new RelayCommand(ExportPackage, () => !string.IsNullOrWhiteSpace(ManualFormType));
            ImportPackageCommand = new RelayCommand(ImportPackage);

            UpdateActiveFormInfo();
        }

        public ICollectionView LayoutsView => _layoutsView;

        public LayoutDefinitionEntry SelectedLayout
        {
            get => _selectedLayout;
            set
            {
                if (_selectedLayout == value) return;
                _selectedLayout = value;
                OnPropertyChanged();
                RaiseCommandStates();
            }
        }

        public string LayoutSearch
        {
            get => _layoutSearch;
            set
            {
                if (_layoutSearch == value) return;
                _layoutSearch = value;
                OnPropertyChanged();
                _layoutsView.Refresh();
            }
        }

        public string LayoutFormFilter
        {
            get => _layoutFormFilter;
            set
            {
                if (_layoutFormFilter == value) return;
                _layoutFormFilter = value;
                OnPropertyChanged();
                _layoutsView.Refresh();
            }
        }

        public string NewLayoutName
        {
            get => _newLayoutName;
            set
            {
                if (_newLayoutName == value) return;
                _newLayoutName = value;
                OnPropertyChanged();
                SaveLayoutCommand.RaiseCanExecuteChanged();
            }
        }

        public string NewLayoutDescription
        {
            get => _newLayoutDescription;
            set { if (_newLayoutDescription == value) return; _newLayoutDescription = value; OnPropertyChanged(); }
        }

        public string ManualFormType
        {
            get => _manualFormType;
            set
            {
                if (_manualFormType == value) return;
                _manualFormType = value;
                OnPropertyChanged();
                ExportPackageCommand.RaiseCanExecuteChanged();
            }
        }

        public string ActiveFormInfo
        {
            get => _activeFormInfo;
            private set { if (_activeFormInfo == value) return; _activeFormInfo = value; OnPropertyChanged(); }
        }

        public string RichTextItemId
        {
            get => _richTextItemId;
            set
            {
                if (_richTextItemId == value) return;
                _richTextItemId = value;
                OnPropertyChanged();
                OpenRichTextCommand.RaiseCanExecuteChanged();
            }
        }

        public string PivotGridId
        {
            get => _pivotGridId;
            set
            {
                if (_pivotGridId == value) return;
                _pivotGridId = value;
                OnPropertyChanged();
                OpenPivotCommand.RaiseCanExecuteChanged();
            }
        }

        public string BarcodeTargetId
        {
            get => _barcodeTargetId;
            set { if (_barcodeTargetId == value) return; _barcodeTargetId = value; OnPropertyChanged(); }
        }

        public string DashboardQuery
        {
            get => _dashboardQuery;
            set { if (_dashboardQuery == value) return; _dashboardQuery = value; OnPropertyChanged(); }
        }

        public string ActiveItemInfo
        {
            get => _activeItemInfo;
            private set { if (_activeItemInfo == value) return; _activeItemInfo = value; OnPropertyChanged(); }
        }

        public string PlacementItemId
        {
            get => _placementItemId;
            set
            {
                if (_placementItemId == value) return;
                _placementItemId = value;
                OnPropertyChanged();
                SavePlacementRuleCommand.RaiseCanExecuteChanged();
            }
        }

        public string PlacementAction
        {
            get => _placementAction;
            set
            {
                if (_placementAction == value) return;
                _placementAction = value;
                OnPropertyChanged();
                SavePlacementRuleCommand.RaiseCanExecuteChanged();
            }
        }

        public string PlacementLabel
        {
            get => _placementLabel;
            set { if (_placementLabel == value) return; _placementLabel = value; OnPropertyChanged(); }
        }

        public int? PlacementLeft
        {
            get => _placementLeft;
            set { if (_placementLeft == value) return; _placementLeft = value; OnPropertyChanged(); }
        }

        public int? PlacementTop
        {
            get => _placementTop;
            set { if (_placementTop == value) return; _placementTop = value; OnPropertyChanged(); }
        }

        public int? PlacementWidth
        {
            get => _placementWidth;
            set { if (_placementWidth == value) return; _placementWidth = value; OnPropertyChanged(); }
        }

        public int? PlacementHeight
        {
            get => _placementHeight;
            set { if (_placementHeight == value) return; _placementHeight = value; OnPropertyChanged(); }
        }

        public string PlacementUserCode
        {
            get => _placementUserCode;
            set { if (_placementUserCode == value) return; _placementUserCode = value; OnPropertyChanged(); }
        }

        public string PlacementCondition
        {
            get => _placementCondition;
            set { if (_placementCondition == value) return; _placementCondition = value; OnPropertyChanged(); }
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
        public RelayCommand SaveLayoutCommand { get; }
        public RelayCommand ApplyLayoutCommand { get; }
        public RelayCommand DeleteLayoutCommand { get; }
        public RelayCommand ExportLayoutCommand { get; }
        public RelayCommand ImportLayoutCommand { get; }

        public RelayCommand DetectActiveFormCommand { get; }
        public RelayCommand OpenPlacementCommand { get; }
        public RelayCommand LaunchDragDropCommand { get; }
        public RelayCommand OpenRichTextCommand { get; }
        public RelayCommand OpenPivotCommand { get; }
        public RelayCommand OpenBarcodeCommand { get; }
        public RelayCommand OpenDashboardCommand { get; }
        public RelayCommand OpenDesignerCommand { get; }
        public RelayCommand CaptureActiveItemCommand { get; }
        public RelayCommand SavePlacementRuleCommand { get; }
        public RelayCommand ExportPackageCommand { get; }
        public RelayCommand ImportPackageCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Task LoadLayoutsAsync()
            => RunSafeAsync("Cargando layouts guardados...", async () =>
            {
                await ReloadLayoutsInternalAsync();
                StatusMessage = $"Se cargaron {_layouts.Count} layouts.";
            });

        private async Task SaveLayoutAsync()
        {
            await RunSafeAsync("Guardando layout del formulario activo...", async () =>
            {
                SaveLayoutFromActiveForm();
                await ReloadLayoutsInternalAsync();
                StatusMessage = $"Layout '{NewLayoutName}' guardado.";
            });
        }

        private async Task ApplyLayoutAsync()
        {
            if (SelectedLayout == null) return;
            await RunSafeAsync("Aplicando layout seleccionado...", () =>
            {
                ApplyLayoutToActiveForm(SelectedLayout);
                StatusMessage = $"Layout '{SelectedLayout.LayoutName}' aplicado.";
                return Task.CompletedTask;
            });
        }

        private async Task DeleteLayoutAsync()
        {
            if (SelectedLayout == null) return;
            await RunSafeAsync("Eliminando layout...", async () =>
            {
                await Task.Run(() => ItemPlacementManager.DeleteLayout(SelectedLayout.LayoutName, SelectedLayout.FormType));
                await ReloadLayoutsInternalAsync();
                StatusMessage = $"Layout '{SelectedLayout.LayoutName}' eliminado.";
            });
        }

        private async Task ExportLayoutAsync()
        {
            if (SelectedLayout == null) return;
            var dlg = new SaveFileDialog
            {
                Filter = "Archivo SRF o XML (*.srf;*.xml)|*.srf;*.xml|Todos los archivos (*.*)|*.*",
                FileName = $"{SelectedLayout.FormType}_{SelectedLayout.LayoutName}.srf"
            };
            if (dlg.ShowDialog() != true) return;

            string targetPath = dlg.FileName;
            await RunSafeAsync("Exportando layout...", () =>
            {
                ExportLayoutToFile(SelectedLayout, targetPath);
                StatusMessage = $"Layout exportado a {Path.GetFileName(targetPath)}.";
                return Task.CompletedTask;
            });
        }

        private async Task ImportLayoutAsync()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Archivo SRF o XML (*.srf;*.xml)|*.srf;*.xml|Todos los archivos (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            string sourceFile = dlg.FileName;
            await RunSafeAsync("Importando layout desde archivo...", async () =>
            {
                ImportLayoutFromFile(sourceFile);
                await ReloadLayoutsInternalAsync();
                StatusMessage = $"Layout importado desde {Path.GetFileName(sourceFile)}.";
            });
        }

        private async Task SavePlacementRuleAsync()
        {
            await RunSafeAsync("Guardando regla de item placement...", async () =>
            {
                string formType = ResolveCurrentFormType();
                var entry = new UiCustomizationEntry
                {
                    FormType = formType,
                    ItemId = PlacementItemId?.Trim(),
                    Action = PlacementAction?.Trim(),
                    Label = PlacementLabel ?? string.Empty,
                    Left = PlacementLeft,
                    Top = PlacementTop,
                    Width = PlacementWidth,
                    Height = PlacementHeight,
                    FromPane = 0,
                    ToPane = 0,
                    UserCode = PlacementUserCode ?? string.Empty,
                    Condition = PlacementCondition ?? string.Empty,
                    Priority = 10
                };

                await Task.Run(() => UICustomizerService.Save(entry));
                UICustomizerService.RefreshActiveForm();
                StatusMessage = $"Regla {entry.Action} guardada para {entry.FormType}/{entry.ItemId}.";
            });
        }

        private void CaptureActiveItem()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                if (form == null)
                {
                    ActiveItemInfo = "Sin formulario activo.";
                    return;
                }

                string activeItemId = GetActiveItemId(form);
                if (string.IsNullOrWhiteSpace(activeItemId))
                {
                    ActiveItemInfo = "Selecciona un item en SAP y vuelve a capturar.";
                    return;
                }

                var item = form.Items.Item(activeItemId);
                ManualFormType = form.TypeEx;
                PlacementItemId = activeItemId;
                PlacementLeft = item.Left;
                PlacementTop = item.Top;
                PlacementWidth = item.Width;
                PlacementHeight = item.Height;
                ActiveItemInfo = $"{form.TypeEx}/{activeItemId} {item.Type} L:{item.Left} T:{item.Top} W:{item.Width} H:{item.Height} Pane:{item.FromPane}-{item.ToPane}";
            }
            catch (Exception ex)
            {
                ActiveItemInfo = ex.Message;
            }
        }

        private void ExportPackage()
        {
            var formType = ResolveCurrentFormType();
            var dlg = new SaveFileDialog
            {
                Filter = "B1TuneUp package (*.json)|*.json|Todos los archivos (*.*)|*.*",
                FileName = $"B1TuneUp_{formType}_package.json"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                UICustomizerService.ExportPackage(formType, dlg.FileName);
                StatusMessage = $"Paquete exportado para {formType}.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void ImportPackage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "B1TuneUp package (*.json)|*.json|Todos los archivos (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var package = UICustomizerService.ImportPackage(dlg.FileName);
                StatusMessage = $"Paquete importado para {package.FormType}.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void SaveLayoutFromActiveForm()
        {
            var app = B1App.Instance?.Application;
            var form = app?.Forms?.ActiveForm;
            if (form == null)
            {
                throw new InvalidOperationException("No hay un formulario activo en SAP Business One.");
            }

            var layoutName = (NewLayoutName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(layoutName))
            {
                throw new InvalidOperationException("Ingresa un nombre para el layout.");
            }

            string type = ResolveFormType(form);
            var tempPath = Path.Combine(Path.GetTempPath(), $"btun_layout_{Guid.NewGuid():N}.srf");
            try
            {
                if (!ItemPlacementManager.ExportSrf(form, tempPath))
                {
                    throw new InvalidOperationException("No se pudo exportar el formulario activo.");
                }
                var xml = File.ReadAllText(tempPath);
                if (!ItemPlacementManager.SaveLayout(type, layoutName, xml, NewLayoutDescription ?? string.Empty))
                {
                    throw new InvalidOperationException("SAP Business One rechazo el guardado del layout.");
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        private void ApplyLayoutToActiveForm(LayoutDefinitionEntry layout)
        {
            var form = B1App.Instance?.Application?.Forms?.ActiveForm;
            if (form == null)
            {
                throw new InvalidOperationException("No hay un formulario activo para aplicar el layout.");
            }

            var def = ItemPlacementManager.GetLayoutDefinition(layout.LayoutName, layout.FormType);
            if (!string.IsNullOrEmpty(def))
            {
                if (!ItemPlacementManager.ApplyLayoutToForm(def, form))
                {
                    throw new InvalidOperationException("Error aplicando la definicion al formulario.");
                }
                return;
            }

            var temp = Path.Combine(Path.GetTempPath(), $"btun_layout_{Guid.NewGuid():N}.srf");
            try
            {
                if (!ItemPlacementManager.RestoreSrfFromLayout(layout.LayoutName, layout.FormType, temp))
                {
                    throw new InvalidOperationException("No se encontro archivo SRF para este layout.");
                }
                if (!ItemPlacementManager.ImportSrf(temp))
                {
                    throw new InvalidOperationException("SAP no pudo cargar el archivo SRF.");
                }
            }
            finally
            {
                if (File.Exists(temp))
                {
                    try { File.Delete(temp); } catch { }
                }
            }
        }

        private void ExportLayoutToFile(LayoutDefinitionEntry layout, string targetPath)
        {
            var def = ItemPlacementManager.GetLayoutDefinition(layout.LayoutName, layout.FormType);
            if (!string.IsNullOrEmpty(def))
            {
                File.WriteAllText(targetPath, def);
                return;
            }

            if (!ItemPlacementManager.RestoreSrfFromLayout(layout.LayoutName, layout.FormType, targetPath))
            {
                throw new InvalidOperationException("No fue posible exportar el layout seleccionado.");
            }
        }

        private void ImportLayoutFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("No se encontro el archivo seleccionado.", filePath);
            }

            var content = File.ReadAllText(filePath);
            var app = B1App.Instance?.Application;
            var activeForm = app?.Forms?.ActiveForm;
            var formType = !string.IsNullOrWhiteSpace(ManualFormType)
                ? ManualFormType.Trim()
                : activeForm?.TypeEx;

            if (string.IsNullOrWhiteSpace(formType))
            {
                throw new InvalidOperationException("Ingresa el tipo de formulario o activa uno en SAP antes de importar.");
            }

            var targetName = string.IsNullOrWhiteSpace(NewLayoutName)
                ? Path.GetFileNameWithoutExtension(filePath)
                : NewLayoutName.Trim();

            if (!ItemPlacementManager.SaveLayout(formType, targetName, content, NewLayoutDescription ?? string.Empty))
            {
                throw new InvalidOperationException("No se pudo guardar el layout importado en SAP.");
            }
        }

        private async Task ReloadLayoutsInternalAsync()
        {
            var table = await Task.Run(() =>
            {
                try
                {
                    return ItemPlacementManager.GetLayouts(null);
                }
                catch
                {
                    return null;
                }
            });
            var dispatcher = _dispatcher ?? Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            await dispatcher.InvokeAsync(() =>
            {
                _layouts.Clear();
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        _layouts.Add(MapLayout(row));
                    }
                }
                _layoutsView.Refresh();
            });
        }

        private LayoutDefinitionEntry MapLayout(DataRow row)
        {
            DateTime? created = TryDate(row["U_CreatedAt"]);
            DateTime? updated = TryDate(row["U_UpdatedAt"]);
            int? version = TryInt(row["U_Version"]);
            return new LayoutDefinitionEntry
            {
                LayoutName = row["U_Name"]?.ToString(),
                FormType = row["U_FormType"]?.ToString(),
                Description = row["U_Desc"]?.ToString(),
                FileName = row["U_FileName"]?.ToString(),
                Owner = row["U_Owner"]?.ToString(),
                CreatedAt = created,
                UpdatedAt = updated,
                Version = version
            };
        }

        private void UpdateActiveFormInfo()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                if (form == null)
                {
                    ActiveFormInfo = "Sin formulario activo.";
                    return;
                }

                ActiveFormInfo = $"{form.TypeEx} - {form.Title}";
                ActiveItemInfo = "Item activo no capturado.";
                if (string.IsNullOrWhiteSpace(ManualFormType))
                {
                    ManualFormType = form.TypeEx;
                }
                ExportPackageCommand.RaiseCanExecuteChanged();
            }
            catch
            {
                ActiveFormInfo = "Sin formulario activo.";
                ActiveItemInfo = "Sin item activo.";
            }
        }

        private bool FilterLayout(LayoutDefinitionEntry entry)
        {
            if (entry == null) return false;
            if (!string.IsNullOrWhiteSpace(LayoutFormFilter) && !Contains(entry.FormType, LayoutFormFilter)) return false;
            if (string.IsNullOrWhiteSpace(LayoutSearch)) return true;
            return Contains(entry.LayoutName, LayoutSearch)
                || Contains(entry.Description, LayoutSearch)
                || Contains(entry.Owner, LayoutSearch)
                || Contains(entry.FileName, LayoutSearch);
        }

        private static bool Contains(string source, string term)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(term)) return false;
            return source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static DateTime? TryDate(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out var dt)) return dt;
            return null;
        }

        private static int? TryInt(object value)
        {
            if (value == null) return null;
            if (int.TryParse(value.ToString(), out var intValue)) return intValue;
            return null;
        }

        private string ResolveFormType(SAPbouiCOM.Form form)
        {
            if (!string.IsNullOrWhiteSpace(ManualFormType)) return ManualFormType.Trim();
            if (form != null && !string.IsNullOrWhiteSpace(form.TypeEx)) return form.TypeEx;
            throw new InvalidOperationException("No se pudo determinar el tipo de formulario. Active uno en SAP o completa el campo Tipo de formulario.");
        }

        private string ResolveCurrentFormType()
        {
            if (!string.IsNullOrWhiteSpace(ManualFormType)) return ManualFormType.Trim();
            var form = B1App.Instance?.Application?.Forms?.ActiveForm;
            return ResolveFormType(form);
        }

        private static string GetActiveItemId(SAPbouiCOM.Form form)
        {
            try
            {
                var prop = form.GetType().GetProperty("ActiveItem");
                var value = prop?.GetValue(form, null)?.ToString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            catch { }

            try
            {
                var prop = form.GetType().GetProperty("ActiveItemUID");
                var value = prop?.GetValue(form, null)?.ToString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            catch { }

            return string.Empty;
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
            SaveLayoutCommand.RaiseCanExecuteChanged();
            ApplyLayoutCommand.RaiseCanExecuteChanged();
            DeleteLayoutCommand.RaiseCanExecuteChanged();
            ExportLayoutCommand.RaiseCanExecuteChanged();
            OpenRichTextCommand.RaiseCanExecuteChanged();
            OpenPivotCommand.RaiseCanExecuteChanged();
            SavePlacementRuleCommand.RaiseCanExecuteChanged();
            ExportPackageCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
