namespace DietSentry
{
    public partial class CopyFoodPage : ContentPage
    {
        public CopyFoodPage()
        {
            InitializeComponent();
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Copy food is not wired yet.", "OK");
            await Shell.Current.GoToAsync("//foodSearch");
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }
    }
}
