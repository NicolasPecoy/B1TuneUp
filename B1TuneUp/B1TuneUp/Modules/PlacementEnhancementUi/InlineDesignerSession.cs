using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using B1TuneUp.Core;
using B1TuneUp.Models;
using B1TuneUp.Modules;
using B1TuneUp.Utils;
using SAPbouiCOM;

namespace B1TuneUp.Modules.PlacementEnhancementUi
{
    public class InlineDesignerSession
    {
        private readonly List<UiCustomizationEntry> _existingEntries;
        private readonly Dictionary<string, List<UiCustomizationEntry>> _entriesByItem;

        public InlineDesignerSession(SAPbouiCOM.Form form,
            ObservableCollection<InlineDesignerItem> items,
            string defaultItemId,
            double screenLeft,
            double screenTop,
            IList<UiCustomizationEntry> existingEntries)
        {
            Form = form;
            Items = items ?? new ObservableCollection<InlineDesignerItem>();
            DefaultItemId = defaultItemId;
            FormType = form?.TypeEx;
            FormTitle = form?.Title ?? "Editar con TuneUp";
            FormWidth = form?.Width ?? 900;
            FormHeight = form?.Height ?? 600;
            ScreenLeft = screenLeft;
            ScreenTop = screenTop;
            _existingEntries = existingEntries != null
                ? new List<UiCustomizationEntry>(existingEntries)
                : new List<UiCustomizationEntry>();
            _entriesByItem = BuildEntryMap(_existingEntries);
            AttachExistingMetadata();
        }

        public SAPbouiCOM.Form Form { get; }
        public ObservableCollection<InlineDesignerItem> Items { get; }
        public string DefaultItemId { get; }
        public string FormType { get; }
        public string FormTitle { get; }
        public double FormWidth { get; private set; }
        public double FormHeight { get; private set; }
        public double ScreenLeft { get; private set; }
        public double ScreenTop { get; private set; }

        public static InlineDesignerSession Create(SAPbouiCOM.Form form, string initialItem = null)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var items = new ObservableCollection<InlineDesignerItem>();
            for (int i = 0; i < form.Items.Count; i++)
            {
                var sapItem = SapUiSafe.TryGetItem(form, i + 1);
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
            var existing = SafeGetCustomizations(form.TypeEx);

            return new InlineDesignerSession(form, items, initialItem, left, top, existing);
        }

        public void ApplyLayout(bool persistToRepository)
        {
            foreach (var item in Items)
            {
                PushLiveItem(item);
            }

            try { Form?.Update(); }
            catch { Form?.Refresh(); }

            if (!persistToRepository)
                return;

            foreach (var item in Items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                    continue;

                var entry = ResolveEntry(item);
                entry.FormType = FormType;
                entry.ItemId = item.ItemId;
                entry.Action = item.ActionType;
                entry.Left = Round(item.Left);
                entry.Top = Round(item.Top);
                entry.Width = Round(item.Width);
                entry.Height = Round(item.Height);
                entry.Label = string.IsNullOrWhiteSpace(item.CustomLabel) ? item.Caption : item.CustomLabel;
                entry.UserCode = item.ScopeUserCode ?? string.Empty;
                entry.UserGroup = item.ScopeUserGroup ?? string.Empty;
                entry.Localization = item.ScopeLocalization ?? string.Empty;
                entry.Variant = item.ScopeVariant ?? string.Empty;
                entry.DependsOn = item.ScopeDependsOn ?? string.Empty;
                entry.InheritFrom = item.ScopeInheritFrom ?? string.Empty;
                entry.Priority = item.ScopePriority <= 0 ? 10 : item.ScopePriority;
                entry.Condition = item.Condition ?? string.Empty;

                var saved = UICustomizerService.Save(entry);
                if (saved != null)
                {
                    item.EntryCode = saved.Code;
                    RegisterEntry(saved);
                }

                AuditLogManager.LogAction("InlineDesigner",
                    $"[{FormType}] {item.ItemId} => L:{entry.Left} T:{entry.Top} W:{entry.Width} H:{entry.Height} Scope:{DescribeScope(entry)}");
            }
        }

        public IReadOnlyList<UiCustomizationEntry> GetEntriesForItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return Array.Empty<UiCustomizationEntry>();
            if (!_entriesByItem.TryGetValue(itemId, out var list) || list.Count == 0)
                return Array.Empty<UiCustomizationEntry>();
            return list
                .OrderBy(e => e.Priority)
                .Select(e => e.Clone())
                .ToList();
        }

        public void RefreshFromForm()
        {
            if (Form == null) return;
            foreach (var item in Items)
            {
                try
                {
                    if (!Form.Items.Exists(item.ItemId)) continue;
                    var sapItem = SapUiSafe.TryGetItem(Form, item.ItemId);
                    item.Left = sapItem.Left;
                    item.Top = sapItem.Top;
                    item.Width = Math.Max(10, sapItem.Width);
                    item.Height = Math.Max(6, sapItem.Height);
                }
                catch
                {
                    // ignore sync issues for disposed items
                }
            }
        }

