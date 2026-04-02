using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Models
{
    public class ProcessDefinition : INotifyPropertyChanged
    {
        private string _code;
        private string _docEntry;
        private string _name;
        private string _description;
        private string _formType;
        private bool _active = true;
        private bool _autoShow;
        public ObservableCollection<ProcessStepDefinition> Steps { get; } = new ObservableCollection<ProcessStepDefinition>();

        public string Code
        {
            get => _code;
            set => Set(ref _code, value);
        }

        public string DocEntry
        {
            get => _docEntry;
            set => Set(ref _docEntry, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => Set(ref _description, value);
        }

        public string FormType
        {
            get => _formType;
            set => Set(ref _formType, value);
        }

        public bool Active
        {
            get => _active;
            set => Set(ref _active, value);
        }

        public bool AutoShow
        {
            get => _autoShow;
            set => Set(ref _autoShow, value);
        }

        public string DisplayName => string.IsNullOrEmpty(Name) ? $"Proceso {DocEntry}" : Name;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProcessStepDefinition : INotifyPropertyChanged
    {
        private string _docEntry;
        private int _order;
        private string _name;
        private string _description;
        private string _doneCondition;
        private string _action;
        private bool _mandatory;

        public string DocEntry
        {
            get => _docEntry;
            set => Set(ref _docEntry, value);
        }

        public int Order
        {
            get => _order;
            set => Set(ref _order, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => Set(ref _description, value);
        }

        public string DoneCondition
        {
            get => _doneCondition;
            set => Set(ref _doneCondition, value);
        }

        public string Action
        {
            get => _action;
            set => Set(ref _action, value);
        }

        public bool Mandatory
        {
            get => _mandatory;
            set => Set(ref _mandatory, value);
        }

        public string DisplayName => $"{Order:00} · {Name}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
