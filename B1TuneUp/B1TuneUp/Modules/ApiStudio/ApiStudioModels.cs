using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Modules.ApiStudio
{
    public class ApiStudioWorkspace
    {
        public ObservableCollection<ApiCollection> Collections { get; set; } = new ObservableCollection<ApiCollection>();
        public ObservableCollection<ApiEnvironment> Environments { get; set; } = new ObservableCollection<ApiEnvironment>();
        public ObservableCollection<ApiHistoryEntry> History { get; set; } = new ObservableCollection<ApiHistoryEntry>();
        public string ActiveEnvironmentId { get; set; }
    }

    public class ApiCollection : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name;
        private string _description;
        private ObservableCollection<ApiRequest> _requests = new ObservableCollection<ApiRequest>();

        public string Id { get => _id; set => Set(ref _id, value); }
        public string Name { get => _name; set => Set(ref _name, value); }
        public string Description { get => _description; set => Set(ref _description, value); }
        public ObservableCollection<ApiRequest> Requests { get => _requests; set => Set(ref _requests, value); }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Nueva coleccion" : Name;
    }

    public class ApiRequest : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name;
        private string _method = "GET";
        private string _url = "{{baseUrl}}/";
        private string _headers = "Accept=application/json";
        private string _body;
        private string _description;
        private string _contentType = "application/json";
        private bool _useEnvironment = true;

        public string Id { get => _id; set => Set(ref _id, value); }
        public string Name { get => _name; set => Set(ref _name, value); }
        public string Method { get => _method; set => Set(ref _method, value); }
        public string Url { get => _url; set => Set(ref _url, value); }
        public string Headers { get => _headers; set => Set(ref _headers, value); }
        public string Body { get => _body; set => Set(ref _body, value); }
        public string Description { get => _description; set => Set(ref _description, value); }
        public string ContentType { get => _contentType; set => Set(ref _contentType, value); }
        public bool UseEnvironment { get => _useEnvironment; set => Set(ref _useEnvironment, value); }

        public ApiRequest Clone()
        {
            return new ApiRequest
            {
                Name = (Name ?? "Request") + " copia",
                Method = Method,
                Url = Url,
                Headers = Headers,
                Body = Body,
                Description = Description,
                ContentType = ContentType,
                UseEnvironment = UseEnvironment
            };
        }
    }

    public class ApiEnvironment : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name;
        private ObservableCollection<ApiVariable> _variables = new ObservableCollection<ApiVariable>();

        public string Id { get => _id; set => Set(ref _id, value); }
        public string Name { get => _name; set => Set(ref _name, value); }
        public ObservableCollection<ApiVariable> Variables { get => _variables; set => Set(ref _variables, value); }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Ambiente" : Name;
    }

    public class ApiVariable : NotifyBase
    {
        private string _key;
        private string _value;
        private bool _secret;
        private bool _enabled = true;

        public string Key { get => _key; set => Set(ref _key, value); }
        public string Value { get => _value; set => Set(ref _value, value); }
        public bool Secret { get => _secret; set => Set(ref _secret, value); }
        public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    }

    public class ApiHistoryEntry : NotifyBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private DateTime _sentAt = DateTime.Now;
        private string _collectionName;
        private string _requestName;
        private string _environmentName;
        private string _method;
        private string _url;
        private int _statusCode;
        private string _statusDescription;
        private long _durationMs;
        private long _responseBytes;
        private string _requestHeaders;
        private string _requestBody;
        private string _responseHeaders;
        private string _responseBody;
        private string _debugLog;

        public string Id { get => _id; set => Set(ref _id, value); }
        public DateTime SentAt { get => _sentAt; set => Set(ref _sentAt, value); }
        public string CollectionName { get => _collectionName; set => Set(ref _collectionName, value); }
        public string RequestName { get => _requestName; set => Set(ref _requestName, value); }
        public string EnvironmentName { get => _environmentName; set => Set(ref _environmentName, value); }
        public string Method { get => _method; set => Set(ref _method, value); }
        public string Url { get => _url; set => Set(ref _url, value); }
        public int StatusCode { get => _statusCode; set => Set(ref _statusCode, value); }
        public string StatusDescription { get => _statusDescription; set => Set(ref _statusDescription, value); }
        public long DurationMs { get => _durationMs; set => Set(ref _durationMs, value); }
        public long ResponseBytes { get => _responseBytes; set => Set(ref _responseBytes, value); }
        public string RequestHeaders { get => _requestHeaders; set => Set(ref _requestHeaders, value); }
        public string RequestBody { get => _requestBody; set => Set(ref _requestBody, value); }
        public string ResponseHeaders { get => _responseHeaders; set => Set(ref _responseHeaders, value); }
        public string ResponseBody { get => _responseBody; set => Set(ref _responseBody, value); }
        public string DebugLog { get => _debugLog; set => Set(ref _debugLog, value); }
        public string Summary => $"{SentAt:HH:mm:ss} {Method} {StatusCode} {RequestName}";
    }

    public class ApiStudioResponse : NotifyBase
    {
        private int _statusCode;
        private string _statusDescription;
        private long _durationMs;
        private long _responseBytes;
        private string _headers;
        private string _body;
        private string _debugLog;
        private string _finalUrl;
        private bool _isError;

        public int StatusCode { get => _statusCode; set => Set(ref _statusCode, value); }
        public string StatusDescription { get => _statusDescription; set => Set(ref _statusDescription, value); }
        public long DurationMs { get => _durationMs; set => Set(ref _durationMs, value); }
        public long ResponseBytes { get => _responseBytes; set => Set(ref _responseBytes, value); }
        public string Headers { get => _headers; set => Set(ref _headers, value); }
        public string Body { get => _body; set => Set(ref _body, value); }
        public string DebugLog { get => _debugLog; set => Set(ref _debugLog, value); }
        public string FinalUrl { get => _finalUrl; set => Set(ref _finalUrl, value); }
        public bool IsError { get => _isError; set => Set(ref _isError, value); }
    }

    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            if (propertyName == "Name") OnPropertyChanged("DisplayName");
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
