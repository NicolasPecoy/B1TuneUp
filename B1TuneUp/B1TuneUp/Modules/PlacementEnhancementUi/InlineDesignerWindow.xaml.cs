using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public partial class InlineDesignerWindow : Window
    {
        private readonly InlineDesignerViewModel _viewModel;
        private DispatcherTimer _syncTimer;
        private const double PanelWidth = 360;

        public InlineDesignerWindow(InlineDesignerSession session)
        {
            InitializeComponent();
            _viewModel = new InlineDesignerViewModel(session);
            DataContext = _viewModel;

            Width = Math.Max(820, session.FormWidth + PanelWidth + 40);
            Height = Math.Max(540, session.FormHeight + 60);
            Left = Math.Max(0, session.ScreenLeft);
            Top = Math.Max(0, session.ScreenTop);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _syncTimer.Tick += OnSyncTick;
            _syncTimer.Start();
            SyncToSurface();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (_syncTimer != null)
            {
                _syncTimer.Stop();
                _syncTimer.Tick -= OnSyncTick;
                _syncTimer = null;
            }
        }

        private void OnSyncTick(object sender, EventArgs e)
        {
            SyncToSurface();
            if (!_viewModel.IsUserInteracting)
            {
                _viewModel.Session.RefreshFromForm();
            }
        }

        private void OnMoveThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.Tag is InlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
                _viewModel.MoveItem(item, e.HorizontalChange, e.VerticalChange);
            }
        }

        private void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.Tag is InlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
                _viewModel.ResizeItem(item, e.HorizontalChange, e.VerticalChange);
            }
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            _viewModel.BeginInteraction();
            if (sender is Thumb thumb && thumb.Tag is InlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
            }
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _viewModel.EndInteraction();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void OnWindowDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SyncToSurface()
        {
            var snapshot = _viewModel.Session?.TryCaptureSurface();
            if (snapshot == null) return;
            var surf = snapshot.Value;
            Left = surf.Left;
            Top = surf.Top;
            Width = Math.Max(820, surf.Width + PanelWidth + 40);
            Height = Math.Max(540, surf.Height + 40);
            _viewModel.NotifySurfaceChanged();
        }
    }
}
