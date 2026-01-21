using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace DietSentry
{
    [QueryProperty(nameof(InsertedFoodDescription), "foodInsertedDescription")]
    public partial class MainPage : ContentPage
    {
        private const string NutritionModeKey = "nutrition_display_mode";
        private readonly FoodDatabaseService _databaseService = new();
        private NutritionDisplayMode _nutritionDisplayMode = NutritionDisplayMode.All;
        private bool _suppressModeEvents;
        private Food? _selectedFood;
        private bool _showLogPanel;
        private string? _insertedFoodDescription;

        public ObservableCollection<Food> Foods { get; } = new();
        public bool ShowNipOnly => _nutritionDisplayMode == NutritionDisplayMode.Nip;
        public bool ShowAll => _nutritionDisplayMode == NutritionDisplayMode.All;
        public Food? SelectedFood
        {
            get => _selectedFood;
            private set
            {
                if (_selectedFood == value)
                {
                    return;
                }

                _selectedFood = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSelectionPanel));
                OnPropertyChanged(nameof(SelectedFoodDescription));
            }
        }

        public bool ShowSelectionPanel => SelectedFood != null;

        public string SelectedFoodDescription => SelectedFood == null
            ? string.Empty
            : SelectedFood.FoodDescription;

        public string? InsertedFoodDescription
        {
            get => _insertedFoodDescription;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _insertedFoodDescription = null;
                    return;
                }

                _insertedFoodDescription = Uri.UnescapeDataString(value);
            }
        }
        public bool ShowLogPanel
        {
            get => _showLogPanel;
            private set
            {
                if (_showLogPanel == value)
                {
                    return;
                }

                _showLogPanel = value;
                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            LoadNutritionDisplayMode();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!string.IsNullOrWhiteSpace(_insertedFoodDescription))
            {
                FoodFilterEntry.Text = _insertedFoodDescription;
                _insertedFoodDescription = null;
            }

            await LoadFoodsAsync(FoodFilterEntry?.Text);
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

        private async void OnClearFilterClicked(object? sender, EventArgs e)
        {
            FoodFilterEntry.Text = string.Empty;
            await LoadFoodsAsync(string.Empty);
        }

        private async void OnFoodFilterCompleted(object? sender, EventArgs e)
        {
            await LoadFoodsAsync(FoodFilterEntry?.Text);
        }

        private async void OnLogClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                await DisplayAlertAsync("Select a food", "Choose a food to log.", "OK");
                return;
            }

            LogFoodNameLabel.Text = FoodDescriptionFormatter.GetDisplayName(SelectedFood.FoodDescription);
            LogFoodUnitLabel.Text = FoodDescriptionFormatter.GetUnit(SelectedFood.FoodDescription);
            LogAmountEntry.Text = string.Empty;
            LogDatePicker.Date = DateTime.Now.Date;
            LogTimePicker.Time = DateTime.Now.TimeOfDay;
            ShowLogPanel = true;
        }

        private async void OnEditFoodClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                await DisplayAlertAsync("Select a food", "Choose a food to edit.", "OK");
                return;
            }

            if (FoodDescriptionFormatter.IsRecipeDescription(SelectedFood.FoodDescription))
            {
                await Shell.Current.GoToAsync($"addRecipe?editRecipeFoodId={SelectedFood.FoodId}");
                return;
            }

            if (FoodDescriptionFormatter.IsLiquidDescription(SelectedFood.FoodDescription))
            {
                await Shell.Current.GoToAsync($"insertLiquidFood?editFoodId={SelectedFood.FoodId}");
                return;
            }

            await Shell.Current.GoToAsync($"insertSolidFood?editFoodId={SelectedFood.FoodId}");
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

        private async void OnExportDbClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Export db is not wired yet.", "OK");
        }

        private async void OnImportDbClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Import db is not wired yet.", "OK");
        }

        private async void OnExportCsvClicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync("Not implemented", "Export csv is not wired yet.", "OK");
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

        private async Task LoadFoodsAsync(string? filterText)
        {
            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                var foods = string.IsNullOrWhiteSpace(filterText)
                    ? await _databaseService.GetFoodsAsync()
                    : await _databaseService.SearchFoodsAsync(filterText);
                Foods.Clear();
                foreach (var food in foods)
                {
                    Foods.Add(food);
                }

                SelectedFood = null;
                if (FoodsCollectionView != null)
                {
                    FoodsCollectionView.SelectedItem = null;
                }
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Error", "Unable to load foods from the database.", "OK");
            }
        }

        private void OnFoodSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedFood = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as Food : null;
        }

        private async void OnLogConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                await DisplayAlertAsync("Select a food", "Choose a food to log.", "OK");
                return;
            }

            if (!double.TryParse(LogAmountEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var amount) ||
                amount <= 0)
            {
                await DisplayAlertAsync("Invalid amount", "Enter a valid amount.", "OK");
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
            var logged = await _databaseService.LogEatenFoodAsync(SelectedFood, amount, dateTime);
            if (!logged)
            {
                await DisplayAlertAsync("Error", "Unable to log the selected food.", "OK");
                return;
            }

            ShowLogPanel = false;
            await Shell.Current.GoToAsync("eatenLog");
        }

        private void OnLogCancelClicked(object? sender, EventArgs e)
        {
            ShowLogPanel = false;
        }

        private void OnLogBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowLogPanel = false;
        }

        private void OnNutritionModeChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (_suppressModeEvents)
            {
                return;
            }

            if (!e.Value)
            {
                return;
            }

            if (sender == MinModeRadio)
            {
                SetNutritionDisplayMode(NutritionDisplayMode.Min);
            }
            else if (sender == NipModeRadio)
            {
                SetNutritionDisplayMode(NutritionDisplayMode.Nip);
            }
            else if (sender == AllModeRadio)
            {
                SetNutritionDisplayMode(NutritionDisplayMode.All);
            }
        }

        private void SetNutritionDisplayMode(NutritionDisplayMode mode)
        {
            if (_nutritionDisplayMode == mode)
            {
                return;
            }

            _nutritionDisplayMode = mode;
            Preferences.Default.Set(NutritionModeKey, (int)mode);
            OnPropertyChanged(nameof(ShowNipOnly));
            OnPropertyChanged(nameof(ShowAll));
        }

        private void LoadNutritionDisplayMode()
        {
            _suppressModeEvents = true;
            var savedMode = (NutritionDisplayMode)Preferences.Default.Get(
                NutritionModeKey,
                (int)NutritionDisplayMode.All);
            _nutritionDisplayMode = savedMode;
            MinModeRadio.IsChecked = savedMode == NutritionDisplayMode.Min;
            NipModeRadio.IsChecked = savedMode == NutritionDisplayMode.Nip;
            AllModeRadio.IsChecked = savedMode == NutritionDisplayMode.All;
            _suppressModeEvents = false;
            OnPropertyChanged(nameof(ShowNipOnly));
            OnPropertyChanged(nameof(ShowAll));
        }

        private enum NutritionDisplayMode
        {
            Min,
            Nip,
            All
        }
    }
}
