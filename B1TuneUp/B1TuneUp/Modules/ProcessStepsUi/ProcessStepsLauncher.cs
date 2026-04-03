using B1TuneUp.Modules;
using B1TuneUp.Utils;
using SAPbouiCOM;
using System.Collections.Generic;

namespace B1TuneUp.Modules.ProcessStepsUi
{
    public static class ProcessStepsLauncher
    {
        public static void Show(ProcessInfo info, List<ProcessStep> steps, Form form, string processEntry)
        {
            var key = $"PSTEP_{processEntry}";
            WpfWindowHost.ShowSingleton(key, () => new ProcessStepsWindow(info, steps, form, processEntry));
        }
    }
}
