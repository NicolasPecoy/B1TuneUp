using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace B1TuneUp.Utils
{
    /// <summary>
    /// Hosts WPF windows on dedicated STA threads so they can be launched safely from SAP B1 event threads.
    /// Ensures one instance per logical window key and marshals activation calls to the owning dispatcher.
    /// </summary>
    public static class WpfWindowHost
    {
        private sealed class WindowHandle
        {
            public Window Window;
            public Dispatcher Dispatcher;
        }

        private static readonly Dictionary<string, WindowHandle> Windows = new Dictionary<string, WindowHandle>();
        private static readonly object SyncRoot = new object();

        public static void ShowSingleton<TWindow>(Func<TWindow> factory)
            where TWindow : Window
        {
            ShowSingleton(typeof(TWindow).FullName, factory);
        }

        public static void ShowSingleton<TWindow>(string key, Func<TWindow> factory)
            where TWindow : Window
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            WindowHandle handle;
            lock (SyncRoot)
            {
                if (Windows.TryGetValue(key, out handle) && handle.Window != null && handle.Dispatcher != null)
                {
                    handle.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (handle.Window == null) return;
                        if (handle.Window.WindowState == WindowState.Minimized)
                        {
                            handle.Window.WindowState = WindowState.Normal;
                        }
                        handle.Window.Activate();
                    }));
                    return;
                }

                handle = new WindowHandle();
                Windows[key] = handle;
            }

            Exception initException = null;
            using (var ready = new ManualResetEventSlim(false))
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        var dispatcher = Dispatcher.CurrentDispatcher;
                        var window = factory();

                        EventHandler closedHandler = null;
                        closedHandler = (_, __) =>
                        {
                            window.Closed -= closedHandler;
                            lock (SyncRoot)
                            {
                                Windows.Remove(key);
                            }

                            if (!dispatcher.HasShutdownStarted)
                            {
                                dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                            }
                        };
                        window.Closed += closedHandler;

                        lock (SyncRoot)
                        {
                            handle.Window = window;
                            handle.Dispatcher = dispatcher;
                        }

                        ready.Set();
                        window.Show();
                        Dispatcher.Run();
                    }
                    catch (Exception ex)
                    {
                        initException = ex;
                        ready.Set();
                    }
                })
                {
                    IsBackground = true
                };

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                ready.Wait();

                if (initException != null)
                {
                    lock (SyncRoot)
                    {
                        Windows.Remove(key);
                    }
                    throw initException;
                }
            }
        }
    }
}
