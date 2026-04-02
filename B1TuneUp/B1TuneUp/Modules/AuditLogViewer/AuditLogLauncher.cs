using System;
using System.Windows;

namespace B1TuneUp.Modules.AuditLogViewer
{
    public static class AuditLogLauncher
    {
        private static AuditLogWindow _window;

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

            _window = new AuditLogWindow();
            _window.Closed += OnClosed;
            _window.Show();
        }

        private static void OnClosed(object sender, EventArgs e)
        {
            if (_window != null)
            {
                _window.Closed -= OnClosed;
                _window = null;
            }
        }
    }
}
