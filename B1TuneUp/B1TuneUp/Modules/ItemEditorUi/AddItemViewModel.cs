using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using SAPbouiCOM;

namespace B1TuneUp.Modules.ItemEditorUi
{
    public class AddItemViewModel : INotifyPropertyChanged
    {
        private readonly string _formUid;
        private string _itemId;
        private string _selectedItemType;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public AddItemViewModel(string formUid)
        {
            _formUid = formUid;
            ItemTypes = new ObservableCollection<string>(new[]
            {
                "EditText",
                "StaticText",
                "Button",
                "ComboBox",
                "Folder"
            });
            _selectedItemType = ItemTypes.FirstOrDefault();

            AddCommand = new RelayCommand(async () => await AddAsync(false), CanExecute);
            AddAndEditCommand = new RelayCommand(async () => await AddAsync(true), CanExecute);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> ItemTypes { get; }

        public string ItemId
        {
            get => _itemId;
            set
            {
                if (_itemId == value) return;
                _itemId = value;
                OnPropertyChanged(nameof(ItemId));
                RaiseCommandStates();
            }
        }

        public string SelectedItemType
        {
            get => _selectedItemType;
            set
            {
                if (_selectedItemType == value) return;
                _selectedItemType = value;
                OnPropertyChanged(nameof(SelectedItemType));
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

        public RelayCommand AddCommand { get; }
        public RelayCommand AddAndEditCommand { get; }

        private bool CanExecute() => !IsBusy;

        private async Task AddAsync(bool openEditor)
        {
            await RunSafeAsync(() =>
            {
                var form = GetTargetForm();
                if (form == null)
                {
                    StatusMessage = "No se encontró un formulario activo.";
                    return Task.CompletedTask;
                }

                var id = (ItemId ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(id))
                {
                    StatusMessage = "Ingresa un ID para el ítem nuevo.";
                    return Task.CompletedTask;
                }

                if (form.Items.Exists(id))
                {
                    StatusMessage = $"El ID {id} ya existe en el formulario.";
                    return Task.CompletedTask;
                }

                try
                {
                    var newItem = form.Items.Add(id, ResolveItemType(SelectedItemType));
                    newItem.Left = 15;
                    newItem.Top = 80;
                    newItem.Width = 120;
                    newItem.Height = 20;
                    StatusMessage = $"Ítem {id} creado.";
                    B1App.Instance?.Application?.SetStatusBarMessage($"Ítem {id} agregado.", BoMessageTime.bmt_Short, false);
                    if (openEditor)
                    {
                        ItemEditorLauncher.ShowItemEditor(form.UniqueID, id);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error creando el ítem: {ex.Message}";
                    B1App.Instance?.Application?.SetStatusBarMessage($"AddItem error: {ex.Message}", BoMessageTime.bmt_Short, true);
                }

                return Task.CompletedTask;
            }, openEditor ? "Agregando y abriendo editor..." : "Agregando ítem...");
        }

        private SAPbouiCOM.Form GetTargetForm()
        {
            try
            {
                if (!string.IsNullOrEmpty(_formUid))
                {
                    return B1App.Instance?.Application?.Forms?.Item(_formUid);
                }
                return B1App.Instance?.Application?.Forms?.ActiveForm;
            }
            catch
            {
                return null;
            }
        }

        private static BoFormItemTypes ResolveItemType(string typeText)
        {
            switch (typeText)
            {
                case "StaticText":
                    return BoFormItemTypes.it_STATIC;
                case "Button":
                    return BoFormItemTypes.it_BUTTON;
                case "ComboBox":
                    return BoFormItemTypes.it_COMBO_BOX;
                case "Folder":
                    return BoFormItemTypes.it_FOLDER;
                default:
                    return BoFormItemTypes.it_EDIT;
            }
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
            AddCommand.RaiseCanExecuteChanged();
            AddAndEditCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
