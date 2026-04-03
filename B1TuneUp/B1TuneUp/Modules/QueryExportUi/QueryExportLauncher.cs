using B1TuneUp.Utils;

namespace B1TuneUp.Modules.QueryExportUi
{
    public static class QueryExportLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new QueryExportWindow());
        }
    }
}
