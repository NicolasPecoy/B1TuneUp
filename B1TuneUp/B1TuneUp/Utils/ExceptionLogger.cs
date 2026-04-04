using System;
using System.Text;
using B1TuneUp.Modules;

namespace B1TuneUp.Utils
{
    /// <summary>
    /// Helper that records exception details into @BTUN_LOG so issues can be traced back to SAP.
    /// </summary>
    public static class ExceptionLogger
    {
        private const int MaxDetailLength = 950;

        public static void Log(Exception exception, string context = null, string status = "Error")
        {
            if (exception == null) return;

            string detail = BuildDetails(exception, context);

            try
            {
                AuditLogManager.LogAction("Exception", detail, status);
            }
            catch
            {
                // ignore errors while logging to user table
            }

            try
            {
                Logger.Error(detail, exception);
            }
            catch
            {
                // ensure we never throw while logging
            }
        }

        public static void LogUnhandled(Exception exception, string source)
        {
            Log(exception, $"Unhandled:{source}", "Failure");
        }

        public static void LogHandled(Exception exception, string context)
        {
            Log(exception, context, "Handled");
        }

        private static string BuildDetails(Exception exception, string context)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(context))
            {
                sb.Append(context.Trim());
                sb.Append(" | ");
            }

            sb.Append(exception.GetType().FullName);
            sb.Append(": ");
            sb.Append(exception.Message);

            if (exception.InnerException != null)
            {
                sb.Append(" | Inner: ");
                sb.Append(exception.InnerException.GetType().FullName);
                sb.Append(": ");
                sb.Append(exception.InnerException.Message);
            }

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                string compressedStack = exception.StackTrace
                    .Replace(Environment.NewLine, " -> ")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
                sb.Append(" | Stack: ");
                sb.Append(compressedStack);
            }

            string result = sb.ToString();
            if (result.Length > MaxDetailLength)
            {
                result = result.Substring(0, MaxDetailLength);
            }

            return result;
        }
    }
}
