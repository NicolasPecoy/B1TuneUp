using System.Windows;
using System.Windows.Forms.Integration;

namespace B1TuneUp.Modules.DragDropUi
{
    public partial class DragDropHelperWindow : Window
    {
        public DragDropHelperWindow()
        {
            InitializeComponent();
            DataContext = new DragDropHelperViewModel();
            ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
