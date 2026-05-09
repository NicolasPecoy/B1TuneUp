using System.Windows;

namespace B1TuneUp.Modules.ConfigCenter
{
    public partial class ConfigCenterWindow : Window
    {
        private readonly ConfigCenterViewModel _viewModel;

        public ConfigCenterWindow()
        {
            InitializeComponent();
            _viewModel = new ConfigCenterViewModel();
            DataContext = _viewModel;
            Loaded += async (_, __) => await _viewModel.LoadAsync();
        }
    }
}
