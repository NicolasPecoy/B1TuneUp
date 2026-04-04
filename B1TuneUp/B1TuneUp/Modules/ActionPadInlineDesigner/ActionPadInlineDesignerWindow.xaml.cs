using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public partial class ActionPadInlineDesignerWindow : Window
    {
        private readonly ActionPadInlineDesignerViewModel _viewModel;

        public ActionPadInlineDesignerWindow(ActionPadInlineDesignerSession session)
        {
            InitializeComponent();
            _viewModel = new ActionPadInlineDesignerViewModel(session);
            DataContext = _viewModel;
        }

        private void OnMoveThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.Tag is ActionPadInlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
                _viewModel.MoveItem(item, e.HorizontalChange, e.VerticalChange);
            }
        }

        private void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.Tag is ActionPadInlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
                _viewModel.ResizeItem(item, e.HorizontalChange, e.VerticalChange);
            }
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            if (sender is Thumb thumb && thumb.Tag is ActionPadInlineDesignerItem item)
            {
                _viewModel.SelectedItem = item;
            }
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
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
