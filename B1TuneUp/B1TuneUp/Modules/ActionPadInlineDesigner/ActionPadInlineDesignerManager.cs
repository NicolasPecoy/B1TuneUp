using System;
using B1TuneUp.Core;
using B1TuneUp.Models;
using SAPbouiCOM;
using WpfApplication = System.Windows.Application;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public static class ActionPadInlineDesignerManager
    {
        public static void ShowOverlay(ActionPadEntry pad)
        {
            if (pad == null)
            {
                B1App.Instance.Application.SetStatusBarMessage("Selecciona un Action Pad antes de abrir el diseñador.", BoMessageTime.bmt_Short, true);
                return;
            }

            SAPbouiCOM.Form form = null;
            try { form = B1App.Instance.Application.Forms.ActiveForm; } catch { }
            var session = ActionPadInlineDesignerSession.Create(pad, form);
            WpfApplication.Current?.Dispatcher?.Invoke(() =>
            {
                var window = new ActionPadInlineDesignerWindow(session);
                window.Show();
            });
        }
    }
}
