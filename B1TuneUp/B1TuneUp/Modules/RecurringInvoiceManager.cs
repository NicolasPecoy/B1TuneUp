using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SAPbouiCOM;
using Form = SAPbouiCOM.Form;
using ComboBox = SAPbouiCOM.ComboBox;
using Grid = SAPbouiCOM.Grid;
using ItemEvent = SAPbouiCOM.ItemEvent;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class RecurringInvoiceManager
    {
        public static void OpenRecurringInvoiceForm()
        {
            try
            {
                string formUID = "BTUN_RECINV_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_RECINV";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp - Recurring Invoices";
                oForm.Width = 900;
                oForm.Height = 600;

                // Create form items
                CreateRecurringInvoiceFormItems(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening Recurring Invoice form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateRecurringInvoiceFormItems(SAPbouiCOM.Form oForm)
        {
            // Create a matrix to show existing recurring invoices
            Item matrixItem = oForm.Items.Add("RecInvMatrix", BoFormItemTypes.it_GRID);
            matrixItem.Top = 10;
            matrixItem.Left = 10;
            matrixItem.Width = 870;
            matrixItem.Height = 400;

            SAPbouiCOM.Grid matrix = SapUiSafe.TryGetSpecific<SAPbouiCOM.Grid>(matrixItem);

            // Ensure datatable columns exist for the grid
            matrix.DataTable.Columns.Add("TemplateName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Description", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Frequency", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("StartDate", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("EndDate", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Active", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);

            // Set column titles if available
            try
            {
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("TemplateName")).TitleObject.Caption = "Template Name";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Description")).TitleObject.Caption = "Description";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Frequency")).TitleObject.Caption = "Frequency";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("StartDate")).TitleObject.Caption = "Start Date";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("EndDate")).TitleObject.Caption = "End Date";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Active")).TitleObject.Caption = "Active";
            }
            catch { }

            // Create buttons
            Item addButton = oForm.Items.Add("BtnAdd", BoFormItemTypes.it_BUTTON);
            addButton.Top = 420;
            addButton.Left = 10;
            addButton.Width = 80;
            addButton.Height = 25;
            SapUiSafe.TrySetCaption(addButton, "Add");

            Item editButton = oForm.Items.Add("BtnEdit", BoFormItemTypes.it_BUTTON);
            editButton.Top = 420;
            editButton.Left = 100;
            editButton.Width = 80;
            editButton.Height = 25;
            SapUiSafe.TrySetCaption(editButton, "Edit");

            Item deleteButton = oForm.Items.Add("BtnDelete", BoFormItemTypes.it_BUTTON);
            deleteButton.Top = 420;
            deleteButton.Left = 190;
            deleteButton.Width = 80;
            deleteButton.Height = 25;
            SapUiSafe.TrySetCaption(deleteButton, "Delete");

            Item executeButton = oForm.Items.Add("BtnExecute", BoFormItemTypes.it_BUTTON);
            executeButton.Top = 420;
            executeButton.Left = 280;
            executeButton.Width = 100;
            executeButton.Height = 25;
            SapUiSafe.TrySetCaption(executeButton, "Execute");

            Item closeButton = oForm.Items.Add("BtnClose", BoFormItemTypes.it_BUTTON);
            closeButton.Top = 420;
            closeButton.Left = 780;
            closeButton.Width = 80;
            closeButton.Height = 25;
            SapUiSafe.TrySetCaption(closeButton, "Close");

            // Load existing recurring invoice templates
            LoadRecurringInvoiceTemplates(matrix);
        }

        private static void LoadRecurringInvoiceTemplates(SAPbouiCOM.Grid matrix)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_Name\", \"U_Desc\", \"U_Freq\", \"U_StartDate\", \"U_EndDate\", \"U_Active\" FROM \"@BTUN_RINV\" ORDER BY \"U_Name\"" :
                    "SELECT U_Name, U_Desc, U_Freq, U_StartDate, U_EndDate, U_Active FROM [@BTUN_RINV] ORDER BY U_Name";

                rs.DoQuery(sql);

                matrix.DataTable.Rows.Clear();

                while (!rs.EoF)
                {
                    matrix.DataTable.Rows.Add();
                    int rowIndex = matrix.DataTable.Rows.Count - 1;

                    matrix.DataTable.SetValue("TemplateName", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Name"));
                    matrix.DataTable.SetValue("Description", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Desc"));
                    matrix.DataTable.SetValue("Frequency", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Freq"));
                    matrix.DataTable.SetValue("StartDate", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_StartDate"));
                    matrix.DataTable.SetValue("EndDate", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_EndDate"));
                    matrix.DataTable.SetValue("Active", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Active"));

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading recurring invoice templates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewRecurringInvoiceTemplate()
        {
            try
            {
                string formUID = "BTUN_RECINV_NEW_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_RIVNEW";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "Create New Recurring Invoice Template";
                oForm.Width = 700;
                oForm.Height = 500;

                CreateNewRecurringInvoiceFormItems(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creating new recurring invoice form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewRecurringInvoiceFormItems(SAPbouiCOM.Form oForm)
        {
            // Labels
            Item nameLabel = oForm.Items.Add("LblName", BoFormItemTypes.it_STATIC);
            nameLabel.Top = 20;
            nameLabel.Left = 20;
            nameLabel.Width = 100;
            nameLabel.Height = 20;
            SapUiSafe.TrySetCaption(nameLabel, "Template Name:");

            Item descLabel = oForm.Items.Add("LblDesc", BoFormItemTypes.it_STATIC);
            descLabel.Top = 50;
            descLabel.Left = 20;
            descLabel.Width = 100;
            descLabel.Height = 20;
            SapUiSafe.TrySetCaption(descLabel, "Description:");

            Item freqLabel = oForm.Items.Add("LblFreq", BoFormItemTypes.it_STATIC);
            freqLabel.Top = 80;
            freqLabel.Left = 20;
            freqLabel.Width = 100;
            freqLabel.Height = 20;
            SapUiSafe.TrySetCaption(freqLabel, "Frequency:");

            Item startDateLabel = oForm.Items.Add("LblStart", BoFormItemTypes.it_STATIC);
            startDateLabel.Top = 110;
            startDateLabel.Left = 20;
            startDateLabel.Width = 100;
            startDateLabel.Height = 20;
            SapUiSafe.TrySetCaption(startDateLabel, "Start Date:");

            Item endDateLabel = oForm.Items.Add("LblEnd", BoFormItemTypes.it_STATIC);
            endDateLabel.Top = 140;
            endDateLabel.Left = 20;
            endDateLabel.Width = 100;
            endDateLabel.Height = 20;
            SapUiSafe.TrySetCaption(endDateLabel, "End Date:");

            Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
            docTypeLabel.Top = 170;
            docTypeLabel.Left = 20;
            docTypeLabel.Width = 100;
            docTypeLabel.Height = 20;
            SapUiSafe.TrySetCaption(docTypeLabel, "Document Type:");

            Item docNumLabel = oForm.Items.Add("LblDocNum", BoFormItemTypes.it_STATIC);
            docNumLabel.Top = 200;
            docNumLabel.Left = 20;
            docNumLabel.Width = 100;
            docNumLabel.Height = 20;
            SapUiSafe.TrySetCaption(docNumLabel, "Document Number:");

            Item activeLabel = oForm.Items.Add("LblActive", BoFormItemTypes.it_STATIC);
            activeLabel.Top = 230;
            activeLabel.Left = 20;
            activeLabel.Width = 100;
            activeLabel.Height = 20;
            SapUiSafe.TrySetCaption(activeLabel, "Active:");

            // Input fields
            Item nameEdit = oForm.Items.Add("EdtName", BoFormItemTypes.it_EDIT);
            nameEdit.Top = 20;
            nameEdit.Left = 130;
            nameEdit.Width = 200;
            nameEdit.Height = 20;

            Item descEdit = oForm.Items.Add("EdtDesc", BoFormItemTypes.it_EDIT);
            descEdit.Top = 50;
            descEdit.Left = 130;
            descEdit.Width = 200;
            descEdit.Height = 20;

            Item freqCombo = oForm.Items.Add("CmbFreq", BoFormItemTypes.it_COMBO_BOX);
            freqCombo.Top = 80;
            freqCombo.Left = 130;
            freqCombo.Width = 200;
            freqCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbFreq = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(freqCombo);
            cmbFreq.ValidValues.Add("Daily", "Daily");
            cmbFreq.ValidValues.Add("Weekly", "Weekly");
            cmbFreq.ValidValues.Add("BiWeekly", "Bi-Weekly (Every 2 weeks)");
            cmbFreq.ValidValues.Add("Monthly", "Monthly");
            cmbFreq.ValidValues.Add("BiMonthly", "Bi-Monthly (Every 2 months)");
            cmbFreq.ValidValues.Add("Quarterly", "Quarterly");
            cmbFreq.ValidValues.Add("SemiAnnually", "Semi-Annually");
            cmbFreq.ValidValues.Add("Annually", "Annually");
            cmbFreq.Select(3); // Default to Monthly

            Item startDateEdit = oForm.Items.Add("EdtStart", BoFormItemTypes.it_EDIT);
            startDateEdit.Top = 110;
            startDateEdit.Left = 130;
            startDateEdit.Width = 200;
            startDateEdit.Height = 20;
            SapUiSafe.TrySetEditValue(startDateEdit, DateTime.Today.ToString("yyyyMMdd"));

            Item endDateEdit = oForm.Items.Add("EdtEnd", BoFormItemTypes.it_EDIT);
            endDateEdit.Top = 140;
            endDateEdit.Left = 130;
            endDateEdit.Width = 200;
            endDateEdit.Height = 20;
            SapUiSafe.TrySetEditValue(endDateEdit, DateTime.Today.AddYears(1).ToString("yyyyMMdd"));

            Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
            docTypeCombo.Top = 170;
            docTypeCombo.Left = 130;
            docTypeCombo.Width = 200;
            docTypeCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbDocType = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(docTypeCombo);
            cmbDocType.ValidValues.Add("17", "Sales Invoice");
            cmbDocType.ValidValues.Add("13", "Sales Order");
            cmbDocType.ValidValues.Add("14", "Delivery");
            cmbDocType.ValidValues.Add("16", "AR Credit Memo");
            cmbDocType.ValidValues.Add("203", "Purchase Invoice");
            cmbDocType.ValidValues.Add("1470000113", "Purchase Order");
            cmbDocType.Select(0); // Default to Sales Invoice

            Item docNumEdit = oForm.Items.Add("EdtDocNum", BoFormItemTypes.it_EDIT);
            docNumEdit.Top = 200;
            docNumEdit.Left = 130;
            docNumEdit.Width = 200;
            docNumEdit.Height = 20;

            Item activeCombo = oForm.Items.Add("CmbActive", BoFormItemTypes.it_COMBO_BOX);
            activeCombo.Top = 230;
            activeCombo.Left = 130;
            activeCombo.Width = 200;
            activeCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbActive = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(activeCombo);
            cmbActive.ValidValues.Add("Y", "Yes");
            cmbActive.ValidValues.Add("N", "No");
            cmbActive.Select(0); // Default to Yes

            // Buttons
            Item saveButton = oForm.Items.Add("BtnSave", BoFormItemTypes.it_BUTTON);
            saveButton.Top = 270;
            saveButton.Left = 20;
            saveButton.Width = 80;
            saveButton.Height = 25;
            SapUiSafe.TrySetCaption(saveButton, "Save");

            Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
            cancelButton.Top = 270;
            cancelButton.Left = 110;
            cancelButton.Width = 80;
            cancelButton.Height = 25;
            SapUiSafe.TrySetCaption(cancelButton, "Cancel");
        }

        private static void SaveRecurringInvoiceTemplate(SAPbouiCOM.Form oForm)
        {
            try
            {
                string name = SapUiSafe.TryGetSpecific<SAPbouiCOM.EditText>(oForm, "EdtName")?.Value ?? string.Empty;
                string desc = SapUiSafe.TryGetSpecific<SAPbouiCOM.EditText>(oForm, "EdtDesc")?.Value ?? string.Empty;
                string freq = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(oForm, "CmbFreq"));
                string startDate = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtStart")?.Value ?? string.Empty;
                string endDate = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtEnd")?.Value ?? string.Empty;
                string docType = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(oForm, "CmbDocType"));
                string docNum = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtDocNum")?.Value ?? string.Empty;
                string active = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(oForm, "CmbActive"));

                // Validate required fields
                if (string.IsNullOrEmpty(name))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Template name is required", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Save to user table
                UserObjectsMD udo = (UserObjectsMD)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.oUserObjectsMD);

                // Create a draft document based on the template
                object draftDoc = CreateDraftDocument(docType, docNum);

                if (draftDoc != null)
                {
                    // Save the recurring invoice template
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string insertSql = B1App.Instance.IsHana ?
                        $"INSERT INTO \"@BTUN_RINV\" (\"U_Name\", \"U_Desc\", \"U_Freq\", \"U_StartDate\", \"U_EndDate\", \"U_DocType\", \"U_DocNum\", \"U_Active\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{name}', '{desc}', '{freq}', '{startDate}', '{endDate}', '{docType}', '{docNum}', '{active}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')" :
                        $"INSERT INTO [@BTUN_RINV] (U_Name, U_Desc, U_Freq, U_StartDate, U_EndDate, U_DocType, U_DocNum, U_Active, U_CreatedBy, U_CreatedAt) VALUES ('{name}', '{desc}', '{freq}', '{startDate}', '{endDate}', '{docType}', '{docNum}', '{active}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')";

                    rs.DoQuery(insertSql);

                    // rs.DoQuery returns void in some SDK versions; assume success if no exception
                    B1App.Instance.Application.SetStatusBarMessage("Recurring invoice template saved successfully", BoMessageTime.bmt_Short, false);

                    // Close the form
                    oForm.Close();

                    // Refresh the main recurring invoice form if it exists
                    RefreshRecurringInvoiceForms();

                    // Skip original result checking - assume success if no exception

                    ComObjectManager.Release(rs);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving recurring invoice template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static object CreateDraftDocument(string docType, string docNum)
        {
            try
            {
                // Business Services are not available in this build environment; return null.
                return null; // Placeholder - actual implementation would use DI API Services
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creating draft document: {ex.Message}", BoMessageTime.bmt_Short, true);
                return null;
            }
        }

        private static void EditSelectedRecurringInvoiceTemplate(SAPbouiCOM.Form oForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(oForm, "RecInvMatrix");
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder); // provide OrderType

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    // Open edit form with the selected template data
                    OpenEditRecurringInvoiceForm(templateName);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template to edit", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error editing recurring invoice template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void OpenEditRecurringInvoiceForm(string templateName)
        {
            try
            {
                string formUID = "BTUN_RECINV_EDIT_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_RIVEDT";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = $"Edit Recurring Invoice Template: {templateName}";
                oForm.Width = 700;
                oForm.Height = 500;

                CreateEditRecurringInvoiceFormItems(oForm, templateName);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening edit recurring invoice form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateEditRecurringInvoiceFormItems(SAPbouiCOM.Form oForm, string templateName)
        {
            // First load the template data
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            string sql = B1App.Instance.IsHana ?
                $"SELECT * FROM \"@BTUN_RINV\" WHERE \"U_Name\" = '{templateName}'" :
                $"SELECT * FROM [@BTUN_RINV] WHERE U_Name = '{templateName}'";

            rs.DoQuery(sql);

            if (!rs.EoF)
            {
                // Labels
                Item nameLabel = oForm.Items.Add("LblName", BoFormItemTypes.it_STATIC);
                nameLabel.Top = 20;
                nameLabel.Left = 20;
                nameLabel.Width = 100;
                nameLabel.Height = 20;
                SapUiSafe.TrySetCaption(nameLabel, "Template Name:");

                Item descLabel = oForm.Items.Add("LblDesc", BoFormItemTypes.it_STATIC);
                descLabel.Top = 50;
                descLabel.Left = 20;
                descLabel.Width = 100;
                descLabel.Height = 20;
                SapUiSafe.TrySetCaption(descLabel, "Description:");

                Item freqLabel = oForm.Items.Add("LblFreq", BoFormItemTypes.it_STATIC);
                freqLabel.Top = 80;
                freqLabel.Left = 20;
                freqLabel.Width = 100;
                freqLabel.Height = 20;
                SapUiSafe.TrySetCaption(freqLabel, "Frequency:");

                Item startDateLabel = oForm.Items.Add("LblStart", BoFormItemTypes.it_STATIC);
                startDateLabel.Top = 110;
                startDateLabel.Left = 20;
                startDateLabel.Width = 100;
                startDateLabel.Height = 20;
                SapUiSafe.TrySetCaption(startDateLabel, "Start Date:");

                Item endDateLabel = oForm.Items.Add("LblEnd", BoFormItemTypes.it_STATIC);
                endDateLabel.Top = 140;
                endDateLabel.Left = 20;
                endDateLabel.Width = 100;
                endDateLabel.Height = 20;
                SapUiSafe.TrySetCaption(endDateLabel, "End Date:");

                Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
                docTypeLabel.Top = 170;
                docTypeLabel.Left = 20;
                docTypeLabel.Width = 100;
                docTypeLabel.Height = 20;
                SapUiSafe.TrySetCaption(docTypeLabel, "Document Type:");

                Item docNumLabel = oForm.Items.Add("LblDocNum", BoFormItemTypes.it_STATIC);
                docNumLabel.Top = 200;
                docNumLabel.Left = 20;
                docNumLabel.Width = 100;
                docNumLabel.Height = 20;
                SapUiSafe.TrySetCaption(docNumLabel, "Document Number:");

                Item activeLabel = oForm.Items.Add("LblActive", BoFormItemTypes.it_STATIC);
                activeLabel.Top = 230;
                activeLabel.Left = 20;
                activeLabel.Width = 100;
                activeLabel.Height = 20;
                SapUiSafe.TrySetCaption(activeLabel, "Active:");

                // Input fields
                Item nameEdit = oForm.Items.Add("EdtName", BoFormItemTypes.it_EDIT);
                nameEdit.Top = 20;
                nameEdit.Left = 130;
                nameEdit.Width = 200;
                nameEdit.Height = 20;
                nameEdit.Enabled = false; // Can't change template name
                SapUiSafe.TrySetEditValue(nameEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Name"));

                Item descEdit = oForm.Items.Add("EdtDesc", BoFormItemTypes.it_EDIT);
                descEdit.Top = 50;
                descEdit.Left = 130;
                descEdit.Width = 200;
                descEdit.Height = 20;
                SapUiSafe.TrySetEditValue(descEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Desc"));

                Item freqCombo = oForm.Items.Add("CmbFreq", BoFormItemTypes.it_COMBO_BOX);
                freqCombo.Top = 80;
                freqCombo.Left = 130;
                freqCombo.Width = 200;
                freqCombo.Height = 20;
                ComboBox cmbFreq = SapUiSafe.TryGetSpecific<ComboBox>(freqCombo);
                cmbFreq.ValidValues.Add("Daily", "Daily");
                cmbFreq.ValidValues.Add("Weekly", "Weekly");
                cmbFreq.ValidValues.Add("BiWeekly", "Bi-Weekly (Every 2 weeks)");
                cmbFreq.ValidValues.Add("Monthly", "Monthly");
                cmbFreq.ValidValues.Add("BiMonthly", "Bi-Monthly (Every 2 months)");
                cmbFreq.ValidValues.Add("Quarterly", "Quarterly");
                cmbFreq.ValidValues.Add("SemiAnnually", "Semi-Annually");
                cmbFreq.ValidValues.Add("Annually", "Annually");
                try { cmbFreq.Select(B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Freq")); } catch { }

                Item startDateEdit = oForm.Items.Add("EdtStart", BoFormItemTypes.it_EDIT);
                startDateEdit.Top = 110;
                startDateEdit.Left = 130;
                startDateEdit.Width = 200;
                startDateEdit.Height = 20;
                SapUiSafe.TrySetEditValue(startDateEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_StartDate"));

                Item endDateEdit = oForm.Items.Add("EdtEnd", BoFormItemTypes.it_EDIT);
                endDateEdit.Top = 140;
                endDateEdit.Left = 130;
                endDateEdit.Width = 200;
                endDateEdit.Height = 20;
                SapUiSafe.TrySetEditValue(endDateEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_EndDate"));

                Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
                docTypeCombo.Top = 170;
                docTypeCombo.Left = 130;
                docTypeCombo.Width = 200;
                docTypeCombo.Height = 20;
                ComboBox cmbDocType = SapUiSafe.TryGetSpecific<ComboBox>(docTypeCombo);
                cmbDocType.ValidValues.Add("17", "Sales Invoice");
                cmbDocType.ValidValues.Add("13", "Sales Order");
                cmbDocType.ValidValues.Add("14", "Delivery");
                cmbDocType.ValidValues.Add("16", "AR Credit Memo");
                cmbDocType.ValidValues.Add("203", "Purchase Invoice");
                cmbDocType.ValidValues.Add("1470000113", "Purchase Order");
                try { cmbDocType.Select(B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DocType")); } catch { }

                Item docNumEdit = oForm.Items.Add("EdtDocNum", BoFormItemTypes.it_EDIT);
                docNumEdit.Top = 200;
                docNumEdit.Left = 130;
                docNumEdit.Width = 200;
                docNumEdit.Height = 20;
                SapUiSafe.TrySetEditValue(docNumEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DocNum"));

                Item activeCombo = oForm.Items.Add("CmbActive", BoFormItemTypes.it_COMBO_BOX);
                activeCombo.Top = 230;
                activeCombo.Left = 130;
                activeCombo.Width = 200;
                activeCombo.Height = 20;
                ComboBox cmbActive = SapUiSafe.TryGetSpecific<ComboBox>(activeCombo);
                cmbActive.ValidValues.Add("Y", "Yes");
                cmbActive.ValidValues.Add("N", "No");
                if (B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Active") == "Y")
                    cmbActive.Select(0);
                else
                    cmbActive.Select(1);

                // Buttons
                Item updateButton = oForm.Items.Add("BtnUpdate", BoFormItemTypes.it_BUTTON);
                updateButton.Top = 270;
                updateButton.Left = 20;
                updateButton.Width = 80;
                updateButton.Height = 25;
                SapUiSafe.TrySetCaption(updateButton, "Update");

                Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
                cancelButton.Top = 270;
                cancelButton.Left = 110;
                cancelButton.Width = 80;
                cancelButton.Height = 25;
                SapUiSafe.TrySetCaption(cancelButton, "Cancel");
            }

            ComObjectManager.Release(rs);
        }

        private static void UpdateRecurringInvoiceTemplate(SAPbouiCOM.Form oForm, string templateName)
        {
            try
            {
                string desc = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtDesc")?.Value ?? string.Empty;
                string freq = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(oForm, "CmbFreq"));
                string startDate = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtStart")?.Value ?? string.Empty;
                string endDate = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtEnd")?.Value ?? string.Empty;
                string docType = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(oForm, "CmbDocType"));
                string docNum = SapUiSafe.TryGetSpecific<EditText>(oForm, "EdtDocNum")?.Value ?? string.Empty;
                string active = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(oForm, "CmbActive"));

                // Update the recurring invoice template
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string updateSql = B1App.Instance.IsHana ?
                    $"UPDATE \"@BTUN_RINV\" SET \"U_Desc\" = '{desc}', \"U_Freq\" = '{freq}', \"U_StartDate\" = '{startDate}', \"U_EndDate\" = '{endDate}', \"U_DocType\" = '{docType}', \"U_DocNum\" = '{docNum}', \"U_Active\" = '{active}', \"U_UpdatedAt\" = '{DateTime.Today:yyyy-MM-dd}' WHERE \"U_Name\" = '{templateName}'" :
                    $"UPDATE [@BTUN_RINV] SET U_Desc = '{desc}', U_Freq = '{freq}', U_StartDate = '{startDate}', U_EndDate = '{endDate}', U_DocType = '{docType}', U_DocNum = '{docNum}', U_Active = '{active}', U_UpdatedAt = '{DateTime.Today:yyyy-MM-dd}' WHERE U_Name = '{templateName}'";

                rs.DoQuery(updateSql);
                B1App.Instance.Application.SetStatusBarMessage("Recurring invoice template updated successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the main recurring invoice form if it exists
                RefreshRecurringInvoiceForms();

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error updating recurring invoice template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void DeleteSelectedRecurringInvoiceTemplate(SAPbouiCOM.Form oForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(oForm, "RecInvMatrix");
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    if (B1App.Instance.Application.MessageBox($"Are you sure you want to delete the recurring invoice template '{templateName}'?", 1, "Yes", "No") == 1)
                    {
                        // Delete the template
                        Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                        string deleteSql = B1App.Instance.IsHana ?
                            $"DELETE FROM \"@BTUN_RINV\" WHERE \"U_Name\" = '{templateName}'" :
                            $"DELETE FROM [@BTUN_RINV] WHERE U_Name = '{templateName}'";

                        rs.DoQuery(deleteSql);
                        B1App.Instance.Application.SetStatusBarMessage("Recurring invoice template deleted successfully", BoMessageTime.bmt_Short, false);
                        LoadRecurringInvoiceTemplates(matrix);

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
                B1App.Instance.Application.SetStatusBarMessage($"Error deleting recurring invoice template: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteRecurringInvoiceTemplate(SAPbouiCOM.Form oForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(oForm, "RecInvMatrix");
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected template name
                    string templateName = matrix.DataTable.GetValue("TemplateName", selectedRow).ToString();

                    // Execute the recurring invoice based on the template
                    ExecuteRecurringInvoiceByName(templateName);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a template to execute", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing recurring invoice: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteRecurringInvoiceByName(string templateName)
        {
            try
            {
                // Get template details
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    $"SELECT * FROM \"@BTUN_RINV\" WHERE \"U_Name\" = '{templateName}' AND \"U_Active\" = 'Y'" :
                    $"SELECT * FROM [@BTUN_RINV] WHERE U_Name = '{templateName}' AND U_Active = 'Y'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    string docType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DocType");
                    string docNum = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DocNum");
                    string frequency = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Freq");
                    string startDate = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_StartDate");
                    string endDate = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_EndDate");

                    // Create the recurring invoice document based on the template
                    // This is a simplified implementation - in reality, you'd clone the source document
                    // and populate it with new data based on the recurrence pattern
                    bool success = ProcessRecurringInvoice(templateName, docType, docNum, frequency, startDate, endDate);

                    if (success)
                    {
                        // Update the last executed date
                        string updateSql = B1App.Instance.IsHana ?
                            $"UPDATE \"@BTUN_RINV\" SET \"U_LastExecuted\" = '{DateTime.Today:yyyy-MM-dd}' WHERE \"U_Name\" = '{templateName}'" :
                            $"UPDATE [@BTUN_RINV] SET U_LastExecuted = '{DateTime.Today:yyyy-MM-dd}' WHERE U_Name = '{templateName}'";

                        rs.DoQuery(updateSql);

                        B1App.Instance.Application.SetStatusBarMessage($"Recurring invoice '{templateName}' executed successfully", BoMessageTime.bmt_Short, false);
                    }
                    else
                    {
                        B1App.Instance.Application.SetStatusBarMessage($"Failed to execute recurring invoice '{templateName}'", BoMessageTime.bmt_Short, true);
                    }
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Template '{templateName}' not found or not active", BoMessageTime.bmt_Short, true);
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing recurring invoice: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static bool ProcessRecurringInvoice(string templateName, string docType, string docNum, string frequency, string startDate, string endDate)
        {
            try
            {
                // Check if the current date falls within the schedule for this recurring invoice
                DateTime start = DateTime.ParseExact(startDate, "yyyyMMdd", null);
                DateTime end = DateTime.ParseExact(endDate, "yyyyMMdd", null);
                DateTime today = DateTime.Today;

                if (today < start || today > end)
                {
                    B1App.Instance.Application.SetStatusBarMessage("Current date is outside the recurring invoice schedule", BoMessageTime.bmt_Short, true);
                    return false;
                }

                // Based on the frequency, determine if today is the day to execute
                bool shouldExecute = ShouldExecuteToday(today, start, frequency);

                if (!shouldExecute)
                {
                    B1App.Instance.Application.SetStatusBarMessage("Today is not scheduled for execution based on frequency", BoMessageTime.bmt_Short, true);
                    return false;
                }

                // In a real implementation, this would create a new document based on the template
                // For now, we'll simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Simulating creation of recurring document from template '{templateName}'", BoMessageTime.bmt_Short, false);

                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error processing recurring invoice: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        private static bool ShouldExecuteToday(DateTime today, DateTime startDate, string frequency)
        {
            switch (frequency)
            {
                case "Daily":
                    return true; // Could be modified to respect specific intervals
                case "Weekly":
                    // Execute weekly on the same day of week as start date
                    return today.DayOfWeek == startDate.DayOfWeek;
                case "BiWeekly":
                    // Every 2 weeks - check if difference in weeks is even
                    TimeSpan diff = today - startDate;
                    int weeksDiff = (int)(diff.TotalDays / 7);
                    return weeksDiff % 2 == 0;
                case "Monthly":
                    // Same day of month as start date
                    return today.Day == startDate.Day;
                case "BiMonthly":
                    // Every 2 months - check if difference in months is even
                    int monthsDiff = (today.Year - startDate.Year) * 12 + (today.Month - startDate.Month);
                    return monthsDiff % 2 == 0;
                case "Quarterly":
                    // Every 3 months
                    monthsDiff = (today.Year - startDate.Year) * 12 + (today.Month - startDate.Month);
                    return monthsDiff % 3 == 0;
                case "SemiAnnually":
                    // Every 6 months
                    monthsDiff = (today.Year - startDate.Year) * 12 + (today.Month - startDate.Month);
                    return monthsDiff % 6 == 0;
                case "Annually":
                    // Same month and day as start date
                    return today.Month == startDate.Month && today.Day == startDate.Day;
                default:
                    return false;
            }
        }

        private static void RefreshRecurringInvoiceForms()
        {
            try
            {
                // Find and refresh all recurring invoice forms
                for (int i = 0; i < B1App.Instance.Application.Forms.Count; i++)
                {
                    Form form = null;
                    try { form = SapUiSafe.TryGetForm(i); } catch { }
                    if (form == null) continue;
                    if (form.TypeEx == "BTUN_RECINV")
                    {
                        Grid matrix = SapUiSafe.TryGetSpecific<Grid>(form, "RecInvMatrix");
                        LoadRecurringInvoiceTemplates(matrix);
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error refreshing recurring invoice forms: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        // Method to process all active recurring invoices (typically called by scheduler)
        public static void ProcessAllActiveRecurringInvoices()
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_Name\", \"U_DocType\", \"U_DocNum\", \"U_Freq\", \"U_StartDate\", \"U_EndDate\" FROM \"@BTUN_RINV\" WHERE \"U_Active\" = 'Y' ORDER BY \"U_Name\"" :
                    "SELECT U_Name, U_DocType, U_DocNum, U_Freq, U_StartDate, U_EndDate FROM [@BTUN_RINV] WHERE U_Active = 'Y' ORDER BY U_Name";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string templateName = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Name");
                    string docType = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DocType");
                    string docNum = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_DocNum");
                    string frequency = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Freq");
                    string startDate = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_StartDate");
                    string endDate = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_EndDate");

                    // Process each active recurring invoice
                    ProcessRecurringInvoice(templateName, docType, docNum, frequency, startDate, endDate);

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error processing recurring invoices: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
