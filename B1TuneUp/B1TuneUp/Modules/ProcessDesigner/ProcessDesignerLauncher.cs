using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ProcessDesigner
{
    public static class ProcessDesignerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ProcessDesignerWindow());
        }
    }
}
