using System;
using System.Threading.Tasks;

namespace DietSentry
{
    public partial class HelpSheetPage : ContentPage
    {
        private readonly string _sectionKey;

        public HelpSheetPage(string title, string body, string sectionKey)
        {
            InitializeComponent();
            TitleLabel.Text = title;
            BodyLabel.Text = body;
            _sectionKey = sectionKey ?? string.Empty;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SheetContainer.TranslationY = 220;
            await SheetContainer.TranslateTo(0, 0, 150, Easing.CubicOut);
        }

        private async void OnDismissTapped(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }

        private async void OnOpenHelpClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
            var section = string.IsNullOrWhiteSpace(_sectionKey) ? string.Empty : $"?section={Uri.EscapeDataString(_sectionKey)}";
            await Shell.Current.GoToAsync($"help{section}");
        }
    }
}
