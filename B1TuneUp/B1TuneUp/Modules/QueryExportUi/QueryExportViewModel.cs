using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.QueryExportUi
{
    public class QueryExportViewModel : INotifyPropertyChanged
    {
        private string _sqlText;
        private string _statusMessage;

        public QueryExportViewModel()
        {
            LoadFromActiveFormCommand = new RelayCommand(LoadFromActiveForm);
            ExportCsvCommand = new RelayCommand(() => Export("CSV"), () => !string.IsNullOrWhiteSpace(SqlText));
            ExportJsonCommand = new RelayCommand(() => Export("JSON"), () => !string.IsNullOrWhiteSpace(SqlText));
            ExportXmlCommand = new RelayCommand(() => Export("XML"), () => !string.IsNullOrWhiteSpace(SqlText));
        }

        public string SqlText
        {
            get => _sqlText;
            set
            {
                if (_sqlText == value) return;
                _sqlText = value;
                OnPropertyChanged();
                ExportCsvCommand.RaiseCanExecuteChanged();
                ExportJsonCommand.RaiseCanExecuteChanged();
                ExportXmlCommand.RaiseCanExecuteChanged();
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

        public RelayCommand LoadFromActiveFormCommand { get; }
        public RelayCommand ExportCsvCommand { get; }
        public RelayCommand ExportJsonCommand { get; }
        public RelayCommand ExportXmlCommand { get; }

        private void LoadFromActiveForm()
        {
            try
            {
                var form = SapUiSafe.TryGetActiveForm();
                if (form == null)
                {
                    StatusMessage = "No hay formulario activo.";
                    return;
                }

                string loaded = TryReadFromCommonItems(form);
                if (string.IsNullOrEmpty(loaded))
                {
                    loaded = TryReadFromSelectedGridRow(form);
                }
                if (!string.IsNullOrEmpty(loaded))
                {
                    SqlText = loaded;
                    StatusMessage = "Consulta obtenida del formulario activo.";
                }
                else
                {
                    StatusMessage = "No se pudo detectar la consulta. Ingresa manualmente.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error leyendo formulario: {ex.Message}";
            }
        }

        private static string TryReadFromCommonItems(Form form)
        {
            string[] ids = { "txtQuery", "txtSQL", "edQuery", "edtQuery", "txt1" };
            foreach (var id in ids)
            {
                try
                {
                    var item = SapUiSafe.TryGetItem(form, id);
                    if (item != null)
                    {
                        if (SapUiSafe.TryGetSpecific<EditText>(item) is EditText editText)
                        {
                            return editText.Value;
                        }
                        if (SapUiSafe.TryGetSpecific<ComboBox>(item) is ComboBox combo)
                        {
                            return combo.Selected?.Description ?? SapUiSafe.SafeComboValue(combo);
                        }
                        if (SapUiSafe.TryGetSpecific<StaticText>(item) is StaticText staticText)
                        {
                            return staticText.Caption;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private static string TryReadFromSelectedGridRow(Form form)
        {
            try
            {
                for (int i = 0; i < form.Items.Count; i++)
                {
                    Item it = null;
                    try { it = SapUiSafe.TryGetItem(form, i + 1); } catch { }
                    if (it == null) continue;
                    if (it.Type != BoFormItemTypes.it_GRID) continue;
                    var grid = SapUiSafe.TryGetSpecific<Grid>(it);
                    if (grid == null) continue;
                    if (grid.Rows.SelectedRows.Count <= 0) continue;
                    int rowIndex = grid.GetDataTableRowIndex(grid.Rows.SelectedRows.Item(0));
                    string[] columns = { "Query", "U_Query", "SQL", "Qry" };
                    foreach (var col in columns)
                    {
                        try
                        {
                            var val = grid.DataTable.GetValue(col, rowIndex)?.ToString();
                            if (!string.IsNullOrEmpty(val)) return val;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        private void Export(string format)
        {
            try
            {
                var sql = SqlText;
                if (string.IsNullOrWhiteSpace(sql))
                {
                    StatusMessage = "Ingresa una consulta.";
                    return;
                }

                var dialog = new SaveFileDialog();
                switch (format)
                {
                    case "CSV":
                        dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                        dialog.FileName = "export.csv";
                        break;
                    case "JSON":
                        dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                        dialog.FileName = "export.json";
                        break;
                    default:
                        dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                        dialog.FileName = "export.xml";
                        break;
                }
                if (dialog.ShowDialog() != true) return;

                var dt = DynamicMapperManager.ExecuteQueryToDataTable(sql);
                switch (format)
                {
                    case "CSV":
                        DynamicMapperManager.ExportDataTableToCsv(dt, dialog.FileName, true);
                        break;
                    case "JSON":
                        var json = DynamicMapperManager.DataTableToJson(dt);
                        System.IO.File.WriteAllText(dialog.FileName, json, System.Text.Encoding.UTF8);
                        break;
                    case "XML":
                        var xml = DynamicMapperManager.DataTableToXml(dt);
                        System.IO.File.WriteAllText(dialog.FileName, xml, System.Text.Encoding.UTF8);
                        break;
                }
                StatusMessage = $"Archivo guardado en {dialog.FileName}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exportando: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
