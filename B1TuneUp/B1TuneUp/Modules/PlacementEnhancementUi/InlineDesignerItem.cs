using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class InlineDesignerItem : INotifyPropertyChanged
    {
        private string _itemId;
        private string _caption;
        private double _left;
        private double _top;
        private double _width;
        private double _height;
        private string _dataBind;
        private bool _isSelected;

        public string ItemId
        {
            get => _itemId;
            set => SetProperty(ref _itemId, value);
        }

        public string Caption
        {
            get => _caption;
            set => SetProperty(ref _caption, value);
        }

        public double Left
        {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        public double Top
        {
            get => _top;
            set => SetProperty(ref _top, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public string DataBind
        {
            get => _dataBind;
            set => SetProperty(ref _dataBind, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
