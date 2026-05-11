using System;
using System.Configuration;
using System.IO;

namespace B1TuneUp.WorkerService
{
    public static class WorkerLogger
    {
        private static readonly object Sync = new object();

        public static void Info(string message) => Write("INFO", message, null);
        public static void Error(string message, Exception ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception ex)
        {
            lock (Sync)
            {
                string dir = ConfigurationManager.AppSettings["LogDirectory"] ?? "Logs";
                Directory.CreateDirectory(dir);
                string line = $"{DateTime.Now:O} [{level}] {message}{(ex == null ? string.Empty : " :: " + ex)}";
                File.AppendAllText(Path.Combine(dir, "B1TuneUp.Worker.log"), line + Environment.NewLine);
                Console.WriteLine(line);
            }
        }
    }
}
