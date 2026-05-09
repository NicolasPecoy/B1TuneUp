using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using System.Threading;
using B1TuneUp.Modules.DragDropUi;
using B1TuneUp.Modules.PlacementEnhancementUi;
using B1TuneUp.Modules.RichTextEditorUi;
using B1TuneUp.Modules.BarcodeScannerUi;
using B1TuneUp.Modules.AutomationDashboardUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class UIEnhancementsManager
    {
        public static void EnableDragAndDrop(SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null) form = SapUiSafe.TryGetActiveForm();
                // Enable basic drag and drop between fields by opening a small helper form that captures drag source and target
                DragDropHelperLauncher.Show();
                PlacementEnhancementLauncher.Show();
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
                if (form == null) form = SapUiSafe.TryGetActiveForm();
                string initial = "";
                try
                {
                    var it = SapUiSafe.TryGetItem(form, itemId);
                    if (it != null)
                    {
                        if (SapUiSafe.TryGetSpecific<SAPbouiCOM.EditText>(it) is SAPbouiCOM.EditText et) initial = et.Value ?? "";
                    }
                }
                catch { }

                RichTextEditorUi.RichTextEditorLauncher.Show(itemId);
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
                if (form == null) form = SapUiSafe.TryGetActiveForm();
                // Attempt to get SQL from grid's underlying DataTable if possible
                string sql = "";
                try
                {
                    var grid = SapUiSafe.TryGetSpecific<SAPbouiCOM.Grid>(form, gridId);
                    if (grid != null)
                    {
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
                if (form == null) form = SapUiSafe.TryGetActiveForm();
                BarcodeScannerUi.BarcodeScannerLauncher.Show(targetItemId);
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
                AutomationDashboardUi.AutomationDashboardLauncher.Show();
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
                if (form == null) form = SapUiSafe.TryGetActiveForm();
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
