using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ValidationUi
{
    public static class ValidationDesignerLauncher
    {
        public static void Show(string formFilter = null, string itemFilter = null)
        {
            WpfWindowHost.ShowSingleton(() => new ValidationDesignerWindow(new ValidationDesignerLaunchOptions
            {
                FormFilter = formFilter,
                ItemFilter = itemFilter
            }));
        }

        public static void ShowQuickValidation(string formFilter, string itemFilter, string columnFilter = null)
        {
            WpfWindowHost.ShowSingleton(() => new ValidationDesignerWindow(new ValidationDesignerLaunchOptions
            {
                FormFilter = formFilter,
                ItemFilter = itemFilter,
                ColumnFilter = columnFilter,
                StartNewValidation = true
            }));
        }

        public static void ShowQuickMandatory(string formFilter, string itemFilter, string columnFilter = null)
        {
            WpfWindowHost.ShowSingleton(() => new ValidationDesignerWindow(new ValidationDesignerLaunchOptions
            {
                FormFilter = formFilter,
                ItemFilter = itemFilter,
                ColumnFilter = columnFilter,
                StartNewMandatory = true
            }));
        }
    }
}
