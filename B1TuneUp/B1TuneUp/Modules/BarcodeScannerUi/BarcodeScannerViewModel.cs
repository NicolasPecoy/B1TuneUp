using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.BarcodeScannerUi
{
    public class BarcodeScannerViewModel : INotifyPropertyChanged
    {
        private string _targetItemId;
        private string _scannedValue;
        private string _statusMessage;

        public BarcodeScannerViewModel(string targetItemId)
        {
            TargetItemId = targetItemId;
            InjectCommand = new RelayCommand(InjectValue, () => !string.IsNullOrWhiteSpace(TargetItemId));
        }

        public string TargetItemId
        {
            get => _targetItemId;
            set
            {
                if (_targetItemId == value) return;
                _targetItemId = value;
                OnPropertyChanged();
                InjectCommand.RaiseCanExecuteChanged();
            }
        }

        public string ScannedValue
        {
            get => _scannedValue;
            set
            {
                if (_scannedValue == value) return;
                _scannedValue = value;
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

        public RelayCommand InjectCommand { get; }

        private void InjectValue()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                if (form == null)
                {
                    StatusMessage = "No hay formulario activo.";
                    return;
                }
                if (!form.Items.Exists(TargetItemId))
                {
                    StatusMessage = $"El item {TargetItemId} no existe.";
                    return;
                }
                var item = form.Items.Item(TargetItemId);
                if (item.Specific is EditText editText)
                {
                    editText.Value = ScannedValue ?? string.Empty;
                    StatusMessage = "Valor insertado en el campo.";
                }
                else
                {
                    StatusMessage = "El item objetivo no admite texto.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error insertando valor: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
