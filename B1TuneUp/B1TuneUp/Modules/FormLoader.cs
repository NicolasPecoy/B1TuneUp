using System;
using System.IO;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Modules
{
    public static class FormLoader
    {
        public static void LoadFromSRF(string fileName, string formUID = "")
        {
            try
            {
                string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SRF", fileName);
                if (!File.Exists(xmlPath))
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Archivo SRF no encontrado: {xmlPath}", BoMessageTime.bmt_Short, true);
                    return;
                }

                string xmlContent = File.ReadAllText(xmlPath);
                
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.XmlData = xmlContent;
                if (!string.IsNullOrEmpty(formUID))
                {
                    fcp.UniqueID = formUID;
                }

                B1App.Instance.Application.Forms.AddEx(fcp);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando SRF {fileName}: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
