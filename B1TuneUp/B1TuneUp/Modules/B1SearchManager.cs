using System;
using System.Collections.Generic;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class B1SearchManager
    {
        private static List<SearchConfig> _configs = new List<SearchConfig>();

        public static void OpenSearchForm()
        {
            try
            {
                string uid = "BTUN_SEARCH_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_SEARCH";
                fcp.UniqueID = uid;

                Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp Search";
                oForm.Width = 400;
                oForm.Height = 350;

                Item item = oForm.Items.Add("txtSrch", BoFormItemTypes.it_EDIT);
                item.Top = 10; item.Left = 10; item.Width = 300;

                item = oForm.Items.Add("btnSrch", BoFormItemTypes.it_BUTTON);
                item.Top = 10; item.Left = 315; item.Width = 60;
                SapUiSafe.TrySetCaption(item, "Buscar");

                item = oForm.Items.Add("grdRes", BoFormItemTypes.it_GRID);
                item.Top = 40; item.Left = 10; item.Width = 365; item.Height = 250;

                // Agregar columna para acciones
                item = oForm.Items.Add("btnOpen", BoFormItemTypes.it_BUTTON);
                item.Top = 300; item.Left = 10; item.Width = 100;
                SapUiSafe.TrySetCaption(item, "Abrir Seleccionado");

                oForm.Visible = true;
            }
            catch { }
        }

        public static void ExecuteSearch(Form oForm)
        {
            if (oForm == null) return;
            string searchText = SapUiSafe.TryGetSpecific<EditText>(oForm, "txtSrch")?.Value ?? string.Empty;
            Grid grid = SapUiSafe.TryGetSpecific<Grid>(oForm, "grdRes");
            if (grid == null) return;

            try
            {
                var results = AdvancedSearchService.Search(searchText, 0, 50);
                SAPbouiCOM.DataTable dt;
                try { dt = oForm.DataSources.DataTables.Item("dtRes"); dt.Rows.Clear(); }
                catch { dt = oForm.DataSources.DataTables.Add("dtRes"); }

                EnsureColumn(dt, "Rank", BoFieldsType.ft_Integer, 10);
                EnsureColumn(dt, "Search", BoFieldsType.ft_AlphaNumeric, 80);
                EnsureColumn(dt, "Key", BoFieldsType.ft_AlphaNumeric, 80);
                EnsureColumn(dt, "Title", BoFieldsType.ft_AlphaNumeric, 254);
                EnsureColumn(dt, "Subtitle", BoFieldsType.ft_AlphaNumeric, 254);
                EnsureColumn(dt, "Action", BoFieldsType.ft_Text, 0);
                EnsureColumn(dt, "DataJson", BoFieldsType.ft_Text, 0);

                for (int i = 0; i < results.Count; i++)
                {
                    dt.Rows.Add();
                    dt.SetValue("Rank", i, results[i].Rank);
                    dt.SetValue("Search", i, results[i].SearchCode ?? string.Empty);
                    dt.SetValue("Key", i, results[i].Key ?? string.Empty);
                    dt.SetValue("Title", i, results[i].Title ?? string.Empty);
                    dt.SetValue("Subtitle", i, results[i].Subtitle ?? string.Empty);
                    dt.SetValue("Action", i, results[i].Action ?? string.Empty);
                    dt.SetValue("DataJson", i, results[i].DataJson ?? string.Empty);
                }
                grid.DataTable = dt;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error buscando: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, "B1SearchManager.ExecuteSearch");
            }
        }
        public static void OpenSelectedRecord(Form oForm)
        {
            try
            {
                Grid grid = SapUiSafe.TryGetSpecific<Grid>(oForm, "grdRes");
                if (grid == null) return;
                if (grid.Rows.SelectedRows.Count <= 0) return;

                int rowIndex = grid.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_RowOrder);
                var result = new AdvancedSearchResult
                {
                    SearchCode = grid.DataTable.GetValue("Search", rowIndex)?.ToString(),
                    Key = grid.DataTable.GetValue("Key", rowIndex)?.ToString(),
                    Title = grid.DataTable.GetValue("Title", rowIndex)?.ToString(),
                    Action = grid.DataTable.GetValue("Action", rowIndex)?.ToString(),
                    DataJson = grid.DataTable.GetValue("DataJson", rowIndex)?.ToString()
                };
                AdvancedSearchService.ExecuteAction(result, oForm);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo registro: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, "B1SearchManager.OpenSelectedRecord");
            }
        }

        private static void EnsureColumn(SAPbouiCOM.DataTable dt, string name, BoFieldsType type, int size)
        {
            try { var _ = dt.Columns.Item(name); }
            catch { dt.Columns.Add(name, type, size); }
        }
        private static string ProcessActionForRow(string action, Grid grid, int rowIndex)
        {
            // Reemplaza placeholders como $[ColumnName] con los valores de la fila
            string processedAction = action;

            for (int colIndex = 0; colIndex < grid.Columns.Count; colIndex++)
            {
                string columnName = grid.Columns.Item(colIndex).UniqueID;
                string cellValue = grid.DataTable.GetValue(colIndex, rowIndex)?.ToString() ?? string.Empty;
                processedAction = processedAction.Replace($"$[{columnName}]", cellValue);
            }

            return processedAction;
        }

        private class SearchConfig
        {
            public string Name { get; set; }
            public string Query { get; set; }
            public string Action { get; set; }
        }
    }
}
