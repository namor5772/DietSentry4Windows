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
    }
}
