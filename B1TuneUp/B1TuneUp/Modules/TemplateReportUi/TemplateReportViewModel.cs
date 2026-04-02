using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;
using Microsoft.Win32;

namespace B1TuneUp.Modules.TemplateReportUi
{
    public class TemplateReportViewModel : INotifyPropertyChanged
    {
        private FormTemplateDefinition _selectedFormTemplate;
        private ReportTemplateDefinition _selectedReportTemplate;
        private string _formSearch;
        private string _formTypeFilter;
        private string _reportSearch;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public TemplateReportViewModel()
        {
            FormTemplates = new ObservableCollection<FormTemplateDefinition>();
            ReportTemplates = new ObservableCollection<ReportTemplateDefinition>();

            RefreshAllCommand = new RelayCommand(async () => await LoadAllAsync());

            RefreshFormTemplatesCommand = new RelayCommand(async () => await LoadFormTemplatesAsync());
            NewFormTemplateCommand = new RelayCommand(NewFormTemplate);
            DuplicateFormTemplateCommand = new RelayCommand(DuplicateFormTemplate, () => SelectedFormTemplate != null);
            SaveFormTemplateCommand = new RelayCommand(async () => await SaveFormTemplateAsync(), () => SelectedFormTemplate != null);
            DeleteFormTemplateCommand = new RelayCommand(async () => await DeleteFormTemplateAsync(), () => SelectedFormTemplate?.DocEntry != null);
            CopyTemplateDataCommand = new RelayCommand(CopyTemplateData, () => SelectedFormTemplate != null && !string.IsNullOrEmpty(SelectedFormTemplate.SerializedData));
            PasteTemplateDataCommand = new RelayCommand(PasteTemplateData, () => SelectedFormTemplate != null);

            RefreshReportTemplatesCommand = new RelayCommand(async () => await LoadReportTemplatesAsync());
            NewReportTemplateCommand = new RelayCommand(NewReportTemplate);
            DuplicateReportTemplateCommand = new RelayCommand(DuplicateReportTemplate, () => SelectedReportTemplate != null);
            SaveReportTemplateCommand = new RelayCommand(async () => await SaveReportTemplateAsync(), () => SelectedReportTemplate != null);
            DeleteReportTemplateCommand = new RelayCommand(async () => await DeleteReportTemplateAsync(), () => SelectedReportTemplate?.DocEntry != null);
            ImportReportFileCommand = new RelayCommand(ImportReportFile, () => SelectedReportTemplate != null);
            ExportReportFileCommand = new RelayCommand(ExportReportFile, () => SelectedReportTemplate != null && !string.IsNullOrEmpty(SelectedReportTemplate.DataBase64));
            CopyReportDataCommand = new RelayCommand(CopyReportData, () => SelectedReportTemplate != null && !string.IsNullOrEmpty(SelectedReportTemplate.DataBase64));
        }

        public ObservableCollection<FormTemplateDefinition> FormTemplates { get; }
        public ObservableCollection<ReportTemplateDefinition> ReportTemplates { get; }

        public RelayCommand RefreshAllCommand { get; }
        public RelayCommand RefreshFormTemplatesCommand { get; }
        public RelayCommand NewFormTemplateCommand { get; }
        public RelayCommand DuplicateFormTemplateCommand { get; }
        public RelayCommand SaveFormTemplateCommand { get; }
        public RelayCommand DeleteFormTemplateCommand { get; }
        public RelayCommand CopyTemplateDataCommand { get; }
        public RelayCommand PasteTemplateDataCommand { get; }

        public RelayCommand RefreshReportTemplatesCommand { get; }
        public RelayCommand NewReportTemplateCommand { get; }
        public RelayCommand DuplicateReportTemplateCommand { get; }
        public RelayCommand SaveReportTemplateCommand { get; }
        public RelayCommand DeleteReportTemplateCommand { get; }
        public RelayCommand ImportReportFileCommand { get; }
        public RelayCommand ExportReportFileCommand { get; }
        public RelayCommand CopyReportDataCommand { get; }

        public FormTemplateDefinition SelectedFormTemplate
        {
            get => _selectedFormTemplate;
            set
            {
                if (_selectedFormTemplate == value) return;
                _selectedFormTemplate = value;
                OnPropertyChanged();
                RaiseFormCommandStates();
            }
        }

        public ReportTemplateDefinition SelectedReportTemplate
        {
            get => _selectedReportTemplate;
            set
            {
                if (_selectedReportTemplate == value) return;
                _selectedReportTemplate = value;
                OnPropertyChanged();
                RaiseReportCommandStates();
            }
        }

        public string FormSearch
        {
            get => _formSearch;
            set
            {
                if (_formSearch == value) return;
                _formSearch = value;
                OnPropertyChanged();
            }
        }

