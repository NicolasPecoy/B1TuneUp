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
        private TextBox _activeBodyBox;

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
            if (e.PropertyName == nameof(ApiStudioViewModel.SelectedRequest) ||
                e.PropertyName == nameof(ApiStudioViewModel.LastResponse))
            {
                RefreshHighlightedBodies();
            }
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

        private void OnBodyTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.RefreshGeneratedArtifacts();
            RefreshHighlightedBodies();
        }

        private void OnBodyGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _activeBodyBox = sender as TextBox;
        }

        private void RefreshHighlightedBodies()
        {
            ApiStudioSyntaxHighlighter.Apply(RequestHighlightViewer, RequestBodyBox?.Text);
            ApiStudioSyntaxHighlighter.Apply(ResponseHighlightViewer, ResponseBodyBox?.Text);
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ShowFindPanel();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && FindPanel.Visibility == Visibility.Visible)
            {
                HideFindPanel();
                e.Handled = true;
            }
        }

        private void ShowFindPanel()
        {
            if (_activeBodyBox == null)
            {
                _activeBodyBox = ResponseBodyBox?.IsKeyboardFocusWithin == true ? ResponseBodyBox : RequestBodyBox;
            }

            if (_activeBodyBox != null && !string.IsNullOrEmpty(_activeBodyBox.SelectedText))
            {
                FindTextBox.Text = _activeBodyBox.SelectedText;
            }

            FindStatusText.Text = _activeBodyBox == ResponseBodyBox ? "response" : "request";
            FindPanel.Visibility = Visibility.Visible;
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }

        private void HideFindPanel()
        {
            FindPanel.Visibility = Visibility.Collapsed;
            _activeBodyBox?.Focus();
        }

        private void OnFindTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideFindPanel();
                e.Handled = true;
            }
        }

        private void OnFindNextClick(object sender, RoutedEventArgs e) => FindNext();

        private void OnFindCloseClick(object sender, RoutedEventArgs e) => HideFindPanel();

        private void FindNext()
        {
            var target = _activeBodyBox ?? RequestBodyBox;
            var term = FindTextBox.Text;
            if (target == null || string.IsNullOrEmpty(term))
            {
                FindStatusText.Text = "sin texto";
                return;
            }

            var text = target.Text ?? string.Empty;
            var start = target.SelectionStart + target.SelectionLength;
            var index = text.IndexOf(term, start, System.StringComparison.OrdinalIgnoreCase);
            if (index < 0 && start > 0)
            {
                index = text.IndexOf(term, 0, System.StringComparison.OrdinalIgnoreCase);
            }

            if (index < 0)
            {
                FindStatusText.Text = "no encontrado";
                return;
            }

            target.Focus();
            target.Select(index, term.Length);
            var line = target.GetLineIndexFromCharacterIndex(index);
            target.ScrollToLine(line);
            FindStatusText.Text = $"{(target == ResponseBodyBox ? "response" : "request")} L{line + 1}";
            FindTextBox.Focus();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
