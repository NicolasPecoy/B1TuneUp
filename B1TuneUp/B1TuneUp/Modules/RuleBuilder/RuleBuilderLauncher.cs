using B1TuneUp.Utils;

namespace B1TuneUp.Modules.RuleBuilder
{
    public static class RuleBuilderLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new RuleBuilderWindow());
        }
    }
}
