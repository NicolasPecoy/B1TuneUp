using B1TuneUp.Utils;

namespace B1TuneUp.Modules.SchedulerUi
{
    public static class SchedulerLauncher
    {
        public static void Show()
        {
            WpfWindowHost.ShowSingleton(() => new SchedulerWindow());
        }
    }
}
