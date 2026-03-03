using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class ItemEditorManager
    {
        public static void OpenItemEditor(string itemId, SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                if (form == null) return;
                if (!form.Items.Exists(itemId))
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Item {itemId} no encontrado.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                    return;
                }

                var it = form.Items.Item(itemId);
                var dlg = new Forms.ItemEditorForm(form, it);
                dlg.ShowDialog();
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
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                var dlg = new Forms.AddItemForm(form);
                dlg.ShowDialog();
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
