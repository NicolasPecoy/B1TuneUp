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

        public void Connect()
        {
            try
            {
                Application = SAPbouiCOM.Framework.Application.SBO_Application;
                Company = (SAPbobsCOM.Company)Application.Company.GetDICompany();
                
                IsHana = Company.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB;

                // Configurar metadatos si es necesario
                MetadataManager.SetupMetadata();

                Application.StatusBar.SetText("B1TuneUp: Conectado con éxito", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error al conectar con SAP B1: {ex.Message}");
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
