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
    }
}
