using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;

namespace DietSentry
{
    public partial class PalettePage : ContentPage
    {
        private static readonly string[] PaletteKeys =
        {
            "Primary",
            "PrimaryDark",
            "PrimaryDarkText",
            "Secondary",
            "SecondaryDarkText",
            "Tertiary",
            "White",
            "Black",
            "Magenta",
            "MidnightBlue",
            "OffBlack",
            "Gray100",
            "Gray200",
            "Gray300",
            "Gray400",
            "Gray500",
            "Gray600",
            "Gray900",
            "Gray950"
        };

        public ObservableCollection<PaletteSwatchGroup> SwatchGroups { get; } = new();
        private bool _themeEventsHooked;

        public PalettePage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSwatches();
            HookThemeEvents();
        }

        protected override void OnDisappearing()
        {
            UnhookThemeEvents();
            base.OnDisappearing();
        }

        private void LoadSwatches()
        {
            SwatchGroups.Clear();

            if (Application.Current?.Resources == null)
            {
                return;
            }

            var buttonGroup = new PaletteSwatchGroup("Button Style (Current Theme)");
            foreach (var swatch in GetButtonSwatches())
            {
                buttonGroup.Add(swatch);
            }

            if (buttonGroup.Count > 0)
            {
                SwatchGroups.Add(buttonGroup);
            }

            var paletteGroup = new PaletteSwatchGroup("Palette Colors");
            foreach (var key in PaletteKeys)
            {
                if (!TryGetColorResource(key, out var color))
                {
                    continue;
                }

                paletteGroup.Add(new PaletteSwatch(key, color));
            }

            if (paletteGroup.Count > 0)
            {
                SwatchGroups.Add(paletteGroup);
            }
        }

        private IEnumerable<PaletteSwatch> GetButtonSwatches()
        {
            var style = GetImplicitStyle<Button>();
            if (style == null)
            {
                yield break;
            }

            var normalButton = new Button { Style = style };
            yield return new PaletteSwatch("Button.Background (Normal)", ResolveColor(normalButton.BackgroundColor));
            yield return new PaletteSwatch("Button.Text (Normal)", ResolveColor(normalButton.TextColor));

            var disabledButton = new Button { Style = style, IsEnabled = false };
            yield return new PaletteSwatch("Button.Background (Disabled)", ResolveColor(disabledButton.BackgroundColor));
            yield return new PaletteSwatch("Button.Text (Disabled)", ResolveColor(disabledButton.TextColor));
        }

        private static bool TryGetColorResource(string key, out Color color)
        {
            color = Colors.Transparent;
            if (Application.Current?.Resources?.TryGetValue(key, out var value) != true)
            {
                return false;
            }

            if (value is Color resourceColor)
            {
                color = resourceColor;
                return true;
            }

            if (value is SolidColorBrush brush)
            {
                color = brush.Color;
                return true;
            }

            return false;
        }

        private static Style? GetImplicitStyle<TControl>() where TControl : BindableObject
        {
            if (Application.Current?.Resources == null)
            {
                return null;
            }

            return FindImplicitStyle<TControl>(Application.Current.Resources);
        }

        private static Style? FindImplicitStyle<TControl>(ResourceDictionary dictionary)
            where TControl : BindableObject
        {
            if (dictionary.TryGetValue(typeof(TControl).Name, out var value) && value is Style namedStyle)
            {
                return namedStyle;
            }

            foreach (var entry in dictionary)
            {
                if (entry.Value is Style style && style.TargetType == typeof(TControl))
                {
                    return style;
                }
            }

            foreach (var merged in dictionary.MergedDictionaries)
            {
                var style = FindImplicitStyle<TControl>(merged);
                if (style != null)
                {
                    return style;
                }
            }

            return null;
        }

        private static Color ResolveColor(Color? color)
        {
            return color ?? Colors.Transparent;
        }

        private void HookThemeEvents()
        {
            if (_themeEventsHooked || Application.Current == null)
            {
                return;
            }

            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
            _themeEventsHooked = true;
        }

        private void UnhookThemeEvents()
        {
            if (!_themeEventsHooked || Application.Current == null)
            {
                return;
            }

            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
            _themeEventsHooked = false;
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            LoadSwatches();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }

        private void OnHelpClicked(object? sender, EventArgs e)
        {
            if (HelpOverlay == null || HelpSheet == null)
            {
                return;
            }

            HelpOverlay.IsVisible = true;
            HelpSheet.TranslationY = 220;
            _ = HelpSheet.TranslateTo(0, 0, 150, Easing.CubicOut);
        }

        private void OnHelpDismissed(object? sender, EventArgs e)
        {
            if (HelpOverlay == null)
            {
                return;
            }

            HelpOverlay.IsVisible = false;
        }

        private async void OnHelpOpenFullClicked(object? sender, EventArgs e)
        {
            if (HelpOverlay != null)
            {
                HelpOverlay.IsVisible = false;
            }

            await Shell.Current.GoToAsync("help?section=palette");
        }
    }

    public sealed class PaletteSwatchGroup : ObservableCollection<PaletteSwatch>
    {
        public PaletteSwatchGroup(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }

    public sealed class PaletteSwatch
    {
        public PaletteSwatch(string name, Color color)
        {
            Name = name;
            SwatchColor = color;
            Hex = FormatHex(color);
        }

        public string Name { get; }
        public Color SwatchColor { get; }
        public string Hex { get; }

        private static string FormatHex(Color color)
        {
            var alpha = ToByte(color.Alpha);
            var red = ToByte(color.Red);
            var green = ToByte(color.Green);
            var blue = ToByte(color.Blue);
            return $"#{alpha:X2}{red:X2}{green:X2}{blue:X2}";
        }

        private static byte ToByte(float value)
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            return (byte)Math.Round(clamped * 255f, MidpointRounding.AwayFromZero);
        }
    }
}
