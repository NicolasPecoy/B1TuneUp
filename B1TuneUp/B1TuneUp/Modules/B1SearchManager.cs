using System;
using System.Collections.Generic;
using SAPbouiCOM;
using B1TuneUp.Core;
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

            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string configSql = B1App.Instance.IsHana
                    ? "SELECT * FROM \"@BTUN_SEARCH\""
                    : "SELECT * FROM [@BTUN_SEARCH]";

                rs.DoQuery(configSql);
                string combinedSql = "";
                while (!rs.EoF)
                {
                    string sql = SapUiSafe.SafeField(rs, "U_Query");
                    sql = sql.Replace("%search%", searchText.Replace("'", "''"));

                    if (combinedSql != "") combinedSql += " UNION ALL ";
                    combinedSql += sql;

                    rs.MoveNext();
                }

                if (!string.IsNullOrEmpty(combinedSql))
                {
                    oForm.DataSources.DataTables.Add("dtRes");
                    oForm.DataSources.DataTables.Item("dtRes").ExecuteQuery(combinedSql);
                    grid.DataTable = oForm.DataSources.DataTables.Item("dtRes");
                }
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void OpenSelectedRecord(Form oForm)
        {
            try
            {
                Grid grid = SapUiSafe.TryGetSpecific<Grid>(oForm, "grdRes");
                if (grid == null) return;
                if (grid.Rows.SelectedRows.Count > 0)
                {
                    int rowIndex = grid.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_RowOrder);

                    // Buscar la configuración de búsqueda correspondiente
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    try
                    {
                        string sql = B1App.Instance.IsHana
                            ? "SELECT * FROM \"@BTUN_SEARCH\""
                            : "SELECT * FROM [@BTUN_SEARCH]";

                        rs.DoQuery(sql);

                        // Aquí se podría ejecutar la acción asociada a la búsqueda
                        // Para ahora simplemente mostramos un mensaje
                        if (!rs.EoF)
                        {
                            string action = SapUiSafe.SafeField(rs, "U_Action");

                            // Procesar la acción reemplazando variables de la fila seleccionada
                            string processedAction = ProcessActionForRow(action, grid, rowIndex);
                            MacroEngine.ExecuteMacro(processedAction, oForm);
                        }
                    }
                    finally
                    {
                        ComObjectManager.Release(rs);
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo registro: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
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
