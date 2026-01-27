using System;
using System.Globalization;
using System.Threading.Tasks;
using Markdig;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DietSentry
{
    public sealed class MarkdownView : ContentView
    {
        public static readonly BindableProperty MarkdownTextProperty = BindableProperty.Create(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownView),
            string.Empty,
            propertyChanged: OnMarkdownTextChanged);

        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

        private readonly WebView _webView;
        private bool _isThemeSubscribed;
        private bool _isContentLoaded;
        private double _lastWidth = -1;
        private double _lastHeight = -1;

        public MarkdownView()
        {
            _webView = new WebView
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            _webView.Navigating += OnWebViewNavigating;
            _webView.Navigated += OnWebViewNavigated;
            SizeChanged += OnViewSizeChanged;

            Content = _webView;
        }

        public string MarkdownText
        {
            get => (string)GetValue(MarkdownTextProperty);
            set => SetValue(MarkdownTextProperty, value);
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent != null)
            {
                SubscribeThemeChanges();
                UpdateHtml();
            }
            else
            {
                UnsubscribeThemeChanges();
            }
        }

        private static void OnMarkdownTextChanged(BindableObject bindable, object? oldValue, object? newValue)
        {
            if (bindable is MarkdownView view)
            {
                view.UpdateHtml();
            }
        }

        private void SubscribeThemeChanges()
        {
            if (_isThemeSubscribed || Application.Current == null)
            {
                return;
            }

            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
            _isThemeSubscribed = true;
        }

        private void UnsubscribeThemeChanges()
        {
            if (!_isThemeSubscribed || Application.Current == null)
            {
                return;
            }

            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
            _isThemeSubscribed = false;
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            UpdateHtml();
        }

        private void UpdateHtml()
        {
            var markdown = MarkdownText ?? string.Empty;
            var htmlBody = Markdown.ToHtml(markdown, Pipeline);

            var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            var isDark = theme == AppTheme.Dark;

            var textColor = ResolveColor(isDark ? "White" : "Black", isDark ? Colors.White : Colors.Black);
            var linkColor = ResolveColor(isDark ? "Secondary" : "Primary", isDark ? Colors.White : Colors.Black);
            var backgroundColor = ResolveColor(isDark ? "Gray950" : "White", isDark ? Colors.Black : Colors.White);
            var codeBackground = ResolveColor(isDark ? "Gray600" : "Gray200", isDark ? Colors.Black : Colors.White);
            var codeBorder = ResolveColor(isDark ? "Gray500" : "Gray300", isDark ? Colors.Black : Colors.White);
            var dividerColor = ResolveColor(isDark ? "Gray600" : "Gray300", isDark ? Colors.Black : Colors.White);

            _webView.BackgroundColor = backgroundColor;
            _webView.HeightRequest = -1;
            _isContentLoaded = false;
            _webView.Source = new HtmlWebViewSource
            {
                Html = BuildHtml(
                    htmlBody,
                    ToHex(textColor),
                    ToHex(linkColor),
                    ToHex(backgroundColor),
                    ToHex(codeBackground),
                    ToHex(codeBorder),
                    ToHex(dividerColor),
                    isDark)
            };
        }

        private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result != WebNavigationResult.Success)
            {
                return;
            }

            _isContentLoaded = true;
            await SyncHeightAsync();
        }

        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (IsInternalUrl(e.Url))
            {
                return;
            }

            if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri))
            {
                return;
            }

            e.Cancel = true;
            _ = OpenExternalAsync(uri);
        }

        private void OnViewSizeChanged(object? sender, EventArgs e)
        {
            if (!_isContentLoaded)
            {
                return;
            }

            if (Math.Abs(Width - _lastWidth) < 0.5 && Math.Abs(Height - _lastHeight) < 0.5)
            {
                return;
            }

            _lastWidth = Width;
            _lastHeight = Height;
            _ = SyncHeightAsync();
        }

        private async Task SyncHeightAsync()
        {
            try
            {
                var raw = await _webView.EvaluateJavaScriptAsync("document.body.scrollHeight.toString()");
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return;
                }

                var normalized = raw.Trim().Trim('"');
                if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
                {
                    return;
                }

                if (height > 0)
                {
                    var availableHeight = Height;
                    var finalHeight = availableHeight > 0 ? Math.Min(height, availableHeight) : height;
                    _webView.HeightRequest = Math.Max(1, finalHeight);
                }
            }
            catch
            {
                // Ignore height sync failures; WebView will still render.
            }
        }

        private static bool IsInternalUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return true;
            }

            if (url.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static async Task OpenExternalAsync(Uri uri)
        {
            try
            {
                if (Launcher.Default != null && await Launcher.Default.CanOpenAsync(uri))
                {
                    await Launcher.Default.OpenAsync(uri);
                }
            }
            catch
            {
                // Ignore external navigation failures.
            }
        }

        private static string BuildHtml(
            string htmlBody,
            string textColor,
            string linkColor,
            string backgroundColor,
            string codeBackground,
            string codeBorder,
            string dividerColor,
            bool isDark)
        {
            var scheme = isDark ? "dark" : "light";
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <style>
        :root {{ color-scheme: {scheme}; }}
        html, body {{
            margin: 0;
            padding: 0;
            background-color: {backgroundColor};
            color: {textColor};
            font-family: 'OpenSansRegular', 'OpenSans', sans-serif;
            font-size: 14px;
            line-height: 1.4;
            -webkit-text-size-adjust: 100%;
            text-size-adjust: 100%;
            word-wrap: break-word;
        }}
        body {{
            padding-bottom: 12px;
        }}
        p {{ margin: 0 0 8px 0; }}
        p:last-child {{ margin-bottom: 0; }}
        h1, h2, h3, h4 {{
            margin: 12px 0 8px 0;
            font-weight: 700;
        }}
        h1 {{ font-size: 20px; }}
        h2 {{ font-size: 18px; }}
        h3 {{ font-size: 16px; }}
        ul, ol {{ padding-left: 22px; margin: 0 0 8px 0; }}
        li {{ margin: 0 0 6px 0; }}
        code {{
            font-family: 'Consolas', 'Courier New', monospace;
            background-color: {codeBackground};
            padding: 1px 4px;
            border-radius: 4px;
        }}
        pre {{
            background-color: {codeBackground};
            border: 1px solid {codeBorder};
            border-radius: 8px;
            padding: 8px;
            overflow-x: auto;
            margin: 0 0 12px 0;
        }}
        pre code {{
            white-space: pre;
            background-color: transparent;
            padding: 0;
        }}
        hr {{
            border: 0;
            border-top: 1px solid {dividerColor};
            margin: 12px 0;
        }}
        a {{
            color: {linkColor};
            text-decoration: underline;
        }}
        blockquote {{
            margin: 0 0 12px 0;
            padding-left: 12px;
            border-left: 3px solid {dividerColor};
            opacity: 0.9;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 0 0 12px 0;
            font-size: 13px;
        }}
        th, td {{
            border: 1px solid {codeBorder};
            padding: 6px 8px;
            text-align: left;
            vertical-align: top;
        }}
        th {{
            background-color: {codeBackground};
            font-weight: 600;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
    </style>
</head>
<body>
{htmlBody}
</body>
</html>";
        }

        private static Color ResolveColor(string key, Color fallback)
        {
            if (Application.Current?.Resources?.TryGetValue(key, out var value) == true)
            {
                if (value is Color color)
                {
                    return color;
                }

                if (value is SolidColorBrush brush)
                {
                    return brush.Color;
                }
            }

            return fallback;
        }

        private static string ToHex(Color color)
        {
            var red = (byte)Math.Round(color.Red * 255);
            var green = (byte)Math.Round(color.Green * 255);
            var blue = (byte)Math.Round(color.Blue * 255);
            return $"#{red:X2}{green:X2}{blue:X2}";
        }
    }
}
