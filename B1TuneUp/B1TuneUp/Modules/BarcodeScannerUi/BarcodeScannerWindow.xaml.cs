using System.Windows;
using System.Windows.Forms.Integration;

namespace B1TuneUp.Modules.BarcodeScannerUi
{
    public partial class BarcodeScannerWindow : Window
    {
        public BarcodeScannerWindow(string targetItemId)
        {
            InitializeComponent();
            DataContext = new BarcodeScannerViewModel(targetItemId);
            ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
