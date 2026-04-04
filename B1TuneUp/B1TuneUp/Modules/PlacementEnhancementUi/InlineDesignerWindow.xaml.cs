using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public partial class InlineDesignerWindow : Window
    {
        private readonly InlineDesignerViewModel _viewModel;

        public InlineDesignerWindow(InlineDesignerSession session)
        {
            InitializeComponent();
            _viewModel = new InlineDesignerViewModel(session);
            DataContext = _viewModel;

            Width = Math.Max(800, session.FormWidth + 360);
            Height = Math.Max(600, session.FormHeight + 120);
            Left = Math.Max(0, session.ScreenLeft - 40);
            Top = Math.Max(0, session.ScreenTop - 40);
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
            if (sender is Thumb thumb && thumb.Tag is InlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
            }
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            // placeholder for future snap/undo if needed
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
    }
}
