namespace DietSentry
{
    public partial class EditRecipePage : ContentPage
    {
        public EditRecipePage()
        {
            InitializeComponent();
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Edit recipe is not wired yet.", "OK");
            await Shell.Current.GoToAsync("//foodSearch");
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }

        private async void OnHelpClicked(object? sender, EventArgs e)
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

            await Shell.Current.GoToAsync("help?section=edit-recipe");
        }
    }
}
