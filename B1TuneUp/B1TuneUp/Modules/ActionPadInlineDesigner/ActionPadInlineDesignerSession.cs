using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules;
using SAPbouiCOM;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public class ActionPadInlineDesignerSession
    {
        public ActionPadInlineDesignerSession(ActionPadEntry pad, SAPbouiCOM.Form form, ObservableCollection<ActionPadInlineDesignerItem> items)
        {
            Pad = pad ?? throw new ArgumentNullException(nameof(pad));
            Items = items ?? new ObservableCollection<ActionPadInlineDesignerItem>();
            Form = form;
            FormWidth = form?.Width ?? 850;
            FormHeight = form?.Height ?? 600;
            FormTitle = form?.Title ?? pad.Title ?? "Action Pad";
            FormType = form?.TypeEx ?? pad.FormType;
            ScreenLeft = TryGetScreenLeft(form);
            ScreenTop = TryGetScreenTop(form);
        }

        public ActionPadEntry Pad { get; }
        public ObservableCollection<ActionPadInlineDesignerItem> Items { get; }
        public SAPbouiCOM.Form Form { get; }
        public double FormWidth { get; private set; }
        public double FormHeight { get; private set; }
        public string FormTitle { get; }
        public string FormType { get; }
        public double ScreenLeft { get; private set; }
        public double ScreenTop { get; private set; }

        public static ActionPadInlineDesignerSession Create(ActionPadEntry pad, SAPbouiCOM.Form form)
        {
            if (pad == null) throw new ArgumentNullException(nameof(pad));
            var items = new ObservableCollection<ActionPadInlineDesignerItem>();
            double defaultWidth = Math.Max(80, pad.ButtonWidth);
            double defaultHeight = Math.Max(24, pad.ButtonHeight);

            foreach (var button in pad.Buttons.OrderBy(b => b.Order))
            {
                var item = new ActionPadInlineDesignerItem(button, defaultWidth, defaultHeight);
                if (item.Width <= 0) item.Width = defaultWidth;
                if (item.Height <= 0) item.Height = defaultHeight;
                items.Add(item);
            }

            return new ActionPadInlineDesignerSession(pad, form, items);
        }

        public void ApplyLayout(bool persist)
        {
            foreach (var item in Items)
            {
                PushLiveItem(item);
            }

            if (persist)
            {
                ActionPadService.Save(Pad);
                foreach (var item in Items)
                {
                    var button = item.Source;
                    if (button == null) continue;
                    string label = string.IsNullOrWhiteSpace(button.Label) ? button.Action : button.Label;
                    AuditLogManager.LogAction("ActionPadDesigner",
                        $"[{Pad.FormType}] {label} => L:{item.Left} T:{item.Top} W:{item.Width} H:{item.Height}");
                }
            }
        }

        public void PushLiveItem(ActionPadInlineDesignerItem item)
        {
            if (item?.Source == null) return;
            item.Source.Left = item.Left;
            item.Source.Top = item.Top;
            item.Source.Width = item.Width;
            item.Source.Height = item.Height;
        }

        public SurfaceSnapshot? TryCaptureSurface()
        {
            try
            {
                var rect = GetSapWindowRect();
                double left = rect.left + (Form?.Left ?? 0);
                double top = rect.top + (Form?.Top ?? 0);
                double width = Form?.Width ?? FormWidth;
                double height = Form?.Height ?? FormHeight;
                ScreenLeft = left;
                ScreenTop = top;
                FormWidth = width;
                FormHeight = height;
                return new SurfaceSnapshot { Left = left, Top = top, Width = width, Height = height };
            }
            catch
            {
                return null;
            }
        }

        private static double TryGetScreenLeft(SAPbouiCOM.Form form)
        {
            try
            {
                var rect = GetSapWindowRect();
                return rect.left + (form?.Left ?? 0);
            }
            catch { return SystemParameters.WorkArea.Left + (form?.Left ?? 0); }
        }

        private static double TryGetScreenTop(SAPbouiCOM.Form form)
        {
            try
            {
                var rect = GetSapWindowRect();
                return rect.top + (form?.Top ?? 0);
            }
            catch { return SystemParameters.WorkArea.Top + (form?.Top ?? 0); }
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
                throw new InvalidOperationException("No se pudo leer el rectangulo de la ventana SAP.");
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

        public struct SurfaceSnapshot
        {
            public double Left;
            public double Top;
            public double Width;
            public double Height;
        }
    }
}
