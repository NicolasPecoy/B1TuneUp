using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace B1TuneUp.Utils
{
    public static class WindowAppearanceHelper
    {
        private const int Windows11Build = 22000;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwcpRoundSmall = 3;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
            _initialized = true;
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                ApplyRoundedCorners(window);
            }
        }

        public static void ApplyRoundedCorners(Window window)
        {
            if (window == null)
            {
                return;
            }

            if (!IsRoundedCornerSupported())
            {
                return;
            }

            if (window.IsLoaded)
            {
                Apply(window);
            }
            else
            {
                window.SourceInitialized += OnWindowSourceInitialized;
            }
        }

        private static bool IsRoundedCornerSupported()
        {
            var version = Environment.OSVersion.Version;
            if (version.Major < 10)
            {
                return false;
            }

            if (version.Major == 10 && version.Build < Windows11Build)
            {
                return false;
            }

            return true;
        }

        private static void OnWindowSourceInitialized(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                window.SourceInitialized -= OnWindowSourceInitialized;
                Apply(window);
            }
        }

        private static void Apply(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var preference = DwmwcpRoundSmall;
            DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
