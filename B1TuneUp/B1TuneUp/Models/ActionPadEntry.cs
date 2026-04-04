using System.Collections.ObjectModel;

namespace B1TuneUp.Models
{
    public class ActionPadEntry
    {
        public int DocEntry { get; set; }
        public string FormType { get; set; }
        public string Title { get; set; }
        public string Position { get; set; }
        public int Columns { get; set; } = 1;
        public int ButtonWidth { get; set; } = 120;
        public int ButtonHeight { get; set; } = 22;
        public string DockMode { get; set; } = "Floating";
        public bool FollowForm { get; set; } = true;
        public ObservableCollection<ActionPadButtonEntry> Buttons { get; set; } = new ObservableCollection<ActionPadButtonEntry>();

        public ActionPadEntry Clone()
        {
            var clone = (ActionPadEntry)MemberwiseClone();
            clone.Buttons = new ObservableCollection<ActionPadButtonEntry>();
            foreach (var button in Buttons)
            {
                clone.Buttons.Add(button.Clone());
            }
            return clone;
        }
    }

    public class ActionPadButtonEntry
    {
        public int DocEntry { get; set; }
        public int PadEntry { get; set; }
        public string Label { get; set; }
        public string Action { get; set; }
        public int Order { get; set; }
        public string Tooltip { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string HotKey { get; set; }
        public int GridRow { get; set; } = -1;
        public int GridCol { get; set; } = -1;
        public int ColSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public ActionPadButtonEntry Clone() => (ActionPadButtonEntry)MemberwiseClone();
    }
}
