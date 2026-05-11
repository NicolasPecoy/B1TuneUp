using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace B1TuneUp.WorkerService
{
    public sealed class WorkerRepository
    {
        private readonly WorkerSettings _settings;

        public WorkerRepository(WorkerSettings settings)
        {
            _settings = settings;
        }

        public IList<WorkerJob> GetPendingJobs()
        {
            var jobs = new List<WorkerJob>();
            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                WorkerLogger.Info("Worker connection string is empty; no queue polling performed.");
                return jobs;
            }

            try
            {
                using (var connection = CreateConnection())
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = BuildSelectSql();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jobs.Add(WorkerJob.FromJson(Convert.ToString(reader["U_Value"])));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("Unable to read worker queue.", ex);
            }
            return jobs;
        }

        public void MarkRunning(string code) => UpdateStatus(code, "Running", null, null, null);
        public void MarkDone(string code, string result) => UpdateStatus(code, "Done", result, null, null);
        public void MarkFailed(string code, string error) => UpdateStatus(code, "Failed", null, error, null);
        public void MarkRetry(string code, int retryCount, string error, DateTime nextRunUtc) => UpdateStatus(code, "Pending", null, error, new RetryInfo { RetryCount = retryCount, DueAt = nextRunUtc });

        private void UpdateStatus(string code, string status, string result, string error, RetryInfo retry)
        {
            try
            {
                var job = GetByCode(code);
                if (job == null) return;
                job.Status = status;
                job.LastResult = result ?? job.LastResult;
                job.LastError = error ?? string.Empty;
                job.UpdatedAt = DateTime.UtcNow;
                if (retry != null)
                {
                    job.RetryCount = retry.RetryCount;
                    job.DueAt = retry.DueAt;
                }

                using (var connection = CreateConnection())
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = BuildUpdateSql();
                    AddParameter(command, "value", job.ToJson());
                    AddParameter(command, "code", code);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("Unable to update worker job " + code + ".", ex);
            }
        }

        private WorkerJob GetByCode(string code)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = BuildGetSql();
                AddParameter(command, "code", code);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? WorkerJob.FromJson(Convert.ToString(reader["U_Value"])) : null;
                }
            }
        }

        private DbConnection CreateConnection()
        {
            if (string.Equals(_settings.Provider, "Odbc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_settings.Provider, "Hana", StringComparison.OrdinalIgnoreCase))
            {
                return new OdbcConnection(_settings.ConnectionString);
            }
            return new SqlConnection(_settings.ConnectionString);
        }

        private string BuildSelectSql()
        {
            if (IsOdbc)
            {
                return "SELECT \"U_Code\", \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" LIKE 'WORKERJOB_%' AND \"U_Value\" LIKE '%\"status\":\"Pending\"%' ORDER BY \"U_Code\"";
            }
            return "SELECT U_Code, U_Value FROM [@BTUN_TBOX] WHERE U_Code LIKE 'WORKERJOB_%' AND U_Value LIKE '%\"status\":\"Pending\"%' ORDER BY U_Code";
        }

        private string BuildGetSql()
        {
            return IsOdbc ? "SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = ?" : "SELECT U_Value FROM [@BTUN_TBOX] WHERE U_Code = @code";
        }

        private string BuildUpdateSql()
        {
            return IsOdbc ? "UPDATE \"@BTUN_TBOX\" SET \"U_Value\" = ? WHERE \"U_Code\" = ?" : "UPDATE [@BTUN_TBOX] SET U_Value = @value WHERE U_Code = @code";
        }

        private bool IsOdbc => string.Equals(_settings.Provider, "Odbc", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(_settings.Provider, "Hana", StringComparison.OrdinalIgnoreCase);

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = command is OdbcCommand ? string.Empty : "@" + Regex.Replace(name, "^@", string.Empty);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private sealed class RetryInfo
        {
            public int RetryCount { get; set; }
            public DateTime DueAt { get; set; }
        }
    }
}
