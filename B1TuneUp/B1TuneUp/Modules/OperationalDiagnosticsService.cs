using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class OperationalDiagnosticsService
    {
        public static IList<OperationalHealthEntry> RunHealthChecks()
        {
            var checks = new List<OperationalHealthEntry>();
            Check(checks, "COM", "Company connected", () => B1App.Instance?.Company?.Connected == true, "DI API Company connection.");
            Check(checks, "COM", "Application available", () => B1App.Instance?.Application != null, "SAPbouiCOM Application.");
            Check(checks, "Permissions", "Current user", () => !string.IsNullOrWhiteSpace(B1App.Instance.Company.UserName), SafeUser());
            Check(checks, "Metadata", "Required schema", () => MetadataRegistryService.Validate().All(m => m.Exists), "UDT/UDF registry.");
            Check(checks, "License", "License/trial", () => ProductLifecycleService.GetInfo().LicenseStatus != "Expired", ProductLifecycleService.GetInfo().Detail);
            Check(checks, "Version", "SAP compatibility", () => ProductLifecycleService.GetInfo().CompatibilityStatus == "OK", ProductLifecycleService.GetInfo().SapVersion);
            Check(checks, "Worker", "Runtime", () => B1TuneUpWorkerRuntime.IsRunning, B1TuneUpWorkerRuntime.IsRunning ? "Worker runtime active." : "Worker runtime stopped.");
            Check(checks, "Crystal", "Runtime", CrystalReportEngineService.IsCrystalRuntimeAvailable, CrystalReportEngineService.IsCrystalRuntimeAvailable() ? "Crystal runtime available." : "Crystal runtime not found.");
            Check(checks, "Filesystem", "Log folder writable", () =>
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "health.tmp");
                File.WriteAllText(file, DateTime.Now.ToString("O"));
                File.Delete(file);
                return true;
            }, "Logs folder.");
            return checks;
        }

        public static IList<TestRunResult> GetOperationalLogSummary()
        {
            return AuditLogService.GetEntries(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1))
                .GroupBy(e => new { e.Type, e.Status, e.User })
                .Select(g => new TestRunResult
                {
                    Area = g.Key.Type,
                    Code = g.Key.User,
                    Status = g.Key.Status,
                    DurationMs = g.Count(),
                    Detail = $"{g.Count()} events"
                })
                .OrderByDescending(r => r.DurationMs)
                .ToList();
        }

        public static string ExportSupportPackage(string path = null)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            path = string.IsNullOrWhiteSpace(path)
                ? Path.Combine(dir, "b1tuneup-support-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip")
                : path;

            string temp = Path.Combine(Path.GetTempPath(), "b1tuneup-support-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                File.WriteAllText(Path.Combine(temp, "health.json"), JsonSerializer.Serialize(RunHealthChecks(), new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(temp, "lifecycle.json"), JsonSerializer.Serialize(ProductLifecycleService.GetInfo(), new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(temp, "diagnostics.json"), JsonSerializer.Serialize(ConfigurationCenterService.RunDiagnostics(), new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(temp, "config-package.json"), JsonSerializer.Serialize(ConfigurationCenterService.BuildPackage(), new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(temp, "audit-summary.json"), JsonSerializer.Serialize(GetOperationalLogSummary(), new JsonSerializerOptions { WriteIndented = true }));
                CopyLatestLogs(dir, temp);
                if (File.Exists(path)) File.Delete(path);
                ZipFile.CreateFromDirectory(temp, path);
                AuditLogManager.LogAction("SupportPackage", $"Exportado paquete soporte {path}", "Export");
                return path;
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        public static long Measure(string area, string code, Action action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                action();
                return sw.ElapsedMilliseconds;
            }
            finally
            {
                sw.Stop();
                AuditLogManager.LogAction("Performance", $"{area}:{code} {sw.ElapsedMilliseconds}ms", "Timing");
            }
        }

        private static void Check(ICollection<OperationalHealthEntry> list, string area, string name, Func<bool> predicate, string detail)
        {
            try
            {
                bool ok = predicate();
                list.Add(new OperationalHealthEntry { Area = area, Check = name, Status = ok ? "OK" : "Warning", IsOk = ok, Detail = detail });
            }
            catch (Exception ex)
            {
                list.Add(new OperationalHealthEntry { Area = area, Check = name, Status = "Error", IsOk = false, Detail = ex.Message });
            }
        }

        private static string SafeUser()
        {
            try { return B1App.Instance.Company.UserName; }
            catch { return string.Empty; }
        }

        private static void CopyLatestLogs(string sourceDir, string targetDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(5))
            {
                try { File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true); } catch { }
            }
        }
    }
}
