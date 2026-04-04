using System;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Threading;
using B1TuneUp.Core;
using B1TuneUp.Modules.IntegrationUi;

namespace B1TuneUp.Modules.AutomationDashboardUi
{
    public partial class AutomationDashboardWindow : Window
    {
        private readonly AutomationDashboardViewModel _viewModel;
        private readonly DispatcherTimer _autoRefreshTimer;

        public AutomationDashboardWindow()
        {
            InitializeComponent();
            _viewModel = new AutomationDashboardViewModel();
            DataContext = _viewModel;

            ElementHost.EnableModelessKeyboardInterop(this);
            Loaded += OnLoaded;
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoRefreshTimer.Tick += OnAutoRefreshTick;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            ApplyTheme();
            Title = B1App.Instance?.IsHana == true
                ? "B1TuneUp Automation Dashboard - SAP Business One HANA"
                : "B1TuneUp Automation Dashboard - SAP Business One SQL";
            await _viewModel.LoadAsync();
            _autoRefreshTimer.Start();
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

        private async void OnAutoRefreshTick(object sender, EventArgs e)
        {
            await _viewModel.TryAutoRefreshAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _autoRefreshTimer.Tick -= OnAutoRefreshTick;
            _autoRefreshTimer.Stop();
        }
    }
}






