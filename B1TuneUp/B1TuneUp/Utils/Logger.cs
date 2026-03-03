using System;
using System.IO;
using System.Text;

namespace B1TuneUp.Utils
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFolder;
        private static bool _initialized = false;

        public static void Init(string logFolder = null)
        {
            if (_initialized) return;
            _initialized = true;

            _logFolder = logFolder;
            if (string.IsNullOrEmpty(_logFolder))
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                _logFolder = Path.Combine(basePath, "Logs");
            }

            try
            {
                if (!Directory.Exists(_logFolder)) Directory.CreateDirectory(_logFolder);
            }
            catch
            {
                // ignore
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message + " | Exception: " + ex);
        }

        private static void Write(string level, string message)
        {
            try
            {
                if (!_initialized) Init(null);
                var file = Path.Combine(_logFolder, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log");
                var sb = new StringBuilder();
                sb.Append(DateTime.UtcNow.ToString("o"));
                sb.Append(" [");
                sb.Append(level);
                sb.Append("] ");
                sb.Append(message);
                sb.AppendLine();

                lock (_lock)
                {
                    File.AppendAllText(file, sb.ToString());
                }
            }
            catch
            {
                // best effort
            }
        }
    }
}
