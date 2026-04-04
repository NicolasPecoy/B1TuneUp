using System;
using System.Collections.Generic;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using SAPbobsCOM;

namespace B1TuneUp.Modules
{
    public static class DashboardManager
    {
        private static readonly Dictionary<string, DashboardWidget> _widgets = new Dictionary<string, DashboardWidget>();

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
                oForm.Width = 900;
                oForm.Height = 640;

                AddToolbar(oForm);
                LoadWidgets(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mostrando Dashboard: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void AddToolbar(SAPbouiCOM.Form oForm)
        {
            try
            {
                Item refreshItem = oForm.Items.Add("btnDashRefresh", BoFormItemTypes.it_BUTTON);
                refreshItem.Left = 10;
                refreshItem.Top = 5;
                refreshItem.Width = 110;
                refreshItem.Height = 18;
                ((SAPbouiCOM.Button)refreshItem.Specific).Caption = "Refrescar todo";
            }
            catch { }
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

                _widgets.Clear();
                int top = 30;
                int left = 10;
                int widgetCount = 0;

                while (!rs.EoF)
                {
                    var widget = new DashboardWidget
                    {
                        InternalId = Guid.NewGuid().ToString("N").Substring(0, 6),
                        Type = rs.Fields.Item("U_WidgetType").Value.ToString(),
                        Title = rs.Fields.Item("U_Title").Value.ToString(),
                        Query = rs.Fields.Item("U_Query").Value.ToString(),
                        Width = SafeInt(rs.Fields.Item("U_Width")?.Value, 320),
                        Height = SafeInt(rs.Fields.Item("U_Height")?.Value, 220),
                        MaxRows = Math.Max(1, SafeInt(rs.Fields.Item("U_MaxRows")?.Value, 6)),
                        RenderMode = rs.Fields.Item("U_RenderMode")?.Value.ToString()
                    };

                    if (widgetCount > 0 && (left + widget.Width + 10) > oForm.Width - 40)
                    {
                        left = 10;
                        top += widget.Height + 20;
                    }

                    CreateWidget(oForm, widget, left, top);

                    left += widget.Width + 10;
                    widgetCount++;

                    rs.MoveNext();
                }

                oForm.Height = Math.Max(oForm.Height, top + 160);
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

        private static void CreateWidget(SAPbouiCOM.Form oForm, DashboardWidget widget, int left, int top)
        {
            try
            {
                Item frameItem = oForm.Items.Add($"ctr_{widget.InternalId}", BoFormItemTypes.it_RECTANGLE);
                frameItem.Left = left;
                frameItem.Top = top;
                frameItem.Width = widget.Width;
                frameItem.Height = widget.Height;

                Item titleItem = oForm.Items.Add($"ttl_{widget.InternalId}", BoFormItemTypes.it_STATIC);
                titleItem.Left = left + 6;
                titleItem.Top = top + 6;
                titleItem.Width = widget.Width - 12;
                titleItem.Height = 16;
                ((StaticText)titleItem.Specific).Caption = widget.Title;

                Item refreshItem = oForm.Items.Add($"ref_{widget.InternalId}", BoFormItemTypes.it_BUTTON);
                refreshItem.Left = left + widget.Width - 38;
                refreshItem.Top = top + 4;
                refreshItem.Width = 28;
                refreshItem.Height = 18;
                ((SAPbouiCOM.Button)refreshItem.Specific).Caption = "↻";

                widget.ParentForm = oForm;
                widget.Container = frameItem;
                widget.RefreshButtonId = refreshItem.UniqueID;

                if (IsListWidget(widget))
                {
                    string gridId = $"grd_{widget.InternalId}";
                    Item gridItem = oForm.Items.Add(gridId, BoFormItemTypes.it_GRID);
                    gridItem.Left = left + 6;
                    gridItem.Top = top + 26;
                    gridItem.Width = widget.Width - 12;
                    gridItem.Height = widget.Height - 32;
                    widget.GridItemId = gridId;
                }
                else
                {
                    string valueId = $"val_{widget.InternalId}";
                    Item valueItem = oForm.Items.Add(valueId, BoFormItemTypes.it_STATIC);
                    valueItem.Left = left + 6;
                    valueItem.Top = top + 26;
                    valueItem.Width = widget.Width - 12;
                    valueItem.Height = widget.Height - 32;
                    ((StaticText)valueItem.Specific).Caption = "Cargando...";
                    widget.ValueItemId = valueId;
                }

                _widgets[widget.InternalId] = widget;
                LoadWidgetData(widget);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creando widget: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void LoadWidgetData(DashboardWidget widget)
        {
            if (IsListWidget(widget))
            {
                RenderListWidget(widget);
                return;
            }

            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery(widget.Query);

                RenderStatsWidget(widget, rs);
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

        private static void RenderStatsWidget(DashboardWidget widget, Recordset rs)
        {
            try
            {
                string summary = rs.RecordCount == 0 ? "Sin datos" : $"{rs.RecordCount} registro(s)";
                if (rs.RecordCount > 0 && rs.Fields.Count > 0)
                {
                    string firstValue = rs.Fields.Item(0).Value?.ToString();
                    if (!string.IsNullOrEmpty(firstValue))
                    {
                        summary = $"{firstValue}\n({rs.RecordCount} registro(s))";
                    }
                }

                if (!string.IsNullOrEmpty(widget.ValueItemId))
                {
                    StaticText text = (StaticText)widget.ParentForm.Items.Item(widget.ValueItemId).Specific;
                    text.Caption = summary;
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error mostrando widget: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void RenderListWidget(DashboardWidget widget)
        {
            try
            {
                Grid grid = (Grid)widget.ParentForm.Items.Item(widget.GridItemId).Specific;
                var dt = grid.DataTable;
                string limitedQuery = ApplyQueryLimit(widget.Query, widget.MaxRows);
                dt.ExecuteQuery(limitedQuery);
                try { grid.AutoResizeColumns(); } catch { }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error renderizando lista: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static bool IsListWidget(DashboardWidget widget)
        {
            string type = widget.Type?.ToUpperInvariant();
            string mode = widget.RenderMode?.ToUpperInvariant();
            return type == "LIST" || type == "TABLE" || mode == "LIST" || mode == "TABLE";
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
            foreach (var widget in _widgets.Values)
            {
                LoadWidgetData(widget);
            }
        }

        private static int SafeInt(object value, int fallback)
        {
            try
            {
                if (value == null) return fallback;
                if (int.TryParse(value.ToString(), out var parsed)) return parsed;
                double dbl;
                if (double.TryParse(value.ToString(), out dbl)) return Convert.ToInt32(dbl);
                return fallback;
            }
            catch { return fallback; }
        }

        private static string ApplyQueryLimit(string baseQuery, int maxRows)
        {
            if (maxRows <= 0 || string.IsNullOrWhiteSpace(baseQuery)) return baseQuery;
            string trimmed = baseQuery.Trim().TrimEnd(';');
            if (B1App.Instance.IsHana)
            {
                return $"SELECT * FROM ({trimmed}) T LIMIT {maxRows}";
            }
            return $"SELECT TOP {maxRows} * FROM ({trimmed}) AS T";
        }
    }

    internal class DashboardWidget
    {
        public string InternalId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Query { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MaxRows { get; set; }
        public string RenderMode { get; set; }
        public SAPbouiCOM.Form ParentForm { get; set; }
        public Item Container { get; set; }
        public string RefreshButtonId { get; set; }
        public string ValueItemId { get; set; }
        public string GridItemId { get; set; }
    }
}
