using B1TuneUp.Utils;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public static class PlacementEnhancementLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new PlacementEnhancementWindow());
        }
    }
}
