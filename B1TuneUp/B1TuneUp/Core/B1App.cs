using System;
using System.Runtime.InteropServices;
using B1TuneUp.Utils;
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
                Application = SAPbouiCOM.Framework.Application.SBO_Application;
                Company = (SAPbobsCOM.Company)Application.Company.GetDICompany();
                
                IsHana = Company.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB;

                // Inicializar localización y logger
                LocalizationManager.Init(language);
                Logger.Init();

                // Configurar metadatos si es necesario
                MetadataManager.SetupMetadata();

                Application.StatusBar.SetText(LocalizationManager.GetString("B1TuneUp.Connected"), SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format(LocalizationManager.GetString("Error.Connecting"), ex.Message));
                Environment.Exit(0);
            }
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
