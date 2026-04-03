using B1TuneUp.Utils;

namespace B1TuneUp.Modules.EmailDesigner
{
    public static class EmailDesignerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new EmailDesignerWindow());
        }
    }
}
