using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using B1TuneUp.Core;
using B1TuneUp.Models;
using SAPbouiCOM;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class InlineDesignerSession
    {
        public InlineDesignerSession(SAPbouiCOM.Form form, ObservableCollection<InlineDesignerItem> items, string defaultItemId, double screenLeft, double screenTop)
        {
            Form = form;
            Items = items;
            DefaultItemId = defaultItemId;
            FormType = form.TypeEx;
            FormTitle = form.Title;
            FormWidth = form.Width;
            FormHeight = form.Height;
            ScreenLeft = screenLeft;
            ScreenTop = screenTop;
        }

        public SAPbouiCOM.Form Form { get; }
        public ObservableCollection<InlineDesignerItem> Items { get; }
        public string DefaultItemId { get; }
        public string FormType { get; }
        public string FormTitle { get; }
        public double FormWidth { get; }
        public double FormHeight { get; }
        public double ScreenLeft { get; }
        public double ScreenTop { get; }

        public static InlineDesignerSession Create(SAPbouiCOM.Form form, string initialItem = null)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var items = new ObservableCollection<InlineDesignerItem>();
            for (int i = 0; i < form.Items.Count; i++)
            {
                var sapItem = form.Items.Item(i + 1);
                var item = new InlineDesignerItem
                {
                    ItemId = sapItem.UniqueID,
                    Caption = TryGetCaption(sapItem),
                    Left = sapItem.Left,
                    Top = sapItem.Top,
                    Width = Math.Max(10, sapItem.Width),
                    Height = Math.Max(6, sapItem.Height),
                    DataBind = TryGetBinding(sapItem)
                };
                items.Add(item);
            }

            double left = TryGetScreenLeft(form);
            double top = TryGetScreenTop(form);

            return new InlineDesignerSession(form, items, initialItem, left, top);
        }

        public void ApplyLayout(bool persistToRepository)
        {
            foreach (var item in Items)
            {
                if (!Form.Items.Exists(item.ItemId)) continue;
                var sapItem = Form.Items.Item(item.ItemId);
                sapItem.Left = (int)Math.Round(item.Left);
                sapItem.Top = (int)Math.Round(item.Top);
                sapItem.Width = (int)Math.Round(item.Width);
                sapItem.Height = (int)Math.Round(item.Height);
            }
            try { Form.Update(); } catch { Form.Refresh(); }
            if (persistToRepository)
            {
                foreach (var item in Items)
                {
                    var entry = new UiCustomizationEntry
                    {
                        Code = null,
                        FormType = FormType,
                        ItemId = item.ItemId,
                        Left = (int)Math.Round(item.Left),
                        Top = (int)Math.Round(item.Top),
                        Width = (int)Math.Round(item.Width),
                        Height = (int)Math.Round(item.Height),
                        Label = item.Caption
                    };
                    UICustomizerService.Save(entry);
                }
            }
        }

        private static string TryGetCaption(Item item)
        {
            try
            {
                if (item?.Specific is EditText et) return et.Value ?? item.UniqueID;
                if (item?.Specific is StaticText st) return st.Caption ?? item.UniqueID;
                if (item?.Specific is Button btn) return btn.Caption ?? item.UniqueID;
                var prop = item?.Specific?.GetType().GetProperty("Caption");
                if (prop != null) return prop.GetValue(item.Specific)?.ToString() ?? item.UniqueID;
            }
            catch { }
            return item?.UniqueID ?? "(item)";
        }

        private static string TryGetBinding(Item item)
        {
            try
            {
                var prop = item?.Specific?.GetType().GetProperty("DataBind");
                if (prop != null)
                {
                    var val = prop.GetValue(item.Specific);
                    if (val != null) return val.ToString();
                }
            }
            catch { }
            return string.Empty;
        }

        private static double TryGetScreenLeft(SAPbouiCOM.Form form)
        {
            try
            {
                var rect = GetSapWindowRect();
                return rect.left + form.Left;
            }
            catch { return SystemParameters.WorkArea.Left + form.Left; }
        }

        private static double TryGetScreenTop(SAPbouiCOM.Form form)
        {
            try
            {
                var rect = GetSapWindowRect();
                return rect.top + form.Top;
            }
            catch { return SystemParameters.WorkArea.Top + form.Top; }
        }

        private static RECT GetSapWindowRect()
        {
            var handle = Process.GetCurrentProcess().MainWindowHandle;
            if (handle == IntPtr.Zero)
            {
                foreach (var proc in Process.GetProcesses().Where(p => p.ProcessName.StartsWith("SAP", StringComparison.OrdinalIgnoreCase)))
                {
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        handle = proc.MainWindowHandle;
                        break;
                    }
                }
            }
            if (handle == IntPtr.Zero) throw new InvalidOperationException("No se pudo determinar la ventana de SAP.");
            if (!GetWindowRect(handle, out var rect))
            {
                throw new InvalidOperationException("No se pudo leer el rectángulo de la ventana SAP.");
            }
            return rect;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
