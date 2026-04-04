using System;
using System.Linq;
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
            SAPbouiCOM.Form form = null;
            try { form = B1App.Instance.Application.Forms.ActiveForm; } catch { }
            ShowOverlay(pad, form);
        }

        public static void ShowOverlay(ActionPadEntry pad, SAPbouiCOM.Form form)
        {
            if (pad == null)
            {
                B1App.Instance.Application.SetStatusBarMessage("Selecciona un Action Pad antes de abrir el diseñador.", BoMessageTime.bmt_Short, true);
                return;
            }

            var session = ActionPadInlineDesignerSession.Create(pad, form);
            WpfApplication.Current?.Dispatcher?.Invoke(() =>
            {
                var window = new ActionPadInlineDesignerWindow(session);
                window.Show();
            });
        }

        public static void ShowOverlayForActiveForm()
        {
            SAPbouiCOM.Form form = null;
            try { form = B1App.Instance.Application.Forms.ActiveForm; }
            catch { }
            ShowOverlayForForm(form);
        }

        public static void ShowOverlayForForm(SAPbouiCOM.Form form)
        {
            if (form == null)
            {
                B1App.Instance.Application.SetStatusBarMessage("No se encontró formulario activo para Action Pad.", BoMessageTime.bmt_Short, true);
                return;
            }

            var pads = ActionPadService.GetAll().Where(p => string.Equals(p.FormType, form.TypeEx, StringComparison.OrdinalIgnoreCase)).ToList();
            var pad = pads.FirstOrDefault();
            if (pad == null)
            {
                pad = new ActionPadEntry
                {
                    FormType = form.TypeEx,
                    Title = $"Action Pad {form.TypeEx}",
                    Position = "Right"
                };
                pad.Buttons.Add(new ActionPadButtonEntry
                {
                    Label = "Acción",
                    Action = "Msg('Nuevo botón');",
                    Order = 10,
                    Width = 140,
                    Height = 26
                });
            }

            ShowOverlay(pad, form);
        }
    }
}
