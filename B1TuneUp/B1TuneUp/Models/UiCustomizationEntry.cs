using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Models
{
    public class UiCustomizationEntry : INotifyPropertyChanged
    {
        private string _code;
        private string _name;
        private string _formType;
        private string _itemId;
        private string _action;
        private int? _top;
        private int? _left;
        private int? _width;
        private int? _height;
        private string _label;
        private int? _fromPane;
        private int? _toPane;
        private DateTime? _updatedAt;

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

        public string FormType
        {
            get => _formType;
            set => Set(ref _formType, value);
        }

        public string ItemId
        {
            get => _itemId;
            set => Set(ref _itemId, value);
        }

        public string Action
        {
            get => _action;
            set => Set(ref _action, value);
        }

        public int? Top
        {
            get => _top;
            set => Set(ref _top, value);
        }

        public int? Left
        {
            get => _left;
            set => Set(ref _left, value);
        }

        public int? Width
        {
            get => _width;
            set => Set(ref _width, value);
        }

        public int? Height
        {
            get => _height;
            set => Set(ref _height, value);
        }

        public string Label
        {
            get => _label;
            set => Set(ref _label, value);
        }

        public int? FromPane
        {
            get => _fromPane;
            set => Set(ref _fromPane, value);
        }

        public int? ToPane
        {
            get => _toPane;
            set => Set(ref _toPane, value);
        }

        public DateTime? UpdatedAt
        {
            get => _updatedAt;
            set => Set(ref _updatedAt, value);
        }

        public string DisplayName => $"{FormType ?? "?"} · {ItemId ?? "(nuevo)"}";

        public UiCustomizationEntry Clone()
        {
            return new UiCustomizationEntry
            {
                Code = Code,
                Name = Name,
                FormType = FormType,
                ItemId = ItemId,
                Action = Action,
                Top = Top,
                Left = Left,
                Width = Width,
                Height = Height,
                Label = Label,
                FromPane = FromPane,
                ToPane = ToPane,
                UpdatedAt = UpdatedAt
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void CopyFrom(UiCustomizationEntry other)
        {
            if (other == null) return;
            Code = other.Code;
            Name = other.Name;
            FormType = other.FormType;
            ItemId = other.ItemId;
            Action = other.Action;
            Top = other.Top;
            Left = other.Left;
            Width = other.Width;
            Height = other.Height;
            Label = other.Label;
            FromPane = other.FromPane;
            ToPane = other.ToPane;
            UpdatedAt = other.UpdatedAt;
        }

        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
