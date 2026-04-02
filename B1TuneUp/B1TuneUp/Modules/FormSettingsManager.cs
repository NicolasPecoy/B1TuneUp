using System;
using System.Text;
using System.Collections.Generic;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    /// <summary>
    /// Guarda y restaura la configuración visual de los formularios SAP B1 por usuario:
    /// posición, tamaño, ancho de columnas de matrices y pestaña activa.
    /// Equivalente al "Form Settings" del B1 Usability Package de Boyum IT.
    ///
    /// Los datos se almacenan en la tabla @BTUN_FSET como un string clave=valor
    /// separado por punto y coma, un registro por (FormType, UserCode).
    /// Formato del campo Data:
    ///   W=500;H=300;L=100;T=50;AP=1;CW_[matrixUID]_[colUID]=80;...
    /// </summary>
    public static class FormSettingsManager
    {
        /// <summary>
        /// Restaura los ajustes guardados al cargar el formulario (et_FORM_LOAD after).
        /// </summary>
        public static void RestoreSettings(Form oForm)
        {
            string data = LoadSettingsData(oForm.TypeEx);
            if (string.IsNullOrEmpty(data)) return;

            var settings = ParseSettings(data);

            try
            {
                // Posición y tamaño
                if (settings.TryGetValue("W", out string w) && int.TryParse(w, out int width)  && width  > 0) oForm.Width  = width;
                if (settings.TryGetValue("H", out string h) && int.TryParse(h, out int height) && height > 0) oForm.Height = height;
                if (settings.TryGetValue("L", out string l) && int.TryParse(l, out int left)   && left   > 0) oForm.Left   = left;
                if (settings.TryGetValue("T", out string t) && int.TryParse(t, out int top)    && top    > 0) oForm.Top    = top;
            }
            catch { }

            // Pestaña activa
            try
            {
                if (settings.TryGetValue("AP", out string ap) && int.TryParse(ap, out int pane) && pane >= 0)
                    oForm.PaneLevel = pane;
            }
            catch { }

            // Anchos de columnas en matrices
            try
            {
                for (int i = 0; i < oForm.Items.Count; i++)
                {
                    Item item;
                    try { item = oForm.Items.Item(i); } catch { continue; }
                    if (item.Type != BoFormItemTypes.it_MATRIX) continue;

                    Matrix matrix;
                    try { matrix = (Matrix)item.Specific; } catch { continue; }

                    for (int j = 0; j < matrix.Columns.Count; j++)
                    {
                        Column col;
                        try { col = matrix.Columns.Item(j); } catch { continue; }
                        string key = $"CW_{item.UniqueID}_{col.UniqueID}";
                        if (settings.TryGetValue(key, out string cw) && int.TryParse(cw, out int colWidth) && colWidth > 0)
                        {
                            try { col.Width = colWidth; } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Guarda los ajustes actuales del formulario (llamar en et_FORM_CLOSE before o et_FORM_RESIZE after).
        /// </summary>
        public static void SaveSettings(Form oForm)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append($"W={oForm.Width};H={oForm.Height};L={oForm.Left};T={oForm.Top}");

                try { sb.Append($";AP={oForm.PaneLevel}"); } catch { }

                // Anchos de columnas en matrices
                try
                {
                    for (int i = 0; i < oForm.Items.Count; i++)
                    {
                        Item item;
                        try { item = oForm.Items.Item(i); } catch { continue; }
                        if (item.Type != BoFormItemTypes.it_MATRIX) continue;

                        Matrix matrix;
                        try { matrix = (Matrix)item.Specific; } catch { continue; }

                        for (int j = 0; j < matrix.Columns.Count; j++)
                        {
                            Column col;
                            try { col = matrix.Columns.Item(j); } catch { continue; }
                            try { sb.Append($";CW_{item.UniqueID}_{col.UniqueID}={col.Width}"); } catch { }
                        }
                    }
                }
                catch { }

                PersistSettingsData(oForm.TypeEx, sb.ToString());
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error guardando ajustes de formulario: {ex.Message}",
                    BoMessageTime.bmt_Short, true);
            }
        }

        // ─── Persistencia ────────────────────────────────────────────────────────

        private static string LoadSettingsData(string formType)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string userCode = B1App.Instance.Company.UserName;
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Data\" FROM \"@BTUN_FSET\" WHERE \"U_FormType\" = '{EscSql(formType)}' AND \"U_UserCode\" = '{EscSql(userCode)}'"
                    : $"SELECT [U_Data] FROM [@BTUN_FSET] WHERE [U_FormType] = '{EscSql(formType)}' AND [U_UserCode] = '{EscSql(userCode)}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                    return rs.Fields.Item("U_Data").Value?.ToString() ?? string.Empty;
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return string.Empty;
        }

        private static void PersistSettingsData(string formType, string data)
        {
            string userCode = B1App.Instance.Company.UserName;

            // Buscar DocEntry existente
            int existingEntry = FindExistingEntry(formType, userCode);

            SAPbobsCOM.UserTable table = null;
            try
            {
                table = (SAPbobsCOM.UserTable)B1App.Instance.Company.UserTables.Item("BTUN_FSET");

                if (existingEntry > 0)
                {
                    table.GetByKey(existingEntry.ToString());
                    table.UserFields.Fields.Item("U_Data").Value = data;
                    table.Update();
                }
                else
                {
                    table.UserFields.Fields.Item("U_FormType").Value = formType;
                    table.UserFields.Fields.Item("U_UserCode").Value  = userCode;
                    table.UserFields.Fields.Item("U_Data").Value      = data;
                    table.Add();
                }
            }
            catch { }
            finally { ComObjectManager.Release(table); }
        }

        private static int FindExistingEntry(string formType, string userCode)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"DocEntry\" FROM \"@BTUN_FSET\" WHERE \"U_FormType\" = '{EscSql(formType)}' AND \"U_UserCode\" = '{EscSql(userCode)}'"
                    : $"SELECT [DocEntry] FROM [@BTUN_FSET] WHERE [U_FormType] = '{EscSql(formType)}' AND [U_UserCode] = '{EscSql(userCode)}'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                    return (int)rs.Fields.Item("DocEntry").Value;
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return -1;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static Dictionary<string, string> ParseSettings(string data)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in data.Split(';'))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0)
                    dict[pair.Substring(0, eq).Trim()] = pair.Substring(eq + 1).Trim();
            }
            return dict;
        }

        private static string EscSql(string value) => value?.Replace("'", "''") ?? string.Empty;
    }
}
