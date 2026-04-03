using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ActionQuickUi
{
    public static class ActionQuickLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ActionQuickWindow());
        }
    }
}
