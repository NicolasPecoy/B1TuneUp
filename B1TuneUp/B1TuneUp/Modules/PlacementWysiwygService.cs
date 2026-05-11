using System;
using System.Collections.Generic;
using System.Linq;
using B1TuneUp.Models;
using B1TuneUp.Utils;
using SAPbouiCOM;

namespace B1TuneUp.Modules
{
    public static class PlacementWysiwygService
    {
        private static readonly Stack<IList<PlacementDesignerItem>> UndoStack = new Stack<IList<PlacementDesignerItem>>();

        public static IList<PlacementDesignerItem> CaptureActiveForm()
        {
            var form = SapUiSafe.TryGetActiveForm();
            if (form == null) return new List<PlacementDesignerItem>();
            var items = new List<PlacementDesignerItem>();
            for (int i = 0; i < form.Items.Count; i++)
            {
                Item item = null;
                try { item = form.Items.Item(i); } catch { }
                if (item == null) continue;
                items.Add(new PlacementDesignerItem
                {
                    ItemId = item.UniqueID,
                    Caption = SapUiSafe.SafeCaption(item),
                    ItemType = item.Type.ToString(),
                    Left = item.Left,
                    Top = item.Top,
                    Width = item.Width,
                    Height = item.Height,
                    FromPane = item.FromPane,
                    ToPane = item.ToPane,
                    Visible = SafeVisible(item),
                    Enabled = SafeEnabled(item)
                });
            }
            PushUndo(items);
            return items;
        }

        public static IList<PlacementDesignerItem> Snap(IList<PlacementDesignerItem> items, int grid = 5)
        {
            var copy = Clone(items);
            foreach (var item in copy)
            {
                item.Left = SnapValue(item.Left, grid);
                item.Top = SnapValue(item.Top, grid);
                item.Width = Math.Max(grid, SnapValue(item.Width, grid));
                item.Height = Math.Max(grid, SnapValue(item.Height, grid));
            }
            PushUndo(items);
            return copy;
        }

        public static IList<PlacementDiffEntry> Diff(IList<PlacementDesignerItem> before, IList<PlacementDesignerItem> after)
        {
            var result = new List<PlacementDiffEntry>();
            var map = (before ?? Array.Empty<PlacementDesignerItem>()).ToDictionary(i => i.ItemId ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            foreach (var item in after ?? Array.Empty<PlacementDesignerItem>())
            {
                if (!map.TryGetValue(item.ItemId ?? string.Empty, out var old))
                {
                    result.Add(new PlacementDiffEntry { ItemId = item.ItemId, Property = "Item", Before = "(missing)", After = "Added" });
                    continue;
                }
                AddDiff(result, item.ItemId, "Left", old.Left, item.Left);
                AddDiff(result, item.ItemId, "Top", old.Top, item.Top);
                AddDiff(result, item.ItemId, "Width", old.Width, item.Width);
                AddDiff(result, item.ItemId, "Height", old.Height, item.Height);
                AddDiff(result, item.ItemId, "Pane", $"{old.FromPane}-{old.ToPane}", $"{item.FromPane}-{item.ToPane}");
                AddDiff(result, item.ItemId, "Visible", old.Visible, item.Visible);
                AddDiff(result, item.ItemId, "Enabled", old.Enabled, item.Enabled);
            }
            return result;
        }

        public static void Preview(IList<PlacementDesignerItem> items, string userCode, string role, string branch)
        {
            Apply(items, false, userCode, role, branch);
        }

        public static void SaveVersion(IList<PlacementDesignerItem> items, string formType, string userCode, string role, string branch)
        {
            foreach (var item in items ?? Array.Empty<PlacementDesignerItem>())
            {
                UICustomizerService.Save(new UiCustomizationEntry
                {
                    FormType = formType,
                    ItemId = item.ItemId,
                    Action = item.Visible ? (item.Enabled ? "Move" : "Disable") : "Hide",
                    Left = item.Left,
                    Top = item.Top,
                    Width = item.Width,
                    Height = item.Height,
                    FromPane = item.FromPane,
                    ToPane = item.ToPane,
                    UserCode = userCode,
                    UserGroup = role,
                    Variant = branch,
                    Priority = 10
                });
            }
            AuditLogManager.LogAction("PlacementWysiwyg", $"Version saved for {formType} user={userCode} role={role} branch={branch}", "Save");
        }

        public static IList<PlacementDesignerItem> Undo()
        {
            return UndoStack.Count == 0 ? new List<PlacementDesignerItem>() : Clone(UndoStack.Pop());
        }

        private static void Apply(IList<PlacementDesignerItem> items, bool persist, string userCode, string role, string branch)
        {
            var form = SapUiSafe.TryGetActiveForm();
            if (form == null) return;
            foreach (var model in items ?? Array.Empty<PlacementDesignerItem>())
            {
                var item = SapUiSafe.TryGetItem(form, model.ItemId);
                if (item == null) continue;
                try
                {
                    item.Left = model.Left;
                    item.Top = model.Top;
                    item.Width = model.Width;
                    item.Height = model.Height;
                    item.FromPane = model.FromPane;
                    item.ToPane = model.ToPane;
                    item.Visible = model.Visible;
                    item.Enabled = model.Enabled;
                }
                catch (Exception ex)
                {
                    ExceptionLogger.LogHandled(ex, $"PlacementWysiwygService.Apply:{model.ItemId}");
                }
            }
            try { form.Update(); } catch { try { form.Refresh(); } catch { } }
        }

        private static int SnapValue(int value, int grid) => (int)Math.Round(value / (double)Math.Max(1, grid)) * Math.Max(1, grid);

        private static void AddDiff<T>(ICollection<PlacementDiffEntry> list, string itemId, string prop, T before, T after)
        {
            if (!Equals(before, after)) list.Add(new PlacementDiffEntry { ItemId = itemId, Property = prop, Before = before?.ToString(), After = after?.ToString() });
        }

        private static void PushUndo(IList<PlacementDesignerItem> items)
        {
            if (items != null) UndoStack.Push(Clone(items));
        }

        private static IList<PlacementDesignerItem> Clone(IList<PlacementDesignerItem> items)
        {
            return (items ?? Array.Empty<PlacementDesignerItem>()).Select(i => new PlacementDesignerItem
            {
                ItemId = i.ItemId,
                Caption = i.Caption,
                ItemType = i.ItemType,
                Left = i.Left,
                Top = i.Top,
                Width = i.Width,
                Height = i.Height,
                FromPane = i.FromPane,
                ToPane = i.ToPane,
                Visible = i.Visible,
                Enabled = i.Enabled
            }).ToList();
        }

        private static bool SafeVisible(Item item) { try { return item.Visible; } catch { return true; } }
        private static bool SafeEnabled(Item item) { try { return item.Enabled; } catch { return true; } }
    }
}
