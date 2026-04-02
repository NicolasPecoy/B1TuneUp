using System.Windows.Media;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.IntegrationUi
{
    public class SapThemePalette
    {
        public Color Primary { get; }
        public Color PrimaryDark { get; }
        public Color Accent { get; }
        public Color AccentAlt { get; }
        public Color Background { get; }
        public Color Surface { get; }
        public Color Border { get; }
        public Color TextPrimary { get; }
        public Color TextSecondary { get; }

        private SapThemePalette(Color primary, Color primaryDark, Color accent, Color accentAlt, Color background, Color surface, Color border, Color textPrimary, Color textSecondary)
        {
            Primary = primary;
            PrimaryDark = primaryDark;
            Accent = accent;
            AccentAlt = accentAlt;
            Background = background;
            Surface = surface;
            Border = border;
            TextPrimary = textPrimary;
            TextSecondary = textSecondary;
        }

        private static readonly SapThemePalette SqlPalette = new SapThemePalette(
            (Color)ColorConverter.ConvertFromString("#1F4E79"),
            (Color)ColorConverter.ConvertFromString("#193D60"),
            (Color)ColorConverter.ConvertFromString("#F39C12"),
            (Color)ColorConverter.ConvertFromString("#2E75B6"),
            (Color)ColorConverter.ConvertFromString("#F4F6F9"),
            (Color)ColorConverter.ConvertFromString("#FFFFFF"),
            (Color)ColorConverter.ConvertFromString("#D7DCE3"),
            (Color)ColorConverter.ConvertFromString("#10263C"),
            (Color)ColorConverter.ConvertFromString("#516173")
        );

        private static readonly SapThemePalette HanaPalette = new SapThemePalette(
            (Color)ColorConverter.ConvertFromString("#004990"),
            (Color)ColorConverter.ConvertFromString("#00345F"),
            (Color)ColorConverter.ConvertFromString("#E8730A"),
            (Color)ColorConverter.ConvertFromString("#76B900"),
            (Color)ColorConverter.ConvertFromString("#0B1C2C"),
            (Color)ColorConverter.ConvertFromString("#13283F"),
            (Color)ColorConverter.ConvertFromString("#1E3D5C"),
            (Color)ColorConverter.ConvertFromString("#F6F8FA"),
            (Color)ColorConverter.ConvertFromString("#AEB9C8")
        );

        public static SapThemePalette ForCurrentCompany()
        {
            var isHana = B1App.Instance?.IsHana ?? false;
            return isHana ? HanaPalette : SqlPalette;
        }
    }
}
