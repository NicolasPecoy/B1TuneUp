using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using B1TuneUp.Models;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.EmailDesigner
{
    public class EmailDesignerViewModel : INotifyPropertyChanged
    {
        private EmailTemplateEntry _selectedTemplate;
        private string _searchText;
        private string _channelFilter = "Todos";
        private bool _onlyActive = true;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;

        public EmailDesignerViewModel()
        {
            Templates = new ObservableCollection<EmailTemplateEntry>();
            ChannelOptions = new[] { "Email", "SAPMessage", "Desktop" };
            ChannelFilterOptions = new[] { "Todos" }.Concat(ChannelOptions).ToArray();
            PriorityOptions = new[] { "Low", "Normal", "High" };

            RefreshCommand = new RelayCommand(async () => await LoadAsync());
            NewCommand = new RelayCommand(NewTemplate);
            DuplicateCommand = new RelayCommand(DuplicateTemplate, () => SelectedTemplate != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => SelectedTemplate != null);
            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedTemplate != null);
            SendTestCommand = new RelayCommand(async () => await SendTestAsync(), () => SelectedTemplate != null);

            SelectedTemplate = CreateNewTemplate();
        }

        public ObservableCollection<EmailTemplateEntry> Templates { get; }
        public string[] ChannelOptions { get; }
        public string[] ChannelFilterOptions { get; }
        public string[] PriorityOptions { get; }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand DuplicateCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SendTestCommand { get; }

        public EmailTemplateEntry SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (_selectedTemplate == value) return;
                _selectedTemplate = value;
                OnPropertyChanged();
                RaiseCommandStates();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public string ChannelFilter
        {
            get => _channelFilter;
            set
            {
                if (_channelFilter == value) return;
                _channelFilter = value;
                OnPropertyChanged();
            }
        }

        public bool OnlyActive
        {
            get => _onlyActive;
            set
            {
                if (_onlyActive == value) return;
                _onlyActive = value;
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

        public async Task LoadAsync()
        {
            await RunSafeAsync("Cargando plantillas...", async () =>
            {
                var list = await Task.Run(() => EmailTemplateService.GetAll(SearchText, ChannelFilter, OnlyActive));
                RunOnUiThread(() =>
                {
                    Templates.Clear();
                    foreach (var item in list.OrderBy(t => t.DisplayName))
                    {
                        Templates.Add(item);
                    }
                    SelectedTemplate = Templates.FirstOrDefault() ?? CreateNewTemplate();
                });
                StatusMessage = $"{Templates.Count} plantillas.";
            });
        }

        private void NewTemplate()
        {
            SelectedTemplate = CreateNewTemplate();
        }

        private void DuplicateTemplate()
        {
            if (SelectedTemplate == null) return;
            var clone = SelectedTemplate.Clone();
            clone.Code = null;
            clone.DocEntry = null;
            clone.Name = $"{clone.DisplayName} (Copy)";
            SelectedTemplate = clone;
        }

        private async Task SaveAsync()
        {
            if (SelectedTemplate == null) return;
            await RunSafeAsync("Guardando plantilla...", async () =>
            {
                var saved = await Task.Run(() => EmailTemplateService.Save(SelectedTemplate));
                await Task.Run(() =>
                {
                    var list = EmailTemplateService.GetAll(SearchText, ChannelFilter, OnlyActive);
                    RunOnUiThread(() =>
                    {
                        Templates.Clear();
                        foreach (var item in list.OrderBy(t => t.DisplayName))
                        {
                            Templates.Add(item);
                        }
                        SelectedTemplate = Templates.FirstOrDefault(t => t.Code == saved.Code) ?? saved;
                    });
                });
                StatusMessage = $"Plantilla '{SelectedTemplate.DisplayName}' guardada.";
            });
        }

        private async Task DeleteAsync()
        {
            if (SelectedTemplate == null || string.IsNullOrWhiteSpace(SelectedTemplate.Code)) return;
            await RunSafeAsync("Eliminando plantilla...", async () =>
            {
                await Task.Run(() => EmailTemplateService.Delete(SelectedTemplate.Code));
                await LoadAsync();
                StatusMessage = "Plantilla eliminada.";
            });
        }

        private async Task SendTestAsync()
        {
            if (SelectedTemplate == null) return;
            await RunSafeAsync("Enviando prueba...", async () =>
            {
                await Task.Run(() => EmailTemplateService.SendTest(SelectedTemplate));
                StatusMessage = "Notificación de prueba enviada.";
            });
        }

        private EmailTemplateEntry CreateNewTemplate()
        {
            return new EmailTemplateEntry
            {
                Name = "Nueva notificación",
                Subject = "Asunto",
                Body = "Contenido del mensaje",
                Channel = "Email",
                Priority = "Normal",
                Active = true
            };
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
            DuplicateCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            SendTestCommand.RaiseCanExecuteChanged();
        }

        private void RunOnUiThread(Action action)
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
