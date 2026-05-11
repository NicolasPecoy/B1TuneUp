using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ApiStudio
{
    public static class ApiStudioLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ApiStudioWindow());
        }
    }
}
