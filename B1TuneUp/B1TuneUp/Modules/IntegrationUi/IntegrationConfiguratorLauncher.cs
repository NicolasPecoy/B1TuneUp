using B1TuneUp.Utils;

namespace B1TuneUp.Modules.IntegrationUi
{
    public static class IntegrationConfiguratorLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new IntegrationConfiguratorWindow());
        }
    }
}
