using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ValidationUi
{
    public static class ValidationDesignerLauncher
    {
        public static void Show(string formFilter = null, string itemFilter = null)
        {
            WpfWindowHost.ShowSingleton(() => new ValidationDesignerWindow(formFilter, itemFilter));
        }
    }
}
