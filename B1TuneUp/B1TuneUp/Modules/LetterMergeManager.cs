using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using SAPbouiCOM;
using Form = SAPbouiCOM.Form;
using ComboBox = SAPbouiCOM.ComboBox;
using Grid = SAPbouiCOM.Grid;
using ItemEvent = SAPbouiCOM.ItemEvent;
using EditText = SAPbouiCOM.EditText;
using StaticText = SAPbouiCOM.StaticText;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class LetterMergeManager
    {
        public static void OpenLetterMergeForm()
        {
            try
            {
                string formUID = "BTUN_LETTER_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_LETTER";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp - Letter Merge";
                oForm.Width = 900;
                oForm.Height = 650;

                // Create form items
                CreateLetterMergeFormItems(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening Letter Merge form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateLetterMergeFormItems(SAPbouiCOM.Form oForm)
        {
            // Create a matrix to show existing letter merge templates
            SAPbouiCOM.Item matrixItem = oForm.Items.Add("LetterMatrix", SAPbouiCOM.BoFormItemTypes.it_GRID);
            matrixItem.Top = 10;
            matrixItem.Left = 10;
            matrixItem.Width = 870;
            matrixItem.Height = 300;

            SAPbouiCOM.Grid matrix = (SAPbouiCOM.Grid)matrixItem.Specific;

            // Ensure datatable columns exist for the grid
            matrix.DataTable.Columns.Add("TemplateName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Description", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("DocType", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("FilePath", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);

            // Set column titles
            ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("TemplateName")).TitleObject.Caption = "Template Name";
            ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Description")).TitleObject.Caption = "Description";
            ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("DocType")).TitleObject.Caption = "Document Type";
            ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("FilePath")).TitleObject.Caption = "Template File Path";

            // Create buttons
            Item addButton = oForm.Items.Add("BtnAdd", BoFormItemTypes.it_BUTTON);
            addButton.Top = 320;
            addButton.Left = 10;
            addButton.Width = 80;
            addButton.Height = 25;
            ((SAPbouiCOM.Button)addButton.Specific).Caption = "Add";
            // attach click via event subscription using Application.ItemEvent in runtime; placeholder no-op here

            Item editButton = oForm.Items.Add("BtnEdit", BoFormItemTypes.it_BUTTON);
            editButton.Top = 320;
            editButton.Left = 100;
            editButton.Width = 80;
            editButton.Height = 25;
            ((SAPbouiCOM.Button)editButton.Specific).Caption = "Edit";

            Item deleteButton = oForm.Items.Add("BtnDelete", BoFormItemTypes.it_BUTTON);
            deleteButton.Top = 320;
            deleteButton.Left = 190;
            deleteButton.Width = 80;
            deleteButton.Height = 25;
            ((SAPbouiCOM.Button)deleteButton.Specific).Caption = "Delete";

            Item executeButton = oForm.Items.Add("BtnExecute", BoFormItemTypes.it_BUTTON);
            executeButton.Top = 320;
            executeButton.Left = 280;
            executeButton.Width = 100;
            executeButton.Height = 25;
            ((SAPbouiCOM.Button)executeButton.Specific).Caption = "Execute";

            Item previewButton = oForm.Items.Add("BtnPreview", BoFormItemTypes.it_BUTTON);
            previewButton.Top = 320;
            previewButton.Left = 390;
            previewButton.Width = 100;
            previewButton.Height = 25;
            ((SAPbouiCOM.Button)previewButton.Specific).Caption = "Preview";

            Item closeButton = oForm.Items.Add("BtnClose", BoFormItemTypes.it_BUTTON);
            closeButton.Top = 320;
            closeButton.Left = 780;
            closeButton.Width = 80;
            closeButton.Height = 25;
            ((SAPbouiCOM.Button)closeButton.Specific).Caption = "Close";

            // Add labels and inputs for quick merge
            Item quickMergeLabel = oForm.Items.Add("LblQM", BoFormItemTypes.it_STATIC);
            quickMergeLabel.Top = 360;
            quickMergeLabel.Left = 10;
            quickMergeLabel.Width = 200;
            quickMergeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)quickMergeLabel.Specific).Caption = "Quick Merge (Current Document):";

            Item docTypeLabel = oForm.Items.Add("LblQMDocType", BoFormItemTypes.it_STATIC);
            docTypeLabel.Top = 390;
            docTypeLabel.Left = 10;
            docTypeLabel.Width = 100;
            docTypeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)docTypeLabel.Specific).Caption = "Document Type:";

            Item docTypeCombo = oForm.Items.Add("CmbQMDocType", BoFormItemTypes.it_COMBO_BOX);
            docTypeCombo.Top = 390;
            docTypeCombo.Left = 120;
            docTypeCombo.Width = 150;
            docTypeCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbQMDocType = (SAPbouiCOM.ComboBox)docTypeCombo.Specific;
            cmbQMDocType.ValidValues.Add("17", "Sales Invoice");
            cmbQMDocType.ValidValues.Add("13", "Sales Order");
            cmbQMDocType.ValidValues.Add("14", "Delivery");
            cmbQMDocType.ValidValues.Add("16", "AR Credit Memo");
            cmbQMDocType.ValidValues.Add("203", "Purchase Invoice");
            cmbQMDocType.ValidValues.Add("1470000113", "Purchase Order");
            cmbQMDocType.ValidValues.Add("150", "Incoming Payment");
            cmbQMDocType.ValidValues.Add("1470000167", "Customer");
            cmbQMDocType.ValidValues.Add("1470000168", "Vendor");
            cmbQMDocType.Select(0); // Default to Sales Invoice

            Item templateLabel = oForm.Items.Add("LblQMTemp", BoFormItemTypes.it_STATIC);
            templateLabel.Top = 390;
            templateLabel.Left = 290;
            templateLabel.Width = 100;
            templateLabel.Height = 20;
            ((SAPbouiCOM.StaticText)templateLabel.Specific).Caption = "Template:";

            Item templateCombo = oForm.Items.Add("CmbQMTemp", BoFormItemTypes.it_COMBO_BOX);
            templateCombo.Top = 390;
            templateCombo.Left = 380;
            templateCombo.Width = 200;
            templateCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbQMTemp = (SAPbouiCOM.ComboBox)templateCombo.Specific;
            LoadLetterTemplatesForCombo(cmbQMTemp, cmbQMDocType.Selected.Value);

            // SDK differences: avoid using PressToSelect/ChooseFromListAfterClick/DataBrowser/Event in compile-time bindings

            Item quickMergeButton = oForm.Items.Add("BtnQuickMerge", BoFormItemTypes.it_BUTTON);
            quickMergeButton.Top = 390;
            quickMergeButton.Left = 600;
            quickMergeButton.Width = 100;
            quickMergeButton.Height = 25;
            ((SAPbouiCOM.Button)quickMergeButton.Specific).Caption = "Quick Merge";

            // Load existing letter merge templates
            LoadLetterMergeTemplates(matrix);
        }

        private static void LoadLetterMergeTemplates(SAPbouiCOM.Grid matrix)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_Name\", \"U_Desc\", \"U_DocType\", \"U_FilePath\" FROM \"@BTUN_LTRM\" ORDER BY \"U_Name\"" :
                    "SELECT U_Name, U_Desc, U_DocType, U_FilePath FROM [@BTUN_LTRM] ORDER BY U_Name";

                rs.DoQuery(sql);

                matrix.DataTable.Rows.Clear();

                while (!rs.EoF)
                {
                    matrix.DataTable.Rows.Add();
                    int rowIndex = matrix.DataTable.Rows.Count - 1;

                    matrix.DataTable.SetValue("TemplateName", rowIndex, rs.Fields.Item("U_Name").Value);
                    matrix.DataTable.SetValue("Description", rowIndex, rs.Fields.Item("U_Desc").Value);
                    matrix.DataTable.SetValue("DocType", rowIndex, rs.Fields.Item("U_DocType").Value);
                    matrix.DataTable.SetValue("FilePath", rowIndex, rs.Fields.Item("U_FilePath").Value);

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading letter merge templates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadLetterTemplatesForCombo(SAPbouiCOM.ComboBox combo, string docType)
        {
            try
            {
                // Clear existing values
                try { for (int i = combo.ValidValues.Count - 1; i >= 0; i--) combo.ValidValues.Remove(i); } catch { }

                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    $"SELECT \"U_Name\" FROM \"@BTUN_LTRM\" WHERE \"U_DocType\" = '{docType}' ORDER BY \"U_Name\"" :
                    $"SELECT U_Name FROM [@BTUN_LTRM] WHERE U_DocType = '{docType}' ORDER BY U_Name";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string templateName = rs.Fields.Item("U_Name").Value.ToString();
                    try { combo.ValidValues.Add(templateName, templateName); } catch { }
                    rs.MoveNext();
                }

                if (combo.ValidValues.Count > 0)
                {
                    combo.Select(0);
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading letter templates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewLetterMergeTemplate(SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formUID = "BTUN_LETTER_NEW_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_LTRNEW";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "Create New Letter Merge Template";
                oForm.Width = 700;
                oForm.Height = 550;

                CreateNewLetterMergeFormItems(oForm, parentForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creating new letter merge form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewLetterMergeFormItems(SAPbouiCOM.Form oForm, SAPbouiCOM.Form parentForm)
        {
            // Labels
            SAPbouiCOM.Item nameLabel = oForm.Items.Add("LblName", SAPbouiCOM.BoFormItemTypes.it_STATIC);
            nameLabel.Top = 20;
            nameLabel.Left = 20;
            nameLabel.Width = 100;
            nameLabel.Height = 20;
            ((SAPbouiCOM.StaticText)nameLabel.Specific).Caption = "Template Name:";

            Item descLabel = oForm.Items.Add("LblDesc", BoFormItemTypes.it_STATIC);
            descLabel.Top = 50;
            descLabel.Left = 20;
            descLabel.Width = 100;
            descLabel.Height = 20;
            ((SAPbouiCOM.StaticText)descLabel.Specific).Caption = "Description:";

            Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
            docTypeLabel.Top = 80;
            docTypeLabel.Left = 20;
            docTypeLabel.Width = 100;
            docTypeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)docTypeLabel.Specific).Caption = "Document Type:";

            Item filePathLabel = oForm.Items.Add("LblFilePath", BoFormItemTypes.it_STATIC);
            filePathLabel.Top = 110;
            filePathLabel.Left = 20;
            filePathLabel.Width = 100;
            filePathLabel.Height = 20;
            ((SAPbouiCOM.StaticText)filePathLabel.Specific).Caption = "Template File Path:";

            Item browseLabel = oForm.Items.Add("LblBrowse", BoFormItemTypes.it_STATIC);
            browseLabel.Top = 110;
            browseLabel.Left = 350;
            browseLabel.Width = 200;
            browseLabel.Height = 20;
            ((SAPbouiCOM.StaticText)browseLabel.Specific).Caption = "(Use Browse button to select .docx template)";

            // Input fields
            Item nameEdit = oForm.Items.Add("EdtName", BoFormItemTypes.it_EDIT);
            nameEdit.Top = 20;
            nameEdit.Left = 130;
            nameEdit.Width = 200;
            nameEdit.Height = 20;

            Item descEdit = oForm.Items.Add("EdtDesc", BoFormItemTypes.it_EDIT);
            descEdit.Top = 50;
            descEdit.Left = 130;
            descEdit.Width = 350;
            descEdit.Height = 20;

            Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
            docTypeCombo.Top = 80;
            docTypeCombo.Left = 130;
            docTypeCombo.Width = 200;
            docTypeCombo.Height = 20;
            ComboBox cmbDocType = (ComboBox)docTypeCombo.Specific;
            cmbDocType.ValidValues.Add("17", "Sales Invoice");
            cmbDocType.ValidValues.Add("13", "Sales Order");
            cmbDocType.ValidValues.Add("14", "Delivery");
            cmbDocType.ValidValues.Add("16", "AR Credit Memo");
            cmbDocType.ValidValues.Add("203", "Purchase Invoice");
            cmbDocType.ValidValues.Add("1470000113", "Purchase Order");
            cmbDocType.ValidValues.Add("150", "Incoming Payment");
            cmbDocType.ValidValues.Add("1470000167", "Customer");
            cmbDocType.ValidValues.Add("1470000168", "Vendor");
            cmbDocType.Select(0); // Default to Sales Invoice

            Item filePathEdit = oForm.Items.Add("EdtFilePath", BoFormItemTypes.it_EDIT);
            filePathEdit.Top = 110;
            filePathEdit.Left = 130;
            filePathEdit.Width = 200;
            filePathEdit.Height = 20;

            Item browseButton = oForm.Items.Add("BtnBrowse", BoFormItemTypes.it_BUTTON);
            browseButton.Top = 108;
            browseButton.Left = 340;
            browseButton.Width = 80;
            browseButton.Height = 22;
            ((SAPbouiCOM.Button)browseButton.Specific).Caption = "Browse...";
            // Click handler attached at runtime via Application.ItemEvent

            // Field mapping section
            Item fieldMapLabel = oForm.Items.Add("LblFieldMap", BoFormItemTypes.it_STATIC);
            fieldMapLabel.Top = 150;
            fieldMapLabel.Left = 20;
            fieldMapLabel.Width = 200;
            fieldMapLabel.Height = 20;
            ((SAPbouiCOM.StaticText)fieldMapLabel.Specific).Caption = "Field Mapping (SAP Field -> Template Field):";

            // Create a matrix for field mappings
            Item matrixItem = oForm.Items.Add("FldMapMatrix", BoFormItemTypes.it_GRID);
            matrixItem.Top = 180;
            matrixItem.Left = 20;
            matrixItem.Width = 650;
            matrixItem.Height = 200;

            Grid matrix = (Grid)matrixItem.Specific;

            // Ensure datatable columns exist for the grid
            matrix.DataTable.Columns.Add("SAPField", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("TmplField", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Description", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);

            try
            {
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("SAPField")).TitleObject.Caption = "SAP Field";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("TmplField")).TitleObject.Caption = "Template Field";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Description")).TitleObject.Caption = "Description";
            }
            catch { }

            // Add some sample rows
            matrix.DataTable.Rows.Add();
            matrix.DataTable.SetValue("SAPField", 0, "CardCode");
            matrix.DataTable.SetValue("TmplField", 0, "CustomerCode");
            matrix.DataTable.SetValue("Description", 0, "Customer Code");

            matrix.DataTable.Rows.Add();
            matrix.DataTable.SetValue("SAPField", 1, "CardName");
            matrix.DataTable.SetValue("TmplField", 1, "CustomerName");
            matrix.DataTable.SetValue("Description", 1, "Customer Name");

            matrix.DataTable.Rows.Add();
            matrix.DataTable.SetValue("SAPField", 2, "DocTotal");
            matrix.DataTable.SetValue("TmplField", 2, "TotalAmount");
            matrix.DataTable.SetValue("Description", 2, "Document Total Amount");

            // Buttons
            Item saveButton = oForm.Items.Add("BtnSave", BoFormItemTypes.it_BUTTON);
            saveButton.Top = 390;
            saveButton.Left = 20;
            saveButton.Width = 80;
            saveButton.Height = 25;
            ((SAPbouiCOM.Button)saveButton.Specific).Caption = "Save";

            Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
            testButton.Top = 390;
            testButton.Left = 110;
            testButton.Width = 80;
            testButton.Height = 25;
            ((SAPbouiCOM.Button)testButton.Specific).Caption = "Test";

            Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
            cancelButton.Top = 390;
            cancelButton.Left = 200;
            cancelButton.Width = 80;
            cancelButton.Height = 25;
            ((SAPbouiCOM.Button)cancelButton.Specific).Caption = "Cancel";
        }

        private static string SelectWordTemplateFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Word Template File";

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error selecting template file: {ex.Message}", BoMessageTime.bmt_Short, true);
            }

            return "";
        }

        private static void SaveLetterMergeTemplate(SAPbouiCOM.Form oForm, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string name = ((SAPbouiCOM.EditText)oForm.Items.Item("EdtName").Specific).Value;
                string desc = ((SAPbouiCOM.EditText)oForm.Items.Item("EdtDesc").Specific).Value;
                string docType = ((SAPbouiCOM.ComboBox)oForm.Items.Item("CmbDocType").Specific).Selected.Value;
                string filePath = ((SAPbouiCOM.EditText)oForm.Items.Item("EdtFilePath").Specific).Value;

                // Validate required fields
                if (string.IsNullOrEmpty(name))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template name is required", BoMessageTime.bmt_Short, true);
                    return;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template file path is required", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Verify the file exists
                if (!File.Exists(filePath))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template file does not exist", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Save to user table
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string insertSql = B1App.Instance.IsHana ?
                    $"INSERT INTO \"@BTUN_LTRM\" (\"U_Name\", \"U_Desc\", \"U_DocType\", \"U_FilePath\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{name}', '{desc}', '{docType}', '{filePath}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')" :
                    $"INSERT INTO [@BTUN_LTRM] (U_Name, U_Desc, U_DocType, U_FilePath, U_CreatedBy, U_CreatedAt) VALUES ('{name}', '{desc}', '{docType}', '{filePath}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')";

                rs.DoQuery(insertSql);
                B1App.Instance.Application.SetStatusBarMessage("Letter merge template saved successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the parent form
                SAPbouiCOM.Grid matrix = (SAPbouiCOM.Grid)parentForm.Items.Item("LetterMatrix").Specific;
                LoadLetterMergeTemplates(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving letter merge template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void TestLetterMergeTemplate(SAPbouiCOM.Form oForm)
        {
            try
            {
                string filePath = ((EditText)oForm.Items.Item("EdtFilePath").Specific).Value;

                if (string.IsNullOrEmpty(filePath))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template file first", BoMessageTime.bmt_Short, true);
                    return;
                }

                if (!File.Exists(filePath))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template file does not exist", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Simulate testing the template
                B1App.Instance.Application.SetStatusBarMessage($"Testing template: {filePath}", BoMessageTime.bmt_Short, false);

                // In a real implementation, this would attempt to open the Word template
                // to verify it's a valid format and can be processed
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error testing template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EditSelectedLetterMergeTemplate(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("LetterMatrix").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    // Open edit form with the selected template data
                    OpenEditLetterMergeForm(templateName, parentForm);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template to edit", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error editing letter merge template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void OpenEditLetterMergeForm(string templateName, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formUID = "BTUN_LETTER_EDIT_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_LTREDT";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = $"Edit Letter Merge Template: {templateName}";
                oForm.Width = 700;
                oForm.Height = 550;

                CreateEditLetterMergeFormItems(oForm, templateName, parentForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening edit letter merge form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateEditLetterMergeFormItems(SAPbouiCOM.Form oForm, string templateName, SAPbouiCOM.Form parentForm)
        {
            // First load the template data
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            string sql = B1App.Instance.IsHana ?
                $"SELECT * FROM \"@BTUN_LTRM\" WHERE \"U_Name\" = '{templateName}'" :
                $"SELECT * FROM [@BTUN_LTRM] WHERE U_Name = '{templateName}'";

            rs.DoQuery(sql);

            if (!rs.EoF)
            {
                // Labels
                Item nameLabel = oForm.Items.Add("LblName", BoFormItemTypes.it_STATIC);
                nameLabel.Top = 20;
                nameLabel.Left = 20;
                nameLabel.Width = 100;
                nameLabel.Height = 20;
                ((SAPbouiCOM.StaticText)nameLabel.Specific).Caption = "Template Name:";

                Item descLabel = oForm.Items.Add("LblDesc", BoFormItemTypes.it_STATIC);
                descLabel.Top = 50;
                descLabel.Left = 20;
                descLabel.Width = 100;
                descLabel.Height = 20;
                ((SAPbouiCOM.StaticText)descLabel.Specific).Caption = "Description:";

                Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
                docTypeLabel.Top = 80;
                docTypeLabel.Left = 20;
                docTypeLabel.Width = 100;
                docTypeLabel.Height = 20;
                ((SAPbouiCOM.StaticText)docTypeLabel.Specific).Caption = "Document Type:";

                Item filePathLabel = oForm.Items.Add("LblFilePath", BoFormItemTypes.it_STATIC);
                filePathLabel.Top = 110;
                filePathLabel.Left = 20;
                filePathLabel.Width = 100;
                filePathLabel.Height = 20;
                ((SAPbouiCOM.StaticText)filePathLabel.Specific).Caption = "Template File Path:";

                // Input fields
                Item nameEdit = oForm.Items.Add("EdtName", BoFormItemTypes.it_EDIT);
                nameEdit.Top = 20;
                nameEdit.Left = 130;
                nameEdit.Width = 200;
                nameEdit.Height = 20;
                nameEdit.Enabled = false; // Can't change template name
                ((SAPbouiCOM.EditText)nameEdit.Specific).Value = rs.Fields.Item("U_Name").Value.ToString();

                Item descEdit = oForm.Items.Add("EdtDesc", BoFormItemTypes.it_EDIT);
                descEdit.Top = 50;
                descEdit.Left = 130;
                descEdit.Width = 350;
                descEdit.Height = 20;
                ((SAPbouiCOM.EditText)descEdit.Specific).Value = rs.Fields.Item("U_Desc").Value.ToString();

                Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
                docTypeCombo.Top = 80;
                docTypeCombo.Left = 130;
                docTypeCombo.Width = 200;
                docTypeCombo.Height = 20;
                ComboBox cmbDocType = (ComboBox)docTypeCombo.Specific;
                cmbDocType.ValidValues.Add("17", "Sales Invoice");
                cmbDocType.ValidValues.Add("13", "Sales Order");
                cmbDocType.ValidValues.Add("14", "Delivery");
                cmbDocType.ValidValues.Add("16", "AR Credit Memo");
                cmbDocType.ValidValues.Add("203", "Purchase Invoice");
                cmbDocType.ValidValues.Add("1470000113", "Purchase Order");
                cmbDocType.ValidValues.Add("150", "Incoming Payment");
                cmbDocType.ValidValues.Add("1470000167", "Customer");
                cmbDocType.ValidValues.Add("1470000168", "Vendor");

                string docType = rs.Fields.Item("U_DocType").Value.ToString();
                for (int i = 0; i < cmbDocType.ValidValues.Count; i++)
                {
                    if (cmbDocType.ValidValues.Item(i).Value == docType)
                    {
                        cmbDocType.Select(i);
                        break;
                    }
                }

                Item filePathEdit = oForm.Items.Add("EdtFilePath", BoFormItemTypes.it_EDIT);
                filePathEdit.Top = 110;
                filePathEdit.Left = 130;
                filePathEdit.Width = 200;
                filePathEdit.Height = 20;
                ((SAPbouiCOM.EditText)filePathEdit.Specific).Value = rs.Fields.Item("U_FilePath").Value.ToString();

                Item browseButton = oForm.Items.Add("BtnBrowse", BoFormItemTypes.it_BUTTON);
                browseButton.Top = 108;
                browseButton.Left = 340;
                browseButton.Width = 80;
                browseButton.Height = 22;
                ((SAPbouiCOM.Button)browseButton.Specific).Caption = "Browse...";
                // Click handler attached at runtime via Application.ItemEvent

                // Field mapping section
            Item fieldMapLabel = oForm.Items.Add("LblFieldMap", BoFormItemTypes.it_STATIC);
            fieldMapLabel.Top = 150;
            fieldMapLabel.Left = 20;
            fieldMapLabel.Width = 200;
            fieldMapLabel.Height = 20;
            ((SAPbouiCOM.StaticText)fieldMapLabel.Specific).Caption = "Field Mapping (SAP Field -> Template Field):";

                // Create a matrix for field mappings
                Item matrixItem = oForm.Items.Add("FldMapMatrix", BoFormItemTypes.it_GRID);
                matrixItem.Top = 180;
                matrixItem.Left = 20;
                matrixItem.Width = 650;
                matrixItem.Height = 200;

                Grid matrix = (Grid)matrixItem.Specific;

                // Add columns to the matrix
            // Ensure datatable columns exist for the grid
            matrix.DataTable.Columns.Add("SAPField", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("TmplField", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Description", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            try
            {
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("SAPField")).TitleObject.Caption = "SAP Field";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("TmplField")).TitleObject.Caption = "Template Field";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Description")).TitleObject.Caption = "Description";
            }
            catch { }

                // Buttons
                Item updateButton = oForm.Items.Add("BtnUpdate", BoFormItemTypes.it_BUTTON);
                updateButton.Top = 390;
                updateButton.Left = 20;
                updateButton.Width = 80;
                updateButton.Height = 25;
                ((SAPbouiCOM.Button)updateButton.Specific).Caption = "Update";

                Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
                testButton.Top = 390;
                testButton.Left = 110;
                testButton.Width = 80;
                testButton.Height = 25;
                ((SAPbouiCOM.Button)testButton.Specific).Caption = "Test";

                Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
                cancelButton.Top = 390;
                cancelButton.Left = 200;
                cancelButton.Width = 80;
                cancelButton.Height = 25;
                ((SAPbouiCOM.Button)cancelButton.Specific).Caption = "Cancel";
            }

            ComObjectManager.Release(rs);
        }

        private static void UpdateLetterMergeTemplate(SAPbouiCOM.Form oForm, string templateName, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string desc = ((EditText)oForm.Items.Item("EdtDesc").Specific).Value;
                string docType = ((ComboBox)oForm.Items.Item("CmbDocType").Specific).Selected.Value;
                string filePath = ((EditText)oForm.Items.Item("EdtFilePath").Specific).Value;

                // Validate required fields
                if (string.IsNullOrEmpty(filePath))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template file path is required", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Verify the file exists
                if (!File.Exists(filePath))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template file does not exist", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Update the letter merge template
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string updateSql = B1App.Instance.IsHana ?
                    $"UPDATE \"@BTUN_LTRM\" SET \"U_Desc\" = '{desc}', \"U_DocType\" = '{docType}', \"U_FilePath\" = '{filePath}', \"U_UpdatedAt\" = '{DateTime.Today:yyyy-MM-dd}' WHERE \"U_Name\" = '{templateName}'" :
                    $"UPDATE [@BTUN_LTRM] SET U_Desc = '{desc}', U_DocType = '{docType}', U_FilePath = '{filePath}', U_UpdatedAt = '{DateTime.Today:yyyy-MM-dd}' WHERE U_Name = '{templateName}'";

                rs.DoQuery(updateSql);

                // Assume success if no exception
                B1App.Instance.Application.SetStatusBarMessage("Letter merge template updated successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the parent form
                Grid matrix = (Grid)parentForm.Items.Item("LetterMatrix").Specific;
                LoadLetterMergeTemplates(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error updating letter merge template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void DeleteSelectedLetterMergeTemplate(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("LetterMatrix").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    if (B1App.Instance.Application.MessageBox($"Are you sure you want to delete the letter merge template '{templateName}'?", 1, "Yes", "No") == 1)
                    {
                        // Delete the template
                        Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                        string deleteSql = B1App.Instance.IsHana ?
                            $"DELETE FROM \"@BTUN_LTRM\" WHERE \"U_Name\" = '{templateName}'" :
                            $"DELETE FROM [@BTUN_LTRM] WHERE U_Name = '{templateName}'";

                        rs.DoQuery(deleteSql);

                        // Assume success
                        B1App.Instance.Application.SetStatusBarMessage("Letter merge template deleted successfully", BoMessageTime.bmt_Short, false);

                        // Reload the matrix
                        LoadLetterMergeTemplates(matrix);

                        ComObjectManager.Release(rs);
                    }
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template to delete", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error deleting letter merge template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteLetterMergeTemplate(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("LetterMatrix").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    // Execute the letter merge based on the template
                    ExecuteLetterMergeByName(templateName);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template to execute", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing letter merge: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void PreviewLetterMergeTemplate(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("LetterMatrix").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    // Show a preview of the merged letter
                    PreviewLetterMergeByName(templateName);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template to preview", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error previewing letter merge: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteLetterMergeByName(string templateName)
        {
            try
            {
                // Get template details
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    $"SELECT * FROM \"@BTUN_LTRM\" WHERE \"U_Name\" = '{templateName}'" :
                    $"SELECT * FROM [@BTUN_LTRM] WHERE U_Name = '{templateName}'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    string filePath = rs.Fields.Item("U_FilePath").Value.ToString();

                    // Perform the letter merge
                    bool success = PerformLetterMerge(templateName, filePath);

                    if (success)
                    {
                        B1App.Instance.Application.SetStatusBarMessage($"Letter merge '{templateName}' executed successfully", BoMessageTime.bmt_Short, false);
                    }
                    else
                    {
                        B1App.Instance.Application.SetStatusBarMessage($"Failed to execute letter merge '{templateName}'", BoMessageTime.bmt_Short, true);
                    }
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Template '{templateName}' not found", BoMessageTime.bmt_Short, true);
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing letter merge: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void PreviewLetterMergeByName(string templateName)
        {
            try
            {
                // Get template details
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    $"SELECT * FROM \"@BTUN_LTRM\" WHERE \"U_Name\" = '{templateName}'" :
                    $"SELECT * FROM [@BTUN_LTRM] WHERE U_Name = '{templateName}'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    string filePath = rs.Fields.Item("U_FilePath").Value.ToString();

                    // Show a preview of the letter merge
                    B1App.Instance.Application.SetStatusBarMessage($"Preview for template '{templateName}' (File: {filePath})", BoMessageTime.bmt_Short, false);

                    // In a real implementation, this would open a preview of the merged document
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Template '{templateName}' not found", BoMessageTime.bmt_Short, true);
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error previewing letter merge: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static bool PerformLetterMerge(string templateName, string templatePath)
        {
            try
            {
                // Get the active form to extract data
                Form activeForm = B1App.Instance.Application.Forms.ActiveForm;

                // In a real implementation, this would:
                // 1. Open the Word template
                // 2. Extract field mappings from the metadata table
                // 3. Populate the template with data from the current SAP document
                // 4. Save or open the resulting document

                // For now, we'll simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Performing letter merge with template: {templatePath}", BoMessageTime.bmt_Short, false);

                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error performing letter merge: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        private static void QuickLetterMerge(SAPbouiCOM.Form parentForm, string docType, string templateName)
        {
            try
            {
                // Perform a quick merge using the current document in the active form
                SAPbouiCOM.Form activeForm = B1App.Instance.Application.Forms.ActiveForm;

                if (activeForm.TypeEx != docType)
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Current form type does not match selected document type", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Execute the letter merge for the current document
                ExecuteLetterMergeByName(templateName);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error in quick letter merge: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        // Method to get available merge fields for a document type
        public static Dictionary<string, string> GetMergeFieldsForDocType(string docType)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();

            switch (docType)
            {
                case "17": // Sales Invoice
                    fields.Add("DocEntry", "Document Entry");
                    fields.Add("DocNum", "Document Number");
                    fields.Add("CardCode", "Customer Code");
                    fields.Add("CardName", "Customer Name");
                    fields.Add("DocDate", "Document Date");
                    fields.Add("DocTotal", "Document Total");
                    fields.Add("Comments", "Comments");
                    fields.Add("Address", "Bill To Address");
                    break;
                case "13": // Sales Order
                    fields.Add("DocEntry", "Document Entry");
                    fields.Add("DocNum", "Document Number");
                    fields.Add("CardCode", "Customer Code");
                    fields.Add("CardName", "Customer Name");
                    fields.Add("DocDate", "Document Date");
                    fields.Add("DocTotal", "Document Total");
                    fields.Add("Comments", "Comments");
                    break;
                case "1470000167": // Customer
                    fields.Add("CardCode", "Customer Code");
                    fields.Add("CardName", "Customer Name");
                    fields.Add("Address", "Address");
                    fields.Add("Phone1", "Phone");
                    fields.Add("E_Mail", "Email");
                    fields.Add("ContactPerson", "Contact Person");
                    break;
                    // Add more document types as needed
            }

            return fields;
        }
    }
}
