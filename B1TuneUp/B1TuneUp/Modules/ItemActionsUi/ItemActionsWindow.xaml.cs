using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.ItemActionsUi
{
    public partial class ItemActionsWindow : Window
    {
        private readonly ItemActionsViewModel _viewModel = new ItemActionsViewModel();

        public ItemActionsWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            ElementHost.EnableModelessKeyboardInterop(this);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            ApplyTheme();
            Title = B1App.Instance?.IsHana == true
                ? "B1TuneUp · Item Actions (HANA)"
                : "B1TuneUp · Item Actions (SQL)";
            await _viewModel.LoadAsync();
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

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
