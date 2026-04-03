using System.Windows;

namespace B1TuneUp.Modules.ToolboxUi
{
    public static class ToolboxDesignerLauncher
    {
        private static ToolboxDesignerWindow _window;

        public static void Show()
        {
            if (_window != null)
            {
                if (_window.WindowState == WindowState.Minimized)
                {
                    _window.WindowState = WindowState.Normal;
                }
                _window.Activate();
                return;
            }

            _window = new ToolboxDesignerWindow();
            _window.Closed += (_, __) => _window = null;
            _window.Show();
        }
    }
}
