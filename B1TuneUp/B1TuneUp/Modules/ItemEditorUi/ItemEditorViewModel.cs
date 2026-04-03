using System;
using System.ComponentModel;
using System.Threading.Tasks;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using SAPbouiCOM;

namespace B1TuneUp.Modules.ItemEditorUi
{
    public class ItemEditorViewModel : INotifyPropertyChanged
    {
        private readonly string _formUid;
        private readonly string _itemId;
        private int _left;
        private int _top;
        private int _width;
        private int _height;
        private string _macroScript;
        private string _statusMessage;
        private bool _isBusy;
        private string _busyMessage;

        public ItemEditorViewModel(string formUid, string itemId)
        {
            _formUid = formUid;
            _itemId = itemId;

            ApplyCommand = new RelayCommand(async () => await ApplyAsync(), () => !IsBusy);
            SaveActionCommand = new RelayCommand(async () => await SaveActionAsync(), () => !IsBusy);

            LoadItemState();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string HeaderTitle => $"Item {_itemId}";
        public string HeaderSubtitle => $"Formulario {_formUid}";

        public int Left
        {
            get => _left;
            set { if (_left != value) { _left = value; OnPropertyChanged(nameof(Left)); } }
        }

        public int Top
        {
            get => _top;
            set { if (_top != value) { _top = value; OnPropertyChanged(nameof(Top)); } }
        }

        public int Width
        {
            get => _width;
            set { if (_width != value) { _width = value; OnPropertyChanged(nameof(Width)); } }
        }

        public int Height
        {
            get => _height;
            set { if (_height != value) { _height = value; OnPropertyChanged(nameof(Height)); } }
        }

        public string MacroScript
        {
            get => _macroScript;
            set { if (_macroScript != value) { _macroScript = value; OnPropertyChanged(nameof(MacroScript)); } }
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

        public RelayCommand ApplyCommand { get; }
        public RelayCommand SaveActionCommand { get; }

        private void LoadItemState()
        {
            try
            {
                var form = GetForm();
                if (form == null || string.IsNullOrEmpty(_itemId) || !form.Items.Exists(_itemId))
                {
                    StatusMessage = "No se encontró el ítem en el formulario.";
                    return;
                }

                var item = form.Items.Item(_itemId);
                Left = item.Left;
                Top = item.Top;
                Width = item.Width;
                Height = item.Height;
                try
                {
                    MacroScript = ItemActionManager.GetAction(form.TypeEx, _itemId);
                }
                catch
                {
                    MacroScript = string.Empty;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cargando ítem: {ex.Message}";
            }
        }

        private SAPbouiCOM.Form GetForm()
        {
            try
            {
                if (!string.IsNullOrEmpty(_formUid))
                {
                    return B1App.Instance?.Application?.Forms?.Item(_formUid);
                }
            }
            catch
            {
                // ignored
            }
            return B1App.Instance?.Application?.Forms?.ActiveForm;
        }

        private async Task ApplyAsync()
        {
            await RunSafeAsync(() =>
            {
                var form = GetForm();
                if (form == null)
                {
                    StatusMessage = "No se pudo obtener el formulario.";
                    return Task.CompletedTask;
                }
                if (string.IsNullOrEmpty(_itemId) || !form.Items.Exists(_itemId))
                {
                    StatusMessage = "El ítem ya no existe.";
                    return Task.CompletedTask;
                }

                try
                {
                    var item = form.Items.Item(_itemId);
                    item.Left = Left;
                    item.Top = Top;
                    item.Width = Width;
                    item.Height = Height;
                    try { form.Update(); } catch { try { form.Refresh(); } catch { } }
                    StatusMessage = "Posición actualizada.";
                    B1App.Instance?.Application?.SetStatusBarMessage($"Item {_itemId} actualizado.", BoMessageTime.bmt_Short, false);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error aplicando cambios: {ex.Message}";
                    B1App.Instance?.Application?.SetStatusBarMessage($"Editor item error: {ex.Message}", BoMessageTime.bmt_Short, true);
                }

                return Task.CompletedTask;
            }, "Aplicando cambios...");
        }

        private async Task SaveActionAsync()
        {
            await RunSafeAsync(() =>
            {
                var form = GetForm();
                if (form == null)
                {
                    StatusMessage = "No se pudo obtener el formulario.";
                    return Task.CompletedTask;
                }

                try
                {
                    ItemActionManager.SaveAction(form.TypeEx, _itemId, MacroScript ?? string.Empty);
                    StatusMessage = "Macro guardada.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error guardando macro: {ex.Message}";
                }

                return Task.CompletedTask;
            }, "Guardando macro...");
        }

        private async Task RunSafeAsync(Func<Task> work, string message)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                BusyMessage = message;
                await work();
            }
            finally
            {
                BusyMessage = string.Empty;
                IsBusy = false;
            }
        }

        private void RaiseCommandStates()
        {
            ApplyCommand.RaiseCanExecuteChanged();
            SaveActionCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
