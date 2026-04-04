using System;
using System.Threading;
using System.Threading.Tasks;

namespace B1TuneUp.Modules
{
    public interface IAutoRefreshRegistration : IDisposable
    {
        void Update(bool enabled, int intervalSeconds);
    }

    public static class AutoRefreshCoordinator
    {
        public static IAutoRefreshRegistration Register(string context, Func<Task> callback)
        {
            return new AutoRefreshRegistration(context, callback);
        }

        private sealed class AutoRefreshRegistration : IAutoRefreshRegistration
        {
            private readonly string _context;
            private readonly Func<Task> _callback;
            private readonly object _tickLock = new object();
            private Timer _timer;
            private bool _enabled;
            private int _intervalSeconds;

            public AutoRefreshRegistration(string context, Func<Task> callback)
            {
                _context = string.IsNullOrWhiteSpace(context) ? "default" : context.Trim();
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
                var pref = AutoRefreshPreferenceService.Load(_context);
                _enabled = pref.Enabled;
                _intervalSeconds = pref.IntervalSeconds;
                if (_enabled)
                {
                    StartTimer(TimeSpan.FromSeconds(_intervalSeconds));
                }
            }

            public void Update(bool enabled, int intervalSeconds)
            {
                _enabled = enabled;
                _intervalSeconds = Math.Max(5, intervalSeconds);
                AutoRefreshPreferenceService.Save(_context, _enabled, _intervalSeconds);
                if (!_enabled)
                {
                    _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    return;
                }
                StartTimer(TimeSpan.FromSeconds(_intervalSeconds));
            }

            public void Dispose()
            {
                _timer?.Dispose();
                _timer = null;
            }

            private void StartTimer(TimeSpan dueTime)
            {
                if (_timer == null)
                {
                    _timer = new Timer(OnTick, null, dueTime, TimeSpan.FromSeconds(_intervalSeconds));
                }
                else
                {
                    _timer.Change(dueTime, TimeSpan.FromSeconds(_intervalSeconds));
                }
            }

            private void OnTick(object state)
            {
                if (!_enabled) return;
                if (!Monitor.TryEnter(_tickLock)) return;
                Task.Run(async () =>
                {
                    try
                    {
                        await _callback().ConfigureAwait(false);
                    }
                    catch
                    {
                        // swallow to keep timer alive
                    }
                    finally
                    {
                        Monitor.Exit(_tickLock);
                    }
                });
            }
        }
    }
}

