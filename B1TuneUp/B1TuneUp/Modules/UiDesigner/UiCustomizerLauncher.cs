using System.Windows;

namespace B1TuneUp.Modules.UiDesigner
{
    public static class UiCustomizerLauncher
    {
        private static UiCustomizerWindow _window;

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

            _window = new UiCustomizerWindow();
            _window.Closed += (_, _) => _window = null;
            _window.Show();
        }
    }
}
