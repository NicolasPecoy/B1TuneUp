using B1TuneUp.Utils;

namespace B1TuneUp.Modules.TemplateReportUi
{
    public static class TemplateReportLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new TemplateReportWindow());
        }
    }
}
