using System.Collections.ObjectModel;

namespace B1TuneUp.Models
{
    public class ActionPadEntry
    {
        public int DocEntry { get; set; }
        public string FormType { get; set; }
        public string Title { get; set; }
        public string Position { get; set; }
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

        public ActionPadButtonEntry Clone() => (ActionPadButtonEntry)MemberwiseClone();
    }
}
