using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ItemActionsUi
{
    public static class ItemActionsLauncher
    {
        public static void Show(string formFilter = null, string itemId = null)
        {
            WpfWindowHost.ShowSingleton(() => new ItemActionsWindow(formFilter, itemId));
        }
    }
}
