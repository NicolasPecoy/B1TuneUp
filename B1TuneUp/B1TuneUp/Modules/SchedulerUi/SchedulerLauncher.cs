using System;
using System.Windows;

namespace B1TuneUp.Modules.SchedulerUi
{
    public static class SchedulerLauncher
    {
        private static SchedulerWindow _window;

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

            _window = new SchedulerWindow();
            _window.Closed += OnWindowClosed;
            _window.Show();
        }

        private static void OnWindowClosed(object sender, EventArgs e)
        {
            if (_window != null)
            {
                _window.Closed -= OnWindowClosed;
                _window = null;
            }
        }
    }
}
