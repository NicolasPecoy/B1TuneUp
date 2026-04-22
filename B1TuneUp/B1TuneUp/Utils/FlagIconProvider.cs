using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace B1TuneUp.Utils
{
    public static class FlagIconProvider
    {
        private static readonly string IconDirectory = Path.Combine(Path.GetTempPath(), "B1TuneUp", "Flags");

        public static string GetIconPath(string countryCode)
        {
            string normalized = NormalizeCountryCode(countryCode);
            if (string.IsNullOrEmpty(normalized)) return string.Empty;

            EnsureDirectory();
            string filePath = Path.Combine(IconDirectory, $"flag_{normalized}.png");
            if (!File.Exists(filePath))
            {
                RenderFlagIcon(filePath, normalized);
            }
            return filePath;
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(IconDirectory))
            {
                Directory.CreateDirectory(IconDirectory);
            }
        }

        private static string NormalizeCountryCode(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return string.Empty;
            var upper = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
            return new string(upper.Where(char.IsLetter).Take(2).ToArray());
        }

        private static void RenderFlagIcon(string filePath, string countryCode)
        {
            const int width = 30;
            const int height = 20;

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                using (var backBrush = new SolidBrush(Color.White))
                using (var borderPen = new Pen(Color.FromArgb(190, 190, 190), 1f))
                {
                    var rect = new RectangleF(0.5f, 0.5f, width - 1f, height - 1f);
                    using (var gp = RoundedRect(rect, 2f))
                    {
                        g.FillPath(backBrush, gp);
                        g.DrawPath(borderPen, gp);
                    }
                }

                string emoji = ToRegionalIndicatorFlag(countryCode);
                if (!string.IsNullOrEmpty(emoji))
                {
                    using (var emojiBrush = new SolidBrush(Color.Black))
                    using (var emojiFont = new Font("Segoe UI Emoji", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
                    {
                        DrawCentered(g, emoji, emojiFont, emojiBrush, width, height);
                    }
                }
                else
                {
                    using (var textBrush = new SolidBrush(Color.FromArgb(31, 78, 121)))
                    using (var textFont = new Font("Segoe UI", 8f, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        DrawCentered(g, countryCode, textFont, textBrush, width, height);
                    }
                }

                bmp.Save(filePath, ImageFormat.Png);
            }
        }

        private static string ToRegionalIndicatorFlag(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2) return string.Empty;

            var upper = countryCode.ToUpperInvariant();
            if (!upper.All(c => c >= 'A' && c <= 'Z')) return string.Empty;

            const int regionalA = 0x1F1E6;
            int first = regionalA + (upper[0] - 'A');
            int second = regionalA + (upper[1] - 'A');
            return char.ConvertFromUtf32(first) + char.ConvertFromUtf32(second);
        }

        private static void DrawCentered(Graphics g, string text, Font font, Brush brush, int width, int height)
        {
            var size = g.MeasureString(text, font);
            float x = (width - size.Width) / 2f;
            float y = (height - size.Height) / 2f;
            g.DrawString(text, font, brush, x, y);
        }

        private static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            float diameter = radius * 2f;
            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
