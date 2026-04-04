using System.ComponentModel;
using System.Runtime.CompilerServices;
using B1TuneUp.Models;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public class ActionPadInlineDesignerItem : INotifyPropertyChanged
    {
        private double _left;
        private double _top;
        private double _width;
        private double _height;
        private bool _isSelected;

        public ActionPadButtonEntry Source { get; }

        public string Label => Source?.Label;
        public string Tooltip => Source?.Tooltip;

        public ActionPadInlineDesignerItem(ActionPadButtonEntry source, double defaultWidth, double defaultHeight)
        {
            Source = source;
            _left = source?.Left ?? 0;
            _top = source?.Top ?? 0;
            _width = source?.Width > 0 ? source.Width : defaultWidth;
            _height = source?.Height > 0 ? source.Height : defaultHeight;
        }

        public double Left
        {
            get => _left;
            set => SetField(ref _left, value);
        }

        public double Top
        {
            get => _top;
            set => SetField(ref _top, value);
        }

        public double Width
        {
            get => _width;
            set => SetField(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetField(ref _height, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
