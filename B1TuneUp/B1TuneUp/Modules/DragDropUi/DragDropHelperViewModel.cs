using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Utils;

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
                var form = SapUiSafe.TryGetActiveForm();
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
                        var sourceItem = SapUiSafe.TryGetItem(sapForm, src);
                        var targetItem = SapUiSafe.TryGetItem(sapForm, tgt);
                        if (sourceItem == null || targetItem == null) return;
                        string value = string.Empty;
                        if (SapUiSafe.TryGetSpecific<EditText>(sourceItem) is EditText editText)
                        {
                            value = editText.Value ?? string.Empty;
                        }
                        else if (SapUiSafe.TryGetSpecific<ComboBox>(sourceItem) is ComboBox comboBox)
                        {
                            value = SapUiSafe.SafeComboValue(comboBox);
                            if (string.IsNullOrEmpty(value)) value = comboBox.Selected?.Description ?? string.Empty;
                        }
                        if (SapUiSafe.TryGetSpecific<EditText>(targetItem) is EditText targetEdit)
                        {
                            targetEdit.Value = value;
                        }
                        else if (SapUiSafe.TryGetSpecific<ComboBox>(targetItem) is ComboBox targetCombo && !string.IsNullOrEmpty(value))
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
