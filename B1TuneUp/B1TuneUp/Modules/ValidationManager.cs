using B1TuneUp.Core;
using B1TuneUp.Modules;
using B1TuneUp.Utils;
using SAPbobsCOM;
using SAPbouiCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B1TuneUp.Modules
{
    public class ValidationManager
    {
        public static void OpenValidationForm()
        {
            try
            {
                string formUID = "BTUN_VALID_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_VALID";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp - Advanced Validation System";
                oForm.Width = 1000;
                oForm.Height = 700;

                // Create form items
                CreateValidationFormItems(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening Validation form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateValidationFormItems(SAPbouiCOM.Form oForm)
        {
            // Create a matrix to show existing validation rules
            Item matrixItem = oForm.Items.Add("ValidationMatrix", BoFormItemTypes.it_GRID);
            matrixItem.Top = 10;
            matrixItem.Left = 10;
            matrixItem.Width = 970;
            matrixItem.Height = 350;

            SAPbouiCOM.Grid matrix = SapUiSafe.TryGetSpecific<SAPbouiCOM.Grid>(matrixItem);

            // Add columns to the matrix via the grid datatable
            matrix.DataTable.Columns.Add("FormType", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("ItemName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Event", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Condition", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Action", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Active", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Severity", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);
            matrix.DataTable.Columns.Add("Message", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric);

            // Set column titles
            try
            {
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("FormType")).TitleObject.Caption = "Form Type";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("ItemName")).TitleObject.Caption = "Item/Field";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Event")).TitleObject.Caption = "Event";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Condition")).TitleObject.Caption = "Condition";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Action")).TitleObject.Caption = "Action";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Active")).TitleObject.Caption = "Active";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Severity")).TitleObject.Caption = "Severity";
                ((SAPbouiCOM.EditTextColumn)matrix.Columns.Item("Message")).TitleObject.Caption = "Message";
            }
            catch { }

            // Create buttons
            Item addButton = oForm.Items.Add("BtnAdd", BoFormItemTypes.it_BUTTON);
            addButton.Top = 370;
            addButton.Left = 10;
            addButton.Width = 80;
            addButton.Height = 25;
            SapUiSafe.TrySetCaption(addButton, "Add");

            Item editButton = oForm.Items.Add("BtnEdit", BoFormItemTypes.it_BUTTON);
            editButton.Top = 370;
            editButton.Left = 100;
            editButton.Width = 80;
            editButton.Height = 25;
            SapUiSafe.TrySetCaption(editButton, "Edit");

            Item deleteButton = oForm.Items.Add("BtnDelete", BoFormItemTypes.it_BUTTON);
            deleteButton.Top = 370;
            deleteButton.Left = 190;
            deleteButton.Width = 80;
            deleteButton.Height = 25;
            SapUiSafe.TrySetCaption(deleteButton, "Delete");

            Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
            testButton.Top = 370;
            testButton.Left = 280;
            testButton.Width = 80;
            testButton.Height = 25;
            SapUiSafe.TrySetCaption(testButton, "Test");

            Item activateButton = oForm.Items.Add("BtnActivate", BoFormItemTypes.it_BUTTON);
            activateButton.Top = 370;
            activateButton.Left = 370;
            activateButton.Width = 80;
            activateButton.Height = 25;
            SapUiSafe.TrySetCaption(activateButton, "Activate");

            Item closeButton = oForm.Items.Add("BtnClose", BoFormItemTypes.it_BUTTON);
            closeButton.Top = 370;
            closeButton.Left = 900;
            closeButton.Width = 80;
            closeButton.Height = 25;
            SapUiSafe.TrySetCaption(closeButton, "Close");

            // Validation execution options
            Item execLabel = oForm.Items.Add("LblExec", BoFormItemTypes.it_STATIC);
            execLabel.Top = 410;
            execLabel.Left = 10;
            execLabel.Width = 200;
            execLabel.Height = 20;
            SapUiSafe.TrySetCaption(execLabel, "Validation Execution:");

            Item execTypeLabel = oForm.Items.Add("LblExecType", BoFormItemTypes.it_STATIC);
            execTypeLabel.Top = 440;
            execTypeLabel.Left = 10;
            execTypeLabel.Width = 150;
            execTypeLabel.Height = 20;
            SapUiSafe.TrySetCaption(execTypeLabel, "Execution Type:");

            Item execTypeCombo = oForm.Items.Add("CmbExecType", BoFormItemTypes.it_COMBO_BOX);
            execTypeCombo.Top = 440;
            execTypeCombo.Left = 170;
            execTypeCombo.Width = 150;
            execTypeCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbExecType = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(execTypeCombo);
            cmbExecType.ValidValues.Add("SAVE", "On Save");
            cmbExecType.ValidValues.Add("CHANGE", "On Change");
            cmbExecType.ValidValues.Add("CLICK", "On Click");
            cmbExecType.ValidValues.Add("LOAD", "On Load");
            cmbExecType.Select(0); // Default to On Save

            Item severityLabel = oForm.Items.Add("LblSeverity", BoFormItemTypes.it_STATIC);
            severityLabel.Top = 440;
            severityLabel.Left = 340;
            severityLabel.Width = 100;
            severityLabel.Height = 20;
            SapUiSafe.TrySetCaption(severityLabel, "Severity:");

            Item severityCombo = oForm.Items.Add("CmbSeverity", BoFormItemTypes.it_COMBO_BOX);
            severityCombo.Top = 440;
            severityCombo.Left = 450;
            severityCombo.Width = 120;
            severityCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbSeverity = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(severityCombo);
            cmbSeverity.ValidValues.Add("ERROR", "Error (Block)");
            cmbSeverity.ValidValues.Add("WARNING", "Warning (Allow Continue)");
            cmbSeverity.ValidValues.Add("INFO", "Information");
            cmbSeverity.Select(0); // Default to Error

            Item validationButton = oForm.Items.Add("BtnValidate", BoFormItemTypes.it_BUTTON);
            validationButton.Top = 435;
            validationButton.Left = 600;
            validationButton.Width = 100;
            validationButton.Height = 25;
            SapUiSafe.TrySetCaption(validationButton, "Validate Now");

            // User-specific validation options
            Item userLabel = oForm.Items.Add("LblUser", BoFormItemTypes.it_STATIC);
            userLabel.Top = 480;
            userLabel.Left = 10;
            userLabel.Width = 200;
            userLabel.Height = 20;
            SapUiSafe.TrySetCaption(userLabel, "User-Specific Validation:");

            Item userSpecLabel = oForm.Items.Add("LblUserSpec", BoFormItemTypes.it_STATIC);
            userSpecLabel.Top = 510;
            userSpecLabel.Left = 10;
            userSpecLabel.Width = 150;
            userSpecLabel.Height = 20;
            SapUiSafe.TrySetCaption(userSpecLabel, "Apply to User:");

            Item userCombo = oForm.Items.Add("CmbUser", BoFormItemTypes.it_COMBO_BOX);
            userCombo.Top = 510;
            userCombo.Left = 170;
            userCombo.Width = 150;
            userCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbUser = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(userCombo);
            LoadUsersIntoCombo(cmbUser);
            cmbUser.ValidValues.Add("", "All Users");
            cmbUser.Select(0);

            Item groupLabel = oForm.Items.Add("LblGroup", BoFormItemTypes.it_STATIC);
            groupLabel.Top = 510;
            groupLabel.Left = 340;
            groupLabel.Width = 100;
            groupLabel.Height = 20;
            SapUiSafe.TrySetCaption(groupLabel, "User Group:");

            Item groupCombo = oForm.Items.Add("CmbGroup", BoFormItemTypes.it_COMBO_BOX);
            groupCombo.Top = 510;
            groupCombo.Left = 450;
            groupCombo.Width = 150;
            groupCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbGroup = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(groupCombo);
            LoadUserGroupsIntoCombo(cmbGroup);
            cmbGroup.ValidValues.Add("", "All Groups");
            cmbGroup.Select(0);

            // Load existing validation rules
            LoadValidationRules(matrix);
        }

        private static void LoadValidationRules(Grid matrix)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_FormType\", \"U_ItemName\", \"U_Event\", \"U_Condition\", \"U_Action\", \"U_Active\", \"U_Severity\", \"U_Message\", \"U_Block\", \"U_Sequence\" FROM \"@BTUN_VAL\" ORDER BY \"U_FormType\", \"U_Sequence\", \"U_ItemName\"" :
                    "SELECT U_FormType, U_ItemName, U_Event, U_Condition, U_Action, U_Active, U_Severity, U_Message, U_Block, U_Sequence FROM [@BTUN_VAL] ORDER BY U_FormType, U_Sequence, U_ItemName";

                rs.DoQuery(sql);

                matrix.DataTable.Rows.Clear();

                while (!rs.EoF)
                {
                    matrix.DataTable.Rows.Add();
                    int rowIndex = matrix.DataTable.Rows.Count - 1;

                    matrix.DataTable.SetValue("FormType", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_FormType"));
                    matrix.DataTable.SetValue("ItemName", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_ItemName"));
                    matrix.DataTable.SetValue("Event", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Event"));
                    matrix.DataTable.SetValue("Condition", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Condition"));
                    matrix.DataTable.SetValue("Action", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Action"));
                    matrix.DataTable.SetValue("Active", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Active"));
                    try { matrix.DataTable.SetValue("Severity", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Severity")); } catch { }
                    try { matrix.DataTable.SetValue("Message", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Message")); } catch { }

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading validation rules: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadUsersIntoCombo(SAPbouiCOM.ComboBox combo)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"UserCode\", \"UserName\" FROM OUSR ORDER BY \"UserName\"" :
                    "SELECT UserCode, UserName FROM OUSR ORDER BY UserName";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string userCode = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "UserCode");
                    string userName = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "UserName");
                    combo.ValidValues.Add(userCode, $"{userName} ({userCode})");
                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading users: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadUserGroupsIntoCombo(SAPbouiCOM.ComboBox combo)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"GroupCode\", \"GroupName\" FROM OUGP ORDER BY \"GroupName\"" :
                    "SELECT GroupCode, GroupName FROM OUGP ORDER BY GroupName";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string groupCode = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "GroupCode");
                    string groupName = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "GroupName");
                    combo.ValidValues.Add(groupCode, $"{groupName} ({groupCode})");
                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading user groups: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewValidationRule(SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formUID = "BTUN_VALID_NEW_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_VALNEW";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "Create New Validation Rule";
                oForm.Width = 800;
                oForm.Height = 600;

                CreateNewValidationFormItems(oForm, parentForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creating new validation rule form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateNewValidationFormItems(SAPbouiCOM.Form oForm, SAPbouiCOM.Form parentForm)
        {
            // Labels
            Item formTypeLabel = oForm.Items.Add("LblFormType", BoFormItemTypes.it_STATIC);
            formTypeLabel.Top = 20;
            formTypeLabel.Left = 20;
            formTypeLabel.Width = 100;
            formTypeLabel.Height = 20;
            SapUiSafe.TrySetCaption(formTypeLabel, "Form Type:");

            Item itemNameLabel = oForm.Items.Add("LblItemName", BoFormItemTypes.it_STATIC);
            itemNameLabel.Top = 50;
            itemNameLabel.Left = 20;
            itemNameLabel.Width = 100;
            itemNameLabel.Height = 20;
            SapUiSafe.TrySetCaption(itemNameLabel, "Item/Field Name:");

            Item eventLabel = oForm.Items.Add("LblEvent", BoFormItemTypes.it_STATIC);
            eventLabel.Top = 80;
            eventLabel.Left = 20;
            eventLabel.Width = 100;
            eventLabel.Height = 20;
            SapUiSafe.TrySetCaption(eventLabel, "Event:");

            Item conditionLabel = oForm.Items.Add("LblCondition", BoFormItemTypes.it_STATIC);
            conditionLabel.Top = 110;
            conditionLabel.Left = 20;
            conditionLabel.Width = 100;
            conditionLabel.Height = 20;
            SapUiSafe.TrySetCaption(conditionLabel, "Condition (SQL):");

            Item actionLabel = oForm.Items.Add("LblAction", BoFormItemTypes.it_STATIC);
            actionLabel.Top = 200;
            actionLabel.Left = 20;
            actionLabel.Width = 100;
            actionLabel.Height = 20;
            SapUiSafe.TrySetCaption(actionLabel, "Action (Macro):");

            Item severityLabel = oForm.Items.Add("LblSeverity", BoFormItemTypes.it_STATIC);
            severityLabel.Top = 360;
            severityLabel.Left = 20;
            severityLabel.Width = 100;
            severityLabel.Height = 20;
            SapUiSafe.TrySetCaption(severityLabel, "Severity:");

            Item activeLabel = oForm.Items.Add("LblActive", BoFormItemTypes.it_STATIC);
            activeLabel.Top = 390;
            activeLabel.Left = 20;
            activeLabel.Width = 100;
            activeLabel.Height = 20;
            SapUiSafe.TrySetCaption(activeLabel, "Active:");

            // Input fields
            Item formTypeEdit = oForm.Items.Add("EdtFormType", BoFormItemTypes.it_EDIT);
            formTypeEdit.Top = 20;
            formTypeEdit.Left = 130;
            formTypeEdit.Width = 150;
            formTypeEdit.Height = 20;
            // Set to current form type if available
            try
            {
                SapUiSafe.TrySetEditValue(formTypeEdit, SapUiSafe.TryGetActiveForm()?.TypeEx ?? string.Empty);
            }
            catch { }

            Item itemNameEdit = oForm.Items.Add("EdtItemName", BoFormItemTypes.it_EDIT);
            itemNameEdit.Top = 50;
            itemNameEdit.Left = 130;
            itemNameEdit.Width = 150;
            itemNameEdit.Height = 20;

            Item eventCombo = oForm.Items.Add("CmbEvent", BoFormItemTypes.it_COMBO_BOX);
            eventCombo.Top = 80;
            eventCombo.Left = 130;
            eventCombo.Width = 150;
            eventCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbEvent = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(eventCombo);
            cmbEvent.ValidValues.Add("DATA_ADD_BEFORE", "Before Data Add");
            cmbEvent.ValidValues.Add("DATA_UPDATE_BEFORE", "Before Data Update");
            cmbEvent.ValidValues.Add("ITEM_PRESSED", "Item Pressed");
            cmbEvent.ValidValues.Add("FORM_LOAD", "Form Load");
            cmbEvent.ValidValues.Add("COMBO_SELECT", "Combo Select");
            cmbEvent.ValidValues.Add("EDIT_VALIDATE", "Edit Validate");
            cmbEvent.Select(0);

            Item conditionEdit = oForm.Items.Add("EdtCondition", BoFormItemTypes.it_EDIT);
            conditionEdit.Top = 110;
            conditionEdit.Left = 20;
            conditionEdit.Width = 760;
            conditionEdit.Height = 80;
            // Some SDKs don't expose multiline/height properties on EditText Specific; skip

            Item actionEdit = oForm.Items.Add("EdtAction", BoFormItemTypes.it_EDIT);
            actionEdit.Top = 200;
            actionEdit.Left = 20;
            actionEdit.Width = 760;
            actionEdit.Height = 150;
            // Skip multiline/height adjustments for action edit control

            Item severityCombo = oForm.Items.Add("CmbSeverity", BoFormItemTypes.it_COMBO_BOX);
            severityCombo.Top = 360;
            severityCombo.Left = 130;
            severityCombo.Width = 150;
            severityCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbSeverity = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(severityCombo);
            cmbSeverity.ValidValues.Add("ERROR", "Error (Block)");
            cmbSeverity.ValidValues.Add("WARNING", "Warning (Allow Continue)");
            cmbSeverity.ValidValues.Add("INFO", "Information");
            cmbSeverity.Select(0);

            Item activeCombo = oForm.Items.Add("CmbActive", BoFormItemTypes.it_COMBO_BOX);
            activeCombo.Top = 390;
            activeCombo.Left = 130;
            activeCombo.Width = 150;
            activeCombo.Height = 20;
            ComboBox cmbActive = SapUiSafe.TryGetSpecific<ComboBox>(activeCombo);
            cmbActive.ValidValues.Add("Y", "Yes");
            cmbActive.ValidValues.Add("N", "No");
            cmbActive.Select(0);

            // Buttons
            Item saveButton = oForm.Items.Add("BtnSave", BoFormItemTypes.it_BUTTON);
            saveButton.Top = 430;
            saveButton.Left = 20;
            saveButton.Width = 80;
            saveButton.Height = 25;
            SapUiSafe.TrySetCaption(saveButton, "Save");

            Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
            testButton.Top = 430;
            testButton.Left = 110;
            testButton.Width = 80;
            testButton.Height = 25;
            SapUiSafe.TrySetCaption(testButton, "Test");

            Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
            cancelButton.Top = 430;
            cancelButton.Left = 200;
            cancelButton.Width = 80;
            cancelButton.Height = 25;
            SapUiSafe.TrySetCaption(cancelButton, "Cancel");
        }

        private static void SaveValidationRule(SAPbouiCOM.Form oForm, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formType = GetEditValue(oForm, "EdtFormType");
                string itemName = GetEditValue(oForm, "EdtItemName");
                string eventName = GetComboValue(oForm, "CmbEvent");
                string condition = GetEditValue(oForm, "EdtCondition");
                string action = GetEditValue(oForm, "EdtAction");
                string severity = GetComboValue(oForm, "CmbSeverity");
                string active = GetComboValue(oForm, "CmbActive");

                // Validate required fields
                if (string.IsNullOrEmpty(formType) || string.IsNullOrEmpty(condition))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Form Type and Condition are required", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Save to user table
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string insertSql = B1App.Instance.IsHana ?
                    $"INSERT INTO \"@BTUN_VAL\" (\"U_FormType\", \"U_ItemName\", \"U_Event\", \"U_Condition\", \"U_Action\", \"U_Severity\", \"U_Active\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{formType}', '{itemName}', '{eventName}', '{condition}', '{action}', '{severity}', '{active}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')" :
                    $"INSERT INTO [@BTUN_VAL] (U_FormType, U_ItemName, U_Event, U_Condition, U_Action, U_Severity, U_Active, U_CreatedBy, U_CreatedAt) VALUES ('{formType}', '{itemName}', '{eventName}', '{condition}', '{action}', '{severity}', '{active}', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')";

                rs.DoQuery(insertSql);
                B1App.Instance.Application.SetStatusBarMessage("Validation rule saved successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the parent form
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ValidationMatrix");
                LoadValidationRules(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving validation rule: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void TestValidationRule(SAPbouiCOM.Form oForm)
        {
            try
            {
                string condition = GetEditValue(oForm, "EdtCondition");

                if (string.IsNullOrEmpty(condition))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please enter a condition to test", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Test the condition by executing it
                bool result = EvaluateCondition(condition);

                string message = result ? "Condition evaluated to TRUE" : "Condition evaluated to FALSE";
                B1App.Instance.Application.SetStatusBarMessage(message, BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error testing condition: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void EditSelectedValidationRule(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ValidationMatrix");
                if (matrix == null) return;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0);

                    // Get the selected validation rule
                    string formType = matrix.DataTable.GetValue("FormType", selectedRow).ToString();
                    string itemName = matrix.DataTable.GetValue("ItemName", selectedRow).ToString();

                    // Open edit form with the selected validation data
                    OpenEditValidationForm(formType, itemName, parentForm);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a validation rule to edit", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error editing validation rule: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void OpenEditValidationForm(string formType, string itemName, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string formUID = "BTUN_VALID_EDIT_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_VALEDT";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = $"Edit Validation Rule: {formType} - {itemName}";
                oForm.Width = 800;
                oForm.Height = 600;

                CreateEditValidationFormItems(oForm, formType, itemName, parentForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening edit validation form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateEditValidationFormItems(SAPbouiCOM.Form oForm, string formType, string itemName, SAPbouiCOM.Form parentForm)
        {
            // First load the validation rule data
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            string sql = B1App.Instance.IsHana ?
                $"SELECT * FROM \"@BTUN_VAL\" WHERE \"U_FormType\" = '{formType}' AND \"U_ItemName\" = '{itemName}'" :
                $"SELECT * FROM [@BTUN_VAL] WHERE U_FormType = '{formType}' AND U_ItemName = '{itemName}'";

            rs.DoQuery(sql);

            if (!rs.EoF)
            {
                // Labels
                Item formTypeLabel = oForm.Items.Add("LblFormType", BoFormItemTypes.it_STATIC);
                formTypeLabel.Top = 20;
                formTypeLabel.Left = 20;
                formTypeLabel.Width = 100;
                formTypeLabel.Height = 20;
                SapUiSafe.TrySetCaption(formTypeLabel, "Form Type:");

                Item itemNameLabel = oForm.Items.Add("LblItemName", BoFormItemTypes.it_STATIC);
                itemNameLabel.Top = 50;
                itemNameLabel.Left = 20;
                itemNameLabel.Width = 100;
                itemNameLabel.Height = 20;
                SapUiSafe.TrySetCaption(itemNameLabel, "Item/Field Name:");

                Item eventLabel = oForm.Items.Add("LblEvent", BoFormItemTypes.it_STATIC);
                eventLabel.Top = 80;
                eventLabel.Left = 20;
                eventLabel.Width = 100;
                eventLabel.Height = 20;
                SapUiSafe.TrySetCaption(eventLabel, "Event:");

                Item conditionLabel = oForm.Items.Add("LblCondition", BoFormItemTypes.it_STATIC);
                conditionLabel.Top = 110;
                conditionLabel.Left = 20;
                conditionLabel.Width = 100;
                conditionLabel.Height = 20;
                SapUiSafe.TrySetCaption(conditionLabel, "Condition (SQL):");

                Item actionLabel = oForm.Items.Add("LblAction", BoFormItemTypes.it_STATIC);
                actionLabel.Top = 200;
                actionLabel.Left = 20;
                actionLabel.Width = 100;
                actionLabel.Height = 20;
                SapUiSafe.TrySetCaption(actionLabel, "Action (Macro):");

                Item severityLabel = oForm.Items.Add("LblSeverity", BoFormItemTypes.it_STATIC);
                severityLabel.Top = 360;
                severityLabel.Left = 20;
                severityLabel.Width = 100;
                severityLabel.Height = 20;
                SapUiSafe.TrySetCaption(severityLabel, "Severity:");

                Item activeLabel = oForm.Items.Add("LblActive", BoFormItemTypes.it_STATIC);
                activeLabel.Top = 390;
                activeLabel.Left = 20;
                activeLabel.Width = 100;
                activeLabel.Height = 20;
                SapUiSafe.TrySetCaption(activeLabel, "Active:");

                // Input fields
                Item formTypeEdit = oForm.Items.Add("EdtFormType", BoFormItemTypes.it_EDIT);
                formTypeEdit.Top = 20;
                formTypeEdit.Left = 130;
                formTypeEdit.Width = 150;
                formTypeEdit.Height = 20;
                formTypeEdit.Enabled = false; // Can't change form type
                SapUiSafe.TrySetEditValue(formTypeEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_FormType"));

                Item itemNameEdit = oForm.Items.Add("EdtItemName", BoFormItemTypes.it_EDIT);
                itemNameEdit.Top = 50;
                itemNameEdit.Left = 130;
                itemNameEdit.Width = 150;
                itemNameEdit.Height = 20;
                itemNameEdit.Enabled = false; // Can't change item name
                SapUiSafe.TrySetEditValue(itemNameEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_ItemName"));

                Item eventCombo = oForm.Items.Add("CmbEvent", BoFormItemTypes.it_COMBO_BOX);
                eventCombo.Top = 80;
                eventCombo.Left = 130;
                eventCombo.Width = 150;
                eventCombo.Height = 20;
                SAPbouiCOM.ComboBox cmbEvent = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(eventCombo);
                cmbEvent.ValidValues.Add("DATA_ADD_BEFORE", "Before Data Add");
                cmbEvent.ValidValues.Add("DATA_UPDATE_BEFORE", "Before Data Update");
                cmbEvent.ValidValues.Add("ITEM_PRESSED", "Item Pressed");
                cmbEvent.ValidValues.Add("FORM_LOAD", "Form Load");
                cmbEvent.ValidValues.Add("COMBO_SELECT", "Combo Select");
                cmbEvent.ValidValues.Add("EDIT_VALIDATE", "Edit Validate");

                string eventValue = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Event");
                for (int i = 0; i < cmbEvent.ValidValues.Count; i++)
                {
                    if (cmbEvent.ValidValues.Item(i).Value == eventValue)
                    {
                        cmbEvent.Select(i);
                        break;
                    }
                }

                Item conditionEdit = oForm.Items.Add("EdtCondition", BoFormItemTypes.it_EDIT);
                conditionEdit.Top = 110;
                conditionEdit.Left = 20;
                conditionEdit.Width = 760;
                conditionEdit.Height = 80;
                // Set condition value
                SapUiSafe.TrySetEditValue(conditionEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Condition"));

                Item actionEdit = oForm.Items.Add("EdtAction", BoFormItemTypes.it_EDIT);
                actionEdit.Top = 200;
                actionEdit.Left = 20;
                actionEdit.Width = 760;
                actionEdit.Height = 150;
                SapUiSafe.TrySetEditValue(actionEdit, B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Action"));

                Item severityCombo = oForm.Items.Add("CmbSeverity", BoFormItemTypes.it_COMBO_BOX);
                severityCombo.Top = 360;
                severityCombo.Left = 130;
                severityCombo.Width = 150;
                severityCombo.Height = 20;
                ComboBox cmbSeverity = SapUiSafe.TryGetSpecific<ComboBox>(severityCombo);
                cmbSeverity.ValidValues.Add("ERROR", "Error (Block)");
                cmbSeverity.ValidValues.Add("WARNING", "Warning (Allow Continue)");
                cmbSeverity.ValidValues.Add("INFO", "Information");

                string severityValue = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Severity");
                for (int i = 0; i < cmbSeverity.ValidValues.Count; i++)
                {
                    if (cmbSeverity.ValidValues.Item(i).Value == severityValue)
                    {
                        cmbSeverity.Select(i);
                        break;
                    }
                }

                Item activeCombo = oForm.Items.Add("CmbActive", BoFormItemTypes.it_COMBO_BOX);
                activeCombo.Top = 390;
                activeCombo.Left = 130;
                activeCombo.Width = 150;
                activeCombo.Height = 20;
                ComboBox cmbActive = SapUiSafe.TryGetSpecific<ComboBox>(activeCombo);
                cmbActive.ValidValues.Add("Y", "Yes");
                cmbActive.ValidValues.Add("N", "No");

                string activeValue = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Active");
                for (int i = 0; i < cmbActive.ValidValues.Count; i++)
                {
                    if (cmbActive.ValidValues.Item(i).Value == activeValue)
                    {
                        cmbActive.Select(i);
                        break;
                    }
                }

                // Buttons
                Item updateButton = oForm.Items.Add("BtnUpdate", BoFormItemTypes.it_BUTTON);
                updateButton.Top = 430;
                updateButton.Left = 20;
                updateButton.Width = 80;
                updateButton.Height = 25;
                SapUiSafe.TrySetCaption(updateButton, "Update");

                Item testButton = oForm.Items.Add("BtnTest", BoFormItemTypes.it_BUTTON);
                testButton.Top = 430;
                testButton.Left = 110;
                testButton.Width = 80;
                testButton.Height = 25;
                SapUiSafe.TrySetCaption(testButton, "Test");

                Item cancelButton = oForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
                cancelButton.Top = 430;
                cancelButton.Left = 200;
                cancelButton.Width = 80;
                cancelButton.Height = 25;
                SapUiSafe.TrySetCaption(cancelButton, "Cancel");
            }

            ComObjectManager.Release(rs);
        }

        private static void UpdateValidationRule(SAPbouiCOM.Form oForm, string formType, string itemName, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string newEvent = GetComboValue(oForm, "CmbEvent");
                string condition = GetEditValue(oForm, "EdtCondition");
                string action = GetEditValue(oForm, "EdtAction");
                string severity = GetComboValue(oForm, "CmbSeverity");
                string active = GetComboValue(oForm, "CmbActive");

                // Update the validation rule
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string updateSql = B1App.Instance.IsHana ?
                    $"UPDATE \"@BTUN_VAL\" SET \"U_Event\" = '{newEvent}', \"U_Condition\" = '{condition}', \"U_Action\" = '{action}', \"U_Severity\" = '{severity}', \"U_Active\" = '{active}', \"U_UpdatedAt\" = '{DateTime.Today:yyyy-MM-dd}' WHERE \"U_FormType\" = '{formType}' AND \"U_ItemName\" = '{itemName}'" :
                    $"UPDATE [@BTUN_VAL] SET U_Event = '{newEvent}', U_Condition = '{condition}', U_Action = '{action}', U_Severity = '{severity}', U_Active = '{active}', U_UpdatedAt = '{DateTime.Today:yyyy-MM-dd}' WHERE U_FormType = '{formType}' AND U_ItemName = '{itemName}'";

                rs.DoQuery(updateSql);

                // Assume success
                B1App.Instance.Application.SetStatusBarMessage("Validation rule updated successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                oForm.Close();

                // Refresh the parent form
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ValidationMatrix");
                LoadValidationRules(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error updating validation rule: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void DeleteSelectedValidationRule(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ValidationMatrix");
                if (matrix == null) return;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0);

                    // Get the selected validation rule
                    string formType = matrix.DataTable.GetValue("FormType", selectedRow).ToString();
                    string itemName = matrix.DataTable.GetValue("ItemName", selectedRow).ToString();

                    if (B1App.Instance.Application.MessageBox($"Are you sure you want to delete the validation rule for form '{formType}' and item '{itemName}'?", 1, "Yes", "No") == 1)
                    {
                        // Delete the validation rule
                        Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                        string deleteSql = B1App.Instance.IsHana ?
                            $"DELETE FROM \"@BTUN_VAL\" WHERE \"U_FormType\" = '{formType}' AND \"U_ItemName\" = '{itemName}'" :
                            $"DELETE FROM [@BTUN_VAL] WHERE U_FormType = '{formType}' AND U_ItemName = '{itemName}'";

                        rs.DoQuery(deleteSql);
                        B1App.Instance.Application.SetStatusBarMessage("Validation rule deleted successfully", BoMessageTime.bmt_Short, false);

                        // Reload the matrix
                        LoadValidationRules(matrix);

                        ComObjectManager.Release(rs);
                    }
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a validation rule to delete", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error deleting validation rule: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void TestSelectedValidationRule(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ValidationMatrix");
                if (matrix == null) return;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0);

                    // Get the selected validation rule
                    string condition = matrix.DataTable.GetValue("Condition", selectedRow).ToString();

                    if (!string.IsNullOrEmpty(condition))
                    {
                        // Test the condition
                        bool result = EvaluateCondition(condition);

                        string message = result ? "Selected condition evaluated to TRUE" : "Selected condition evaluated to FALSE";
                        B1App.Instance.Application.SetStatusBarMessage(message, BoMessageTime.bmt_Short, false);
                    }
                    else
                    {
                        B1App.Instance.Application.SetStatusBarMessage("Selected validation rule has no condition to test", BoMessageTime.bmt_Short, true);
                    }
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a validation rule to test", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error testing validation rule: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ToggleValidationRuleActivation(SAPbouiCOM.Form parentForm)
        {
            try
            {
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ValidationMatrix");
                if (matrix == null) return;
                if (matrix.Rows.SelectedRows.Count > 0)
                {
                    int selectedRow = matrix.Rows.SelectedRows.Item(0);

                    // Get the selected validation rule
                    string formType = matrix.DataTable.GetValue("FormType", selectedRow).ToString();
                    string itemName = matrix.DataTable.GetValue("ItemName", selectedRow).ToString();
                    string currentActive = matrix.DataTable.GetValue("Active", selectedRow).ToString();

                    // Toggle the active status
                    string newActive = currentActive == "Y" ? "N" : "Y";

                    // Update the validation rule
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string updateSql = B1App.Instance.IsHana ?
                        $"UPDATE \"@BTUN_VAL\" SET \"U_Active\" = '{newActive}' WHERE \"U_FormType\" = '{formType}' AND \"U_ItemName\" = '{itemName}'" :
                        $"UPDATE [@BTUN_VAL] SET U_Active = '{newActive}' WHERE U_FormType = '{formType}' AND U_ItemName = '{itemName}'";

                    rs.DoQuery(updateSql);
                    string status = newActive == "Y" ? "activated" : "deactivated";
                    B1App.Instance.Application.SetStatusBarMessage($"Validation rule {status} successfully", BoMessageTime.bmt_Short, false);

                    // Reload the matrix
                    LoadValidationRules(matrix);

                    ComObjectManager.Release(rs);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Please select a validation rule to activate/deactivate", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error toggling validation rule activation: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void ExecuteValidationsForActiveForm(string execType, string severity)
        {
            try
            {
                Form activeForm = SapUiSafe.TryGetActiveForm();
                if (activeForm == null)
                {
                    B1App.Instance.Application.SetStatusBarMessage("No active form found", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Execute validations for the active form
                ExecuteValidations(activeForm, execType, severity);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error executing validations: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        // Main method to execute validations for a given form and event
        public static bool ExecuteValidations(SAPbouiCOM.Form form, string eventType, string targetSeverity = "", string itemId = null)
        {
            try
            {
                if (form == null) return true;
                var rules = ValidationRuleService.GetAll()
                    .Where(rule => rule.Active)
                    .Where(rule => string.Equals(rule.FormType, form.TypeEx, StringComparison.OrdinalIgnoreCase))
                    .Where(rule => MatchesValidationEvent(rule.EventType, eventType))
                    .Where(rule => MatchesItem(rule.ItemName, itemId))
                    .OrderBy(rule => rule.Sequence)
                    .ThenBy(rule => rule.Code)
                    .ToList();
                bool allValid = true;

                foreach (var rule in rules)
                {
                    string severity = string.IsNullOrWhiteSpace(rule.Severity) ? "ERROR" : rule.Severity;
                    if (!string.IsNullOrEmpty(targetSeverity) && !severity.Equals(targetSeverity, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!AuthorizationScopeService.MatchesValidation(rule))
                    {
                        continue;
                    }

                    bool conditionResult = EvaluateCondition(rule.Condition, form);

                    if (conditionResult)
                    {
                        string processedMessage = BuildValidationMessage(form, rule.Message, rule.Condition);
                        string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? rule.Code : rule.Name;

                        if (!string.IsNullOrEmpty(rule.Action))
                        {
                            MacroEngine.ExecuteMacro(rule.Action, form);
                        }

                        AuditLogManager.LogAction("ValidationRule", $"Rule {ruleName} ({severity}) triggered on {form.TypeEx}");

                        switch (severity.ToUpperInvariant())
                        {
                            case "ERROR":
                                ShowMessage(processedMessage, "Validación");
                                allValid = false;
                                break;
                            case "WARNING":
                                bool userContinue = HandlePrompt(processedMessage, rule.PromptButtons);
                                if (!userContinue || rule.BlockAlways)
                                {
                                    allValid = false;
                                }
                                break;
                            default: // INFO
                                if (!string.IsNullOrEmpty(processedMessage))
                                {
                                    ShowMessage(processedMessage, "Información");
                                }
                                break;
                        }
                    }
                }

                return allValid;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error in validation execution: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false; // Fail on error to be safe
            }
        }

        private static bool MatchesValidationEvent(string configuredEvent, string runtimeEvent)
        {
            if (string.IsNullOrWhiteSpace(configuredEvent) || string.IsNullOrWhiteSpace(runtimeEvent))
            {
                return false;
            }

            string configured = configuredEvent.Trim();
            string runtime = runtimeEvent.Trim();
            if (string.Equals(configured, runtime, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            switch (configured.ToUpperInvariant())
            {
                case "EDIT_VALIDATE":
                    return string.Equals(runtime, "VALIDATE", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(runtime, "ET_VALIDATE", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(runtime, "EDIT_VALIDATE", StringComparison.OrdinalIgnoreCase);
                case "ITEM_PRESSED":
                    return string.Equals(runtime, "ITEM_PRESSED", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(runtime, "ET_ITEM_PRESSED", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(runtime, "CLICK", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(runtime, "ET_CLICK", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private static bool MatchesItem(string configuredItem, string runtimeItem)
        {
            if (string.IsNullOrWhiteSpace(configuredItem))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(runtimeItem))
            {
                return false;
            }

            string configured = configuredItem.Trim();
            string runtime = runtimeItem.Trim();
            if (string.Equals(configured, runtime, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (configured.StartsWith(runtime + ".", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool EvaluateCondition(string condition, SAPbouiCOM.Form form = null)
        {
            try
            {
                // If form is null, use the active form
                if (form == null)
                {
                    form = SapUiSafe.TryGetActiveForm();
                    if (form == null) return false;
                }

                // Process variables in the condition (e.g., replace $[CardCode] with actual value)
                string processedCondition = ProcessVariables(condition, form);

                // Execute the condition as an SQL query to get a boolean result
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);

                // The condition should return a result that can be evaluated as true/false
                // For example: SELECT CASE WHEN [some condition] THEN 1 ELSE 0 END
                string sql = processedCondition;

                // If it's a simple condition, wrap it appropriately
                if (!sql.ToUpper().Contains("SELECT"))
                {
                    // Assume it's a boolean expression that needs to be embedded in a SELECT
                    sql = B1App.Instance.IsHana ?
                        $"SELECT CASE WHEN ({processedCondition}) THEN 1 ELSE 0 END AS Result" :
                        $"SELECT CASE WHEN ({processedCondition}) THEN 1 ELSE 0 END AS Result";
                }

                rs.DoQuery(sql);

                bool result = false;
                if (!rs.EoF)
                {
                    result = Convert.ToInt32(SapUiSafe.SafeField(rs, 0)) == 1;
                }

                ComObjectManager.Release(rs);

                return result;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error evaluating condition: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false; // Return false on error to be safe
            }
        }

        private static string ProcessVariables(string input, SAPbouiCOM.Form form)
        {
            if (string.IsNullOrEmpty(input) || form == null) return input;

            try
            {
                // Replace variables like $[CardCode] with actual values from the form
                // This is a simplified implementation - a full implementation would handle more complex variable patterns
                string result = input;

                // Look for patterns like $[FieldName] or $[ItemType.FieldName]
                var matches = System.Text.RegularExpressions.Regex.Matches(input, @"\$+\[([^\]]+)\]");

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string fullMatch = match.Value;
                    string fieldName = match.Groups[1].Value;

                    // Try to get the value from the form
                    string value = GetFieldValueFromForm(form, fieldName);

                    result = result.Replace(fullMatch, value);
                }

                return result;
            }
            catch
            {
                return input; // Return original on error
            }
        }

        private static string ReadField(Recordset rs, string fieldName)
        {
            try { return B1TuneUp.Utils.SapUiSafe.SafeField(rs, fieldName); }
            catch { return string.Empty; }
        }

        private static string BuildValidationMessage(SAPbouiCOM.Form form, string template, string condition)
        {
            if (string.IsNullOrWhiteSpace(template))
                return $"Validation failed: {condition}";
            return ProcessVariables(template, form);
        }

        private static void ShowMessage(string message, string title)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            B1App.Instance.Application.MessageBox(message, 1, "OK");
        }

        private static bool HandlePrompt(string message, string promptButtons)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "Validation warning. Continue?";

            if (string.IsNullOrWhiteSpace(promptButtons))
            {
                int resp = B1App.Instance.Application.MessageBox(message, 2, "Continue", "Cancel");
                return resp == 1;
            }

            var tokens = promptButtons.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            string btn1 = tokens.Length > 0 ? tokens[0].Trim() : "Yes";
            string btn2 = tokens.Length > 1 ? tokens[1].Trim() : "No";
            string btn3 = tokens.Length > 2 ? tokens[2].Trim() : string.Empty;

            int result = string.IsNullOrWhiteSpace(btn3)
                ? B1App.Instance.Application.MessageBox(message, 2, btn1, btn2)
                : B1App.Instance.Application.MessageBox(message, 3, btn1, btn2, btn3);

            return result == 1;
        }

        private static bool MatchesCurrentUser(string userFilter, string groupFilter)
        {
            if (string.IsNullOrWhiteSpace(userFilter) && string.IsNullOrWhiteSpace(groupFilter))
                return true;

            var context = GetUserContext();
            if (!AllowListContains(userFilter, new[] { context.UserCode, context.UserName }))
                return false;

            if (!AllowListContains(groupFilter, context.GroupCodes))
                return false;

            return true;
        }

        private static bool AllowListContains(string filter, IEnumerable<string> candidates)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            if (candidates == null) candidates = Array.Empty<string>();

            var tokens = filter.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string value = token.Trim();
                if (value == "*") return true;
                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrEmpty(candidate) && value.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static readonly object _userContextLock = new object();
        private static UserContext _userContext;

        private static UserContext GetUserContext()
        {
            if (_userContext != null) return _userContext;
            lock (_userContextLock)
            {
                if (_userContext != null) return _userContext;
                var ctx = new UserContext
                {
                    UserCode = SafeString(() => B1App.Instance.Company.UserName),
                    UserName = SafeString(() => B1App.Instance.Company.UserName)
                };

                try
                {
                    Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string sql = B1App.Instance.IsHana
                        ? $"SELECT G.\"GroupCode\" FROM OUSR U INNER JOIN USR6 UG ON U.\"USERID\" = UG.\"USERID\" INNER JOIN OUGR G ON UG.\"GroupCode\" = G.\"GroupCode\" WHERE U.\"USER_CODE\" = '{ctx.UserCode}'"
                        : $"SELECT G.GroupCode FROM OUSR U WITH (NOLOCK) INNER JOIN USR6 UG ON U.USERID = UG.USERID INNER JOIN OUGR G ON UG.GroupCode = G.GroupCode WHERE U.USER_CODE = '{ctx.UserCode}'";
                    rs.DoQuery(sql);
                    while (!rs.EoF)
                    {
                        string code = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0);
                        if (!string.IsNullOrEmpty(code)) ctx.GroupCodes.Add(code);
                        rs.MoveNext();
                    }
                    ComObjectManager.Release(rs);
                }
                catch { }

                _userContext = ctx;
                return ctx;
            }
        }

        private static string SafeString(Func<string> getter)
        {
            try { return getter() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private class UserContext
        {
            public string UserCode { get; set; }
            public string UserName { get; set; }
            public HashSet<string> GroupCodes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static string GetFieldValueFromForm(SAPbouiCOM.Form form, string fieldName)
        {
            try
            {
                // Simplified field value extraction
                // In a real implementation, this would handle various field types and locations
                Item item = SapUiSafe.TryGetItem(form, fieldName);
                if (item != null)
                {
                    if (item.Type == BoFormItemTypes.it_EDIT || item.Type == BoFormItemTypes.it_EXTEDIT)
                    {
                        return SapUiSafe.TryGetSpecific<EditText>(item)?.Value ?? string.Empty;
                    }
                    else if (item.Type == BoFormItemTypes.it_COMBO_BOX)
                    {
                        return SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(item));
                    }
                }

                // If field not found, return empty string
                return "";
            }
            catch
            {
                return "";
            }
        }

        private static string GetEditValue(SAPbouiCOM.Form form, string itemId)
        {
            return SapUiSafe.TryGetSpecific<EditText>(form, itemId)?.Value ?? string.Empty;
        }

        private static string GetComboValue(SAPbouiCOM.Form form, string itemId)
        {
            return SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<ComboBox>(form, itemId));
        }
    }
}
