using B1TuneUp.Utils;

namespace B1TuneUp.Modules.AutomationDashboardUi
{
    public static class AutomationDashboardLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new AutomationDashboardWindow());
        }
    }
}
