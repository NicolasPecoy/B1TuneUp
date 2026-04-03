using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ItemActionsUi
{
    public static class ItemActionsLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ItemActionsWindow());
        }
    }
}