        public string FormTypeFilter
        {
            get => _formTypeFilter;
            set
            {
                if (_formTypeFilter == value) return;
                _formTypeFilter = value;
                OnPropertyChanged();
            }
        }

        public string ReportSearch
        {
            get => _reportSearch;
            set
            {
                if (_reportSearch == value) return;
                _reportSearch = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set
            {
                if (_busyMessage == value) return;
                _busyMessage = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Task LoadAllAsync()
        {
            return RunSafeAsync("Cargando templates y reportes...", async () =>
            {
                await Task.WhenAll(
                    LoadFormTemplatesInternalAsync(),
                    LoadReportTemplatesInternalAsync());
            });
        }

        public Task LoadFormTemplatesAsync()
            => RunSafeAsync("Cargando templates de formulario...", LoadFormTemplatesInternalAsync);

        public Task LoadReportTemplatesAsync()
            => RunSafeAsync("Cargando report templates...", LoadReportTemplatesInternalAsync);

        private async Task LoadFormTemplatesInternalAsync()
        {
            var previousId = SelectedFormTemplate?.DocEntry;
            var list = await Task.Run(() => TemplateStorageService.GetTemplates(FormTypeFilter, FormSearch));
            RunOnUiThread(() =>
            {
                FormTemplates.Clear();
                foreach (var item in list.OrderBy(t => t.Name))
                {
                    FormTemplates.Add(item);
                }
                var selection = FormTemplates.FirstOrDefault(t => t.DocEntry == previousId) ?? FormTemplates.FirstOrDefault();
                SelectedFormTemplate = selection ?? CreateFormTemplateShell();
            });
            StatusMessage = $"{FormTemplates.Count} templates de formulario.";
        }

        private async Task LoadReportTemplatesInternalAsync()
        {
            var previousId = SelectedReportTemplate?.DocEntry;
            var list = await Task.Run(() => ReportTemplateStorageService.GetTemplates(ReportSearch));
            RunOnUiThread(() =>
            {
                ReportTemplates.Clear();
                foreach (var item in list.OrderBy(r => r.Name))
                {
                    ReportTemplates.Add(item);
                }
                var selection = ReportTemplates.FirstOrDefault(r => r.DocEntry == previousId) ?? ReportTemplates.FirstOrDefault();
                SelectedReportTemplate = selection ?? CreateReportTemplateShell();
            });
            StatusMessage = $"{ReportTemplates.Count} templates de reporte.";
        }

        private void NewFormTemplate()
        {
            SelectedFormTemplate = CreateFormTemplateShell();
        }

        private void DuplicateFormTemplate()
        {
            if (SelectedFormTemplate == null) return;
            var clone = SelectedFormTemplate.Clone();
            clone.DocEntry = null;
            clone.Name = $"{clone.Name} (Copy)";
            clone.CreatedAt = DateTime.Now;
            clone.UpdatedAt = null;
            SelectedFormTemplate = clone;
        }

        private async Task SaveFormTemplateAsync()
        {
            if (SelectedFormTemplate == null) return;
            await RunSafeAsync("Guardando template...", async () =>
            {
                await Task.Run(() => TemplateStorageService.Save(SelectedFormTemplate));
                await LoadFormTemplatesInternalAsync();
            });
            StatusMessage = $"Template '{SelectedFormTemplate.Name}' guardado.";
        }

        private async Task DeleteFormTemplateAsync()
        {
            if (SelectedFormTemplate?.DocEntry == null) return;
            if (!Confirm($"¿Eliminar el template '{SelectedFormTemplate.Name}'?")) return;

            await RunSafeAsync("Eliminando template...", async () =>
            {
                await Task.Run(() => TemplateStorageService.Delete(SelectedFormTemplate.DocEntry));
                await LoadFormTemplatesInternalAsync();
            });
            StatusMessage = "Template eliminado.";
        }

        private void CopyTemplateData()
        {
            try
            {
                if (SelectedFormTemplate == null) return;
                Clipboard.SetText(SelectedFormTemplate.SerializedData ?? string.Empty);
                StatusMessage = "Datos serializados copiados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error copiando datos: {ex.Message}";
            }
        }

        private void PasteTemplateData()
        {
            try
            {
                if (SelectedFormTemplate == null) return;
                if (Clipboard.ContainsText())
                {
                    SelectedFormTemplate.SerializedData = Clipboard.GetText();
                    OnPropertyChanged(nameof(SelectedFormTemplate));
                    StatusMessage = "Datos pegados desde el portapapeles.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error pegando datos: {ex.Message}";
            }
        }

        private void NewReportTemplate()
        {
            SelectedReportTemplate = CreateReportTemplateShell();
        }

        private void DuplicateReportTemplate()
        {
            if (SelectedReportTemplate == null) return;
            var clone = SelectedReportTemplate.Clone();
            clone.DocEntry = null;
            clone.Name = $"{clone.Name} (Copy)";
            clone.CreatedAt = DateTime.Now;
            clone.UpdatedAt = null;
            SelectedReportTemplate = clone;
        }

        private async Task SaveReportTemplateAsync()
        {
            if (SelectedReportTemplate == null) return;
            await RunSafeAsync("Guardando template de reporte...", async () =>
            {
                await Task.Run(() => ReportTemplateStorageService.Save(SelectedReportTemplate));
                await LoadReportTemplatesInternalAsync();
            });
            StatusMessage = $"Report template '{SelectedReportTemplate.Name}' guardado.";
        }

        private async Task DeleteReportTemplateAsync()
        {
            if (SelectedReportTemplate?.DocEntry == null) return;
            if (!Confirm($"¿Eliminar el report template '{SelectedReportTemplate.Name}'?")) return;

            await RunSafeAsync("Eliminando reporte...", async () =>
            {
                await Task.Run(() => ReportTemplateStorageService.Delete(SelectedReportTemplate.DocEntry));
                await LoadReportTemplatesInternalAsync();
            });
            StatusMessage = "Report template eliminado.";
        }

        private void ImportReportFile()
        {
            try
            {
                if (SelectedReportTemplate == null) return;
                var dialog = new OpenFileDialog
                {
                    Title = "Seleccionar archivo de reporte",
                    Filter = "Crystal Reports (*.rpt)|*.rpt|Archivos PLD/XML (*.xml)|*.xml|Todos los archivos (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    var bytes = File.ReadAllBytes(dialog.FileName);
                    SelectedReportTemplate.DataBase64 = Convert.ToBase64String(bytes);
                    StatusMessage = $"Archivo '{Path.GetFileName(dialog.FileName)}' importado ({bytes.Length:N0} bytes).";
                    OnPropertyChanged(nameof(SelectedReportTemplate));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importando archivo: {ex.Message}";
            }
        }

        private void ExportReportFile()
        {
            try
            {
                if (SelectedReportTemplate == null || string.IsNullOrEmpty(SelectedReportTemplate.DataBase64)) return;
                var dialog = new SaveFileDialog
                {
                    Title = "Exportar template de reporte",
                    FileName = $"{SelectedReportTemplate.Name}.rpt",
                    Filter = "Crystal Reports (*.rpt)|*.rpt|Todos los archivos (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    var bytes = Convert.FromBase64String(SelectedReportTemplate.DataBase64);
                    File.WriteAllBytes(dialog.FileName, bytes);
                    StatusMessage = $"Archivo exportado a {dialog.FileName}.";
                }
            }
            catch (FormatException)
            {
                StatusMessage = "El campo DataBase64 no es válido.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exportando archivo: {ex.Message}";
            }
        }

        private void CopyReportData()
        {
            try
            {
                if (SelectedReportTemplate == null) return;
                Clipboard.SetText(SelectedReportTemplate.DataBase64 ?? string.Empty);
                StatusMessage = "Contenido base64 copiado al portapapeles.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error copiando base64: {ex.Message}";
            }
        }

        private static bool Confirm(string message)
        {
            return MessageBox.Show(message, "B1TuneUp", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
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
                RaiseFormCommandStates();
                RaiseReportCommandStates();
            }
        }

        private void RaiseFormCommandStates()
        {
            DuplicateFormTemplateCommand.RaiseCanExecuteChanged();
            SaveFormTemplateCommand.RaiseCanExecuteChanged();
            DeleteFormTemplateCommand.RaiseCanExecuteChanged();
            CopyTemplateDataCommand.RaiseCanExecuteChanged();
            PasteTemplateDataCommand.RaiseCanExecuteChanged();
        }

        private void RaiseReportCommandStates()
        {
            DuplicateReportTemplateCommand.RaiseCanExecuteChanged();
            SaveReportTemplateCommand.RaiseCanExecuteChanged();
            DeleteReportTemplateCommand.RaiseCanExecuteChanged();
            ImportReportFileCommand.RaiseCanExecuteChanged();
            ExportReportFileCommand.RaiseCanExecuteChanged();
            CopyReportDataCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private static void RunOnUiThread(Action action)
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

        private FormTemplateDefinition CreateFormTemplateShell()
        {
            return new FormTemplateDefinition
            {
                Name = "Nuevo template",
                Description = string.Empty,
                FormType = string.Empty,
                SerializedData = string.Empty,
                CreatedBy = B1App.Instance?.Company?.UserName,
                CreatedAt = DateTime.Now
            };
        }

        private ReportTemplateDefinition CreateReportTemplateShell()
        {
            return new ReportTemplateDefinition
            {
                Name = "Nuevo reporte",
                Description = string.Empty,
                DataBase64 = string.Empty,
                Parameters = string.Empty,
                CreatedAt = DateTime.Now
            };
        }
    }
}
