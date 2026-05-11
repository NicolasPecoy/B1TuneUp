using System;
using System.Threading;

namespace B1TuneUp.WorkerService
{
    public sealed class WorkerHost
    {
        private readonly WorkerSettings _settings;
        private readonly WorkerRepository _repository;
        private readonly WorkerJobExecutor _executor;
        private readonly WatchdogMonitor _watchdog;
        private Timer _timer;
        private int _running;

        public WorkerHost(WorkerSettings settings)
        {
            _settings = settings ?? WorkerSettings.Load();
            _repository = new WorkerRepository(_settings);
            _executor = new WorkerJobExecutor(_settings);
            _watchdog = new WatchdogMonitor(_settings);
        }

        public void Start()
        {
            WorkerLogger.Info("Worker host starting.");
            _watchdog.WriteHeartbeat("Starting");
            TryConnectDiApi();
            _timer = new Timer(_ => RunOnce(), null, TimeSpan.Zero, TimeSpan.FromSeconds(Math.Max(5, _settings.PollSeconds)));
        }

        public void Stop()
        {
            WorkerLogger.Info("Worker host stopping.");
            _timer?.Dispose();
            _watchdog.WriteHeartbeat("Stopped");
        }

        public void RunOnce()
        {
            if (Interlocked.Exchange(ref _running, 1) == 1) return;
            try
            {
                _watchdog.WriteHeartbeat("Polling");
                foreach (var job in _repository.GetPendingJobs())
                {
                    Execute(job);
                }
                _watchdog.WriteHeartbeat("Idle");
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("Unhandled worker tick error.", ex);
                _watchdog.WriteHeartbeat("Error: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        private void Execute(WorkerJob job)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.Code)) return;
            try
            {
                WorkerLogger.Info("Executing job " + job.Code + " (" + job.JobType + ").");
                _repository.MarkRunning(job.Code);
                var result = _executor.Execute(job);
                _repository.MarkDone(job.Code, result);
                WorkerLogger.Info("Job " + job.Code + " completed: " + result);
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("Job " + job.Code + " failed.", ex);
                if (job.RetryCount + 1 <= Math.Max(0, job.MaxRetries))
                {
                    _repository.MarkRetry(job.Code, job.RetryCount + 1, ex.Message, DateTime.UtcNow.AddMinutes(Math.Min(60, 1 + job.RetryCount * 2)));
                    return;
                }
                _repository.MarkFailed(job.Code, ex.Message);
            }
        }

        private void TryConnectDiApi()
        {
            try
            {
                if (!_settings.ConnectSapDi) return;
                using (var connector = new SapDiConnector(_settings))
                {
                    connector.Connect();
                    WorkerLogger.Info("SAP DI API connection test succeeded.");
                }
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("SAP DI API connection test failed.", ex);
            }
        }
    }
}
