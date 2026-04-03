using B1TuneUp.Utils;

namespace B1TuneUp.Modules.BarcodeScannerUi
{
    public static class BarcodeScannerLauncher
    {
        public static void Show(string targetItemId)
        {
            var key = string.IsNullOrWhiteSpace(targetItemId) ? typeof(BarcodeScannerWindow).FullName : $"BCR_{targetItemId}";
            WpfWindowHost.ShowSingleton(key, () => new BarcodeScannerWindow(targetItemId));
        }
    }
}
