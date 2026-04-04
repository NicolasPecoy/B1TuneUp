using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using B1TuneUp.Core;
using B1TuneUp.Models;
using SAPbouiCOM;

namespace B1TuneUp.Modules.ActionPadInlineDesigner
{
    public class ActionPadInlineDesignerSession
    {
        public ActionPadInlineDesignerSession(ActionPadEntry pad, SAPbouiCOM.Form form, ObservableCollection<ActionPadInlineDesignerItem> items)
        {
            Pad = pad ?? throw new ArgumentNullException(nameof(pad));
            Items = items ?? new ObservableCollection<ActionPadInlineDesignerItem>();
            FormWidth = form?.Width ?? 850;
            FormHeight = form?.Height ?? 600;
            FormTitle = form?.Title ?? pad.Title ?? "Action Pad";
        }

        public ActionPadEntry Pad { get; }
        public ObservableCollection<ActionPadInlineDesignerItem> Items { get; }
        public double FormWidth { get; }
        public double FormHeight { get; }
        public string FormTitle { get; }

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
                if (item.Source == null) continue;
                item.Source.Left = item.Left;
                item.Source.Top = item.Top;
                item.Source.Width = item.Width;
                item.Source.Height = item.Height;
            }

            if (persist)
            {
                ActionPadService.Save(Pad);
            }
        }
    }
}
