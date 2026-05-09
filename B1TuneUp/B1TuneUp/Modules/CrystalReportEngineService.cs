using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using B1TuneUp.Models;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class CrystalReportEngineService
    {
        public static bool IsCrystalRuntimeAvailable()
        {
            return Type.GetType("CrystalDecisions.CrystalReports.Engine.ReportDocument, CrystalDecisions.CrystalReports.Engine") != null;
        }

        public static string ExportToPdf(string reportCode, string parametersJson = null, string outputPath = null)
        {
            var report = FindReport(reportCode);
            if (report == null) throw new InvalidOperationException($"Report template '{reportCode}' no existe.");
            if (string.IsNullOrWhiteSpace(report.DataBase64)) throw new InvalidOperationException($"Report template '{reportCode}' no tiene archivo RPT/XML.");
            if (!IsCrystalRuntimeAvailable()) throw new InvalidOperationException("Crystal Reports runtime no esta instalado o no esta disponible para el add-on.");

            string tempRpt = Path.Combine(Path.GetTempPath(), "b1tuneup-" + Guid.NewGuid().ToString("N") + ".rpt");
            outputPath = string.IsNullOrWhiteSpace(outputPath)
                ? Path.Combine(Path.GetTempPath(), SanitizeFileName(report.Name) + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf")
                : outputPath;

            object doc = null;
            try
            {
                File.WriteAllBytes(tempRpt, Convert.FromBase64String(report.DataBase64));
                var docType = Type.GetType("CrystalDecisions.CrystalReports.Engine.ReportDocument, CrystalDecisions.CrystalReports.Engine", true);
                doc = Activator.CreateInstance(docType);
                docType.InvokeMember("Load", BindingFlags.InvokeMethod, null, doc, new object[] { tempRpt });
                ApplyDatabaseLogon(doc);
                ApplyParameters(doc, MergeParameters(report.Parameters, parametersJson));
                var exportFormatType = Type.GetType("CrystalDecisions.Shared.ExportFormatType, CrystalDecisions.Shared", true);
                object portableDoc = Enum.Parse(exportFormatType, "PortableDocFormat");
                docType.InvokeMember("ExportToDisk", BindingFlags.InvokeMethod, null, doc, new[] { portableDoc, outputPath });
                AuditLogManager.LogAction("CrystalReport", $"Exported {report.Name} to {outputPath}", "Export");
                return outputPath;
            }
            finally
            {
                try { doc?.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, null); } catch { }
                try { doc?.GetType().InvokeMember("Dispose", BindingFlags.InvokeMethod, null, doc, null); } catch { }
                try { if (File.Exists(tempRpt)) File.Delete(tempRpt); } catch { }
            }
        }

        public static string Print(string reportCode, string parametersJson = null, string printerName = null, int copies = 1)
        {
            var report = FindReport(reportCode);
            if (report == null) throw new InvalidOperationException($"Report template '{reportCode}' no existe.");
            if (!IsCrystalRuntimeAvailable()) throw new InvalidOperationException("Crystal Reports runtime no esta instalado o no esta disponible para el add-on.");

            string tempRpt = Path.Combine(Path.GetTempPath(), "b1tuneup-" + Guid.NewGuid().ToString("N") + ".rpt");
            object doc = null;
            try
            {
                File.WriteAllBytes(tempRpt, Convert.FromBase64String(report.DataBase64));
                var docType = Type.GetType("CrystalDecisions.CrystalReports.Engine.ReportDocument, CrystalDecisions.CrystalReports.Engine", true);
                doc = Activator.CreateInstance(docType);
                docType.InvokeMember("Load", BindingFlags.InvokeMethod, null, doc, new object[] { tempRpt });
                ApplyDatabaseLogon(doc);
                ApplyParameters(doc, MergeParameters(report.Parameters, parametersJson));
                if (!string.IsNullOrWhiteSpace(printerName))
                {
                    docType.InvokeMember("PrintOptions", BindingFlags.GetProperty, null, doc, null)
                        ?.GetType().InvokeMember("PrinterName", BindingFlags.SetProperty, null, docType.InvokeMember("PrintOptions", BindingFlags.GetProperty, null, doc, null), new object[] { printerName });
                }
                docType.InvokeMember("PrintToPrinter", BindingFlags.InvokeMethod, null, doc, new object[] { Math.Max(1, copies), false, 0, 0 });
                AuditLogManager.LogAction("CrystalReport", $"Printed {report.Name}", "Print");
                return report.Name;
            }
            finally
            {
                try { doc?.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, null); } catch { }
                try { doc?.GetType().InvokeMember("Dispose", BindingFlags.InvokeMethod, null, doc, null); } catch { }
                try { if (File.Exists(tempRpt)) File.Delete(tempRpt); } catch { }
            }
        }

        private static ReportTemplateDefinition FindReport(string code)
        {
            return ReportTemplateStorageService.GetTemplates(code)
                .FirstOrDefault(r => string.Equals(r.Name, code, StringComparison.OrdinalIgnoreCase))
                ?? ReportTemplateStorageService.GetTemplates(code).FirstOrDefault();
        }

        private static Dictionary<string, string> MergeParameters(string stored, string runtime)
        {
            var values = ParseKeyValue(stored);
            foreach (var pair in ParseKeyValue(runtime)) values[pair.Key] = pair.Value;
            return values;
        }

        private static Dictionary<string, string> ParseKeyValue(string raw)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return values;
            raw = raw.Trim();
            if (raw.StartsWith("{"))
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(raw)
                           ?? values;
                }
                catch { }
            }
            foreach (var part in raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split(new[] { '=' }, 2);
                if (pieces.Length == 2) values[pieces[0].Trim()] = pieces[1].Trim();
            }
            return values;
        }

        private static void ApplyParameters(object doc, Dictionary<string, string> values)
        {
            if (doc == null || values == null) return;
            foreach (var pair in values)
            {
                try { doc.GetType().InvokeMember("SetParameterValue", BindingFlags.InvokeMethod, null, doc, new object[] { pair.Key, pair.Value }); }
                catch (Exception ex) { ExceptionLogger.LogHandled(ex, $"CrystalReportEngineService.Parameter:{pair.Key}"); }
            }
        }

        private static void ApplyDatabaseLogon(object doc)
        {
            try
            {
                string user = B1TuneUp.Core.B1App.Instance.Company.DbUserName;
                string password = B1TuneUp.Core.B1App.Instance.Company.DbPassword;
                string server = B1TuneUp.Core.B1App.Instance.Company.Server;
                string database = B1TuneUp.Core.B1App.Instance.Company.CompanyDB;
                doc.GetType().InvokeMember("SetDatabaseLogon", BindingFlags.InvokeMethod, null, doc, new object[] { user, password, server, database });
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogHandled(ex, "CrystalReportEngineService.ApplyDatabaseLogon");
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = (name ?? "report").Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "report" : name;
        }
    }
}
