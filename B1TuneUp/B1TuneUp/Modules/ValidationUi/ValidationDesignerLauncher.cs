using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ValidationUi
{
    public static class ValidationDesignerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new ValidationDesignerWindow());
        }
    }
}
