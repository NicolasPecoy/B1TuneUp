using System;
using System.Runtime.InteropServices;
using B1TuneUp.Utils;
using SAPbouiCOM;
using SAPbouiCOM.Framework;

namespace B1TuneUp.Core
{
    public class B1App
    {
        private static B1App _instance;
        public static B1App Instance => _instance ?? (_instance = new B1App());

        public SAPbouiCOM.Application Application { get; private set; }
        public SAPbobsCOM.Company Company { get; private set; }
        public bool IsHana { get; private set; }

        private B1App() { }

        public void Connect(string language = null)
        {
            try
            {
                Application = EnsureUiApiConnection();
                Company = (SAPbobsCOM.Company)Application.Company.GetDICompany();
                
                IsHana = Company.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB;

                // Inicializar localización y logger
                if (string.IsNullOrEmpty(language))
                {
                    // try to read persisted setting
                    try { language = Utils.SettingsManager.GetSetting("Language", null); } catch { }
                }
                LocalizationManager.Init(language);
                Logger.Init();
                Logger.Info("B1App starting connect...");

                // Configurar metadatos si es necesario
                MetadataManager.SetupMetadata();

                Application.StatusBar.SetText(LocalizationManager.GetString("B1TuneUp.Connected"), SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                Logger.Info("Connected to SAP B1 successfully");
                // capture unhandled exceptions globally
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        var ex = e.ExceptionObject as Exception;
                        if (ex != null) Logger.Error("Unhandled exception", ex);
                    }
                    catch { }
                };
                System.Windows.Forms.Application.ThreadException += (s, e) =>
                {
                    try { Logger.Error("UI thread exception", e.Exception); } catch { }
                };
            }
            catch (Exception ex)
            {
                var msg = string.Format(LocalizationManager.GetString("Error.Connecting"), ex.Message);
                Logger.Error(msg, ex);
                System.Windows.Forms.MessageBox.Show(msg);
                Environment.Exit(0);
            }
        }

        private SAPbouiCOM.Application EnsureUiApiConnection()
        {
            // When launched from SAP Business One the framework automatically passes the
            // connection string via command line, so instantiating the Application is enough.
            if (SAPbouiCOM.Framework.Application.SBO_Application != null)
            {
                return SAPbouiCOM.Framework.Application.SBO_Application;
            }

            try
            {
                new SAPbouiCOM.Framework.Application();
                if (SAPbouiCOM.Framework.Application.SBO_Application != null)
                {
                    return SAPbouiCOM.Framework.Application.SBO_Application;
                }
            }
            catch
            {
                // Swallow and try manual connection below.
            }

            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                var guiApi = new SboGuiApi();
                guiApi.Connect(args[1]);
                return guiApi.GetApplication(-1);
            }

            throw new InvalidOperationException("No se pudo inicializar la UI API de SAP Business One. Ejecute el add-on desde el cliente SAP o pase el connection string como primer argumento.");
        }

        public void ReleaseComObject(object obj)
        {
            if (obj != null && Marshal.IsComObject(obj))
            {
                Marshal.ReleaseComObject(obj);
            }
        }
    }
}
