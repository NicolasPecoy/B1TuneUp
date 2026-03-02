using System;
using System.Windows.Forms;
using B1TuneUp.Core;

using B1TuneUp.Modules;

namespace B1TuneUp
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Conectar con SAP B1
                B1App.Instance.Connect();

                // Inicializar el despachador de eventos
                EventDispatcher.Instance.Init();

                // Inicializar el Programador de Tareas
                SchedulerManager.Init();

                // Mantener el Add-on ejecutándose
                System.Windows.Forms.Application.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal Error: {ex.Message}");
            }
        }
    }
}
