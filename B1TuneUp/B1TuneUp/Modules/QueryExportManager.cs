using System;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules.QueryExportUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class QueryExportManager
    {
        public static void OpenQueryExportWindow(Form activeForm = null)
        {
            try
            {
                if (activeForm == null)
                {
                    activeForm = SapUiSafe.TryGetActiveForm();
                }

                QueryExportLauncher.Show();
            }
            catch (Exception ex)
            {
                try
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Query Exporter: {ex.Message}", BoMessageTime.bmt_Short, true);
                }
                catch { }
            }
        }
    }
}
