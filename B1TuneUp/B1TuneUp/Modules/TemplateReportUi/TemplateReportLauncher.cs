using System.Windows;

namespace B1TuneUp.Modules.TemplateReportUi
{
    public static class TemplateReportLauncher
    {
        private static TemplateReportWindow _window;

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

            _window = new TemplateReportWindow();
            _window.Closed += (_, __) => _window = null;
            _window.Show();
        }
    }
}
