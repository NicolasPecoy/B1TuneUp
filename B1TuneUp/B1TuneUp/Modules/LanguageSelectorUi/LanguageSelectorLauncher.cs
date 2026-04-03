using B1TuneUp.Utils;

namespace B1TuneUp.Modules.LanguageSelectorUi
{
    public static class LanguageSelectorLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new LanguageSelectorWindow());
        }
    }
}
