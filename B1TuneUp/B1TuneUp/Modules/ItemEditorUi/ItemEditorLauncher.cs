using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ItemEditorUi
{
    public static class ItemEditorLauncher
    {
        public static void ShowAddItem(string formUid)
        {
            string key = $"BTUN_ADD_ITEM_{formUid ?? "GLOBAL"}";
            WpfWindowHost.ShowSingleton(key, () => new AddItemWindow(formUid));
        }

        public static void ShowItemEditor(string formUid, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            string key = $"BTUN_ITEM_EDITOR_{formUid ?? "GLOBAL"}_{itemId}";
            WpfWindowHost.ShowSingleton(key, () => new ItemEditorWindow(formUid, itemId));
        }
    }
}
