using System;
using Serilog;
using Serilog.Events;

namespace B1TuneUp.Utils
{
    public static class Logger
    {
        private static bool _initialized = false;

        public static void Init(string logFolder = null)
        {
            if (_initialized) return;
            _initialized = true;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var folder = logFolder;
            if (string.IsNullOrEmpty(folder)) folder = System.IO.Path.Combine(basePath, "Logs");

            try
            {
                if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
            }
            catch
            {
                // ignore
            }

            var logFile = System.IO.Path.Combine(folder, "B1TuneUp-.log");

            var cfg = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);

            // also write to console for interactive debugging
            try
            {
                cfg = cfg.WriteTo.Console();
            }
            catch
            {
                // ignore platform where console sink might not be available
            }

            // if SEQ is configured via environment variable, enable it
            var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");
            if (!string.IsNullOrEmpty(seqUrl))
            {
                try
                {
                    cfg = cfg.WriteTo.Seq(seqUrl);
                }
                catch
                {
                    // ignore
                }
            }

            Log.Logger = cfg.CreateLogger();

            Log.Information("Logger initialized");
        }

        public static void Info(string message)
        {
            Log.Information(message);
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Error(string message, Exception ex)
        {
            Log.Error(ex, message);
        }
    }
}
