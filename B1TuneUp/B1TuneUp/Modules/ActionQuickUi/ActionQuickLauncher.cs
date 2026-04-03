using System;
using System.Windows;

namespace B1TuneUp.Modules.ActionQuickUi
{
    public static class ActionQuickLauncher
    {
        private static ActionQuickWindow _window;

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

            _window = new ActionQuickWindow();
            _window.Closed += OnWindowClosed;
            _window.Show();
        }

        private static void OnWindowClosed(object sender, EventArgs e)
        {
            if (_window == null) return;
            _window.Closed -= OnWindowClosed;
            _window = null;
        }
    }
}
