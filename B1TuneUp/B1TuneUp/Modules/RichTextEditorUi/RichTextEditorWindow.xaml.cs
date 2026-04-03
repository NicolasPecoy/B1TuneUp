using System.Windows;
using System.Windows.Forms.Integration;

namespace B1TuneUp.Modules.RichTextEditorUi
{
    public partial class RichTextEditorWindow : Window
    {
        public RichTextEditorWindow(string itemId)
        {
            InitializeComponent();
            DataContext = new RichTextEditorViewModel(itemId);
            ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
