using System.Windows;
using System.Windows.Forms.Integration;

namespace B1TuneUp.Modules.LanguageSelectorUi
{
    public partial class LanguageSelectorWindow : Window
    {
        public LanguageSelectorWindow()
        {
            InitializeComponent();
            DataContext = new LanguageSelectorViewModel();
            ElementHost.EnableModelessKeyboardInterop(this);
        }
    }
}
