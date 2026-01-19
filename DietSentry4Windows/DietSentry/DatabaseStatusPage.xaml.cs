using System;
using System.Threading.Tasks;

namespace DietSentry
{
    public partial class DatabaseStatusPage : ContentPage
    {
        private readonly FoodDatabaseService _databaseService = new();

        public DatabaseStatusPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshStatusAsync();
        }

        private async void OnRefreshClicked(object? sender, EventArgs e)
        {
            await RefreshStatusAsync();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async Task RefreshStatusAsync()
        {
            ErrorLabel.IsVisible = false;
            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                DbPathLabel.Text = $"Path: {DatabaseInitializer.GetDatabasePath()}";
                var foodCount = await _databaseService.GetFoodCountAsync();
                FoodCountLabel.Text = $"Foods: {foodCount}";
            }
            catch (Exception ex)
            {
                FoodCountLabel.Text = "Foods: error";
                ErrorLabel.Text = ex.Message;
                ErrorLabel.IsVisible = true;
            }
        }
    }
}
