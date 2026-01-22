using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private Food? _convertFood;
        private Food? _deleteFood;
        private bool _showLogPanel;
        private bool _showConvertPanel;
        private bool _showDeletePanel;
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
            var directory = GetExchangeDirectory();
            var targetPath = Path.Combine(directory, DatabaseFileName);
            var confirmed = await DisplayAlertAsync(
                "Export db",
                $"This will overwrite:\n{targetPath}\n\nProceed?",
                "Confirm",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
                await DatabaseInitializer.EnsureDatabaseAsync();
                var sourcePath = DatabaseInitializer.GetDatabasePath();
                File.Copy(sourcePath, targetPath, true);
                await DisplayAlertAsync("Export db", $"Database exported to:\n{targetPath}", "OK");
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Export db", $"Failed to export database to:\n{targetPath}", "OK");
            }
        }

        private async void OnImportDbClicked(object? sender, EventArgs e)
        {
            var directory = GetExchangeDirectory();
            var sourcePath = Path.Combine(directory, DatabaseFileName);
            if (!File.Exists(sourcePath))
            {
                await DisplayAlertAsync(
                    "Import db",
                    $"Place {DatabaseFileName} in:\n{directory}\n\nThen try again.",
                    "OK");
                return;
            }

            var confirmed = await DisplayAlertAsync(
                "Import db",
                $"This will replace the current database with:\n{sourcePath}\n\nProceed?",
                "Confirm",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                var targetPath = DatabaseInitializer.GetDatabasePath();
                File.Copy(sourcePath, targetPath, true);
                await LoadFoodsAsync(FoodFilterEntry?.Text);
                await DisplayAlertAsync("Import db", $"Database imported from:\n{sourcePath}", "OK");
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Import db", $"Failed to import database from:\n{sourcePath}", "OK");
            }
        }

        private async void OnExportCsvClicked(object? sender, EventArgs e)
        {
            var directory = GetExchangeDirectory();
            var targetPath = Path.Combine(directory, DailyCsvFileName);
            try
            {
                Directory.CreateDirectory(directory);
                await DatabaseInitializer.EnsureDatabaseAsync();
                var eatenFoods = (await _databaseService.GetEatenFoodsAsync()).ToList();
                var weightEntries = (await _databaseService.GetWeightEntriesAsync()).ToList();
                var csv = BuildEatenDailyAllCsv(eatenFoods, weightEntries);
                await File.WriteAllTextAsync(targetPath, csv, new UTF8Encoding(false));
                await DisplayAlertAsync("Export csv", $"CSV exported to:\n{targetPath}", "OK");
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Export csv", $"Failed to export CSV to:\n{targetPath}", "OK");
            }
        }

        private async void OnPaletteClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("palette");
        }

        private async void OnCopyFoodClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                await DisplayAlertAsync("Select a food", "Choose a food to copy.", "OK");
                return;
            }

            if (FoodDescriptionFormatter.IsRecipeDescription(SelectedFood.FoodDescription))
            {
                await Shell.Current.GoToAsync($"addRecipe?copyRecipeFoodId={SelectedFood.FoodId}");
                return;
            }

            if (FoodDescriptionFormatter.IsLiquidDescription(SelectedFood.FoodDescription))
            {
                await Shell.Current.GoToAsync($"insertLiquidFood?copyFoodId={SelectedFood.FoodId}");
                return;
            }

            await Shell.Current.GoToAsync($"insertSolidFood?copyFoodId={SelectedFood.FoodId}");
        }

        private async void OnAddRecipeClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("addRecipe");
        }

        private async void OnConvertClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                await DisplayAlertAsync("Select a food", "Choose a food to convert.", "OK");
                return;
            }

            if (!FoodDescriptionFormatter.IsLiquidDescription(SelectedFood.FoodDescription))
            {
                await DisplayAlertAsync("Not available", "Convert is only available for liquid foods.", "OK");
                return;
            }

            _convertFood = SelectedFood;
            if (ConvertFoodNameLabel != null)
            {
                ConvertFoodNameLabel.Text =
                    FoodDescriptionFormatter.GetDisplayName(SelectedFood.FoodDescription);
            }

            if (ConvertDensityEntry != null)
            {
                ConvertDensityEntry.Text = string.Empty;
                ConvertDensityEntry.Focus();
            }

            ShowConvertPanel = true;
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                await DisplayAlertAsync("Select a food", "Choose a food to delete.", "OK");
                return;
            }

            _deleteFood = SelectedFood;
            if (DeleteFoodDescriptionLabel != null)
            {
                DeleteFoodDescriptionLabel.Text = SelectedFood.FoodDescription;
            }

            ShowDeletePanel = true;
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

        public bool ShowConvertPanel
        {
            get => _showConvertPanel;
            private set
            {
                if (_showConvertPanel == value)
                {
                    return;
                }

                _showConvertPanel = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDeletePanel
        {
            get => _showDeletePanel;
            private set
            {
                if (_showDeletePanel == value)
                {
                    return;
                }

                _showDeletePanel = value;
                OnPropertyChanged();
            }
        }

        private static string ExtractBaseDescription(string description)
        {
            if (description.EndsWith(" mL#", StringComparison.OrdinalIgnoreCase))
            {
                return description[..^4].TrimEnd();
            }

            if (description.EndsWith(" mL", StringComparison.OrdinalIgnoreCase))
            {
                return description[..^3].TrimEnd();
            }

            if (description.EndsWith(" #", StringComparison.OrdinalIgnoreCase))
            {
                return description[..^2].TrimEnd();
            }

            return description.TrimEnd();
        }

        private static string NormalizeDensityText(string densityInput, double density)
        {
            var cleaned = densityInput.Trim().Replace(',', '.');
            cleaned = cleaned.TrimEnd('0').TrimEnd('.');
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return density.ToString("G", CultureInfo.InvariantCulture);
            }

            return cleaned;
        }

        private static bool TryParsePositiveDouble(string? input, out double value)
        {
            var normalized = (input ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace(',', '.');
            if (string.IsNullOrEmpty(normalized))
            {
                value = 0;
                return false;
            }

            var parsed = double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
            return parsed && value > 0;
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

        private async void OnConvertConfirmClicked(object? sender, EventArgs e)
        {
            if (_convertFood == null)
            {
                ShowConvertPanel = false;
                return;
            }

            var densityInput = ConvertDensityEntry?.Text ?? string.Empty;
            if (!TryParsePositiveDouble(densityInput, out var density))
            {
                await DisplayAlertAsync("Invalid density", "Enter a valid density value.", "OK");
                return;
            }

            var baseDescription = ExtractBaseDescription(_convertFood.FoodDescription);
            var densityText = NormalizeDensityText(densityInput, density);
            var newDescription = $"{baseDescription} {{density={densityText}g/mL}} #";

            var newFood = new Food
            {
                FoodId = 0,
                FoodDescription = newDescription,
                Energy = _convertFood.Energy / density,
                Protein = _convertFood.Protein / density,
                FatTotal = _convertFood.FatTotal / density,
                SaturatedFat = _convertFood.SaturatedFat / density,
                TransFat = _convertFood.TransFat / density,
                PolyunsaturatedFat = _convertFood.PolyunsaturatedFat / density,
                MonounsaturatedFat = _convertFood.MonounsaturatedFat / density,
                Carbohydrate = _convertFood.Carbohydrate / density,
                Sugars = _convertFood.Sugars / density,
                DietaryFibre = _convertFood.DietaryFibre / density,
                Sodium = _convertFood.Sodium / density,
                CalciumCa = _convertFood.CalciumCa / density,
                PotassiumK = _convertFood.PotassiumK / density,
                ThiaminB1 = _convertFood.ThiaminB1 / density,
                RiboflavinB2 = _convertFood.RiboflavinB2 / density,
                NiacinB3 = _convertFood.NiacinB3 / density,
                Folate = _convertFood.Folate / density,
                IronFe = _convertFood.IronFe / density,
                MagnesiumMg = _convertFood.MagnesiumMg / density,
                VitaminC = _convertFood.VitaminC / density,
                Caffeine = _convertFood.Caffeine / density,
                Cholesterol = _convertFood.Cholesterol / density,
                Alcohol = _convertFood.Alcohol / density,
                Notes = _convertFood.Notes
            };

            var inserted = await _databaseService.InsertFoodAsync(newFood);
            if (!inserted)
            {
                await DisplayAlertAsync("Convert failed", "Failed to convert food.", "OK");
                return;
            }

            ShowConvertPanel = false;
            _convertFood = null;
            FoodFilterEntry.Text = newDescription;
            await LoadFoodsAsync(newDescription);
        }

        private void OnConvertCancelClicked(object? sender, EventArgs e)
        {
            ShowConvertPanel = false;
            _convertFood = null;
        }

        private void OnConvertBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowConvertPanel = false;
            _convertFood = null;
        }

        private async void OnDeleteConfirmClicked(object? sender, EventArgs e)
        {
            if (_deleteFood == null)
            {
                ShowDeletePanel = false;
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var isRecipeFood = FoodDescriptionFormatter.IsRecipeDescription(_deleteFood.FoodDescription);
            var deletedFood = await _databaseService.DeleteFoodAsync(_deleteFood.FoodId);
            if (isRecipeFood)
            {
                await _databaseService.DeleteRecipesByFoodIdAsync(_deleteFood.FoodId);
            }

            if (!deletedFood)
            {
                await DisplayAlertAsync("Error", "Unable to delete the selected food.", "OK");
                return;
            }

            ShowDeletePanel = false;
            _deleteFood = null;
            SelectedFood = null;
            await LoadFoodsAsync(FoodFilterEntry?.Text);
        }

        private void OnDeleteCancelClicked(object? sender, EventArgs e)
        {
            ShowDeletePanel = false;
            _deleteFood = null;
        }

        private void OnDeleteBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowDeletePanel = false;
            _deleteFood = null;
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

        private static string GetExchangeDirectory()
        {
#if WINDOWS
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloads = string.IsNullOrWhiteSpace(profile) ? string.Empty : Path.Combine(profile, "Downloads");
            if (!string.IsNullOrWhiteSpace(downloads) && Directory.Exists(downloads))
            {
                return downloads;
            }
#endif
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(documents))
            {
                return documents;
            }

            return FileSystem.AppDataDirectory;
        }

        private static string BuildEatenDailyAllCsv(
            IReadOnlyList<EatenFood> eatenFoods,
            IReadOnlyList<WeightEntry> weightEntries)
        {
            var lines = new List<string>();
            var header = new[]
            {
                "Date",
                "My weight (kg)",
                "Comments",
                "Amount (g or mL)",
                "Energy (kJ):",
                "Protein (g):",
                "Fat, total (g):",
                "- Saturated (g):",
                "- Trans (mg):",
                "- Polyunsaturated (g):",
                "- Monounsaturated (g):",
                "Carbohydrate (g):",
                "- Sugars (g):",
                "Sodium (mg):",
                "Dietary Fibre (g):",
                "Calcium (mg):",
                "Potassium (mg):",
                "Thiamin B1 (mg):",
                "Riboflavin B2 (mg):",
                "Niacin B3 (mg):",
                "Folate (ug):",
                "Iron (mg):",
                "Magnesium (mg):",
                "Vitamin C (mg):",
                "Caffeine (mg):",
                "Cholesterol (mg):",
                "Alcohol (g):"
            };
            lines.Add(string.Join(",", header.Select(CsvCell)));

            var weightByDate = weightEntries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.DateWeight))
                .GroupBy(entry => entry.DateWeight)
                .ToDictionary(group => group.Key, group => group.First());
            var totals = AggregateDailyTotals(eatenFoods, weightByDate);
            foreach (var total in totals)
            {
                var weightText = weightByDate.TryGetValue(total.Date, out var weightEntry)
                    ? FormatWeight(weightEntry.Weight)
                    : "NA";
                var commentsText = weightEntry?.Comments ?? string.Empty;
                var row = new[]
                {
                    total.Date,
                    weightText,
                    commentsText,
                    FormatNumber(total.AmountEaten),
                    FormatNumber(total.Energy),
                    FormatNumber(total.Protein),
                    FormatNumber(total.FatTotal),
                    FormatNumber(total.SaturatedFat),
                    FormatNumber(total.TransFat),
                    FormatNumber(total.PolyunsaturatedFat),
                    FormatNumber(total.MonounsaturatedFat),
                    FormatNumber(total.Carbohydrate),
                    FormatNumber(total.Sugars),
                    FormatNumber(total.SodiumNa),
                    FormatNumber(total.DietaryFibre),
                    FormatNumber(total.CalciumCa),
                    FormatNumber(total.PotassiumK),
                    FormatNumber(total.ThiaminB1),
                    FormatNumber(total.RiboflavinB2),
                    FormatNumber(total.NiacinB3),
                    FormatNumber(total.Folate),
                    FormatNumber(total.IronFe),
                    FormatNumber(total.MagnesiumMg),
                    FormatNumber(total.VitaminC),
                    FormatNumber(total.Caffeine),
                    FormatNumber(total.Cholesterol),
                    FormatNumber(total.Alcohol)
                };
                lines.Add(string.Join(",", row.Select(CsvCell)));
            }

            return string.Join("\n", lines);
        }

        private static IReadOnlyList<DailyTotals> AggregateDailyTotals(
            IReadOnlyList<EatenFood> eatenFoods,
            IReadOnlyDictionary<string, WeightEntry> weightByDate)
        {
            var grouped = eatenFoods.GroupBy(food => food.DateEaten);
            var totals = new List<DailyTotals>();
            foreach (var group in grouped)
            {
                var items = group.ToList();
                weightByDate.TryGetValue(group.Key, out var weightEntry);
                var comments = weightEntry?.Comments ?? string.Empty;
                var weightDisplay = weightEntry == null
                    ? "NA"
                    : weightEntry.Weight.ToString("N1", CultureInfo.InvariantCulture);

                totals.Add(new DailyTotals
                {
                    Date = group.Key,
                    UnitLabel = "mixed units",
                    AmountEaten = items.Sum(item => item.AmountEaten),
                    Energy = items.Sum(item => item.Energy),
                    Protein = items.Sum(item => item.Protein),
                    FatTotal = items.Sum(item => item.FatTotal),
                    SaturatedFat = items.Sum(item => item.SaturatedFat),
                    TransFat = items.Sum(item => item.TransFat),
                    PolyunsaturatedFat = items.Sum(item => item.PolyunsaturatedFat),
                    MonounsaturatedFat = items.Sum(item => item.MonounsaturatedFat),
                    Carbohydrate = items.Sum(item => item.Carbohydrate),
                    Sugars = items.Sum(item => item.Sugars),
                    DietaryFibre = items.Sum(item => item.DietaryFibre),
                    SodiumNa = items.Sum(item => item.SodiumNa),
                    CalciumCa = items.Sum(item => item.CalciumCa),
                    PotassiumK = items.Sum(item => item.PotassiumK),
                    ThiaminB1 = items.Sum(item => item.ThiaminB1),
                    RiboflavinB2 = items.Sum(item => item.RiboflavinB2),
                    NiacinB3 = items.Sum(item => item.NiacinB3),
                    Folate = items.Sum(item => item.Folate),
                    IronFe = items.Sum(item => item.IronFe),
                    MagnesiumMg = items.Sum(item => item.MagnesiumMg),
                    VitaminC = items.Sum(item => item.VitaminC),
                    Caffeine = items.Sum(item => item.Caffeine),
                    Cholesterol = items.Sum(item => item.Cholesterol),
                    Alcohol = items.Sum(item => item.Alcohol),
                    WeightDisplay = weightDisplay,
                    Comments = comments
                });
            }

            return totals
                .OrderByDescending(total => ParseDate(total.Date))
                .ToList();
        }

        private static DateTime ParseDate(string value)
        {
            if (DateTime.TryParseExact(
                value,
                "d-MMM-yy",
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out var result))
            {
                return result;
            }

            return DateTime.MinValue;
        }

        private static string CsvCell(string value)
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("N1", CultureInfo.InvariantCulture);
        }

        private static string FormatWeight(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private enum NutritionDisplayMode
        {
            Min,
            Nip,
            All
        }

        private const string DatabaseFileName = "foods.db";
        private const string DailyCsvFileName = "EatenDailyAll.csv";
    }
}
