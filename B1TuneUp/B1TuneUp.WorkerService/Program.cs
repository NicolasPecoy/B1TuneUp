using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;

namespace B1TuneUp.WorkerService
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--service", StringComparison.OrdinalIgnoreCase))
            {
                ServiceBase.Run(new WorkerWindowsService());
                return 0;
            }

            WorkerLogger.Info("B1TuneUp worker console starting.");
            var worker = new WorkerHost(WorkerSettings.Load());
            if (args.Length > 0 && args[0].Equals("--once", StringComparison.OrdinalIgnoreCase))
            {
                worker.RunOnce();
                return 0;
            }

            worker.Start();
            Console.WriteLine("B1TuneUp Worker running. Press Ctrl+C to stop.");
            var stop = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) => { eventArgs.Cancel = true; stop.Set(); };
            stop.WaitOne();
            worker.Stop();
            return 0;
        }
    }
}
