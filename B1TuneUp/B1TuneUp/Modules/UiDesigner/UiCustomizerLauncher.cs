using B1TuneUp.Utils;

namespace B1TuneUp.Modules.UiDesigner
{
    public static class UiCustomizerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new UiCustomizerWindow());
        }
    }
}
