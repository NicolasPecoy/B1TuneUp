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

                // Determine language from args or environment
                string lang = null;
                if (args != null && args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                        {
                            lang = args[i].Substring("--lang=".Length);
                        }
                    }
                }

                // Conectar con SAP B1 (pasar idioma opcional)
                B1App.Instance.Connect(lang);

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
