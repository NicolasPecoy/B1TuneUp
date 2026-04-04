using System;
using SAPbobsCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Utils
{
    public static class MetadataManager
    {
        public static void CreateUDT(string tableName, string tableDesc, BoUTBTableType tableType)
        {
            UserTablesMD userTableMD = (UserTablesMD)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.oUserTables);
            try
            {
                if (!userTableMD.GetByKey(tableName))
                {
                    userTableMD.TableName = tableName;
                    userTableMD.TableDescription = tableDesc;
                    userTableMD.TableType = tableType;

                    int res = userTableMD.Add();
                    if (res != 0)
                    {
                        string err = B1App.Instance.Company.GetLastErrorDescription();
                        B1App.Instance.Application.SetStatusBarMessage($"Error creando UDT {tableName}: {err}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(userTableMD);
            }
        }

        public static void CreateUDF(string tableName, string fieldName, string fieldDesc, BoFieldTypes type, int size = 0, string defaultValue = "", string validValues = "", BoFldSubTypes subType = BoFldSubTypes.st_None)
        {
            UserFieldsMD userFieldMD = (UserFieldsMD)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.oUserFields);
            try
            {
                int fieldID = GetFieldID(tableName, fieldName);
                if (fieldID == -1)
                {
                    userFieldMD.TableName = tableName;
                    userFieldMD.Name = fieldName;
                    userFieldMD.Description = fieldDesc;
                    userFieldMD.Type = type;
                    if (subType != BoFldSubTypes.st_None)
                    {
                        userFieldMD.SubType = subType;
                    }
                    if (size > 0) userFieldMD.EditSize = size;
                    if (!string.IsNullOrEmpty(defaultValue)) userFieldMD.DefaultValue = defaultValue;

                    if (!string.IsNullOrEmpty(validValues))
                    {
                        string[] pairs = validValues.Split(';');
                        foreach (var pair in pairs)
                        {
                            string[] kv = pair.Split(':');
                            if (kv.Length == 2)
                            {
                                userFieldMD.ValidValues.Value = kv[0];
                                userFieldMD.ValidValues.Description = kv[1];
                                userFieldMD.ValidValues.Add();
                            }
                        }
                    }

                    int res = userFieldMD.Add();
                    if (res != 0)
                    {
                        string err = B1App.Instance.Company.GetLastErrorDescription();
                        B1App.Instance.Application.SetStatusBarMessage($"Error creando UDF {fieldName} en {tableName}: {err}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                    }
                }
            }
            finally
            {
                ComObjectManager.Release(userFieldMD);
            }
        }

        private static int GetFieldID(string tableName, string fieldName)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string tableID = tableName.StartsWith("@") ? tableName.Substring(1) : tableName;
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"FieldID\" FROM CUFD WHERE \"TableID\" = '{tableName}' AND \"AliasID\" = '{fieldName}'"
                    : $"SELECT FieldID FROM CUFD WHERE TableID = '{tableName}' AND AliasID = '{fieldName}'";

                rs.DoQuery(sql);
                if (rs.RecordCount > 0)
                {
                    return (int)rs.Fields.Item(0).Value;
                }
                return -1;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void SetupMetadata()
        {
            B1App.Instance.Application.SetStatusBarMessage("Iniciando configuración de metadatos B1TuneUp...", SAPbouiCOM.BoMessageTime.bmt_Short, false);

            // Tabla para Reglas
            CreateUDT("BTUN_RULES", "B1TuneUp Rules", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_RULES", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_RULES", "Type", "Rule Type", BoFieldTypes.db_Alpha, 20, "Macro", "Macro:Macro;Validation:Validation;UICustomization:UI Customization");
            CreateUDF("@BTUN_RULES", "EventType", "Event Type", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_RULES", "Before", "Before Action", BoFieldTypes.db_Alpha, 1, "N", "Y:Yes;N:No");
            CreateUDF("@BTUN_RULES", "Condition", "Condition SQL", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_RULES", "Action", "Action Macro", BoFieldTypes.db_Memo);

            // Tabla para Menús Personalizados
            CreateUDT("BTUN_MENUS", "B1TuneUp Menus", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_MENUS", "ParentID", "Parent Menu ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_MENUS", "MenuID", "Menu ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_MENUS", "Name", "Menu Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_MENUS", "Position", "Position", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_MENUS", "Action", "Action Macro", BoFieldTypes.db_Memo);

            // Tabla para Personalización de UI (Item Placement Tool)
            CreateUDT("BTUN_UI", "B1TuneUp UI Customization", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_UI", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_UI", "ItemID", "Item ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_UI", "Action", "Action (Hide/Move/Add)", BoFieldTypes.db_Alpha, 20, "Hide", "Hide:Hide;Move:Move;AddButton:Add Button");
            CreateUDF("@BTUN_UI", "Top", "Top", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_UI", "Left", "Left", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_UI", "Width", "Width", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_UI", "Height", "Height", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_UI", "Label", "Label/Value", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_UI", "FromPane", "From Pane", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_UI", "ToPane", "To Pane", BoFieldTypes.db_Numeric, 5);

            // Tabla para Layouts guardados por Item Placement
            CreateUDT("BTUN_LAYOUT", "B1TuneUp Layouts", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_LAYOUT", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LAYOUT", "Name", "Layout Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_LAYOUT", "Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_LAYOUT", "Def", "Layout Definition (XML)", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_LAYOUT", "Owner", "Owner User", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_LAYOUT", "Role", "Role", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_LAYOUT", "Version", "Version", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_LAYOUT", "SRF", "SRF/Base64 or XML", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_LAYOUT", "FileName", "SRF File Name", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_LAYOUT", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_LAYOUT", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para acciones asociadas a items (Item Actions)
            CreateUDT("BTUN_ITEMACT", "B1TuneUp Item Actions", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_ITEMACT", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_ITEMACT", "ItemID", "Item ID", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_ITEMACT", "Event", "Event Type", BoFieldTypes.db_Alpha, 50, "Change", "Change:Change;ItemPressed:ItemPressed;DoubleClick:DoubleClick;MatrixRow:MatrixRow");
            CreateUDF("@BTUN_ITEMACT", "Action", "Action Macro", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_ITEMACT", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_ITEMACT", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para Campos Obligatorios
            CreateUDT("BTUN_MAND", "B1TuneUp Mandatory Fields", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_MAND", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_MAND", "ItemID", "Item ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_MAND", "ColumnID", "Column ID (if Matrix)", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_MAND", "Condition", "Condition SQL", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_MAND", "ErrorMsg", "Error Message", BoFieldTypes.db_Alpha, 254);

            // Tabla para Menús Contextuales (Right-Click)
            CreateUDT("BTUN_RCLICK", "B1TuneUp Right-Click Menus", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_RCLICK", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_RCLICK", "ItemID", "Item ID (Optional)", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_RCLICK", "MenuID", "Menu ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_RCLICK", "Name", "Menu Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_RCLICK", "Action", "Action Macro", BoFieldTypes.db_Memo);

            // Tabla para Valores por Defecto
            CreateUDT("BTUN_DEFAULTS", "B1TuneUp Default Values", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_DEFAULTS", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_DEFAULTS", "ItemID", "Item ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_DEFAULTS", "ColID", "Column ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_DEFAULTS", "Query", "SQL Query", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_DEFAULTS", "OnEvent", "On Event (Load/Change)", BoFieldTypes.db_Alpha, 20, "Load", "Load:Form Load;Change:Item Change");

            // Tabla para Action Pad (Grupos de botones flotantes/laterales)
            CreateUDT("BTUN_PAD", "B1TuneUp Action Pad", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_PAD", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_PAD", "Title", "Pad Title", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_PAD", "Position", "Position (Left/Right)", BoFieldTypes.db_Alpha, 10, "Right", "Left:Left;Right:Right");

            // Tabla para Botones del Action Pad
            CreateUDT("BTUN_PADB", "B1TuneUp Action Pad Buttons", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_PADB", "PadEntry", "Pad Entry ID", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_PADB", "Label", "Button Label", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_PADB", "Action", "Action Macro", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_PADB", "Order", "Sort Order", BoFieldTypes.db_Numeric, 5);

            // Tabla para Código C# Dinámico (Universal Functions - Code)
            CreateUDT("BTUN_CODE", "B1TuneUp Dynamic Code", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_CODE", "CodeName", "Code Name/ID", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_CODE", "Source", "C# Source Code", BoFieldTypes.db_Memo);

            // Tabla para B1 Search (Búsqueda Universal)
            CreateUDT("BTUN_SEARCH", "B1TuneUp Search", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_SEARCH", "Name", "Search Name", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_SEARCH", "Query", "SQL Search Query (use %search% tag)", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_SEARCH", "Action", "Action on Click (e.g. OpenForm($[DocEntry]))", BoFieldTypes.db_Memo);

            // Tabla para Email (Universal Functions)
            CreateUDT("BTUN_EMAIL", "B1TuneUp Email Config", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_EMAIL", "Subject", "Subject", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EMAIL", "Body", "Body (HTML/Text)", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_EMAIL", "To", "Recipient Email", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EMAIL", "CC", "CC Recipient", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EMAIL", "BCC", "BCC Recipient", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EMAIL", "Sender", "Sender Address", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EMAIL", "Attach", "Attachment Path", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EMAIL", "Channel", "Delivery Channel", BoFieldTypes.db_Alpha, 20, "Email", "Email:Email;SAPMessage:SAP Message;Desktop:Desktop Toast");
            CreateUDF("@BTUN_EMAIL", "Priority", "Priority", BoFieldTypes.db_Alpha, 10, "Normal", "Low:Low;Normal:Normal;High:High");
            CreateUDF("@BTUN_EMAIL", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            CreateUDF("@BTUN_EMAIL", "Trigger", "Trigger/Usage Notes", BoFieldTypes.db_Memo);

            // Tabla para Scheduler (Tareas Programadas)
            CreateUDT("BTUN_SCHED", "B1TuneUp Scheduler", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_SCHED", "TaskName", "Task Name", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_SCHED", "Action", "Action Macro", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_SCHED", "Interval", "Interval (Minutes)", BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_SCHED", "LastRun", "Last Run Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_SCHED", "Active", "Is Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");

            // Tabla para Toolbox (Configuraciones varias)
            CreateUDT("BTUN_TBOX", "B1TuneUp Toolbox", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_TBOX", "Code", "Setting Code", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_TBOX", "Value", "Setting Value", BoFieldTypes.db_Alpha, 100);

            // Tabla para Audit Log
            CreateUDT("BTUN_LOG", "B1TuneUp Audit Log", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_LOG", "Date", "Log Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_LOG", "Type", "Action Type", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_LOG", "Details", "Details", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_LOG", "Status", "Status", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LOG", "User", "User", BoFieldTypes.db_Alpha, 50);

            // Tabla para MDM (Master Data Manager)
            CreateUDT("BTUN_MDM", "B1TuneUp Master Data Manager", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_MDM", "Name", "Config Name", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_MDM", "SelectSQL", "Select Query", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_MDM", "Action", "Action Macro", BoFieldTypes.db_Memo);

            // Tabla para Dashboard (Widgets del Dashboard)
            CreateUDT("BTUN_DASH", "B1TuneUp Dashboard", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_DASH", "WidgetType", "Widget Type", BoFieldTypes.db_Alpha, 20, "Stats", "Stats:Statistics;Chart:Chart;Graph:Graph;List:List;Table:Table");
            CreateUDF("@BTUN_DASH", "Title", "Widget Title", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_DASH", "Query", "SQL Query", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_DASH", "Width", "Width (pixels)", BoFieldTypes.db_Numeric, 5, "300");
            CreateUDF("@BTUN_DASH", "Height", "Height (pixels)", BoFieldTypes.db_Numeric, 5, "200");
            CreateUDF("@BTUN_DASH", "Position", "Position Order", BoFieldTypes.db_Numeric, 5, "0");
            CreateUDF("@BTUN_DASH", "Color", "Background Color", BoFieldTypes.db_Alpha, 20, "#FFFFFF");

            // Tabla para Templates
            CreateUDT("BTUN_TMPL", "B1TuneUp Templates", BoUTBTableType.bott_NoObject);
            // Legacy aliases without prefix (kept for backward compatibility)
            CreateUDF("@BTUN_TMPL", "Name", "Template Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_TMPL", "Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_TMPL", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_TMPL", "Data", "Serialized Data", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_TMPL", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_TMPL", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_TMPL", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);
            // Fields actually consumed by TemplateManager (U_* columns)
            CreateUDF("@BTUN_TMPL", "U_Name", "Template Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_TMPL", "U_Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_TMPL", "U_FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_TMPL", "U_Data", "Serialized Data", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_TMPL", "U_CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_TMPL", "U_CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_TMPL", "U_UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para Recurring Invoices
            CreateUDT("BTUN_RINV", "B1TuneUp Recurring Invoices", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_RINV", "Name", "Template Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_RINV", "Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_RINV", "Freq", "Frequency", BoFieldTypes.db_Alpha, 20, "Monthly", "Daily:Daily;Weekly:Weekly;BiWeekly:Bi-Weekly;Monthly:Monthly;BiMonthly:Bi-Monthly;Quarterly:Quarterly;SemiAnnually:Semi-Annually;Annually:Annually");
            CreateUDF("@BTUN_RINV", "StartDate", "Start Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_RINV", "EndDate", "End Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_RINV", "DocType", "Document Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_RINV", "DocNum", "Document Number", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_RINV", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            CreateUDF("@BTUN_RINV", "LastExecuted", "Last Executed Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_RINV", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_RINV", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_RINV", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para Letter Merge
            CreateUDT("BTUN_LTRM", "B1TuneUp Letter Merge", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_LTRM", "Name", "Template Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_LTRM", "Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_LTRM", "DocType", "Document Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LTRM", "FilePath", "Template File Path", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_LTRM", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_LTRM", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_LTRM", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para Exchange Rates
            CreateUDT("BTUN_EXCH", "B1TuneUp Exchange Rates", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_EXCH", "FromCurr", "From Currency", BoFieldTypes.db_Alpha, 3);
            CreateUDF("@BTUN_EXCH", "ToCurr", "To Currency", BoFieldTypes.db_Alpha, 3);
            CreateUDF("@BTUN_EXCH", "Rate", "Exchange Rate", BoFieldTypes.db_Float, 15, "1.000000");
            CreateUDF("@BTUN_EXCH", "LastUpdate", "Last Update Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_EXCH", "Source", "Data Source", BoFieldTypes.db_Alpha, 20, "Manual", "Manual:Manual;ECB:European Central Bank;Fixer:Fixer.io");
            CreateUDF("@BTUN_EXCH", "Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_EXCH", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            CreateUDF("@BTUN_EXCH", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_EXCH", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_EXCH", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para PLD Extensions (Import/Export de Report Layouts)
            CreateUDT("BTUN_PLD", "B1TuneUp PLD Extensions", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_PLD", "Name", "Layout Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_PLD", "ObjType", "Object Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_PLD", "Desc", "Description", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_PLD", "ExportDate", "Export Date", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_PLD", "Language", "Language Code", BoFieldTypes.db_Alpha, 10);
            CreateUDF("@BTUN_PLD", "XMLData", "XML Layout Data", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_PLD", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_PLD", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_PLD", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para Validation System
            CreateUDT("BTUN_VAL", "B1TuneUp Validation System", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_VAL", "FormType", "Form Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_VAL", "ItemName", "Item/Field Name", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_VAL", "Event", "Event Type", BoFieldTypes.db_Alpha, 30, "DATA_ADD_BEFORE", "DATA_ADD_BEFORE:Before Data Add;DATA_UPDATE_BEFORE:Before Data Update;ITEM_PRESSED:Item Pressed;FORM_LOAD:Form Load;COMBO_SELECT:Combo Select;EDIT_VALIDATE:Edit Validate");
            CreateUDF("@BTUN_VAL", "Condition", "Validation Condition (SQL)", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_VAL", "Action", "Action to Take (Macro)", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_VAL", "Severity", "Severity Level", BoFieldTypes.db_Alpha, 10, "ERROR", "ERROR:Error (Block);WARNING:Warning (Allow Continue);INFO:Information");
            CreateUDF("@BTUN_VAL", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            CreateUDF("@BTUN_VAL", "User", "Apply to User", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_VAL", "UserGroup", "Apply to User Group", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_VAL", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_VAL", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_VAL", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // Tabla para Print & Delivery
            CreateUDT("BTUN_PD", "B1TuneUp Print & Delivery", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_PD", "Name", "Configuration Name", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_PD", "DocType", "Document Type", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_PD", "Trigger", "Trigger Event", BoFieldTypes.db_Alpha, 30, "DOC_ADD", "DOC_ADD:Document Added;DOC_UPDATE:Document Updated;DOC_APPROVE:Document Approved;MANUAL:Manual Trigger;SCHEDULED:Scheduled");
            CreateUDF("@BTUN_PD", "Action", "Action to Perform", BoFieldTypes.db_Alpha, 20, "EMAIL", "EMAIL:Email;PRINT:Print;SAVE:Save to Folder;FTP:Upload to FTP;SHAREPOINT:SharePoint Upload");
            CreateUDF("@BTUN_PD", "EmailSubject", "Email Subject", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_PD", "EmailBody", "Email Body", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_PD", "AttachmentType", "Attachment Type", BoFieldTypes.db_Alpha, 20, "PDF", "PDF:PDF Document;EXCEL:Excel Export;WORD:Word Document;ORIGINAL:Original Format");
            CreateUDF("@BTUN_PD", "PrintSetup", "Print Setup", BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_PD", "SavePath", "Save Path", BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_PD", "Active", "Active", BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            CreateUDF("@BTUN_PD", "CreatedBy", "Created By", BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_PD", "CreatedAt", "Created At", BoFieldTypes.db_Date);
            CreateUDF("@BTUN_PD", "UpdatedAt", "Updated At", BoFieldTypes.db_Date);

            // ── Tabla para Lock Fields (Bloqueo dinámico de campos) ────────────────
            CreateUDT("BTUN_LOCK", "B1TuneUp Lock Fields", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_LOCK", "FormType",    "Form Type",                    BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LOCK", "ItemID",      "Item ID",                      BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LOCK", "ColID",       "Column ID (if Matrix)",        BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LOCK", "LockType",    "Lock Type",                    BoFieldTypes.db_Alpha, 15, "ReadOnly", "ReadOnly:Read Only;Hidden:Hidden;Disabled:Disabled");
            CreateUDF("@BTUN_LOCK", "Condition",   "Condition SQL (empty=always)", BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_LOCK", "TriggerItem", "Trigger Item ID (Change)",     BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_LOCK", "OnEvent",     "On Event",                     BoFieldTypes.db_Alpha, 10, "Load", "Load:Form Load;Change:Item Change");

            // ── Tabla para Quick Copy (Copia rápida de documentos) ─────────────────
            CreateUDT("BTUN_QCOPY", "B1TuneUp Quick Copy", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_QCOPY", "Name",        "Config Name",                 BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_QCOPY", "SrcFormType", "Source Form Type",              BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_QCOPY", "SrcObjType",  "Source DI Object Type (enum)", BoFieldTypes.db_Alpha, 30);
            CreateUDF("@BTUN_QCOPY", "TgtObjType",  "Target DI Object Type (enum)", BoFieldTypes.db_Alpha, 30);
            CreateUDF("@BTUN_QCOPY", "BtnLabel",    "Button Label",                BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_QCOPY", "CopyMode",    "Copy Mode",                   BoFieldTypes.db_Alpha, 15, "Full", "Full:Full Copy;HeaderOnly:Header Only");
            CreateUDF("@BTUN_QCOPY", "PostMacro",   "Post-Copy Macro",             BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_QCOPY", "Active",      "Active",                      BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");

            // ── Tabla para Form Settings (Ajustes visuales por usuario) ────────────
            CreateUDT("BTUN_FSET", "B1TuneUp Form Settings", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_FSET", "FormType", "Form Type",  BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_FSET", "UserCode", "User Code",  BoFieldTypes.db_Alpha, 50);
            CreateUDF("@BTUN_FSET", "Data",     "Settings (key=val;...)", BoFieldTypes.db_Memo);

            // ── Tabla para configuración de Integraciones (REST/SOAP) ──────────────
            CreateUDT("BTUN_INTCFG", "B1TuneUp Integration Config", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_INTCFG", "Channel",   "Channel (REST/SOAP)",       BoFieldTypes.db_Alpha, 10, "REST", "REST:REST;SOAP:SOAP");
            CreateUDF("@BTUN_INTCFG", "Method",    "HTTP Method",               BoFieldTypes.db_Alpha, 10);
            CreateUDF("@BTUN_INTCFG", "Endpoint",  "Endpoint/Base URL",         BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_INTCFG", "Headers",   "HTTP Headers key=value|",   BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_INTCFG", "Body",      "Body Template",             BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_INTCFG", "AuthMode",  "Authentication Mode",       BoFieldTypes.db_Alpha, 20, "None", "None:Ninguno;Basic:Basic;Bearer:Bearer Token;ApiKey:API Key");
            CreateUDF("@BTUN_INTCFG", "AuthUser",  "Auth User",                 BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_INTCFG", "AuthSecret","Auth Secret/Token",         BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_INTCFG", "Schedule",  "Scheduler Interval (min)",  BoFieldTypes.db_Numeric, 11);
            CreateUDF("@BTUN_INTCFG", "Handler",   "Macro Handler",             BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_INTCFG", "Notes",     "Notes/Description",         BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_INTCFG", "Active",    "Active",                    BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");

            // ── Tabla para Process Steps – cabecera ────────────────────────────────
            CreateUDT("BTUN_PSTEP", "B1TuneUp Process Steps", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_PSTEP", "Name",     "Process Name",          BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_PSTEP", "FormType", "Form Type",             BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_PSTEP", "Desc",     "Description",           BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_PSTEP", "Active",   "Active",                BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");
            CreateUDF("@BTUN_PSTEP", "AutoShow", "Auto Show on Form Load",BoFieldTypes.db_Alpha, 1, "N", "Y:Yes;N:No");

            // ── Tabla para Process Steps – detalle ─────────────────────────────────
            CreateUDT("BTUN_PSTEPD", "B1TuneUp Process Step Details", BoUTBTableType.bott_NoObject);
            CreateUDF("@BTUN_PSTEPD", "ProcessEntry", "Process Entry ID (DocEntry de BTUN_PSTEP)", BoFieldTypes.db_Alpha, 20);
            CreateUDF("@BTUN_PSTEPD", "StepOrder",    "Step Order",                                BoFieldTypes.db_Numeric, 5);
            CreateUDF("@BTUN_PSTEPD", "StepName",     "Step Name",                                 BoFieldTypes.db_Alpha, 100);
            CreateUDF("@BTUN_PSTEPD", "StepDesc",     "Step Description",                          BoFieldTypes.db_Alpha, 254);
            CreateUDF("@BTUN_PSTEPD", "DoneCondition","Completion Condition SQL",                   BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_PSTEPD", "Action",       "Step Action Macro",                         BoFieldTypes.db_Memo);
            CreateUDF("@BTUN_PSTEPD", "Mandatory",    "Mandatory Step",                            BoFieldTypes.db_Alpha, 1, "Y", "Y:Yes;N:No");

            B1App.Instance.Application.SetStatusBarMessage("Metadatos B1TuneUp configurados con éxito.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
        }
    }
}
