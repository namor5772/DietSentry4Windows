namespace DietSentry
{
    public partial class UtilitiesPage : ContentPage
    {
        public UtilitiesPage()
        {
            InitializeComponent();
        }

        private async void OnUtilityClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Utility action is not wired yet.", "OK");
        }

        private async void OnDatabaseStatusClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("dbStatus");
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }
    }
}
