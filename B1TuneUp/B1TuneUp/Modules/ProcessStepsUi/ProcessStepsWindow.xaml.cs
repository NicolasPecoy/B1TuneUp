using System.Windows;
using System.Windows.Forms.Integration;
using SAPbouiCOM;
using B1TuneUp.Modules;

namespace B1TuneUp.Modules.ProcessStepsUi
{
    public partial class ProcessStepsWindow : Window
    {
        public ProcessStepsWindow(ProcessInfo info, System.Collections.Generic.List<ProcessStep> steps, Form form, string processEntry)
        {
            InitializeComponent();
            DataContext = new ProcessStepsViewModel(info, steps, form, processEntry);
            ElementHost.EnableModelessKeyboardInterop(this);
        }
    }
}