        public void PushLiveItem(InlineDesignerItem item)
        {
            if (item == null || Form == null) return;
            try
            {
                if (!Form.Items.Exists(item.ItemId)) return;
                var sapItem = SapUiSafe.TryGetItem(Form, item.ItemId);
                sapItem.Left = Round(item.Left);
                sapItem.Top = Round(item.Top);
                sapItem.Width = Round(Math.Max(10, item.Width));
                sapItem.Height = Round(Math.Max(6, item.Height));
            }
            catch
            {
                // live push best-effort only
            }
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

                return new SurfaceSnapshot
                {
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height
                };
            }
            catch
            {
                return null;
            }
        }

        private void AttachExistingMetadata()
        {
            foreach (var item in Items)
            {
                var entry = GetDefaultEntryFor(item.ItemId);
                if (entry != null)
                {
                    item.ApplyEntry(entry);
                }
            }
        }

        private UiCustomizationEntry ResolveEntry(InlineDesignerItem item)
        {
            UiCustomizationEntry entry = null;
            if (!string.IsNullOrWhiteSpace(item.EntryCode))
            {
                entry = _existingEntries.FirstOrDefault(e =>
                    string.Equals(e.Code, item.EntryCode, StringComparison.OrdinalIgnoreCase));
            }

            if (entry == null)
            {
                entry = _existingEntries.FirstOrDefault(e =>
                    string.Equals(e.ItemId, item.ItemId, StringComparison.OrdinalIgnoreCase) &&
                    MatchesScope(e, item.ToScope()));
            }

            if (entry == null)
            {
                entry = new UiCustomizationEntry
                {
                    ItemId = item.ItemId,
                    FormType = FormType,
                    Action = item.ActionType
                };
                _existingEntries.Add(entry);
                RegisterEntry(entry);
            }
            return entry;
        }

        private UiCustomizationEntry GetDefaultEntryFor(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            if (!_entriesByItem.TryGetValue(itemId, out var list) || list.Count == 0) return null;
            return list.OrderBy(e => e.Priority).FirstOrDefault();
        }

        private void RegisterEntry(UiCustomizationEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId)) return;

            if (!_existingEntries.Contains(entry))
            {
                _existingEntries.Add(entry);
            }

            if (!_entriesByItem.TryGetValue(entry.ItemId, out var list))
            {
                list = new List<UiCustomizationEntry>();
                _entriesByItem[entry.ItemId] = list;
            }

            if (!list.Contains(entry))
            {
                list.Add(entry);
            }
        }

        private static Dictionary<string, List<UiCustomizationEntry>> BuildEntryMap(IEnumerable<UiCustomizationEntry> entries)
        {
            return entries?
                .Where(e => !string.IsNullOrWhiteSpace(e.ItemId))
                .GroupBy(e => e.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase)
                   ?? new Dictionary<string, List<UiCustomizationEntry>>(StringComparer.OrdinalIgnoreCase);
        }

        private static bool MatchesScope(UiCustomizationEntry entry, UiCustomizationScope scope)
        {
            if (scope == null)
            {
                return string.IsNullOrWhiteSpace(entry.UserCode)
                    && string.IsNullOrWhiteSpace(entry.UserGroup)
                    && string.IsNullOrWhiteSpace(entry.Localization)
                    && string.IsNullOrWhiteSpace(entry.Variant);
            }

            return string.Equals(Normalize(entry.UserCode), Normalize(scope.UserCode), StringComparison.OrdinalIgnoreCase)
                && string.Equals(Normalize(entry.UserGroup), Normalize(scope.UserGroup), StringComparison.OrdinalIgnoreCase)
                && string.Equals(Normalize(entry.Localization), Normalize(scope.Localization), StringComparison.OrdinalIgnoreCase)
                && string.Equals(Normalize(entry.Variant), Normalize(scope.Variant), StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static IList<UiCustomizationEntry> SafeGetCustomizations(string formType)
        {
            try
            {
                return UICustomizerService.GetAll(formType);
            }
            catch
            {
                return new List<UiCustomizationEntry>();
            }
        }

        private static string TryGetCaption(Item item)
        {
            try
            {
                return SapUiSafe.SafeCaption(item);
            }
            catch { }
            return item?.UniqueID ?? "(item)";
        }

        private static string TryGetBinding(Item item)
        {
            try
            {
                return SapUiSafe.SafeSpecificProperty(item, "DataBind");
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

        private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        private static string DescribeScope(UiCustomizationEntry entry)
        {
            string user = string.IsNullOrWhiteSpace(entry.UserCode) ? "*" : entry.UserCode;
            string group = string.IsNullOrWhiteSpace(entry.UserGroup) ? "*" : entry.UserGroup;
            string locale = string.IsNullOrWhiteSpace(entry.Localization) ? "*" : entry.Localization;
            string variant = string.IsNullOrWhiteSpace(entry.Variant) ? "*" : entry.Variant;
            return $"U:{user} G:{group} L:{locale} V:{variant}";
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
