using B1TuneUp.Utils;

namespace B1TuneUp.Modules.DashboardSearchMacroUi
{
    public static class DashboardSearchMacroLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new DashboardSearchMacroWindow());
        }
    }
}
