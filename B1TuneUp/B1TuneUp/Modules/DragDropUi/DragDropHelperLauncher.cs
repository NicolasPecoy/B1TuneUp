using B1TuneUp.Utils;

namespace B1TuneUp.Modules.DragDropUi
{
    public static class DragDropHelperLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new DragDropHelperWindow());
        }
    }
}
