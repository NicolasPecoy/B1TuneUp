using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Modules.ValidationUi;
using B1TuneUp.Modules.ItemActionsUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public class ActionPadInlineDesignerViewModel : INotifyPropertyChanged
    {
        private readonly ActionPadInlineDesignerSession _session;
        private ActionPadInlineDesignerItem _selectedItem;
        private bool _snapToGrid = true;
        private double _gridSize = 5;
        private string _statusMessage;
        private int _interactionDepth;
        private readonly ObservableCollection<string> _activityLog = new ObservableCollection<string>();
        private ActionPadInlineDesignerItem _activeItemSnapshot;
        private double _startLeft;
        private double _startTop;
        private double _startWidth;
        private double _startHeight;

        public ActionPadInlineDesignerViewModel(ActionPadInlineDesignerSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = session.Items;
            ApplyCommand = new RelayCommand(_ => Apply(false));
            SaveCommand = new RelayCommand(_ => Apply(true));
            CloseCommand = new RelayCommand(window => (window as Window)?.Close());
            OpenValidationCommand = new RelayCommand(_ => OpenValidation(), _ => SelectedItem != null);
            OpenActionCommand = new RelayCommand(_ => OpenActions(), _ => SelectedItem != null);
        }

        public ObservableCollection<ActionPadInlineDesignerItem> Items { get; }
        public string PadTitle => _session.Pad.Title;
        public double CanvasWidth => _session.FormWidth;
        public double CanvasHeight => _session.FormHeight;
        public string FormType => _session.FormType;
        public ActionPadInlineDesignerSession Session => _session;
        public ObservableCollection<string> ActivityLog => _activityLog;

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
                OpenValidationCommand.RaiseCanExecuteChanged();
                OpenActionCommand.RaiseCanExecuteChanged();
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
        public RelayCommand OpenValidationCommand { get; }
        public RelayCommand OpenActionCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsUserInteracting => _interactionDepth > 0;

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
            _session.PushLiveItem(item);
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
            _session.PushLiveItem(item);
        }

        public void BeginInteraction(ActionPadInlineDesignerItem item = null)
        {
            _interactionDepth++;
            if (_interactionDepth == 1)
            {
                _activeItemSnapshot = item ?? SelectedItem;
                if (_activeItemSnapshot != null)
                {
                    _startLeft = _activeItemSnapshot.Left;
                    _startTop = _activeItemSnapshot.Top;
                    _startWidth = _activeItemSnapshot.Width;
                    _startHeight = _activeItemSnapshot.Height;
                }
            }
        }

        public void EndInteraction()
        {
            if (_interactionDepth > 0) _interactionDepth--;
            if (_interactionDepth == 0 && _activeItemSnapshot != null)
            {
                if (HasItemChanged(_activeItemSnapshot))
                {
                    LogSnapshot(_activeItemSnapshot);
                }
                _activeItemSnapshot = null;
            }
        }

        private void Apply(bool persist)
        {
            try
            {
                _session.ApplyLayout(persist);
                string message = persist ? "Action Pad actualizado y guardado." : "Vista previa aplicada.";
                StatusMessage = message;
                if (persist)
                {
                    PushLog(message);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void OpenValidation()
        {
            var itemId = SelectedItem?.Source?.Label ?? SelectedItem?.Source?.Action;
            ValidationDesignerLauncher.Show(FormType, itemId);
            StatusMessage = "Abriendo diseñador de validaciones...";
        }

        private void OpenActions()
        {
            var itemId = SelectedItem?.Source?.Label ?? SelectedItem?.Source?.PadEntry.ToString();
            ItemActionsLauncher.Show(FormType, itemId);
            StatusMessage = "Abriendo acciones vinculadas...";
        }

        private bool HasItemChanged(ActionPadInlineDesignerItem item)
        {
            if (item == null) return false;
            return Math.Abs(item.Left - _startLeft) > 0.5 ||
                   Math.Abs(item.Top - _startTop) > 0.5 ||
                   Math.Abs(item.Width - _startWidth) > 0.5 ||
                   Math.Abs(item.Height - _startHeight) > 0.5;
        }

        private void LogSnapshot(ActionPadInlineDesignerItem item)
        {
            string label = item?.Source?.Label ?? item?.Source?.Action ?? "(botón)";
            string entry = $"Botón '{label}' => L:{item.Left:F0} T:{item.Top:F0} W:{item.Width:F0} H:{item.Height:F0}";
            PushLog(entry);
        }

        private void PushLog(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _activityLog.Insert(0, entry);
            while (_activityLog.Count > 100)
            {
                _activityLog.RemoveAt(_activityLog.Count - 1);
            }
        }

        public void NotifySurfaceChanged()
        {
            OnPropertyChanged(nameof(CanvasWidth));
            OnPropertyChanged(nameof(CanvasHeight));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


