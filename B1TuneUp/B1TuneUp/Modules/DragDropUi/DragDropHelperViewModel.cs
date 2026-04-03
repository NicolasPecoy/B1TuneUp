using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.DragDropUi
{
    public class DragDropHelperViewModel : INotifyPropertyChanged
    {
        private string _sourceItemId;
        private string _targetItemId;
        private string _statusMessage;

        public DragDropHelperViewModel()
        {
            BindCommand = new RelayCommand(BindItems, CanBind);
        }

        public string SourceItemId
        {
            get => _sourceItemId;
            set
            {
                if (_sourceItemId == value) return;
                _sourceItemId = value;
                OnPropertyChanged();
                BindCommand.RaiseCanExecuteChanged();
            }
        }

        public string TargetItemId
        {
            get => _targetItemId;
            set
            {
                if (_targetItemId == value) return;
                _targetItemId = value;
                OnPropertyChanged();
                BindCommand.RaiseCanExecuteChanged();
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

        public RelayCommand BindCommand { get; }

        private bool CanBind()
        {
            return !string.IsNullOrWhiteSpace(SourceItemId) && !string.IsNullOrWhiteSpace(TargetItemId);
        }

        private void BindItems()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                if (form == null)
                {
                    StatusMessage = "No hay formulario activo en SAP Business One.";
                    return;
                }
                var src = SourceItemId.Trim();
                var tgt = TargetItemId.Trim();
                EventDispatcher.Instance.RegisterLocalItemChangeHandler(form, src, (sapForm, _) =>
                {
                    try
                    {
                        if (!sapForm.Items.Exists(src) || !sapForm.Items.Exists(tgt)) return;
                        var sourceItem = sapForm.Items.Item(src);
                        var targetItem = sapForm.Items.Item(tgt);
                        string value = string.Empty;
                        if (sourceItem.Specific is EditText editText)
                        {
                            value = editText.Value ?? string.Empty;
                        }
                        else if (sourceItem.Specific is ComboBox comboBox)
                        {
                            value = comboBox.Selected?.Value ?? comboBox.Selected?.Description ?? string.Empty;
                        }
                        if (targetItem.Specific is EditText targetEdit)
                        {
                            targetEdit.Value = value;
                        }
                        else if (targetItem.Specific is ComboBox targetCombo && !string.IsNullOrEmpty(value))
                        {
                            targetCombo.Select(value, BoSearchKey.psk_ByValue);
                        }
                    }
                    catch { }
                });
                StatusMessage = $"Bind registrado: {src} → {tgt}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error registrando bind: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
