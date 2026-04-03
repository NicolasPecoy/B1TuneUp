using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.RichTextEditorUi
{
    public class RichTextEditorViewModel : INotifyPropertyChanged
    {
        private string _itemId;
        private string _textContent;
        private string _statusMessage;

        public RichTextEditorViewModel(string itemId)
        {
            ItemId = itemId;
            LoadInitialValue();
            SaveCommand = new RelayCommand(SaveContent, () => !string.IsNullOrWhiteSpace(ItemId));
        }

        public string ItemId
        {
            get => _itemId;
            set
            {
                if (_itemId == value) return;
                _itemId = value;
                OnPropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string TextContent
        {
            get => _textContent;
            set
            {
                if (_textContent == value) return;
                _textContent = value;
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

        public RelayCommand SaveCommand { get; }

        private void LoadInitialValue()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                if (form == null || string.IsNullOrWhiteSpace(ItemId))
                {
                    return;
                }
                if (form.Items.Exists(ItemId))
                {
                    var item = form.Items.Item(ItemId);
                    if (item.Specific is EditText editText)
                    {
                        TextContent = editText.Value ?? string.Empty;
                    }
                }
            }
            catch { }
        }

        private void SaveContent()
        {
            try
            {
                var form = B1App.Instance?.Application?.Forms?.ActiveForm;
                if (form == null)
                {
                    StatusMessage = "No hay formulario activo.";
                    return;
                }
                if (!form.Items.Exists(ItemId))
                {
                    StatusMessage = $"El item {ItemId} no existe en el formulario actual.";
                    return;
                }
                var item = form.Items.Item(ItemId);
                if (item.Specific is EditText editText)
                {
                    editText.Value = TextContent ?? string.Empty;
                    StatusMessage = "Texto guardado correctamente.";
                    return;
                }
                StatusMessage = "El item no admite texto enriquecido.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error guardando: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
