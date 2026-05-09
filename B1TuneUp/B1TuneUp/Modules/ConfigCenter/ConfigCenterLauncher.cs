using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ConfigCenter
{
    public static class ConfigCenterLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ConfigCenterWindow());
        }
    }
}
