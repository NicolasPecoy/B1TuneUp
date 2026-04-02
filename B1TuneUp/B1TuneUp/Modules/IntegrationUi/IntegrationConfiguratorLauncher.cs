using System.Windows;

namespace B1TuneUp.Modules.IntegrationUi
{
    public static class IntegrationConfiguratorLauncher
    {
        private static IntegrationConfiguratorWindow _window;

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

            _window = new IntegrationConfiguratorWindow();
            _window.Closed += (_, __) => _window = null;
            _window.Show();
        }
    }
}
