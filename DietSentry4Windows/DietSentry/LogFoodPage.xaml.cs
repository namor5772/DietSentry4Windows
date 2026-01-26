using System;
using System.Threading.Tasks;
using System.Globalization;

namespace DietSentry
{
    public partial class LogFoodPage : ContentPage
    {
        private readonly Food _food;
        private readonly FoodDatabaseService _databaseService = new();

        public LogFoodPage(Food food)
        {
            InitializeComponent();
            _food = food;

            FoodNameLabel.Text = FoodDescriptionFormatter.GetDisplayName(food.FoodDescription);
            UnitLabel.Text = FoodDescriptionFormatter.GetUnit(food.FoodDescription);
            LogDatePicker.Date = DateTime.Now.Date;
            LogTimePicker.Time = DateTime.Now.TimeOfDay;
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            if (!double.TryParse(AmountEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var amount) ||
                amount <= 0)
            {
                ShowInvalidAmountOverlay();
                return;
            }

            var selectedDate = LogDatePicker.Date ?? DateTime.Now.Date;
            var selectedTime = LogTimePicker.Time ?? DateTime.Now.TimeOfDay;
            var dateTime = new DateTime(
                selectedDate.Year,
                selectedDate.Month,
                selectedDate.Day,
                selectedTime.Hours,
                selectedTime.Minutes,
                0,
                DateTimeKind.Local);

            await DatabaseInitializer.EnsureDatabaseAsync();
            var logged = await _databaseService.LogEatenFoodAsync(_food, amount, dateTime);
            if (!logged)
            {
                await DisplayAlertAsync("Error", "Unable to log the selected food.", "OK");
                return;
            }

            await Navigation.PopModalAsync();
            await Shell.Current.GoToAsync("eatenLog");
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnHelpClicked(object? sender, EventArgs e)
        {
            if (HelpOverlay == null || HelpSheet == null)
            {
                return;
            }

            HelpOverlay.IsVisible = true;

            await HelpSheetLayout.ApplyMaxHeightAsync(HelpOverlay, HelpSheet, 0.8);

            HelpSheet.TranslationY = 220;
            _ = HelpSheet.TranslateToAsync(0, 0, 150, Easing.CubicOut);
        }

        private void OnHelpDismissed(object? sender, EventArgs e)
        {
            if (HelpOverlay == null)
            {
                return;
            }

            HelpOverlay.IsVisible = false;
        }

        private void ShowInvalidAmountOverlay()
        {
            if (InvalidAmountOverlay == null)
            {
                return;
            }

            InvalidAmountOverlay.IsVisible = true;
        }

        private void OnInvalidAmountOkClicked(object? sender, EventArgs e)
        {
            if (InvalidAmountOverlay == null)
            {
                return;
            }

            InvalidAmountOverlay.IsVisible = false;
        }

        private void OnInvalidAmountBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (InvalidAmountOverlay == null)
            {
                return;
            }

            InvalidAmountOverlay.IsVisible = false;
        }

    }
}
