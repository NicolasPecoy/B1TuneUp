using System;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using B1TuneUp.Modules.ItemEditorUi;

namespace B1TuneUp.Modules
{
    public static class ItemEditorManager
    {
        public static void OpenItemEditor(string itemId, SAPbouiCOM.Form form)
        {
            try
            {
                var targetForm = form ?? B1App.Instance.Application.Forms.ActiveForm;
                if (targetForm == null)
                {
                    B1App.Instance.Application.SetStatusBarMessage("No hay un formulario activo.", BoMessageTime.bmt_Short, true);
                    return;
                }
                if (!targetForm.Items.Exists(itemId))
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Item {itemId} no encontrado.", BoMessageTime.bmt_Short, true);
                    return;
                }

                ItemEditorLauncher.ShowItemEditor(targetForm.UniqueID, itemId);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo editor de item: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        public static void OpenAddItemForm(SAPbouiCOM.Form form)
        {
            try
            {
                var targetForm = form ?? B1App.Instance.Application.Forms.ActiveForm;
                if (targetForm == null)
                {
                    B1App.Instance.Application.SetStatusBarMessage("No hay un formulario activo en SAP Business One.", BoMessageTime.bmt_Short, true);
                    return;
                }

                ItemEditorLauncher.ShowAddItem(targetForm.UniqueID);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo AddItem: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        public static void DeleteItem(string itemId, SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                if (form == null) return;
                if (!form.Items.Exists(itemId)) return;
                try
                {
                    // Some SDK environments may not allow Remove; we attempt to hide the item as a safe operation
                    var it = form.Items.Item(itemId);
                    it.Enabled = false;
                    it.Visible = false;
                    B1App.Instance.Application.SetStatusBarMessage($"Item {itemId} ocultado (disabled).", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                }
                catch
                {
                    B1App.Instance.Application.SetStatusBarMessage($"No se pudo eliminar el item {itemId}.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error eliminando item: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }
    }
}
