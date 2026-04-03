using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ToolboxUi
{
    public static class ToolboxDesignerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ToolboxDesignerWindow());
        }
    }
}
