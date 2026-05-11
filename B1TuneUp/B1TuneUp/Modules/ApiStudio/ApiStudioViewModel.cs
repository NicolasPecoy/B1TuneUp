using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.ApiStudio
{
    public class ApiStudioViewModel : INotifyPropertyChanged
    {
        private ApiStudioWorkspace _workspace;
        private ApiCollection _selectedCollection;
        private ApiRequest _selectedRequest;
        private ApiEnvironment _selectedEnvironment;
        private ApiHistoryEntry _selectedHistory;
        private ApiStudioResponse _lastResponse = new ApiStudioResponse();
        private string _collectionSearchText;
        private string _searchText;
        private bool _isBusy;
        private string _busyMessage;
        private string _statusMessage;
        private string _previewUrl;

        public ObservableCollection<ApiCollection> Collections { get; } = new ObservableCollection<ApiCollection>();
        public ObservableCollection<ApiCollection> FilteredCollections { get; } = new ObservableCollection<ApiCollection>();
        public ObservableCollection<ApiEnvironment> Environments { get; } = new ObservableCollection<ApiEnvironment>();
        public ObservableCollection<ApiHistoryEntry> History { get; } = new ObservableCollection<ApiHistoryEntry>();
        public ObservableCollection<ApiRequestSearchResult> SearchResults { get; } = new ObservableCollection<ApiRequestSearchResult>();
        public ObservableCollection<string> Methods { get; } = new ObservableCollection<string> { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };
        public ObservableCollection<string> ContentTypes { get; } = new ObservableCollection<string> { "application/json", "application/xml", "text/xml", "text/plain", "application/x-www-form-urlencoded" };

        public RelayCommand LoadCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand NewCollectionCommand { get; }
        public RelayCommand NewRequestCommand { get; }
        public RelayCommand DuplicateRequestCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand SendCommand { get; }
        public RelayCommand BeautifyRequestJsonCommand { get; }
        public RelayCommand BeautifyRequestXmlCommand { get; }
        public RelayCommand BeautifyResponseJsonCommand { get; }
        public RelayCommand BeautifyResponseXmlCommand { get; }
        public RelayCommand NewEnvironmentCommand { get; }
        public RelayCommand AddVariableCommand { get; }
        public RelayCommand DeleteVariableCommand { get; }
        public RelayCommand ClearHistoryCommand { get; }
        public RelayCommand LoadHistoryCommand { get; }
        public RelayCommand InstallSeedCommand { get; }

        public ApiStudioViewModel()
        {
            LoadCommand = new RelayCommand(Load);
            SaveCommand = new RelayCommand(Save);
            NewCollectionCommand = new RelayCommand(NewCollection);
            NewRequestCommand = new RelayCommand(NewRequest, () => SelectedCollection != null);
            DuplicateRequestCommand = new RelayCommand(DuplicateRequest, () => SelectedCollection != null && SelectedRequest != null);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedCollection != null);
            SendCommand = new RelayCommand(async () => await SendAsync(), () => SelectedRequest != null && !IsBusy);
            BeautifyRequestJsonCommand = new RelayCommand(() => BeautifyRequest("json"), () => SelectedRequest != null);
            BeautifyRequestXmlCommand = new RelayCommand(() => BeautifyRequest("xml"), () => SelectedRequest != null);
            BeautifyResponseJsonCommand = new RelayCommand(() => BeautifyResponse("json"), () => LastResponse != null && !string.IsNullOrWhiteSpace(LastResponse.Body));
            BeautifyResponseXmlCommand = new RelayCommand(() => BeautifyResponse("xml"), () => LastResponse != null && !string.IsNullOrWhiteSpace(LastResponse.Body));
            NewEnvironmentCommand = new RelayCommand(NewEnvironment);
            AddVariableCommand = new RelayCommand(AddVariable, () => SelectedEnvironment != null);
            DeleteVariableCommand = new RelayCommand(DeleteVariable, variable => SelectedEnvironment != null && variable is ApiVariable);
            ClearHistoryCommand = new RelayCommand(ClearHistory, () => History.Any());
            LoadHistoryCommand = new RelayCommand(LoadHistory, () => SelectedHistory != null);
            InstallSeedCommand = new RelayCommand(InstallSeed);
        }

        public ApiCollection SelectedCollection
        {
            get => _selectedCollection;
            set
            {
                if (_selectedCollection == value) return;
                _selectedCollection = value;
                OnPropertyChanged();
                if (value != null && !value.Requests.Contains(SelectedRequest)) SelectedRequest = value.Requests.FirstOrDefault();
                RaiseCommandStates();
            }
        }

        public ApiRequest SelectedRequest
        {
            get => _selectedRequest;
            set
            {
                if (_selectedRequest == value) return;
                _selectedRequest = value;
                OnPropertyChanged();
                UpdatePreviewUrl();
                RaiseCommandStates();
            }
        }

        public ApiEnvironment SelectedEnvironment
        {
            get => _selectedEnvironment;
            set
            {
                if (_selectedEnvironment == value) return;
                _selectedEnvironment = value;
                OnPropertyChanged();
                if (_workspace != null) _workspace.ActiveEnvironmentId = value?.Id;
                UpdatePreviewUrl();
                RaiseCommandStates();
            }
        }

        public ApiHistoryEntry SelectedHistory
        {
            get => _selectedHistory;
            set { if (_selectedHistory != value) { _selectedHistory = value; OnPropertyChanged(); RaiseCommandStates(); } }
        }

        public ApiStudioResponse LastResponse
        {
            get => _lastResponse;
            set { if (_lastResponse != value) { _lastResponse = value; OnPropertyChanged(); RaiseCommandStates(); } }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                RefreshSearchResults();
            }
        }

        public string CollectionSearchText
        {
            get => _collectionSearchText;
            set
            {
                if (_collectionSearchText == value) return;
                _collectionSearchText = value;
                OnPropertyChanged();
                RefreshCollectionFilter();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); RaiseCommandStates(); } }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            private set { if (_busyMessage != value) { _busyMessage = value; OnPropertyChanged(); } }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public string PreviewUrl
        {
            get => _previewUrl;
            private set { if (_previewUrl != value) { _previewUrl = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Load()
        {
            _workspace = ApiStudioStorage.Load();
            Replace(Collections, _workspace.Collections);
            Replace(Environments, _workspace.Environments);
            Replace(History, _workspace.History);
            SelectedEnvironment = Environments.FirstOrDefault(e => e.Id == _workspace.ActiveEnvironmentId) ?? Environments.FirstOrDefault();
            SelectedCollection = Collections.FirstOrDefault();
            SelectedRequest = SelectedCollection?.Requests.FirstOrDefault();
            RefreshCollectionFilter();
            RefreshSearchResults();
            StatusMessage = "API Studio listo.";
        }

        public void Save()
        {
            EnsureWorkspace();
            ApiStudioStorage.Save(_workspace);
            StatusMessage = "Workspace guardado.";
        }

        public void SelectNode(object node)
        {
            if (node is ApiCollection collection)
            {
                SelectedCollection = collection;
                SelectedRequest = null;
                return;
            }
            if (node is ApiRequest request)
            {
                var owner = Collections.FirstOrDefault(c => c.Requests.Contains(request));
                if (owner != null) SelectedCollection = owner;
                SelectedRequest = request;
            }
        }

        public void SelectSearchResult(ApiRequestSearchResult result)
        {
            if (result == null) return;
            SelectedCollection = result.Collection;
            SelectedRequest = result.Request;
        }

        private void NewCollection()
        {
            var collection = new ApiCollection { Name = "Nueva coleccion", Description = "Coleccion creada en B1TuneUp API Studio." };
            Collections.Add(collection);
            SelectedCollection = collection;
            RefreshCollectionFilter();
            RefreshSearchResults();
            Save();
        }

        private void NewRequest()
        {
            if (SelectedCollection == null) NewCollection();
            var request = new ApiRequest { Name = "Nueva request", Method = "GET", Url = "{{baseUrl}}/", Headers = "Accept=application/json" };
            SelectedCollection.Requests.Add(request);
            SelectedRequest = request;
            RefreshCollectionFilter();
            RefreshSearchResults();
            Save();
        }

        private void DuplicateRequest()
        {
            if (SelectedCollection == null || SelectedRequest == null) return;
            var copy = SelectedRequest.Clone();
            SelectedCollection.Requests.Add(copy);
            SelectedRequest = copy;
            RefreshCollectionFilter();
            RefreshSearchResults();
            Save();
        }

        private void DeleteSelected()
        {
            if (SelectedCollection == null) return;
            if (SelectedRequest != null && SelectedCollection.Requests.Contains(SelectedRequest))
            {
                var old = SelectedRequest;
                SelectedCollection.Requests.Remove(old);
                SelectedRequest = SelectedCollection.Requests.FirstOrDefault();
            }
            else if (Collections.Contains(SelectedCollection))
            {
                Collections.Remove(SelectedCollection);
                SelectedCollection = Collections.FirstOrDefault();
            }
            RefreshCollectionFilter();
            RefreshSearchResults();
            Save();
        }

        private async Task SendAsync()
        {
            if (SelectedRequest == null) return;
            try
            {
                IsBusy = true;
                BusyMessage = "Ejecutando request...";
                Save();
                var result = await ApiStudioHttpRunner.SendAsync(SelectedRequest, SelectedEnvironment);
                LastResponse = result;
                var entry = new ApiHistoryEntry
                {
                    CollectionName = SelectedCollection?.DisplayName,
                    RequestName = SelectedRequest.Name,
                    EnvironmentName = SelectedEnvironment?.DisplayName,
                    Method = SelectedRequest.Method,
                    Url = result.FinalUrl,
                    StatusCode = result.StatusCode,
                    StatusDescription = result.StatusDescription,
                    DurationMs = result.DurationMs,
                    ResponseBytes = result.ResponseBytes,
                    RequestHeaders = SelectedRequest.Headers,
                    RequestBody = SelectedRequest.Body,
                    ResponseHeaders = result.Headers,
                    ResponseBody = result.Body,
                    DebugLog = result.DebugLog
                };
                History.Insert(0, entry);
                while (History.Count > 300) History.RemoveAt(History.Count - 1);
                StatusMessage = $"{SelectedRequest.Method} {result.StatusCode} en {result.DurationMs} ms.";
                Save();
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private void BeautifyRequest(string format)
        {
            try
            {
                if (SelectedRequest == null) return;
                SelectedRequest.Body = format == "xml"
                    ? ApiStudioHttpRunner.BeautifyXml(SelectedRequest.Body)
                    : ApiStudioHttpRunner.BeautifyJson(SelectedRequest.Body);
                StatusMessage = "Body formateado.";
            }
            catch (Exception ex)
            {
                StatusMessage = "No se pudo formatear: " + ex.Message;
            }
        }

        private void BeautifyResponse(string format)
        {
            try
            {
                LastResponse.Body = format == "xml"
                    ? ApiStudioHttpRunner.BeautifyXml(LastResponse.Body)
                    : ApiStudioHttpRunner.BeautifyJson(LastResponse.Body);
                StatusMessage = "Respuesta formateada.";
            }
            catch (Exception ex)
            {
                StatusMessage = "No se pudo formatear: " + ex.Message;
            }
        }

        private void NewEnvironment()
        {
            var env = new ApiEnvironment { Name = "Nuevo ambiente" };
            env.Variables.Add(new ApiVariable { Key = "baseUrl", Value = "https://server:50000/b1s/v1", Enabled = true });
            Environments.Add(env);
            SelectedEnvironment = env;
            Save();
        }

        private void AddVariable()
        {
            SelectedEnvironment?.Variables.Add(new ApiVariable { Key = "variable", Value = "valor", Enabled = true });
            Save();
        }

        private void DeleteVariable(object variable)
        {
            if (SelectedEnvironment == null || !(variable is ApiVariable apiVariable)) return;
            SelectedEnvironment.Variables.Remove(apiVariable);
            Save();
        }

        private void ClearHistory()
        {
            History.Clear();
            SelectedHistory = null;
            Save();
        }

        private void LoadHistory()
        {
            if (SelectedHistory == null) return;
            LastResponse = new ApiStudioResponse
            {
                StatusCode = SelectedHistory.StatusCode,
                StatusDescription = SelectedHistory.StatusDescription,
                DurationMs = SelectedHistory.DurationMs,
                ResponseBytes = SelectedHistory.ResponseBytes,
                Headers = SelectedHistory.ResponseHeaders,
                Body = SelectedHistory.ResponseBody,
                DebugLog = SelectedHistory.DebugLog,
                FinalUrl = SelectedHistory.Url,
                IsError = SelectedHistory.StatusCode >= 400
            };
            StatusMessage = "Entrada de historial cargada en el panel de respuesta.";
        }

        private void InstallSeed()
        {
            var copy = ApiStudioStorage.CreateServiceLayerCollection();
            if (Collections.Any(c => c.Name == copy.Name))
            {
                copy.Name = copy.Name + " (seed)";
            }
            Collections.Add(copy);
            SelectedCollection = copy;
            SelectedRequest = copy.Requests.FirstOrDefault();
            RefreshCollectionFilter();
            RefreshSearchResults();
            Save();
            StatusMessage = "Coleccion Service Layer agregada.";
        }

        private void RefreshCollectionFilter()
        {
            FilteredCollections.Clear();
            var term = (CollectionSearchText ?? string.Empty).Trim();
            foreach (var collection in Collections)
            {
                if (string.IsNullOrEmpty(term) || CollectionMatches(collection, term))
                {
                    FilteredCollections.Add(collection);
                }
            }
        }

        private bool CollectionMatches(ApiCollection collection, string term)
        {
            if (collection == null) return false;
            return (collection.Name ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (collection.Description ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   collection.Requests.Any(r =>
                       (r.Name ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (r.Url ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (r.Description ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void RefreshSearchResults()
        {
            SearchResults.Clear();
            var term = (SearchText ?? string.Empty).Trim();
            foreach (var collection in Collections)
            {
                foreach (var request in collection.Requests)
                {
                    if (string.IsNullOrEmpty(term) ||
                        (collection.Name ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (request.Name ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (request.Url ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (request.Description ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SearchResults.Add(new ApiRequestSearchResult(collection, request));
                    }
                }
            }
        }

        private void UpdatePreviewUrl()
        {
            try
            {
                PreviewUrl = SelectedRequest == null ? string.Empty : ApiStudioHttpRunner.ResolveVariables(SelectedRequest.Url, SelectedEnvironment);
            }
            catch
            {
                PreviewUrl = SelectedRequest?.Url;
            }
        }

        private void EnsureWorkspace()
        {
            if (_workspace == null) _workspace = new ApiStudioWorkspace();
            _workspace.Collections = Collections;
            _workspace.Environments = Environments;
            _workspace.History = History;
            _workspace.ActiveEnvironmentId = SelectedEnvironment?.Id;
        }

        private void Replace<T>(ObservableCollection<T> target, ObservableCollection<T> source)
        {
            target.Clear();
            if (source == null) return;
            foreach (var item in source) target.Add(item);
        }

        private void RaiseCommandStates()
        {
            NewRequestCommand.RaiseCanExecuteChanged();
            DuplicateRequestCommand.RaiseCanExecuteChanged();
            DeleteSelectedCommand.RaiseCanExecuteChanged();
            SendCommand.RaiseCanExecuteChanged();
            BeautifyRequestJsonCommand.RaiseCanExecuteChanged();
            BeautifyRequestXmlCommand.RaiseCanExecuteChanged();
            BeautifyResponseJsonCommand.RaiseCanExecuteChanged();
            BeautifyResponseXmlCommand.RaiseCanExecuteChanged();
            AddVariableCommand.RaiseCanExecuteChanged();
            ClearHistoryCommand.RaiseCanExecuteChanged();
            LoadHistoryCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ApiRequestSearchResult
    {
        public ApiRequestSearchResult(ApiCollection collection, ApiRequest request)
        {
            Collection = collection;
            Request = request;
        }

        public ApiCollection Collection { get; }
        public ApiRequest Request { get; }
        public string DisplayName => $"{Collection.DisplayName} / {Request.Method} {Request.Name}";
        public string Url => Request.Url;
    }
}
