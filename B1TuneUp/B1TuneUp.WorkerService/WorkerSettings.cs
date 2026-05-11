using System;
using System.Configuration;

namespace B1TuneUp.WorkerService
{
    public sealed class WorkerSettings
    {
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public int PollSeconds { get; set; }
        public int WatchdogSeconds { get; set; }
        public string LogDirectory { get; set; }
        public string WorkerId { get; set; }
        public string ReportRunnerCommand { get; set; }
        public string PrintRunnerCommand { get; set; }
        public string AddonBridgeCommand { get; set; }
        public bool ConnectSapDi { get; set; }
        public string SapServer { get; set; }
        public string SapCompanyDb { get; set; }
        public string SapUser { get; set; }
        public string SapPassword { get; set; }
        public string DbUser { get; set; }
        public string DbPassword { get; set; }
        public string DbServerType { get; set; }
        public string SapLanguage { get; set; }

        public static WorkerSettings Load()
        {
            return new WorkerSettings
            {
                Provider = Read("Provider", "SqlServer"),
                ConnectionString = Read("ConnectionString", string.Empty),
                PollSeconds = ReadInt("PollSeconds", 30),
                WatchdogSeconds = ReadInt("WatchdogSeconds", 180),
                LogDirectory = Read("LogDirectory", "Logs"),
                WorkerId = Read("WorkerId", Environment.MachineName),
                ReportRunnerCommand = Read("ReportRunnerCommand", string.Empty),
                PrintRunnerCommand = Read("PrintRunnerCommand", string.Empty),
                AddonBridgeCommand = Read("AddonBridgeCommand", string.Empty),
                ConnectSapDi = ReadBool("ConnectSapDi", false),
                SapServer = Read("SapServer", string.Empty),
                SapCompanyDb = Read("SapCompanyDb", string.Empty),
                SapUser = Read("SapUser", string.Empty),
                SapPassword = Read("SapPassword", string.Empty),
                DbUser = Read("DbUser", string.Empty),
                DbPassword = Read("DbPassword", string.Empty),
                DbServerType = Read("DbServerType", string.Empty),
                SapLanguage = Read("SapLanguage", string.Empty)
            };
        }

        private static string Read(string key, string fallback)
            => ConfigurationManager.AppSettings[key] ?? Environment.GetEnvironmentVariable("B1TUNEUP_WORKER_" + key.ToUpperInvariant()) ?? fallback;

        private static int ReadInt(string key, int fallback)
            => int.TryParse(Read(key, fallback.ToString()), out var value) ? value : fallback;

        private static bool ReadBool(string key, bool fallback)
            => bool.TryParse(Read(key, fallback.ToString()), out var value) ? value : fallback;
    }
}
