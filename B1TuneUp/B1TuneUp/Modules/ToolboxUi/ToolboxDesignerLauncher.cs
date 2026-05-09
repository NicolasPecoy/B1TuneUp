using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ToolboxUi
{
    public static class ToolboxDesignerLauncher
    {
        public static void Show(string initialCategory = null)
        {
            WpfWindowHost.ShowSingleton(() => new ToolboxDesignerWindow(initialCategory));
        }
    }
}
