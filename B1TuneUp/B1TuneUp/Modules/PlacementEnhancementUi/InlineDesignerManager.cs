using System;
using System.Windows;
using B1TuneUp.Core;
using SAPbouiCOM;
using Application = System.Windows.Application;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public static class InlineDesignerManager
    {
        public static void ShowOverlay(SAPbouiCOM.Form form, string initialItemId = null)
        {
            try
            {
                if (form == null)
                {
                    form = B1App.Instance.Application.Forms.ActiveForm;
                }
                if (form == null)
                {
                    B1App.Instance.Application.SetStatusBarMessage("No hay formulario activo para diseñar.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                    return;
                }

                var session = InlineDesignerSession.Create(form, initialItemId);
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    var window = new InlineDesignerWindow(session);
                    window.Show();
                });
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"InlineDesigner: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }
    }
}
