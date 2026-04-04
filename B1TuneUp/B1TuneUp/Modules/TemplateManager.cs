using System;
using System.Collections.Generic;
using System.Globalization;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class TemplateManager
    {
        public static void CreateTemplate(Form oForm)
        {
            try
            {
                // Show dialog to get template name and description
                // InputBox not available in this environment - use a default or abort
                string templateName = "";
                if (string.IsNullOrEmpty(templateName)) return;

                string description = "";

                // Save form data as template
                SaveFormAsTemplate(oForm, templateName, description);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creando template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void LoadTemplate(Form oForm)
        {
            try
            {
                // Show list of available templates
                string templateName = ShowTemplateSelectionDialog();
                if (string.IsNullOrEmpty(templateName)) return;

                // Load template data to form
                LoadTemplateToForm(oForm, templateName);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void SaveFormAsTemplate(Form oForm, string templateName, string description)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                // First, check if template already exists
                bool isHana = B1App.Instance.IsHana;
                string safeName = templateName.Replace("'", "''");
                string checkSql = isHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_TMPL\" WHERE \"U_Name\" = '{safeName}'"
                    : $"SELECT [Code] FROM [@BTUN_TMPL] WHERE [U_Name] = '{safeName}'";

                rs.DoQuery(checkSql);
                string docEntry = "";

                if (!rs.EoF)
                {
                    docEntry = rs.Fields.Item(0).Value.ToString();
                }

                // Serialize form data to save as template
                string formData = SerializeFormData(oForm);
                string safeData = formData.Replace("'", "''");
                string safeDesc = (description ?? string.Empty).Replace("'", "''");

                if (string.IsNullOrEmpty(docEntry))
                {
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_TMPL");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string recordName = $"TMPL_{safeName}";
                    string insertSql = isHana
                        ? $"INSERT INTO \"@BTUN_TMPL\" (\"Code\",\"Name\",\"U_Name\", \"U_Desc\", \"U_FormType\", \"U_Data\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{codeValue}','{recordName}','{safeName}', '{safeDesc}', '{oForm.TypeEx}', '{safeData}', '{B1App.Instance.Company.UserName}', CURRENT_TIMESTAMP)"
                        : $"INSERT INTO [@BTUN_TMPL] ([Code],[Name],U_Name, U_Desc, U_FormType, U_Data, U_CreatedBy, U_CreatedAt) VALUES ('{codeValue}','{recordName}','{safeName}', '{safeDesc}', '{oForm.TypeEx}', '{safeData}', '{B1App.Instance.Company.UserName}', GETDATE())";

                    rs.DoQuery(insertSql);
                }
                else
                {
                    // Update existing template
                    string updateSql = isHana
                        ? $"UPDATE \"@BTUN_TMPL\" SET \"U_Desc\" = '{safeDesc}', \"U_Data\" = '{safeData}', \"U_UpdatedAt\" = CURRENT_TIMESTAMP WHERE \"Code\" = '{docEntry}'"
                        : $"UPDATE [@BTUN_TMPL] SET U_Desc = '{safeDesc}', U_Data = '{safeData}', U_UpdatedAt = GETDATE() WHERE [Code] = '{docEntry}'";

                    rs.DoQuery(updateSql);
                }

                B1App.Instance.Application.SetStatusBarMessage($"Template '{templateName}' guardado exitosamente.", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error guardando template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void LoadTemplateToForm(Form oForm, string templateName)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Data\" FROM \"@BTUN_TMPL\" WHERE \"U_Name\" = '{templateName}'"
                    : $"SELECT [U_Data] FROM [@BTUN_TMPL] WHERE [U_Name] = '{templateName}'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    string formData = rs.Fields.Item(0).Value.ToString();
                    DeserializeFormData(oForm, formData);

                    B1App.Instance.Application.SetStatusBarMessage($"Template '{templateName}' cargado exitosamente.", BoMessageTime.bmt_Short, false);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Template '{templateName}' no encontrado.", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static string SerializeFormData(Form oForm)
        {
            try
            {
                var dataDict = new Dictionary<string, string>();

                // Extract data from editable fields
                foreach (Item item in oForm.Items)
                {
                    try
                    {
                        if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_COMBO_BOX)
                        {
                            if (item.Specific is EditText editText)
                            {
                                dataDict[item.UniqueID] = editText.Value ?? "";
                            }
                            else if (item.Specific is ComboBox comboBox)
                            {
                                dataDict[item.UniqueID] = comboBox.Selected?.Value ?? "";
                            }
                        }
                        // Handle matrix fields
                        else if (item.Type == BoFormItemTypes.it_MATRIX)
                        {
                            Matrix matrix = (Matrix)item.Specific;
                            for (int i = 1; i <= matrix.RowCount; i++)
                            {
                                for (int j = 0; j < matrix.Columns.Count; j++)
                                {
                                    Column column = matrix.Columns.Item(j);
                                    if (column.Type == BoFormItemTypes.it_EDIT)
                                    {
                                        EditText cellEdit = (EditText)column.Cells.Item(i).Specific;
                                        string key = $"{item.UniqueID}.{column.UniqueID}.{i}";
                                        dataDict[key] = cellEdit.Value ?? "";
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception) { /* Skip problematic items */ }
                }

                // Convert dictionary to a serialized string format
                var parts = new List<string>();
                foreach (var kvp in dataDict)
                {
                    parts.Add($"{kvp.Key}={kvp.Value.Replace(";", "_SEMICOLON_").Replace("|", "_PIPE_")}");
                }

                return string.Join("|", parts);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error serializando datos: {ex.Message}", BoMessageTime.bmt_Short, true);
                return "";
            }
        }

        private static void DeserializeFormData(Form oForm, string formData)
        {
            try
            {
                if (string.IsNullOrEmpty(formData)) return;

                string[] parts = formData.Split('|');
                var dataDict = new Dictionary<string, string>();

                foreach (string part in parts)
                {
                    if (part.Contains("="))
                    {
                        int idx = part.IndexOf('=');
                        string key = part.Substring(0, idx);
                        string value = part.Substring(idx + 1).Replace("_SEMICOLON_", ";").Replace("_PIPE_", "|");
                        dataDict[key] = value;
                    }
                }

                // Apply data to form fields
                foreach (var kvp in dataDict)
                {
                    try
                    {
                        if (kvp.Key.Contains(".") && kvp.Key.Split('.').Length == 3)
                        {
                            // Matrix field: ItemId.ColumnId.RowIndex
                            string[] keyParts = kvp.Key.Split('.');
                            string itemId = keyParts[0];
                            string columnId = keyParts[1];
                            int rowIndex = int.Parse(keyParts[2]);

                            if (oForm.Items.Exists(itemId))
                            {
                                Item item = oForm.Items.Item(itemId);
                                if (item.Type == BoFormItemTypes.it_MATRIX)
                                {
                                    Matrix matrix = (Matrix)item.Specific;
                                    if (matrix.Columns.Exists(columnId))
                                    {
                                        Column column = matrix.Columns.Item(columnId);
                                        EditText cellEdit = (EditText)column.Cells.Item(rowIndex).Specific;
                                        cellEdit.Value = kvp.Value;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Regular field
                            if (oForm.Items.Exists(kvp.Key))
                            {
                                Item item = oForm.Items.Item(kvp.Key);
                                if (item.Specific is EditText editText)
                                {
                                    editText.Value = kvp.Value;
                                }
                                else if (item.Specific is ComboBox comboBox)
                                {
                                    try { comboBox.Select(kvp.Value, BoSearchKey.psk_ByValue); } catch { }
                                }
                            }
                        }
                    }
                    catch (Exception) { /* Skip problematic items */ }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error deserializando datos: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static string ShowTemplateSelectionDialog()
        {
            // Create a simple form to select template
            try
            {
                string formUID = "BTUN_TMPL_SEL_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_TMPLSEL";
                fcp.UniqueID = formUID;

                Form selForm = B1App.Instance.Application.Forms.AddEx(fcp);
                selForm.Title = "Seleccionar Template";
                selForm.Width = 400;
                selForm.Height = 300;

                // Add a combo box with available templates
                Item comboItem = selForm.Items.Add("cmbTemplates", BoFormItemTypes.it_COMBO_BOX);
                comboItem.Top = 10;
                comboItem.Left = 10;
                comboItem.Width = 300;

                SAPbouiCOM.ComboBox cmbTemplates = (SAPbouiCOM.ComboBox)comboItem.Specific;

                // Load available templates
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                try
                {
                    string sql = B1App.Instance.IsHana
                        ? "SELECT \"U_Name\" FROM \"@BTUN_TMPL\" ORDER BY \"U_Name\""
                        : "SELECT [U_Name] FROM [@BTUN_TMPL] ORDER BY [U_Name]";

                    rs.DoQuery(sql);

                    while (!rs.EoF)
                    {
                        string templateName = rs.Fields.Item(0).Value.ToString();
                        cmbTemplates.ValidValues.Add(templateName, templateName);
                        rs.MoveNext();
                    }
                }
                finally
                {
                    ComObjectManager.Release(rs);
                }

                // Add OK button
                Item okItem = selForm.Items.Add("btnOK", BoFormItemTypes.it_BUTTON);
                okItem.Top = 40;
                okItem.Left = 10;
                okItem.Width = 80;

                SAPbouiCOM.Button okBtn = (SAPbouiCOM.Button)okItem.Specific;
                okBtn.Caption = "OK";

                // Add Cancel button
                Item cancelItem = selForm.Items.Add("btnCancel", BoFormItemTypes.it_BUTTON);
                cancelItem.Top = 40;
                cancelItem.Left = 100;
                cancelItem.Width = 80;

                SAPbouiCOM.Button cancelBtn = (SAPbouiCOM.Button)cancelItem.Specific;
                cancelBtn.Caption = "Cancelar";

                selForm.Visible = true;

                // Wait for user interaction (simplified - in real implementation would use events)
                System.Threading.Thread.Sleep(100); // Allow form to render

                string selectedTemplate = cmbTemplates.Selected?.Value ?? "";

                selForm.Close();

                return selectedTemplate;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mostrando selección de template: {ex.Message}", BoMessageTime.bmt_Short, true);
                return "";
            }
        }

        public static void ManageTemplates()
        {
            // Create management form for templates
            try
            {
                string formUID = "BTUN_TMPL_MNG_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_TMPLMNG";
                fcp.UniqueID = formUID;

                Form mngForm = B1App.Instance.Application.Forms.AddEx(fcp);
                mngForm.Title = "Gestión de Templates";
                mngForm.Width = 600;
                mngForm.Height = 400;

                // Add grid to display templates
                Item gridItem = mngForm.Items.Add("grdTemplates", BoFormItemTypes.it_GRID);
                gridItem.Top = 10;
                gridItem.Left = 10;
                gridItem.Width = 560;
                gridItem.Height = 300;

                Grid grid = (Grid)gridItem.Specific;

                // Add buttons
                Item deleteItem = mngForm.Items.Add("btnDelete", BoFormItemTypes.it_BUTTON);
                deleteItem.Top = 320;
                deleteItem.Left = 10;
                deleteItem.Width = 80;

                SAPbouiCOM.Button deleteBtn = (SAPbouiCOM.Button)deleteItem.Specific;
                deleteBtn.Caption = "Eliminar";

                Item refreshItem = mngForm.Items.Add("btnRefresh", BoFormItemTypes.it_BUTTON);
                refreshItem.Top = 320;
                refreshItem.Left = 100;
                refreshItem.Width = 80;

                SAPbouiCOM.Button refreshBtn = (SAPbouiCOM.Button)refreshItem.Specific;
                refreshBtn.Caption = "Actualizar";

                mngForm.Visible = true;

                // Load templates into grid
                LoadTemplatesToGrid(grid);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error gestionando templates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadTemplatesToGrid(Grid grid)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_Name\", \"U_Desc\", \"U_FormType\", \"U_CreatedAt\" FROM \"@BTUN_TMPL\" ORDER BY \"U_Name\""
                    : "SELECT [U_Name], [U_Desc], [U_FormType], [U_CreatedAt] FROM [@BTUN_TMPL] ORDER BY [U_Name]";

                rs.DoQuery(sql);

                // Set up grid columns via the DataTable
                    try
                    {
                        grid.DataTable = null; // Clear existing data table
                        // Use active form's datasources (ParentForm isn't available in all SDKs)
                        grid.DataTable = B1App.Instance.Application.Forms.ActiveForm.DataSources.DataTables.Add("tmplDt");
                        grid.DataTable.ExecuteQuery(sql);

                    // Ensure expected columns exist
                    if (!grid.DataTable.Columns.IsNameExists("U_Name")) grid.DataTable.Columns.Add("U_Name", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 50);
                    if (!grid.DataTable.Columns.IsNameExists("U_Desc")) grid.DataTable.Columns.Add("U_Desc", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 100);
                    if (!grid.DataTable.Columns.IsNameExists("U_FormType")) grid.DataTable.Columns.Add("U_FormType", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 20);
                    if (!grid.DataTable.Columns.IsNameExists("U_CreatedAt")) grid.DataTable.Columns.Add("U_CreatedAt", SAPbouiCOM.BoFieldsType.ft_Date, 20);

                    // Re-execute query to populate data
                    grid.DataTable.ExecuteQuery(sql);

                    try { grid.AutoResizeColumns(); } catch { }
                }
                catch (Exception)
                {
                    // Fallback: do nothing on unsupported operations in this environment
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando templates a la grilla: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }
    }
}
