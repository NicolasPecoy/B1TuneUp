using B1TuneUp.Utils;

namespace B1TuneUp.Modules.LayoutManagerUi
{
    public static class LayoutManagerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new LayoutManagerWindow());
        }
    }
}
