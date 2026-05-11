using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace B1TuneUp.Modules.ApiStudio
{
    public static class ApiStudioSyntaxHighlighter
    {
        private static readonly Brush JsonPropertyBrush = new SolidColorBrush(Color.FromRgb(31, 78, 121));
        private static readonly Brush JsonStringBrush = new SolidColorBrush(Color.FromRgb(39, 124, 75));
        private static readonly Brush JsonNumberBrush = new SolidColorBrush(Color.FromRgb(128, 83, 172));
        private static readonly Brush JsonLiteralBrush = new SolidColorBrush(Color.FromRgb(190, 96, 24));
        private static readonly Brush XmlTagBrush = new SolidColorBrush(Color.FromRgb(31, 78, 121));
        private static readonly Brush XmlAttributeBrush = new SolidColorBrush(Color.FromRgb(128, 83, 172));
        private static readonly Brush XmlValueBrush = new SolidColorBrush(Color.FromRgb(39, 124, 75));
        private static readonly Brush CommentBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125));
        private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(16, 38, 60));

        public static void Apply(RichTextBox viewer, string text)
        {
            if (viewer == null) return;
            var document = new FlowDocument
            {
                PagePadding = new System.Windows.Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };

            var paragraph = new Paragraph { Margin = new System.Windows.Thickness(0) };
            document.Blocks.Add(paragraph);

            if (string.IsNullOrWhiteSpace(text))
            {
                viewer.Document = document;
                return;
            }

            var trimmed = text.TrimStart();
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                AppendXml(paragraph, text);
            }
            else
            {
                AppendJson(paragraph, text);
            }

            viewer.Document = document;
        }

        private static void AppendJson(Paragraph paragraph, string text)
        {
            var pattern = "\"(?:\\\\.|[^\"\\\\])*\"\\s*:|\"(?:\\\\.|[^\"\\\\])*\"|-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?|\\b(?:true|false|null)\\b|[{}\\[\\],:]";
            AppendMatches(paragraph, text, pattern, match =>
            {
                var value = match.Value;
                if (value.TrimEnd().EndsWith(":", StringComparison.Ordinal))
                {
                    return JsonPropertyBrush;
                }
                if (value.StartsWith("\"", StringComparison.Ordinal))
                {
                    return JsonStringBrush;
                }
                if (Regex.IsMatch(value, "^-?\\d"))
                {
                    return JsonNumberBrush;
                }
                if (value == "true" || value == "false" || value == "null")
                {
                    return JsonLiteralBrush;
                }
                return TextBrush;
            });
        }

        private static void AppendXml(Paragraph paragraph, string text)
        {
            var pattern = "<!--[\\s\\S]*?-->|<[^>]+>|[^<]+";
            AppendMatches(paragraph, text, pattern, match =>
            {
                var value = match.Value;
                if (value.StartsWith("<!--", StringComparison.Ordinal)) return CommentBrush;
                if (value.StartsWith("<", StringComparison.Ordinal)) return XmlTagBrush;
                return TextBrush;
            }, AppendXmlToken);
        }

        private static void AppendXmlToken(Paragraph paragraph, string token, Brush fallback)
        {
            if (!token.StartsWith("<", StringComparison.Ordinal) || token.StartsWith("<!--", StringComparison.Ordinal))
            {
                AppendRun(paragraph, token, fallback);
                return;
            }

            var attrPattern = "\\s+([\\w:.-]+)(=)(\"[^\"]*\"|'[^']*')";
            int index = 0;
            foreach (Match attr in Regex.Matches(token, attrPattern))
            {
                if (attr.Index > index)
                {
                    AppendRun(paragraph, token.Substring(index, attr.Index - index), XmlTagBrush);
                }

                AppendRun(paragraph, attr.Groups[1].Value, XmlAttributeBrush);
                AppendRun(paragraph, attr.Groups[2].Value, TextBrush);
                AppendRun(paragraph, attr.Groups[3].Value, XmlValueBrush);
                index = attr.Index + attr.Length;
            }

            if (index < token.Length)
            {
                AppendRun(paragraph, token.Substring(index), XmlTagBrush);
            }
        }

        private static void AppendMatches(Paragraph paragraph, string text, string pattern, Func<Match, Brush> brushSelector, Action<Paragraph, string, Brush> customAppender = null)
        {
            int index = 0;
            foreach (Match match in Regex.Matches(text, pattern))
            {
                if (match.Index > index)
                {
                    AppendRun(paragraph, text.Substring(index, match.Index - index), TextBrush);
                }

                var brush = brushSelector(match);
                if (customAppender != null)
                {
                    customAppender(paragraph, match.Value, brush);
                }
                else
                {
                    AppendRun(paragraph, match.Value, brush);
                }
                index = match.Index + match.Length;
            }

            if (index < text.Length)
            {
                AppendRun(paragraph, text.Substring(index), TextBrush);
            }
        }

        private static void AppendRun(Paragraph paragraph, string text, Brush brush)
        {
            paragraph.Inlines.Add(new Run(text) { Foreground = brush });
        }
    }
}
