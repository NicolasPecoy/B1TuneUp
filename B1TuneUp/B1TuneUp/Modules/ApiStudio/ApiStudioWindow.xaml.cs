using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.ApiStudio
{
    public partial class ApiStudioWindow : Window
    {
        private readonly ApiStudioViewModel _viewModel;

        public ApiStudioWindow()
        {
            InitializeComponent();
            _viewModel = new ApiStudioViewModel();
            DataContext = _viewModel;

            ElementHost.EnableModelessKeyboardInterop(this);
            Loaded += OnLoaded;
            Closing += OnClosing;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            Title = B1App.Instance?.IsHana == true
                ? "B1TuneUp API Studio - SAP Business One HANA"
                : "B1TuneUp API Studio - SAP Business One SQL";
            _viewModel.Load();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _viewModel.Save();
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        private void ApplyTheme()
        {
            var palette = SapThemePalette.ForCurrentCompany();
            Resources["PrimaryColor"] = palette.Primary;
            Resources["PrimaryDarkColor"] = palette.PrimaryDark;
            Resources["AccentColor"] = palette.Accent;
            Resources["AccentAltColor"] = palette.AccentAlt;
            Resources["SurfaceColor"] = palette.Surface;
            Resources["SurfaceAltColor"] = palette.Surface;
            Resources["BorderColor"] = palette.Border;
            Resources["TextPrimaryColor"] = palette.TextPrimary;
            Resources["TextSecondaryColor"] = palette.TextSecondary;
            Resources["WindowBackgroundBrush"] = new SolidColorBrush(palette.Background);

            Resources["PrimaryBrush"] = new SolidColorBrush(palette.Primary);
            Resources["PrimaryDarkBrush"] = new SolidColorBrush(palette.PrimaryDark);
            Resources["AccentBrush"] = new SolidColorBrush(palette.Accent);
            Resources["AccentAltBrush"] = new SolidColorBrush(palette.AccentAlt);
            Resources["SurfaceBrush"] = new SolidColorBrush(palette.Surface);
            Resources["SurfaceAltBrush"] = new SolidColorBrush(palette.Surface);
            Resources["BorderBrush"] = new SolidColorBrush(palette.Border);
            Resources["TextPrimaryBrush"] = new SolidColorBrush(palette.TextPrimary);
            Resources["TextSecondaryBrush"] = new SolidColorBrush(palette.TextSecondary);
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void OnTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _viewModel.SelectNode(e.NewValue);
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ApiRequestSearchResult result)
            {
                _viewModel.SelectSearchResult(result);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
