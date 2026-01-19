namespace DietSentry
{
    public partial class EatenLogPage : ContentPage
    {
        public EatenLogPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }
    }
}
