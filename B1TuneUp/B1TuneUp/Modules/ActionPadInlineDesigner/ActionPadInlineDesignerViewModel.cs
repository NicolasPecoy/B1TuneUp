using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules.ItemActionsUi;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public class ActionPadInlineDesignerViewModel : INotifyPropertyChanged
    {
        private readonly ActionPadInlineDesignerSession _session;
        private ActionPadInlineDesignerItem _selectedItem;
        private bool _snapToGrid = true;
        private double _gridSize = 5;
        private string _statusMessage;

        public ActionPadInlineDesignerViewModel(ActionPadInlineDesignerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = session.Items;
            ApplyCommand = new RelayCommand(_ => Apply(false));
            SaveCommand = new RelayCommand(_ => Apply(true));
            CloseCommand = new RelayCommand(window => (window as Window)?.Close());
        }

        public ObservableCollection<ActionPadInlineDesignerItem> Items { get; }
        public string PadTitle => _session.Pad.Title;
        public double CanvasWidth => _session.FormWidth;
        public double CanvasHeight => _session.FormHeight;

        public ActionPadInlineDesignerItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                if (_selectedItem != null) _selectedItem.IsSelected = false;
                _selectedItem = value;
                if (_selectedItem != null) _selectedItem.IsSelected = true;
                OnPropertyChanged();
            }
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set
            {
                if (_snapToGrid == value) return;
                _snapToGrid = value;
                OnPropertyChanged();
            }
        }

        public double GridSize
        {
            get => _gridSize;
            set
            {
                if (Math.Abs(_gridSize - value) < 0.1) return;
                _gridSize = Math.Max(1, value);
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

        public RelayCommand ApplyCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CloseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void MoveItem(ActionPadInlineDesignerItem item, double deltaX, double deltaY)
        {
            if (item == null) return;
            double nx = item.Left + deltaX;
            double ny = item.Top + deltaY;
            if (SnapToGrid)
            {
                nx = Math.Round(nx / GridSize) * GridSize;
                ny = Math.Round(ny / GridSize) * GridSize;
            }
            item.Left = Math.Max(0, nx);
            item.Top = Math.Max(0, ny);
        }

        public void ResizeItem(ActionPadInlineDesignerItem item, double deltaX, double deltaY)
        {
            if (item == null) return;
            double nw = Math.Max(20, item.Width + deltaX);
            double nh = Math.Max(18, item.Height + deltaY);
            if (SnapToGrid)
            {
                nw = Math.Round(nw / GridSize) * GridSize;
                nh = Math.Round(nh / GridSize) * GridSize;
            }
            item.Width = nw;
            item.Height = nh;
        }

        private void Apply(bool persist)
        {
            try
            {
                _session.ApplyLayout(persist);
                StatusMessage = persist ? "Action Pad actualizado y guardado." : "Vista previa aplicada.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
