using B1TuneUp.Utils;

namespace B1TuneUp.Modules.MacroEngineUi
{
    public static class MacroEngineLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new MacroEngineWindow());
        }
    }
}
