using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using System.Threading;

namespace B1TuneUp.Modules
{
    public static class UIEnhancementsManager
    {
        public static void EnableDragAndDrop(SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                // Enable basic drag and drop between fields by opening a small helper form that captures drag source and target
                var helper = new Forms.DragDropHelperForm(form);
                helper.Show();
                // Open Item Placement manager for the form
                ItemPlacementManager.OpenPlacementForm(form);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error habilitando drag & drop: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void OpenRichTextEditor(string itemId, SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                string initial = "";
                try
                {
                    if (form.Items.Exists(itemId))
                    {
                        var it = form.Items.Item(itemId);
                        if (it.Specific is SAPbouiCOM.EditText et) initial = et.Value ?? "";
                    }
                }
                catch { }

                var rte = new Forms.RichTextEditorForm(itemId, form, initial);
                rte.Show();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo editor enriquecido: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void EnhanceGridWithPivot(SAPbouiCOM.Form form, string gridId)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                // Attempt to get SQL from grid's underlying DataTable if possible
                string sql = "";
                try
                {
                    if (form.Items.Exists(gridId))
                    {
                        var grid = (SAPbouiCOM.Grid)form.Items.Item(gridId).Specific;
                        // If grid has an associated DataTable with a query, use it (best-effort)
                        // We'll fall back to asking user via dialog
                        sql = "";
                    }
                }
                catch { }

                var pivot = new Forms.PivotGridForm(form, gridId, sql);
                pivot.Show();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mejorando grilla: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void ScanBarcode(string targetItemId = null, SAPbouiCOM.Form form = null)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                var scanner = new Forms.BarcodeScannerForm(form, targetItemId);
                scanner.Show();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error iniciando escáner: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void ShowAdvancedDashboard(string sqlQuery = null)
        {
            try
            {
                var dash = new Forms.AdvancedDashboardForm(sqlQuery);
                dash.Show();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mostrando dashboard avanzado: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void OpenVisualDesigner(SAPbouiCOM.Form form = null)
        {
            try
            {
                if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
                var d = new Forms.DesignSurfaceForm(form);
                d.Show();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Visual Designer: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
