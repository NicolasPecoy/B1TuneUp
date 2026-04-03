using System.Windows;
using System.Windows.Forms.Integration;

namespace B1TuneUp.Modules.QueryExportUi
{
    public partial class QueryExportWindow : Window
    {
        public QueryExportWindow()
        {
            InitializeComponent();
            DataContext = new QueryExportViewModel();
            ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
