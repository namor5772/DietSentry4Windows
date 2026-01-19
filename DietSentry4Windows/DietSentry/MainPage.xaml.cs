namespace DietSentry
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnEatenLogClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("eatenLog");
        }

        private async void OnWeightTableClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("weightTable");
        }

        private async void OnUtilitiesClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("utilities");
        }

        private void OnClearFilterClicked(object? sender, EventArgs e)
        {
            FoodFilterEntry.Text = string.Empty;
        }

        private async void OnLogClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Logging food is not wired yet.", "OK");
        }

        private async void OnEditFoodClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("editFood");
        }

        private async void OnInsertFoodClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("insertSolidFood");
        }

        private async void OnInsertLiquidFoodClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("insertLiquidFood");
        }

        private async void OnAddFoodByJsonClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("addFoodByJson");
        }

        private async void OnCopyFoodClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("copyFood");
        }

        private async void OnAddRecipeClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("addRecipe");
        }

        private async void OnConvertClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Convert is not wired yet.", "OK");
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Delete is not wired yet.", "OK");
        }
    }
}
