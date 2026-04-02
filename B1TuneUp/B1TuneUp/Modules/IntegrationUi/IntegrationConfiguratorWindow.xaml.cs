using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.IntegrationUi
{
    public partial class IntegrationConfiguratorWindow : Window
    {
        private readonly IntegrationConfiguratorViewModel _viewModel;

        public IntegrationConfiguratorWindow()
        {
            InitializeComponent();
            _viewModel = new IntegrationConfiguratorViewModel();
            DataContext = _viewModel;

            ElementHost.EnableModelessKeyboardInterop(this);
            Loaded += OnLoaded;
            Closed += (_, __) => _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            Title = _viewModel != null && B1App.Instance?.IsHana == true
                ? "B1TuneUp Integration Studio · SAP Business One HANA"
                : "B1TuneUp Integration Studio · SAP Business One SQL";
            await _viewModel.LoadAsync();
            UpdateSecretBox();
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
            if (e.PropertyName == nameof(IntegrationConfiguratorViewModel.SelectedIntegration))
            {
                UpdateSecretBox();
            }
        }

        private void UpdateSecretBox()
        {
            if (SecretBox == null) return;
            SecretBox.PasswordChanged -= OnSecretChanged;
            var currentSecret = _viewModel.SelectedIntegration?.AuthSecret ?? string.Empty;
            SecretBox.Password = currentSecret;
            SecretBox.PasswordChanged += OnSecretChanged;
        }

        private void OnSecretChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedIntegration == null) return;
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.SelectedIntegration.AuthSecret = passwordBox.Password;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
