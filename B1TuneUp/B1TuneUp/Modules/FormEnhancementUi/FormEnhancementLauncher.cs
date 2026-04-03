using B1TuneUp.Utils;

namespace B1TuneUp.Modules.FormEnhancementUi
{
    public static class FormEnhancementLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new FormEnhancementWindow());
        }
    }
}
