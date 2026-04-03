using System;
using System.Drawing;
using System.IO;

namespace B1TuneUp.Utils
{
    public static class MenuIconProvider
    {
        private static readonly string IconDirectory = Path.Combine(Path.GetTempPath(), "B1TuneUp", "MenuIcons");

        public static string GetIcon(string key, string label, Color background, Color foreground)
        {
            if (string.IsNullOrWhiteSpace(key)) key = Guid.NewGuid().ToString("N");
            if (!Directory.Exists(IconDirectory))
            {
                Directory.CreateDirectory(IconDirectory);
            }
            var filePath = Path.Combine(IconDirectory, $"{key}.png");
            if (!File.Exists(filePath))
            {
                using (var bmp = new Bitmap(32, 32))
                using (var g = Graphics.FromImage(bmp))
                using (var brush = new SolidBrush(background))
                using (var pen = new Pen(Color.White, 1))
                using (var textBrush = new SolidBrush(foreground))
                using (var font = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
                    g.DrawRectangle(pen, 0, 0, bmp.Width - 1, bmp.Height - 1);
                    var text = string.IsNullOrWhiteSpace(label) ? "B1" : label.Trim().ToUpperInvariant();
                    var size = g.MeasureString(text, font);
                    g.DrawString(text, font, textBrush, (bmp.Width - size.Width) / 2, (bmp.Height - size.Height) / 2);
                    bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            return filePath;
        }
    }
}
