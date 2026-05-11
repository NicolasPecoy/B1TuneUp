using System.ServiceProcess;

namespace B1TuneUp.WorkerService
{
    public sealed class WorkerWindowsService : ServiceBase
    {
        private WorkerHost _host;

        public WorkerWindowsService()
        {
            ServiceName = "B1TuneUpWorker";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _host = new WorkerHost(WorkerSettings.Load());
            _host.Start();
        }

        protected override void OnStop()
        {
            _host?.Stop();
        }
    }
}
