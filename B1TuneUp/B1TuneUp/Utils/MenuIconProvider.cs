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
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
                    g.DrawRectangle(pen, 0, 0, bmp.Width - 1, bmp.Height - 1);
                    if (!DrawKnownGlyph(g, key, foreground))
                    {
                        var text = BuildInitials(label);
                        var size = g.MeasureString(text, font);
                        g.DrawString(text, font, textBrush, (bmp.Width - size.Width) / 2, (bmp.Height - size.Height) / 2);
                    }
                    bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            return filePath;
        }

        private static string BuildInitials(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return "B1";
            var parts = label.Trim().Split(new[] { ' ', '/', '&', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
        }

        private static bool DrawKnownGlyph(Graphics g, string key, Color color)
        {
            var normalized = (key ?? string.Empty).ToUpperInvariant();
            using (var pen = new Pen(color, 2))
            using (var brush = new SolidBrush(color))
            {
                if (normalized.Contains("API") || normalized.Contains("INT"))
                {
                    g.DrawString("{}", new Font("Consolas", 15, FontStyle.Bold, GraphicsUnit.Pixel), brush, 6, 7);
                    return true;
                }
                if (normalized.Contains("CONFIG") || normalized.Contains("TOOL") || normalized.Contains("MODULE"))
                {
                    g.DrawEllipse(pen, 8, 8, 16, 16);
                    g.FillEllipse(brush, 13, 13, 6, 6);
                    g.DrawLine(pen, 16, 4, 16, 9);
                    g.DrawLine(pen, 16, 23, 16, 28);
                    g.DrawLine(pen, 4, 16, 9, 16);
                    g.DrawLine(pen, 23, 16, 28, 16);
                    return true;
                }
                if (normalized.Contains("SCHED") || normalized.Contains("TIME"))
                {
                    g.DrawEllipse(pen, 6, 6, 20, 20);
                    g.DrawLine(pen, 16, 16, 16, 9);
                    g.DrawLine(pen, 16, 16, 22, 19);
                    return true;
                }
                if (normalized.Contains("VALID") || normalized.Contains("RULE") || normalized.Contains("MAND"))
                {
                    g.DrawLine(pen, 7, 17, 13, 23);
                    g.DrawLine(pen, 13, 23, 25, 9);
                    return true;
                }
                if (normalized.Contains("EMAIL") || normalized.Contains("LETTER"))
                {
                    g.DrawRectangle(pen, 6, 9, 20, 14);
                    g.DrawLine(pen, 7, 10, 16, 17);
                    g.DrawLine(pen, 25, 10, 16, 17);
                    return true;
                }
                if (normalized.Contains("REPORT") || normalized.Contains("TMPL") || normalized.Contains("SRF"))
                {
                    g.DrawRectangle(pen, 9, 6, 14, 20);
                    g.DrawLine(pen, 12, 12, 20, 12);
                    g.DrawLine(pen, 12, 17, 20, 17);
                    g.DrawLine(pen, 12, 22, 17, 22);
                    return true;
                }
                if (normalized.Contains("DASH") || normalized.Contains("AUDIT") || normalized.Contains("LOG"))
                {
                    g.DrawLine(pen, 7, 24, 25, 24);
                    g.FillRectangle(brush, 9, 16, 3, 8);
                    g.FillRectangle(brush, 15, 10, 3, 14);
                    g.FillRectangle(brush, 21, 13, 3, 11);
                    return true;
                }
                if (normalized.Contains("UI") || normalized.Contains("FORM") || normalized.Contains("ITEM") || normalized.Contains("LAYOUT") || normalized.Contains("PLACEMENT"))
                {
                    g.DrawRectangle(pen, 6, 7, 20, 18);
                    g.DrawLine(pen, 6, 13, 26, 13);
                    g.DrawLine(pen, 13, 13, 13, 25);
                    return true;
                }
                if (normalized.Contains("MAC") || normalized.Contains("AUTO") || normalized.Contains("ACTION") || normalized.Contains("PAD"))
                {
                    Point[] bolt = { new Point(17, 4), new Point(8, 18), new Point(15, 18), new Point(12, 28), new Point(24, 13), new Point(17, 13) };
                    g.FillPolygon(brush, bolt);
                    return true;
                }
                if (normalized.Contains("LANG"))
                {
                    g.DrawEllipse(pen, 6, 6, 20, 20);
                    g.DrawLine(pen, 16, 6, 16, 26);
                    g.DrawArc(pen, 10, 6, 12, 20, 90, 180);
                    g.DrawArc(pen, 10, 6, 12, 20, -90, 180);
                    return true;
                }
                if (normalized.Contains("DELETE"))
                {
                    g.DrawLine(pen, 9, 9, 23, 23);
                    g.DrawLine(pen, 23, 9, 9, 23);
                    return true;
                }
                if (normalized.Contains("ADD") || normalized.Contains("CREATE"))
                {
                    g.DrawLine(pen, 16, 8, 16, 24);
                    g.DrawLine(pen, 8, 16, 24, 16);
                    return true;
                }
                if (normalized.Contains("IMPORT") || normalized.Contains("LOAD"))
                {
                    g.DrawLine(pen, 16, 6, 16, 22);
                    g.DrawLine(pen, 10, 16, 16, 22);
                    g.DrawLine(pen, 22, 16, 16, 22);
                    g.DrawLine(pen, 8, 25, 24, 25);
                    return true;
                }
                if (normalized.Contains("EXPORT") || normalized.Contains("BACKUP"))
                {
                    g.DrawLine(pen, 16, 24, 16, 8);
                    g.DrawLine(pen, 10, 14, 16, 8);
                    g.DrawLine(pen, 22, 14, 16, 8);
                    g.DrawLine(pen, 8, 25, 24, 25);
                    return true;
                }
            }
            return false;
        }
    }
}
