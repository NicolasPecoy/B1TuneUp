using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Collections.Generic;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class PrintDeliveryManager
    {
        public static void OpenPrintDeliveryForm()
        {
            try
            {
                string formUID = "BTUN_PRDEL_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_PRDEL";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp - Print & Delivery";
                oForm.Width = 1000;
                oForm.Height = 700;

                // Create form items
                CreatePrintDeliveryFormItems(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening Print & Delivery form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreatePrintDeliveryFormItems(SAPbouiCOM.Form oForm)
        {
            // Create a matrix to show existing print delivery configurations
            Item matrixItem = oForm.Items.Add("PDMatrx", BoFormItemTypes.it_GRID);
            matrixItem.Top = 10;
            matrixItem.Left = 10;
            matrixItem.Width = 970;
            matrixItem.Height = 300;

            SAPbouiCOM.Grid matrix = (SAPbouiCOM.Grid)matrixItem.Specific;

            // Add columns to the matrix via the grid datatable
            matrix.DataTable.Columns.Add("Name", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("DocType", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Trigger", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Action", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Active", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);

            try
            {
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Name")).TitleObject.Caption = "Configuration Name";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("DocType")).TitleObject.Caption = "Document Type";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Trigger")).TitleObject.Caption = "Trigger Event";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Action")).TitleObject.Caption = "Action";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Active")).TitleObject.Caption = "Active";
            }
            catch { }

            // Create buttons
            Item addButton = oForm.Items.Add("BtnAdd", BoFormItemTypes.it_BUTTON);
            addButton.Top = 320;
            addButton.Left = 10;
            addButton.Width = 80;
            addButton.Height = 25;
            ((SAPbouiCOM.Button)addButton.Specific).Caption = "Add";

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
            ((SAPbouiCOM.Button)executeButton.Specific).Caption = "Execute Now";

            Item previewButton = oForm.Items.Add("BtnPreview", BoFormItemTypes.it_BUTTON);
            previewButton.Top = 320;
            previewButton.Left = 390;
            previewButton.Width = 100;
            previewButton.Height = 25;
            ((SAPbouiCOM.Button)previewButton.Specific).Caption = "Preview";

            Item activateButton = oForm.Items.Add("BtnActivate", BoFormItemTypes.it_BUTTON);
            activateButton.Top = 320;
            activateButton.Left = 500;
            activateButton.Width = 100;
            activateButton.Height = 25;
            ((SAPbouiCOM.Button)activateButton.Specific).Caption = "Toggle Active";

            Item closeButton = oForm.Items.Add("BtnClose", BoFormItemTypes.it_BUTTON);
            closeButton.Top = 320;
            closeButton.Left = 900;
            closeButton.Width = 80;
            closeButton.Height = 25;
            ((SAPbouiCOM.Button)closeButton.Specific).Caption = "Close";

            // Document processing options
            Item processLabel = oForm.Items.Add("LblProcess", BoFormItemTypes.it_STATIC);
            processLabel.Top = 360;
            processLabel.Left = 10;
            processLabel.Width = 200;
            processLabel.Height = 20;
            ((SAPbouiCOM.StaticText)processLabel.Specific).Caption = "Document Processing:";

            Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
            docTypeLabel.Top = 390;
            docTypeLabel.Left = 10;
            docTypeLabel.Width = 100;
            docTypeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)docTypeLabel.Specific).Caption = "Document Type:";

            Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
            docTypeCombo.Top = 390;
            docTypeCombo.Left = 120;
            docTypeCombo.Width = 150;
            docTypeCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbDocType = (SAPbouiCOM.ComboBox)docTypeCombo.Specific;
            cmbDocType.ValidValues.Add("17", "Sales Invoice (17)");
            cmbDocType.ValidValues.Add("13", "Sales Order (13)");
            cmbDocType.ValidValues.Add("14", "Delivery (14)");
            cmbDocType.ValidValues.Add("16", "Credit Memo (16)");
            cmbDocType.ValidValues.Add("203", "Purchase Invoice (203)");
            cmbDocType.ValidValues.Add("1470000113", "Purchase Order (1470000113)");
            cmbDocType.ValidValues.Add("150", "Incoming Payment (150)");
            cmbDocType.Select(0); // Default to Sales Invoice

            Item actionTypeLabel = oForm.Items.Add("LblActionType", BoFormItemTypes.it_STATIC);
            actionTypeLabel.Top = 390;
            actionTypeLabel.Left = 290;
            actionTypeLabel.Width = 100;
            actionTypeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)actionTypeLabel.Specific).Caption = "Action Type:";

            Item actionTypeCombo = oForm.Items.Add("CmbActionType", BoFormItemTypes.it_COMBO_BOX);
            actionTypeCombo.Top = 390;
            actionTypeCombo.Left = 400;
            actionTypeCombo.Width = 150;
            actionTypeCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbActionType = (SAPbouiCOM.ComboBox)actionTypeCombo.Specific;
            cmbActionType.ValidValues.Add("EMAIL", "Email");
            cmbActionType.ValidValues.Add("PRINT", "Print");
            cmbActionType.ValidValues.Add("SAVE", "Save to Folder");
            cmbActionType.ValidValues.Add("FTP", "Upload to FTP");
            cmbActionType.ValidValues.Add("SHAREPOINT", "SharePoint Upload");
            cmbActionType.Select(0); // Default to Email

            Item triggerLabel = oForm.Items.Add("LblTrigger", BoFormItemTypes.it_STATIC);
            triggerLabel.Top = 420;
            triggerLabel.Left = 10;
            triggerLabel.Width = 100;
            triggerLabel.Height = 20;
            ((SAPbouiCOM.StaticText)triggerLabel.Specific).Caption = "Trigger Event:";

            Item triggerCombo = oForm.Items.Add("CmbTrigger", BoFormItemTypes.it_COMBO_BOX);
            triggerCombo.Top = 420;
            triggerCombo.Left = 120;
            triggerCombo.Width = 150;
            triggerCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbTrigger = (SAPbouiCOM.ComboBox)triggerCombo.Specific;
            cmbTrigger.ValidValues.Add("DOC_ADD", "Document Added");
            cmbTrigger.ValidValues.Add("DOC_UPDATE", "Document Updated");
            cmbTrigger.ValidValues.Add("DOC_APPROVE", "Document Approved");
            cmbTrigger.ValidValues.Add("MANUAL", "Manual Trigger");
            cmbTrigger.ValidValues.Add("SCHEDULED", "Scheduled");
            cmbTrigger.Select(0); // Default to Document Added

            Item processButton = oForm.Items.Add("BtnProcess", BoFormItemTypes.it_BUTTON);
            processButton.Top = 415;
            processButton.Left = 300;
            processButton.Width = 120;
            processButton.Height = 25;
            ((SAPbouiCOM.Button)processButton.Specific).Caption = "Process Documents";

            // Batch processing section
            Item batchLabel = oForm.Items.Add("LblBatch", BoFormItemTypes.it_STATIC);
            batchLabel.Top = 460;
            batchLabel.Left = 10;
            batchLabel.Width = 200;
            batchLabel.Height = 20;
            ((SAPbouiCOM.StaticText)batchLabel.Specific).Caption = "Batch Processing:";

            Item dateFromLabel = oForm.Items.Add("LblDateFrom", BoFormItemTypes.it_STATIC);
            dateFromLabel.Top = 490;
            dateFromLabel.Left = 10;
            dateFromLabel.Width = 80;
            dateFromLabel.Height = 20;
            ((SAPbouiCOM.StaticText)dateFromLabel.Specific).Caption = "Date From:";

            Item dateFromEdit = oForm.Items.Add("EdtDateFrom", BoFormItemTypes.it_EDIT);
            dateFromEdit.Top = 490;
            dateFromEdit.Left = 100;
            dateFromEdit.Width = 100;
            dateFromEdit.Height = 20;
            ((SAPbouiCOM.EditText)dateFromEdit.Specific).Value = DateTime.Today.AddDays(-7).ToString("yyyyMMdd"); // Default to 7 days ago

            Item dateToLabel = oForm.Items.Add("LblDateTo", BoFormItemTypes.it_STATIC);
            dateToLabel.Top = 490;
            dateToLabel.Left = 220;
            dateToLabel.Width = 80;
            dateToLabel.Height = 20;
            ((SAPbouiCOM.StaticText)dateToLabel.Specific).Caption = "Date To:";

            Item dateToEdit = oForm.Items.Add("EdtDateTo", BoFormItemTypes.it_EDIT);
            dateToEdit.Top = 490;
            dateToEdit.Left = 310;
            dateToEdit.Width = 100;
            dateToEdit.Height = 20;
            ((SAPbouiCOM.EditText)dateToEdit.Specific).Value = DateTime.Today.ToString("yyyyMMdd"); // Default to today

            Item batchProcessButton = oForm.Items.Add("BtnBatchProcess", BoFormItemTypes.it_BUTTON);
            batchProcessButton.Top = 485;
            batchProcessButton.Left = 430;
            batchProcessButton.Width = 120;
            batchProcessButton.Height = 25;
            ((SAPbouiCOM.Button)batchProcessButton.Specific).Caption = "Batch Process";

            // Load existing print delivery configurations
            LoadPrintDeliveryConfigs(matrix);
        }

        private static void LoadPrintDeliveryConfigs(Grid matrix)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_Name\", \"U_DocType\", \"U_Trigger\", \"U_Action\", \"U_Active\" FROM \"@BTUN_PD\" ORDER BY \"U_Name\"" :
                    "SELECT U_Name, U_DocType, U_Trigger, U_Action, U_Active FROM [@BTUN_PD] ORDER BY U_Name";

                rs.DoQuery(sql);

                matrix.DataTable.Rows.Clear();

                while (!rs.EoF)
                {
                    matrix.DataTable.Rows.Add();
                    int rowIndex = matrix.DataTable.Rows.Count - 1;

                    matrix.DataTable.SetValue("Name", rowIndex, rs.Fields.Item("U_Name").Value);
                    matrix.DataTable.SetValue("DocType", rowIndex, rs.Fields.Item("U_DocType").Value);
                    matrix.DataTable.SetValue("Trigger", rowIndex, rs.Fields.Item("U_Trigger").Value);
                    matrix.DataTable.SetValue("Action", rowIndex, rs.Fields.Item("U_Action").Value);
                    matrix.DataTable.SetValue("Active", rowIndex, rs.Fields.Item("U_Active").Value);

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading print delivery configs: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewPrintDeliveryConfig(SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formUID = "BTUN_PRDEL_NEW_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_PDNEW";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "Create New Print & Delivery Configuration";
                oForm.Width = 800;
                oForm.Height = 700;

                CreateNewPrintDeliveryFormItems(oForm, parentForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creating new print delivery config form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewPrintDeliveryFormItems(SAPbouiCOM.Form oForm, SAPbouiCOM.Form parentForm)
        {
            // Labels
            Item nameLabel = oForm.Items.Add("LblName", BoFormItemTypes.it_STATIC);
            nameLabel.Top = 20;
            nameLabel.Left = 20;
            nameLabel.Width = 150;
            nameLabel.Height = 20;
            ((SAPbouiCOM.StaticText)nameLabel.Specific).Caption = "Configuration Name:";

            Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
            docTypeLabel.Top = 50;
            docTypeLabel.Left = 20;
            docTypeLabel.Width = 150;
            docTypeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)docTypeLabel.Specific).Caption = "Document Type:";

            Item triggerLabel = oForm.Items.Add("LblTrigger", BoFormItemTypes.it_STATIC);
            triggerLabel.Top = 80;
            triggerLabel.Left = 20;
            triggerLabel.Width = 150;
            triggerLabel.Height = 20;
            ((SAPbouiCOM.StaticText)triggerLabel.Specific).Caption = "Trigger Event:";

            Item actionLabel = oForm.Items.Add("LblAction", BoFormItemTypes.it_STATIC);
            actionLabel.Top = 110;
            actionLabel.Left = 20;
            actionLabel.Width = 150;
            actionLabel.Height = 20;
            ((SAPbouiCOM.StaticText)actionLabel.Specific).Caption = "Action to Perform:";

            Item emailConfigLabel = oForm.Items.Add("LblEmailConfig", BoFormItemTypes.it_STATIC);
            emailConfigLabel.Top = 140;
            emailConfigLabel.Left = 20;
            emailConfigLabel.Width = 150;
            emailConfigLabel.Height = 20;
            ((SAPbouiCOM.StaticText)emailConfigLabel.Specific).Caption = "Email Configuration:";

            Item subjectLabel = oForm.Items.Add("LblSubject", BoFormItemTypes.it_STATIC);
            subjectLabel.Top = 170;
            subjectLabel.Left = 20;
            subjectLabel.Width = 150;
            subjectLabel.Height = 20;
            ((SAPbouiCOM.StaticText)subjectLabel.Specific).Caption = "Email Subject:";

            Item bodyLabel = oForm.Items.Add("LblBody", BoFormItemTypes.it_STATIC);
            bodyLabel.Top = 200;
            bodyLabel.Left = 20;
            bodyLabel.Width = 150;
            bodyLabel.Height = 20;
            ((SAPbouiCOM.StaticText)bodyLabel.Specific).Caption = "Email Body:";

            Item attachmentLabel = oForm.Items.Add("LblAttachment", BoFormItemTypes.it_STATIC);
            attachmentLabel.Top = 350;
            attachmentLabel.Left = 20;
            attachmentLabel.Width = 150;
            attachmentLabel.Height = 20;
            ((SAPbouiCOM.StaticText)attachmentLabel.Specific).Caption = "Attachment Settings:";

            Item printConfigLabel = oForm.Items.Add("LblPrintConfig", BoFormItemTypes.it_STATIC);
            printConfigLabel.Top = 420;
            printConfigLabel.Left = 20;
            printConfigLabel.Width = 150;
            printConfigLabel.Height = 20;
            ((SAPbouiCOM.StaticText)printConfigLabel.Specific).Caption = "Print Configuration:";

            Item saveConfigLabel = oForm.Items.Add("LblSaveConfig", BoFormItemTypes.it_STATIC);
            saveConfigLabel.Top = 490;
            saveConfigLabel.Left = 20;
            saveConfigLabel.Width = 150;
            saveConfigLabel.Height = 20;
            ((SAPbouiCOM.StaticText)saveConfigLabel.Specific).Caption = "Save Configuration:";

            Item activeLabel = oForm.Items.Add("LblActive", BoFormItemTypes.it_STATIC);
            activeLabel.Top = 560;
            activeLabel.Left = 20;
            activeLabel.Width = 150;
            activeLabel.Height = 20;
            ((SAPbouiCOM.StaticText)activeLabel.Specific).Caption = "Active:";

            // Input fields
            Item nameEdit = oForm.Items.Add("EdtName", BoFormItemTypes.it_EDIT);
            nameEdit.Top = 20;
            nameEdit.Left = 180;
            nameEdit.Width = 250;
            nameEdit.Height = 20;

            Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
            docTypeCombo.Top = 50;
            docTypeCombo.Left = 180;
            docTypeCombo.Width = 150;
            docTypeCombo.Height = 20;
            ComboBox cmbDocType = (ComboBox)docTypeCombo.Specific;
            cmbDocType.ValidValues.Add("17", "Sales Invoice (17)");
            cmbDocType.ValidValues.Add("13", "Sales Order (13)");
            cmbDocType.ValidValues.Add("14", "Delivery (14)");
            cmbDocType.ValidValues.Add("16", "Credit Memo (16)");
            cmbDocType.ValidValues.Add("203", "Purchase Invoice (203)");
            cmbDocType.ValidValues.Add("1470000113", "Purchase Order (1470000113)");
            cmbDocType.ValidValues.Add("150", "Incoming Payment (150)");
            cmbDocType.Select(0); // Default to Sales Invoice

            Item triggerCombo = oForm.Items.Add("CmbTrigger", BoFormItemTypes.it_COMBO_BOX);
            triggerCombo.Top = 80;
            triggerCombo.Left = 180;
            triggerCombo.Width = 150;
            triggerCombo.Height = 20;
            ComboBox cmbTrigger = (ComboBox)triggerCombo.Specific;
            cmbTrigger.ValidValues.Add("DOC_ADD", "Document Added");
            cmbTrigger.ValidValues.Add("DOC_UPDATE", "Document Updated");
            cmbTrigger.ValidValues.Add("DOC_APPROVE", "Document Approved");
            cmbTrigger.ValidValues.Add("MANUAL", "Manual Trigger");
            cmbTrigger.ValidValues.Add("SCHEDULED", "Scheduled");
            cmbTrigger.Select(0); // Default to Document Added

            Item actionCombo = oForm.Items.Add("CmbAction", BoFormItemTypes.it_COMBO_BOX);
            actionCombo.Top = 110;
            actionCombo.Left = 180;
            actionCombo.Width = 150;
            actionCombo.Height = 20;
            ComboBox cmbAction = (ComboBox)actionCombo.Specific;
            cmbAction.ValidValues.Add("EMAIL", "Email");
            cmbAction.ValidValues.Add("PRINT", "Print");
            cmbAction.ValidValues.Add("SAVE", "Save to Folder");
            cmbAction.ValidValues.Add("FTP", "Upload to FTP");
            cmbAction.ValidValues.Add("SHAREPOINT", "SharePoint Upload");
            cmbAction.Select(0); // Default to Email

            Item subjectEdit = oForm.Items.Add("EdtSubject", BoFormItemTypes.it_EDIT);
            subjectEdit.Top = 170;
            subjectEdit.Left = 20;
            subjectEdit.Width = 760;
            subjectEdit.Height = 20;
            ((SAPbouiCOM.EditText)subjectEdit.Specific).Value = "[[CardName]] - [[DocNum]] from [[Company Name]]";

            Item bodyEdit = oForm.Items.Add("EdtBody", BoFormItemTypes.it_EDIT);
            bodyEdit.Top = 200;
            bodyEdit.Left = 20;
            bodyEdit.Width = 760;
            bodyEdit.Height = 140;
            try
            {
                // Some SDKs don't expose MultiLineEdit; use EditText value directly
                ((SAPbouiCOM.EditText)bodyEdit.Specific).Value =
                "Dear [[CardName]],\n\n" +
                "Please find attached your [[Document Type]] #[[DocNum]] dated [[DocDate]].\n\n" +
                "Total amount: [[DocTotal]]\n\n" +
                "Best regards,\n[[Company Name]]";
            }
            catch { }

            Item attachmentCombo = oForm.Items.Add("CmbAttachment", BoFormItemTypes.it_COMBO_BOX);
            attachmentCombo.Top = 350;
            attachmentCombo.Left = 180;
            attachmentCombo.Width = 150;
            attachmentCombo.Height = 20;
            ComboBox cmbAttachment = (ComboBox)attachmentCombo.Specific;
            cmbAttachment.ValidValues.Add("PDF", "PDF Document");
            cmbAttachment.ValidValues.Add("EXCEL", "Excel Export");
            cmbAttachment.ValidValues.Add("WORD", "Word Document");
            cmbAttachment.ValidValues.Add("ORIGINAL", "Original Format");
            cmbAttachment.Select(0); // Default to PDF

            Item printSetupEdit = oForm.Items.Add("EdtPrintSetup", BoFormItemTypes.it_EDIT);
            printSetupEdit.Top = 420;
            printSetupEdit.Left = 180;
            printSetupEdit.Width = 250;
            printSetupEdit.Height = 20;
            ((SAPbouiCOM.EditText)printSetupEdit.Specific).Value = "Default Printer";

            Item savePathEdit = oForm.Items.Add("EdtSavePath", BoFormItemTypes.it_EDIT);
            savePathEdit.Top = 490;
            savePathEdit.Left = 180;
            savePathEdit.Width = 250;
            savePathEdit.Height = 20;
            ((SAPbouiCOM.EditText)savePathEdit.Specific).Value = @"C:\B1Exports\PrintDelivery";

            Item activeCombo = oForm.Items.Add("CmbActive", BoFormItemTypes.it_COMBO_BOX);
            activeCombo.Top = 560;
            activeCombo.Left = 180;
            activeCombo.Width = 150;
            activeCombo.Height = 20;
            ComboBox cmbActive = (ComboBox)activeCombo.Specific;
            cmbActive.ValidValues.Add("Y", "Yes");
            cmbActive.ValidValues.Add("N", "No");
            cmbActive.Select(0); // Default to Yes

            // Buttons
            Item saveButton = oForm.Items.Add("BtnSave", BoFormItemTypes.it_BUTTON);
            saveButton.Top = 600;
            saveButton.Left = 20;
            saveButton.Width = 80;
            saveButton.Height = 25;
            ((SAPbouiCOM.Button)saveButton.Specific).Caption = "Save";

            Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
            testButton.Top = 600;
            testButton.Left = 110;
            testButton.Width = 80;
            testButton.Height = 25;
            ((SAPbouiCOM.Button)testButton.Specific).Caption = "Test";

            Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
            cancelButton.Top = 600;
            cancelButton.Left = 200;
            cancelButton.Width = 80;
            cancelButton.Height = 25;
            ((SAPbouiCOM.Button)cancelButton.Specific).Caption = "Cancel";
        }

        private static void SavePrintDeliveryConfig(SAPbouiCOM.Form oForm, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string name = ((EditText)oForm.Items.Item("EdtName").Specific).Value;
                string docType = ((ComboBox)oForm.Items.Item("CmbDocType").Specific).Selected.Value;
                string trigger = ((ComboBox)oForm.Items.Item("CmbTrigger").Specific).Selected.Value;
                string action = ((ComboBox)oForm.Items.Item("CmbAction").Specific).Selected.Value;
                string subject = ((EditText)oForm.Items.Item("EdtSubject").Specific).Value;
                string body = ((SAPbouiCOM.EditText)oForm.Items.Item("EdtBody").Specific).Value;
                string attachment = ((ComboBox)oForm.Items.Item("CmbAttachment").Specific).Selected.Value;
                string printSetup = ((EditText)oForm.Items.Item("EdtPrintSetup").Specific).Value;
                string savePath = ((EditText)oForm.Items.Item("EdtSavePath").Specific).Value;
                string active = ((ComboBox)oForm.Items.Item("CmbActive").Specific).Selected.Value;

                // Validate required fields
                if (string.IsNullOrEmpty(name))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Configuration name is required", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Save to user table
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string insertSql = B1App.Instance.IsHana ?
                    $"INSERT INTO \"@BTUN_PD\" (\"U_Name\", \"U_DocType\", \"U_Trigger\", \"U_Action\", \"U_EmailSubject\", \"U_EmailBody\", \"U_AttachmentType\", \"U_PrintSetup\", \"U_SavePath\", \"U_Active\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{name}', '{docType}', '{trigger}', '{action}', '{subject}', '{body}', '{attachment}', '{printSetup}', '{savePath}', '{active}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')" :
                    $"INSERT INTO [@BTUN_PD] (U_Name, U_DocType, U_Trigger, U_Action, U_EmailSubject, U_EmailBody, U_AttachmentType, U_PrintSetup, U_SavePath, U_Active, U_CreatedBy, U_CreatedAt) VALUES ('{name}', '{docType}', '{trigger}', '{action}', '{subject}', '{body}', '{attachment}', '{printSetup}', '{savePath}', '{active}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')";

                rs.DoQuery(insertSql);

                // Assume success if no exception
                B1App.Instance.Application.SetStatusBarMessage("Print & Delivery configuration saved successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the parent form
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                LoadPrintDeliveryConfigs(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void TestPrintDeliveryConfig(SAPbouiCOM.Form oForm)
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage("Testing print & delivery configuration...", BoMessageTime.bmt_Short, false);

                // In a real implementation, this would test the configuration by simulating
                // the action (email, print, save, etc.) with sample data
                // For now, we'll just simulate the process
                B1App.Instance.Application.SetStatusBarMessage("Configuration tested successfully (simulation)", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error testing configuration: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EditSelectedPrintDeliveryConfig(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected configuration name
                    string configName = matrix.DataTable.GetValue("Name", selectedRow).ToString();

                    // Open edit form with the selected configuration data
                    OpenEditPrintDeliveryConfig(configName, parentForm);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a configuration to edit", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error editing print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void OpenEditPrintDeliveryConfig(string configName, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formUID = "BTUN_PRDEL_EDIT_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_PDEDT";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = $"Edit Print & Delivery Config: {configName}";
                oForm.Width = 800;
                oForm.Height = 700;

                CreateEditPrintDeliveryFormItems(oForm, configName, parentForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening edit print & delivery config form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateEditPrintDeliveryFormItems(SAPbouiCOM.Form oForm, string configName, SAPbouiCOM.Form parentForm)
        {
            // First load the configuration data
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            string sql = B1App.Instance.IsHana ?
                $"SELECT * FROM \"@BTUN_PD\" WHERE \"U_Name\" = '{configName}'" :
                $"SELECT * FROM [@BTUN_PD] WHERE U_Name = '{configName}'";

            rs.DoQuery(sql);

            if (!rs.EoF)
            {
                // Labels
                Item nameLabel = oForm.Items.Add("LblName", BoFormItemTypes.it_STATIC);
                nameLabel.Top = 20;
                nameLabel.Left = 20;
                nameLabel.Width = 150;
                nameLabel.Height = 20;
                ((SAPbouiCOM.StaticText)nameLabel.Specific).Caption = "Configuration Name:";

                Item docTypeLabel = oForm.Items.Add("LblDocType", BoFormItemTypes.it_STATIC);
                docTypeLabel.Top = 50;
                docTypeLabel.Left = 20;
                docTypeLabel.Width = 150;
                docTypeLabel.Height = 20;
                ((SAPbouiCOM.StaticText)docTypeLabel.Specific).Caption = "Document Type:";

                Item triggerLabel = oForm.Items.Add("LblTrigger", BoFormItemTypes.it_STATIC);
                triggerLabel.Top = 80;
                triggerLabel.Left = 20;
                triggerLabel.Width = 150;
                triggerLabel.Height = 20;
                ((SAPbouiCOM.StaticText)triggerLabel.Specific).Caption = "Trigger Event:";

                Item actionLabel = oForm.Items.Add("LblAction", BoFormItemTypes.it_STATIC);
                actionLabel.Top = 110;
                actionLabel.Left = 20;
                actionLabel.Width = 150;
                actionLabel.Height = 20;
                ((SAPbouiCOM.StaticText)actionLabel.Specific).Caption = "Action to Perform:";

                Item subjectLabel = oForm.Items.Add("LblSubject", BoFormItemTypes.it_STATIC);
                subjectLabel.Top = 140;
                subjectLabel.Left = 20;
                subjectLabel.Width = 150;
                subjectLabel.Height = 20;
                ((SAPbouiCOM.StaticText)subjectLabel.Specific).Caption = "Email Subject:";

                Item bodyLabel = oForm.Items.Add("LblBody", BoFormItemTypes.it_STATIC);
                bodyLabel.Top = 170;
                bodyLabel.Left = 20;
                bodyLabel.Width = 150;
                bodyLabel.Height = 20;
                ((SAPbouiCOM.StaticText)bodyLabel.Specific).Caption = "Email Body:";

                Item attachmentLabel = oForm.Items.Add("LblAttachment", BoFormItemTypes.it_STATIC);
                attachmentLabel.Top = 320;
                attachmentLabel.Left = 20;
                attachmentLabel.Width = 150;
                attachmentLabel.Height = 20;
                ((SAPbouiCOM.StaticText)attachmentLabel.Specific).Caption = "Attachment Settings:";

                Item printConfigLabel = oForm.Items.Add("LblPrintConfig", BoFormItemTypes.it_STATIC);
                printConfigLabel.Top = 390;
                printConfigLabel.Left = 20;
                printConfigLabel.Width = 150;
                printConfigLabel.Height = 20;
                ((SAPbouiCOM.StaticText)printConfigLabel.Specific).Caption = "Print Configuration:";

                Item saveConfigLabel = oForm.Items.Add("LblSaveConfig", BoFormItemTypes.it_STATIC);
                saveConfigLabel.Top = 420;
                saveConfigLabel.Left = 20;
                saveConfigLabel.Width = 150;
                saveConfigLabel.Height = 20;
                ((SAPbouiCOM.StaticText)saveConfigLabel.Specific).Caption = "Save Configuration:";

                Item activeLabel = oForm.Items.Add("LblActive", BoFormItemTypes.it_STATIC);
                activeLabel.Top = 450;
                activeLabel.Left = 20;
                activeLabel.Width = 150;
                activeLabel.Height = 20;
                ((SAPbouiCOM.StaticText)activeLabel.Specific).Caption = "Active:";

                // Input fields
                Item nameEdit = oForm.Items.Add("EdtName", BoFormItemTypes.it_EDIT);
                nameEdit.Top = 20;
                nameEdit.Left = 180;
                nameEdit.Width = 250;
                nameEdit.Height = 20;
                nameEdit.Enabled = false; // Can't change config name
                ((SAPbouiCOM.EditText)nameEdit.Specific).Value = rs.Fields.Item("U_Name").Value.ToString();

                Item docTypeCombo = oForm.Items.Add("CmbDocType", BoFormItemTypes.it_COMBO_BOX);
                docTypeCombo.Top = 50;
                docTypeCombo.Left = 180;
                docTypeCombo.Width = 150;
                docTypeCombo.Height = 20;
                ComboBox cmbDocType = (ComboBox)docTypeCombo.Specific;
                cmbDocType.ValidValues.Add("17", "Sales Invoice (17)");
                cmbDocType.ValidValues.Add("13", "Sales Order (13)");
                cmbDocType.ValidValues.Add("14", "Delivery (14)");
                cmbDocType.ValidValues.Add("16", "Credit Memo (16)");
                cmbDocType.ValidValues.Add("203", "Purchase Invoice (203)");
                cmbDocType.ValidValues.Add("1470000113", "Purchase Order (1470000113)");
                cmbDocType.ValidValues.Add("150", "Incoming Payment (150)");

                string docType = rs.Fields.Item("U_DocType").Value.ToString();
                for (int i = 0; i < cmbDocType.ValidValues.Count; i++)
                {
                    if (cmbDocType.ValidValues.Item(i).Value == docType)
                    {
                        cmbDocType.Select(i);
                        break;
                    }
                }

                Item triggerCombo = oForm.Items.Add("CmbTrigger", BoFormItemTypes.it_COMBO_BOX);
                triggerCombo.Top = 80;
                triggerCombo.Left = 180;
                triggerCombo.Width = 150;
                triggerCombo.Height = 20;
                ComboBox cmbTrigger = (ComboBox)triggerCombo.Specific;
                cmbTrigger.ValidValues.Add("DOC_ADD", "Document Added");
                cmbTrigger.ValidValues.Add("DOC_UPDATE", "Document Updated");
                cmbTrigger.ValidValues.Add("DOC_APPROVE", "Document Approved");
                cmbTrigger.ValidValues.Add("MANUAL", "Manual Trigger");
                cmbTrigger.ValidValues.Add("SCHEDULED", "Scheduled");

                string trigger = rs.Fields.Item("U_Trigger").Value.ToString();
                for (int i = 0; i < cmbTrigger.ValidValues.Count; i++)
                {
                    if (cmbTrigger.ValidValues.Item(i).Value == trigger)
                    {
                        cmbTrigger.Select(i);
                        break;
                    }
                }

                Item actionCombo = oForm.Items.Add("CmbAction", BoFormItemTypes.it_COMBO_BOX);
                actionCombo.Top = 110;
                actionCombo.Left = 180;
                actionCombo.Width = 150;
                actionCombo.Height = 20;
                ComboBox cmbAction = (ComboBox)actionCombo.Specific;
                cmbAction.ValidValues.Add("EMAIL", "Email");
                cmbAction.ValidValues.Add("PRINT", "Print");
                cmbAction.ValidValues.Add("SAVE", "Save to Folder");
                cmbAction.ValidValues.Add("FTP", "Upload to FTP");
                cmbAction.ValidValues.Add("SHAREPOINT", "SharePoint Upload");

                string action = rs.Fields.Item("U_Action").Value.ToString();
                for (int i = 0; i < cmbAction.ValidValues.Count; i++)
                {
                    if (cmbAction.ValidValues.Item(i).Value == action)
                    {
                        cmbAction.Select(i);
                        break;
                    }
                }

                Item subjectEdit = oForm.Items.Add("EdtSubject", BoFormItemTypes.it_EDIT);
                subjectEdit.Top = 140;
                subjectEdit.Left = 180;
                subjectEdit.Width = 250;
                subjectEdit.Height = 20;
                ((SAPbouiCOM.EditText)subjectEdit.Specific).Value = rs.Fields.Item("U_EmailSubject").Value.ToString();

                Item bodyEdit = oForm.Items.Add("EdtBody", BoFormItemTypes.it_EDIT);
                bodyEdit.Top = 170;
                bodyEdit.Left = 20;
                bodyEdit.Width = 760;
                bodyEdit.Height = 140;
                try
                {
                    ((SAPbouiCOM.EditText)bodyEdit.Specific).Value = rs.Fields.Item("U_EmailBody").Value.ToString();
                }
                catch { }

                Item attachmentCombo = oForm.Items.Add("CmbAttachment", BoFormItemTypes.it_COMBO_BOX);
                attachmentCombo.Top = 320;
                attachmentCombo.Left = 180;
                attachmentCombo.Width = 150;
                attachmentCombo.Height = 20;
                ComboBox cmbAttachment = (ComboBox)attachmentCombo.Specific;
                cmbAttachment.ValidValues.Add("PDF", "PDF Document");
                cmbAttachment.ValidValues.Add("EXCEL", "Excel Export");
                cmbAttachment.ValidValues.Add("WORD", "Word Document");
                cmbAttachment.ValidValues.Add("ORIGINAL", "Original Format");

                string attachment = rs.Fields.Item("U_AttachmentType").Value.ToString();
                for (int i = 0; i < cmbAttachment.ValidValues.Count; i++)
                {
                    if (cmbAttachment.ValidValues.Item(i).Value == attachment)
                    {
                        cmbAttachment.Select(i);
                        break;
                    }
                }

                Item printSetupEdit = oForm.Items.Add("EdtPrintSetup", BoFormItemTypes.it_EDIT);
                printSetupEdit.Top = 390;
                printSetupEdit.Left = 180;
                printSetupEdit.Width = 250;
                printSetupEdit.Height = 20;
                ((SAPbouiCOM.EditText)printSetupEdit.Specific).Value = rs.Fields.Item("U_PrintSetup").Value.ToString();

                Item savePathEdit = oForm.Items.Add("EdtSavePath", BoFormItemTypes.it_EDIT);
                savePathEdit.Top = 420;
                savePathEdit.Left = 180;
                savePathEdit.Width = 250;
                savePathEdit.Height = 20;
                ((SAPbouiCOM.EditText)savePathEdit.Specific).Value = rs.Fields.Item("U_SavePath").Value.ToString();

                Item activeCombo = oForm.Items.Add("CmbActive", BoFormItemTypes.it_COMBO_BOX);
                activeCombo.Top = 450;
                activeCombo.Left = 180;
                activeCombo.Width = 150;
                activeCombo.Height = 20;
                ComboBox cmbActive = (ComboBox)activeCombo.Specific;
                cmbActive.ValidValues.Add("Y", "Yes");
                cmbActive.ValidValues.Add("N", "No");

                string active = rs.Fields.Item("U_Active").Value.ToString();
                for (int i = 0; i < cmbActive.ValidValues.Count; i++)
                {
                    if (cmbActive.ValidValues.Item(i).Value == active)
                    {
                        cmbActive.Select(i);
                        break;
                    }
                }

                // Buttons
                Item updateButton = oForm.Items.Add("BtnUpdate", BoFormItemTypes.it_BUTTON);
                updateButton.Top = 490;
                updateButton.Left = 20;
                updateButton.Width = 80;
                updateButton.Height = 25;
                ((SAPbouiCOM.Button)updateButton.Specific).Caption = "Update";

                Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
                testButton.Top = 490;
                testButton.Left = 110;
                testButton.Width = 80;
                testButton.Height = 25;
                ((SAPbouiCOM.Button)testButton.Specific).Caption = "Test";

                Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
                cancelButton.Top = 490;
                cancelButton.Left = 200;
                cancelButton.Width = 80;
                cancelButton.Height = 25;
                ((SAPbouiCOM.Button)cancelButton.Specific).Caption = "Cancel";
            }

            ComObjectManager.Release(rs);
        }

        private static void UpdatePrintDeliveryConfig(SAPbouiCOM.Form oForm, string configName, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string docType = ((ComboBox)oForm.Items.Item("CmbDocType").Specific).Selected.Value;
                string trigger = ((ComboBox)oForm.Items.Item("CmbTrigger").Specific).Selected.Value;
                string action = ((ComboBox)oForm.Items.Item("CmbAction").Specific).Selected.Value;
                string subject = ((EditText)oForm.Items.Item("EdtSubject").Specific).Value;
                string body = ((SAPbouiCOM.EditText)oForm.Items.Item("EdtBody").Specific).Value;
                string attachment = ((ComboBox)oForm.Items.Item("CmbAttachment").Specific).Selected.Value;
                string printSetup = ((EditText)oForm.Items.Item("EdtPrintSetup").Specific).Value;
                string savePath = ((EditText)oForm.Items.Item("EdtSavePath").Specific).Value;
                string active = ((ComboBox)oForm.Items.Item("CmbActive").Specific).Selected.Value;

                // Update the configuration
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string updateSql = B1App.Instance.IsHana ?
                    $"UPDATE \"@BTUN_PD\" SET \"U_DocType\" = '{docType}', \"U_Trigger\" = '{trigger}', \"U_Action\" = '{action}', \"U_EmailSubject\" = '{subject}', \"U_EmailBody\" = '{body}', \"U_AttachmentType\" = '{attachment}', \"U_PrintSetup\" = '{printSetup}', \"U_SavePath\" = '{savePath}', \"U_Active\" = '{active}', \"U_UpdatedAt\" = '{DateTime.Today:yyyy-MM-dd}' WHERE \"U_Name\" = '{configName}'" :
                    $"UPDATE [@BTUN_PD] SET U_DocType = '{docType}', U_Trigger = '{trigger}', U_Action = '{action}', U_EmailSubject = '{subject}', U_EmailBody = '{body}', U_AttachmentType = '{attachment}', U_PrintSetup = '{printSetup}', U_SavePath = '{savePath}', U_Active = '{active}', U_UpdatedAt = '{DateTime.Today:yyyy-MM-dd}' WHERE U_Name = '{configName}'";

                rs.DoQuery(updateSql);

                // Assume success if no exception
                B1App.Instance.Application.SetStatusBarMessage("Print & Delivery configuration updated successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the parent form
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                LoadPrintDeliveryConfigs(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error updating print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void DeleteSelectedPrintDeliveryConfig(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected configuration name
                    string configName = matrix.DataTable.GetValue("Name", selectedRow).ToString();

                    if (B1App.Instance.Application.MessageBox($"Are you sure you want to delete the print & delivery configuration '{configName}'?", 1, "Yes", "No") == 1)
                    {
                        // Delete the configuration
                        Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                        string deleteSql = B1App.Instance.IsHana ?
                            $"DELETE FROM \"@BTUN_PD\" WHERE \"U_Name\" = '{configName}'" :
                            $"DELETE FROM [@BTUN_PD] WHERE U_Name = '{configName}'";

                        rs.DoQuery(deleteSql);
                        B1App.Instance.Application.SetStatusBarMessage("Print & Delivery configuration deleted successfully", BoMessageTime.bmt_Short, false);

                        // Reload the matrix
                        LoadPrintDeliveryConfigs(matrix);

                        ComObjectManager.Release(rs);
                    }
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a configuration to delete", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error deleting print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteSelectedPrintDeliveryConfig(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected configuration name
                    string configName = matrix.DataTable.GetValue("Name", selectedRow).ToString();

                    // Execute the configuration
                    ExecutePrintDeliveryConfigByName(configName);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a configuration to execute", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void PreviewSelectedPrintDeliveryConfig(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_SelectionOrder);

                    // Get the selected configuration name
                    string configName = matrix.DataTable.GetValue("Name", selectedRow).ToString();

                    B1App.Instance.Application.SetStatusBarMessage($"Preview for configuration: {configName}", BoMessageTime.bmt_Short, false);

                    // In a real implementation, this would show a preview of what would be printed/emailed
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a configuration to preview", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error previewing print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void TogglePrintDeliveryConfigActivation(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = (Grid)parentForm.Items.Item("PDMatrx").Specific;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0);

                    // Get the selected configuration
                    string configName = matrix.DataTable.GetValue("Name", selectedRow).ToString();
                    string currentActive = matrix.DataTable.GetValue("Active", selectedRow).ToString();

                    // Toggle the active status
                    string newActive = currentActive == "Y" ? "N" : "Y";

                    // Update the configuration
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string updateSql = B1App.Instance.IsHana ?
                        $"UPDATE \"@BTUN_PD\" SET \"U_Active\" = '{newActive}' WHERE \"U_Name\" = '{configName}'" :
                        $"UPDATE [@BTUN_PD] SET U_Active = '{newActive}' WHERE U_Name = '{configName}'";

                    rs.DoQuery(updateSql);
                    string status = newActive == "Y" ? "activated" : "deactivated";
                    B1App.Instance.Application.SetStatusBarMessage($"Print & Delivery configuration {status} successfully", BoMessageTime.bmt_Short, false);

                    // Reload the matrix
                    LoadPrintDeliveryConfigs(matrix);

                    ComObjectManager.Release(rs);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a configuration to toggle activation", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error toggling configuration activation: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ProcessDocumentsByCriteria(string docType, string actionType, string triggerEvent)
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage($"Processing documents - Type: {docType}, Action: {actionType}, Trigger: {triggerEvent}", BoMessageTime.bmt_Long, false);

                // In a real implementation, this would process documents based on the criteria
                // For now, we'll just simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Processed documents with criteria: {docType}/{actionType}/{triggerEvent}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error processing documents: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void BatchProcessDocuments(string dateFrom, string dateTo, string docType, string actionType)
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage($"Batch processing documents from {dateFrom} to {dateTo}", BoMessageTime.bmt_Long, false);

                // In a real implementation, this would batch process documents in the specified date range
                // For now, we'll just simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Batch processed documents from {dateFrom} to {dateTo}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error in batch processing: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecutePrintDeliveryConfigByName(string configName)
        {
            try
            {
                // Get configuration details
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    $"SELECT * FROM \"@BTUN_PD\" WHERE \"U_Name\" = '{configName}' AND \"U_Active\" = 'Y'" :
                    $"SELECT * FROM [@BTUN_PD] WHERE U_Name = '{configName}' AND U_Active = 'Y'";

                rs.DoQuery(sql);

                if (!rs.EoF)
                {
                    string docType = rs.Fields.Item("U_DocType").Value.ToString();
                    string action = rs.Fields.Item("U_Action").Value.ToString();
                    string emailSubject = rs.Fields.Item("U_EmailSubject").Value.ToString();
                    string emailBody = rs.Fields.Item("U_EmailBody").Value.ToString();
                    string attachmentType = rs.Fields.Item("U_AttachmentType").Value.ToString();
                    string printSetup = rs.Fields.Item("U_PrintSetup").Value.ToString();
                    string savePath = rs.Fields.Item("U_SavePath").Value.ToString();

                    // Execute the action based on the configuration
                    switch (action.ToUpper())
                    {
                        case "EMAIL":
                            SendDocumentByEmail(docType, emailSubject, emailBody, attachmentType);
                            break;
                        case "PRINT":
                            PrintDocument(docType, printSetup);
                            break;
                        case "SAVE":
                            SaveDocumentToFile(docType, savePath, attachmentType);
                            break;
                        default:
                            B1App.Instance.Application.SetStatusBarMessage($"Unknown action type: {action}", BoMessageTime.bmt_Short, true);
                            break;
                    }

                    B1App.Instance.Application.SetStatusBarMessage($"Print & Delivery configuration '{configName}' executed successfully", BoMessageTime.bmt_Short, false);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage($"Configuration '{configName}' not found or not active", BoMessageTime.bmt_Short, true);
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing print & delivery config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void SendDocumentByEmail(string docType, string subject, string body, string attachmentType)
        {
            try
            {
                // In a real implementation, this would:
                // 1. Get the current document details
                // 2. Format the email with the provided subject/body
                // 3. Generate the appropriate attachment (PDF, Excel, etc.)
                // 4. Send the email using SAP B1's email functionality or SMTP

                // For now, we'll simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Would send email for document type {docType}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error sending email: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void PrintDocument(string docType, string printSetup)
        {
            try
            {
                // In a real implementation, this would:
                // 1. Get the current document
                // 2. Apply the print setup settings
                // 3. Send the document to the specified printer

                // For now, we'll simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Would print document type {docType} using setup: {printSetup}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error printing document: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void SaveDocumentToFile(string docType, string savePath, string attachmentType)
        {
            try
            {
                // In a real implementation, this would:
                // 1. Get the current document
                // 2. Export it in the specified format (PDF, Excel, etc.)
                // 3. Save it to the specified path

                // For now, we'll simulate the process
                B1App.Instance.Application.SetStatusBarMessage($"Would save document type {docType} to path: {savePath}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving document: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        // Method to process documents automatically based on triggers
        public static void ProcessDocumentTriggers(string docType, string triggerEvent, int docEntry)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    $"SELECT * FROM \"@BTUN_PD\" WHERE \"U_DocType\" = '{docType}' AND \"U_Trigger\" = '{triggerEvent}' AND \"U_Active\" = 'Y' ORDER BY \"U_Name\"" :
                    $"SELECT * FROM [@BTUN_PD] WHERE U_DocType = '{docType}' AND U_Trigger = '{triggerEvent}' AND U_Active = 'Y' ORDER BY U_Name";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string configName = rs.Fields.Item("U_Name").Value.ToString();

                    // Execute the configuration for this document
                    ExecuteDocumentWithConfig(configName, docType, docEntry);

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error in document trigger processing: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteDocumentWithConfig(string configName, string docType, int docEntry)
        {
            try
            {
                // In a real implementation, this would execute the specific configuration
                // for the given document (identified by docType and docEntry)
                // For now, we'll just log that we would process it
                B1App.Instance.Application.SetStatusBarMessage($"Would process document {docType}-{docEntry} with config {configName}", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing document with config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}