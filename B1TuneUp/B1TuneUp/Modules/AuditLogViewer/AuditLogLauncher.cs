using B1TuneUp.Utils;

namespace B1TuneUp.Modules.AuditLogViewer
{
    public static class AuditLogLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new AuditLogWindow());
        }
    }
}
