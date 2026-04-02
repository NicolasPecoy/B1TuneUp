using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Models
{
    public class IntegrationConfig : INotifyPropertyChanged
    {
        private string _code;
        private string _name;
        private string _channel = "REST";
        private string _method = "GET";
        private string _endpoint;
        private string _headers;
        private string _body;
        private string _authMode = "None";
        private string _authUser;
        private string _authSecret;
        private int? _scheduleMinutes;
        private string _handlerMacro;
        private string _notes;
        private bool _active = true;
        private string _lastTestResult;

        public string Code
        {
            get => _code;
            set => Set(ref _code, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string Channel
        {
            get => _channel;
            set => Set(ref _channel, value);
        }

        public string Method
        {
            get => _method;
            set => Set(ref _method, value);
        }

        public string Endpoint
        {
            get => _endpoint;
            set => Set(ref _endpoint, value);
        }

        public string Headers
        {
            get => _headers;
            set => Set(ref _headers, value);
        }

        public string Body
        {
            get => _body;
            set => Set(ref _body, value);
        }

        public string AuthMode
        {
            get => _authMode;
            set => Set(ref _authMode, value);
        }

        public string AuthUser
        {
            get => _authUser;
            set => Set(ref _authUser, value);
        }

        public string AuthSecret
        {
            get => _authSecret;
            set => Set(ref _authSecret, value);
        }

        public int? ScheduleMinutes
        {
            get => _scheduleMinutes;
            set => Set(ref _scheduleMinutes, value);
        }

        public string HandlerMacro
        {
            get => _handlerMacro;
            set => Set(ref _handlerMacro, value);
        }

        public string Notes
        {
            get => _notes;
            set => Set(ref _notes, value);
        }

        public bool Active
        {
            get => _active;
            set => Set(ref _active, value);
        }

        public string LastTestResult
        {
            get => _lastTestResult;
            set => Set(ref _lastTestResult, value);
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Code : Name;

        public IntegrationConfig Clone()
        {
            return new IntegrationConfig
            {
                Code = Code,
                Name = Name,
                Channel = Channel,
                Method = Method,
                Endpoint = Endpoint,
                Headers = Headers,
                Body = Body,
                AuthMode = AuthMode,
                AuthUser = AuthUser,
                AuthSecret = AuthSecret,
                ScheduleMinutes = ScheduleMinutes,
                HandlerMacro = HandlerMacro,
                Notes = Notes,
                Active = Active,
                LastTestResult = LastTestResult
            };
        }

        public void CopyFrom(IntegrationConfig other)
        {
            if (other == null) return;
            Code = other.Code;
            Name = other.Name;
            Channel = other.Channel;
            Method = other.Method;
            Endpoint = other.Endpoint;
            Headers = other.Headers;
            Body = other.Body;
            AuthMode = other.AuthMode;
            AuthUser = other.AuthUser;
            AuthSecret = other.AuthSecret;
            ScheduleMinutes = other.ScheduleMinutes;
            HandlerMacro = other.HandlerMacro;
            Notes = other.Notes;
            Active = other.Active;
            LastTestResult = other.LastTestResult;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
