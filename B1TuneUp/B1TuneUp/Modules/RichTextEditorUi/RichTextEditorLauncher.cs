using B1TuneUp.Utils;

namespace B1TuneUp.Modules.RichTextEditorUi
{
    public static class RichTextEditorLauncher
    {
        public static void Show(string itemId)
        {
            var key = string.IsNullOrWhiteSpace(itemId) ? typeof(RichTextEditorWindow).FullName : $"RTE_{itemId}";
            WpfWindowHost.ShowSingleton(key, () => new RichTextEditorWindow(itemId));
        }
    }
}
