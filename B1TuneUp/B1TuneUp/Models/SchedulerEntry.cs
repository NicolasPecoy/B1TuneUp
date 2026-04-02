using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Models
{
    public class SchedulerEntry : INotifyPropertyChanged
    {
        private string _code;
        private string _name;
        private string _action;
        private int _intervalMinutes = 60;
        private bool _active = true;
        private DateTime? _lastRun;

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

        public string Action
        {
            get => _action;
            set => Set(ref _action, value);
        }

        public int IntervalMinutes
        {
            get => _intervalMinutes;
            set => Set(ref _intervalMinutes, value);
        }

        public bool Active
        {
            get => _active;
            set => Set(ref _active, value);
        }

        public DateTime? LastRun
        {
            get => _lastRun;
            set
            {
                if (Set(ref _lastRun, value))
                {
                    OnPropertyChanged(nameof(NextRunEstimate));
                }
            }
        }

        public DateTime? NextRunEstimate
        {
            get
            {
                if (!LastRun.HasValue) return null;
                return LastRun.Value.AddMinutes(IntervalMinutes);
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Code : Name;

        public SchedulerEntry Clone()
        {
            return new SchedulerEntry
            {
                Code = Code,
                Name = Name,
                Action = Action,
                IntervalMinutes = IntervalMinutes,
                Active = Active,
                LastRun = LastRun
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void CopyFrom(SchedulerEntry other)
        {
            if (other == null) return;
            Code = other.Code;
            Name = other.Name;
            Action = other.Action;
            IntervalMinutes = other.IntervalMinutes;
            Active = other.Active;
            LastRun = other.LastRun;
        }

        protected bool Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
