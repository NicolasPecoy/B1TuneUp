using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class DashboardManager
    {
        private static Dictionary<string, DashboardWidget> _widgets = new Dictionary<string, DashboardWidget>();

        public static void ShowDashboard()
        {
            try
            {
                string formUID = "BTUN_DASHBOARD_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_DASH";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp Dashboard";
                oForm.Width = 800;
                oForm.Height = 600;

                // Load dashboard widgets from configuration
                LoadWidgets(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mostrando Dashboard: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadWidgets(SAPbouiCOM.Form oForm)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT * FROM \"@BTUN_DASH\" ORDER BY \"U_Position\" ASC"
                    : "SELECT * FROM [@BTUN_DASH] ORDER BY [U_Position] ASC";

                rs.DoQuery(sql);

                int top = 10;
                int left = 10;
                int widgetCount = 0;

                while (!rs.EoF)
                {
                    string widgetType = rs.Fields.Item("U_WidgetType").Value.ToString();
                    string title = rs.Fields.Item("U_Title").Value.ToString();
                    string query = rs.Fields.Item("U_Query").Value.ToString();
                    int width = (int)rs.Fields.Item("U_Width").Value;
                    int height = (int)rs.Fields.Item("U_Height").Value;

                    // Position widgets in a grid layout
                    if (widgetCount > 0 && (left + width + 10) > oForm.Width - 50)
                    {
                        left = 10;
                        top += height + 20;
                    }

                    CreateWidget(oForm, widgetType, title, query, left, top, width, height);

                    left += width + 10;
                    widgetCount++;

                    rs.MoveNext();
                }

                // Adjust form height based on content
                oForm.Height = Math.Max(oForm.Height, top + 150);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando widgets del Dashboard: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void CreateWidget(SAPbouiCOM.Form oForm, string widgetType, string title, string query, int left, int top, int width, int height)
        {
            try
            {
                string widgetId = $"wdg_{Guid.NewGuid().ToString().Substring(0, 5)}";

                // Create a container item for the widget
                Item containerItem = oForm.Items.Add(widgetId, BoFormItemTypes.it_FOLDER);
                containerItem.Left = left;
                containerItem.Top = top;
                containerItem.Width = width;
                containerItem.Height = height;

                Folder container = (Folder)containerItem.Specific;
                container.Caption = title;

                // Add refresh button to the widget
                string refreshBtnId = $"ref_{widgetId}";
                Item refreshItem = oForm.Items.Add(refreshBtnId, BoFormItemTypes.it_BUTTON);
                refreshItem.Left = left + width - 40;
                refreshItem.Top = top + 5;
                refreshItem.Width = 30;
                refreshItem.Height = 20;

                SAPbouiCOM.Button refreshBtn = (SAPbouiCOM.Button)refreshItem.Specific;
                refreshBtn.Caption = "↻";

                // Store widget information
                DashboardWidget widget = new DashboardWidget
                {
                    Id = widgetId,
                    Type = widgetType,
                    Query = query,
                    Container = container,
                    RefreshButtonId = refreshBtnId
                };

                _widgets[widgetId] = widget;

                // Load initial data
                LoadWidgetData(widget);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creando widget: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadWidgetData(DashboardWidget widget)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery(widget.Query);

                // Clear existing content
                if (widget.Container != null)
                {
                    // Add content based on widget type
                    if (widget.Type == "Chart" || widget.Type == "Graph")
                    {
                        // For now, just show a label with record count
                        string countText = $"Registros: {rs.RecordCount}";
                        widget.Container.Caption = $"{widget.Container.Caption} ({countText})";
                    }
                    else if (widget.Type == "List" || widget.Type == "Table")
                    {
                        // Display first few records as text
                        string summary = "";
                        int count = 0;
                        while (!rs.EoF && count < 5)
                        {
                            // Get first column value as example
                            if (rs.Fields.Count > 0)
                            {
                                summary += rs.Fields.Item(0).Value?.ToString() + "; ";
                            }
                            rs.MoveNext();
                            count++;
                        }
                        // Add a text box with summary
                    }
                    else // Default to statistics
                    {
                        widget.Container.Caption = $"{widget.Container.Caption} (Records: {rs.RecordCount})";
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando datos del widget: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static void RefreshWidget(string widgetId)
        {
            if (_widgets.ContainsKey(widgetId))
            {
                LoadWidgetData(_widgets[widgetId]);
            }
        }

        public static void RefreshAllWidgets()
        {
            foreach (string widgetId in _widgets.Keys)
            {
                LoadWidgetData(_widgets[widgetId]);
            }
        }
    }

    internal class DashboardWidget
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Query { get; set; }
        public Folder Container { get; set; }
        public string RefreshButtonId { get; set; }
    }
}