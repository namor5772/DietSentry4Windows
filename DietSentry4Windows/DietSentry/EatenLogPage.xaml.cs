using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
#if ANDROID
using Android.App;
using Android.Net;
using Android.Provider;
#endif
#if WINDOWS
using Microsoft.Maui.Platform;
using Windows.Storage.Pickers;
using WinRT.Interop;
#endif

namespace DietSentry
{
    public partial class EatenLogPage : ContentPage
    {
        private const string NutritionModeKey = "eaten_nutrition_display_mode";
        private const string DisplayDailyTotalsKey = "eaten_display_daily_totals";
        private const string FilterByDateKey = "eaten_filter_by_date";
        private static DateTime? _sessionSelectedFilterDate;
        private readonly FoodDatabaseService _databaseService = new();
        private NutritionDisplayMode _nutritionDisplayMode = NutritionDisplayMode.All;
        private bool _suppressModeEvents;
        private bool _suppressToggleEvents;
        private bool _displayDailyTotals;
        private bool _filterByDate;
        private DateTime _selectedFilterDate = DateTime.Today;
        private EatenFood? _selectedEatenFood;
        private bool _showEditPanel;
        private bool _showDeletePanel;
        private bool _showExportCsvPanel;
        private string? _exportCsvTargetPath;
        private List<EatenFood> _allEatenFoods = new();
        private List<WeightEntry> _weightEntries = new();
#if ANDROID
        private Android.Net.Uri? _exportCsvTargetFolderUri;
#endif

        public ObservableCollection<EatenFood> EatenFoods { get; } = new();
        public ObservableCollection<DailyTotals> DailyTotals { get; } = new();

        public bool ShowNipOnly => _nutritionDisplayMode == NutritionDisplayMode.Nip;
        public bool ShowAll => _nutritionDisplayMode == NutritionDisplayMode.All;
        public bool ShowMinOnly => _nutritionDisplayMode == NutritionDisplayMode.Min;

        public bool DisplayDailyTotals
        {
            get => _displayDailyTotals;
            private set
            {
                if (_displayDailyTotals == value)
                {
                    return;
                }

                _displayDailyTotals = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowDailyTotals));
                OnPropertyChanged(nameof(ShowEatenFoods));
                OnPropertyChanged(nameof(ShowSelectionPanel));
                if (value)
                {
                    SelectedEatenFood = null;
                    if (EatenCollectionView != null)
                    {
                        EatenCollectionView.SelectedItem = null;
                    }
                }

                _ = LoadWeightsIfNeededAsync();
                ApplyFilters();
            }
        }

        public bool ShowDailyTotals => DisplayDailyTotals;
        public bool ShowEatenFoods => !DisplayDailyTotals;

        public bool FilterByDate
        {
            get => _filterByDate;
            private set
            {
                if (_filterByDate == value)
                {
                    return;
                }

                _filterByDate = value;
                OnPropertyChanged();
                ApplyFilters();
                if (value)
                {
                    SelectedEatenFood = null;
                    if (EatenCollectionView != null)
                    {
                        EatenCollectionView.SelectedItem = null;
                    }
                }
            }
        }

        public DateTime SelectedFilterDate
        {
            get => _selectedFilterDate;
            private set
            {
                if (_selectedFilterDate == value)
                {
                    return;
                }

                _selectedFilterDate = value;
                _sessionSelectedFilterDate = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public EatenFood? SelectedEatenFood
        {
            get => _selectedEatenFood;
            private set
            {
                if (_selectedEatenFood == value)
                {
                    return;
                }

                _selectedEatenFood = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSelectionPanel));
                OnPropertyChanged(nameof(SelectedEatenFoodTimestamp));
            }
        }

        public bool ShowSelectionPanel => SelectedEatenFood != null && !DisplayDailyTotals;

        public string SelectedEatenFoodTimestamp => SelectedEatenFood == null
            ? string.Empty
            : $"Logged on: {SelectedEatenFood.DateEaten} at {SelectedEatenFood.TimeEaten}";

        public bool ShowEditPanel
        {
            get => _showEditPanel;
            private set
            {
                if (_showEditPanel == value)
                {
                    return;
                }

                _showEditPanel = value;
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

        public bool ShowExportCsvPanel
        {
            get => _showExportCsvPanel;
            private set
            {
                if (_showExportCsvPanel == value)
                {
                    return;
                }

                _showExportCsvPanel = value;
                OnPropertyChanged();
            }
        }

        public EatenLogPage()
        {
            InitializeComponent();
            BindingContext = this;
            LoadNutritionDisplayMode();
            LoadViewPreferences();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadEatenFoodsAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (ShowDeletePanel)
            {
                ShowDeletePanel = false;
                SelectedEatenFood = null;
                return true;
            }

            if (ShowEditPanel)
            {
                ShowEditPanel = false;
                SelectedEatenFood = null;
                return true;
            }

            if (SelectedEatenFood != null)
            {
                SelectedEatenFood = null;
                if (EatenCollectionView != null)
                {
                    EatenCollectionView.SelectedItem = null;
                }

                return true;
            }

            return base.OnBackButtonPressed();
        }

        private async Task LoadEatenFoodsAsync()
        {
            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                _allEatenFoods = (await _databaseService.GetEatenFoodsAsync()).ToList();
                await LoadWeightsIfNeededAsync();
                ApplyFilters();
                SelectedEatenFood = null;
                if (EatenCollectionView != null)
                {
                    EatenCollectionView.SelectedItem = null;
                }
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Error", "Unable to load eaten logs.", "OK");
            }
        }

        private async Task LoadWeightsIfNeededAsync()
        {
            if (!DisplayDailyTotals)
            {
                return;
            }

            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                _weightEntries = (await _databaseService.GetWeightEntriesAsync()).ToList();
                ApplyFilters();
            }
            catch (Exception)
            {
                _weightEntries = new List<WeightEntry>();
            }
        }

        private void ApplyFilters()
        {
            var filtered = FilterByDate
                ? _allEatenFoods
                    .Where(food => string.Equals(
                        food.DateEaten,
                        SelectedFilterDate.ToString("d-MMM-yy", CultureInfo.CurrentCulture),
                        StringComparison.CurrentCulture))
                    .ToList()
                : _allEatenFoods.ToList();

            EatenFoods.Clear();
            foreach (var food in filtered)
            {
                EatenFoods.Add(food);
            }

            var totals = AggregateDailyTotals(filtered);
            DailyTotals.Clear();
            foreach (var total in totals)
            {
                DailyTotals.Add(total);
            }
        }

        private IReadOnlyList<DailyTotals> AggregateDailyTotals(IReadOnlyList<EatenFood> eatenFoods)
        {
            var weightByDate = _weightEntries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.DateWeight))
                .GroupBy(entry => entry.DateWeight)
                .ToDictionary(group => group.Key, group => group.First());

            var grouped = eatenFoods.GroupBy(food => food.DateEaten);
            var totals = new List<DailyTotals>();
            foreach (var group in grouped)
            {
                var items = group.ToList();
                var allMl = items.All(item => string.Equals(item.UnitLabel, "mL", StringComparison.OrdinalIgnoreCase));
                var allGrams = items.All(item => string.Equals(item.UnitLabel, "g", StringComparison.OrdinalIgnoreCase));
                var unitLabel = allMl
                    ? "mL"
                    : allGrams
                        ? "g"
                        : "mixed units";

                weightByDate.TryGetValue(group.Key, out var weightEntry);
                var comments = weightEntry?.Comments ?? string.Empty;
                var weightDisplay = weightEntry == null
                    ? "NA"
                    : weightEntry.Weight.ToString("N1", CultureInfo.InvariantCulture);

                totals.Add(new DailyTotals
                {
                    Date = group.Key,
                    UnitLabel = unitLabel,
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

        private void OnNutritionModeChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (_suppressModeEvents || !e.Value)
            {
                return;
            }

            if (sender == MinModeRadio || sender == MinModeRadioTop)
            {
                SetNutritionDisplayMode(NutritionDisplayMode.Min);
            }
            else if (sender == NipModeRadio || sender == NipModeRadioTop)
            {
                SetNutritionDisplayMode(NutritionDisplayMode.Nip);
            }
            else if (sender == AllModeRadio || sender == AllModeRadioTop)
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
            OnPropertyChanged(nameof(ShowMinOnly));
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
            MinModeRadioTop.IsChecked = savedMode == NutritionDisplayMode.Min;
            NipModeRadioTop.IsChecked = savedMode == NutritionDisplayMode.Nip;
            AllModeRadioTop.IsChecked = savedMode == NutritionDisplayMode.All;
            _suppressModeEvents = false;
            OnPropertyChanged(nameof(ShowMinOnly));
            OnPropertyChanged(nameof(ShowNipOnly));
            OnPropertyChanged(nameof(ShowAll));
        }

        private void LoadViewPreferences()
        {
            _suppressToggleEvents = true;
            _displayDailyTotals = Preferences.Default.Get(DisplayDailyTotalsKey, false);
            _filterByDate = Preferences.Default.Get(FilterByDateKey, false);
            _selectedFilterDate = _sessionSelectedFilterDate ?? DateTime.Today;
            DailyTotalsCheckBox.IsChecked = _displayDailyTotals;
            FilterByDateCheckBox.IsChecked = _filterByDate;
            FilterDatePicker.Date = _selectedFilterDate;
            OnPropertyChanged(nameof(DisplayDailyTotals));
            OnPropertyChanged(nameof(ShowDailyTotals));
            OnPropertyChanged(nameof(ShowEatenFoods));
            OnPropertyChanged(nameof(ShowSelectionPanel));
            OnPropertyChanged(nameof(FilterByDate));
            _suppressToggleEvents = false;
        }

        private void OnDailyTotalsChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (_suppressToggleEvents)
            {
                return;
            }

            DisplayDailyTotals = e.Value;
            Preferences.Default.Set(DisplayDailyTotalsKey, DisplayDailyTotals);
        }

        private void OnFilterByDateChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (_suppressToggleEvents)
            {
                return;
            }

            FilterByDate = e.Value;
            Preferences.Default.Set(FilterByDateKey, FilterByDate);
        }

        private void OnFilterDateSelected(object? sender, DateChangedEventArgs e)
        {
            SelectedFilterDate = e.NewDate ?? DateTime.Today;
        }

        private void OnEatenSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedEatenFood = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as EatenFood : null;
        }

        private void OnEditSelectedClicked(object? sender, EventArgs e)
        {
            if (SelectedEatenFood == null)
            {
                return;
            }

            EditFoodNameLabel.Text = SelectedEatenFood.FoodDescription;
            EditUnitLabel.Text = SelectedEatenFood.UnitLabel;
            EditAmountEntry.Text = SelectedEatenFood.AmountEaten.ToString("N1", CultureInfo.CurrentCulture);

            var combined = $"{SelectedEatenFood.DateEaten} {SelectedEatenFood.TimeEaten}";
            if (!DateTime.TryParseExact(
                combined,
                "d-MMM-yy HH:mm",
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out var dateTime))
            {
                dateTime = DateTime.Now;
            }

            EditDatePicker.Date = dateTime.Date;
            EditTimePicker.Time = dateTime.TimeOfDay;
            ShowEditPanel = true;
        }

        private async void OnEditConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedEatenFood == null)
            {
                ShowEditPanel = false;
                return;
            }

            if (!double.TryParse(EditAmountEntry.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var amount) ||
                amount <= 0)
            {
                ShowInvalidAmountOverlay();
                return;
            }

            var selectedDate = EditDatePicker.Date ?? DateTime.Now.Date;
            var selectedTime = EditTimePicker.Time ?? DateTime.Now.TimeOfDay;
            var dateTime = new DateTime(
                selectedDate.Year,
                selectedDate.Month,
                selectedDate.Day,
                selectedTime.Hours,
                selectedTime.Minutes,
                0,
                DateTimeKind.Local);

            await DatabaseInitializer.EnsureDatabaseAsync();
            var updated = await _databaseService.UpdateEatenFoodAsync(SelectedEatenFood, amount, dateTime);
            if (!updated)
            {
                await DisplayAlertAsync("Error", "Unable to update the eaten log.", "OK");
                return;
            }

            ShowEditPanel = false;
            SelectedEatenFood = null;
            await LoadEatenFoodsAsync();
        }

        private void OnEditCancelClicked(object? sender, EventArgs e)
        {
            ShowEditPanel = false;
            SelectedEatenFood = null;
        }

        private void OnEditBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowEditPanel = false;
            SelectedEatenFood = null;
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

        private void OnDeleteSelectedClicked(object? sender, EventArgs e)
        {
            if (SelectedEatenFood == null)
            {
                return;
            }

            DeleteDescriptionLabel.Text = SelectedEatenFood.FoodDescription;
            var amountText = SelectedEatenFood.AmountEaten.ToString("N1", CultureInfo.CurrentCulture);
            DeleteAmountLabel.Text = $"Amount: {amountText} {SelectedEatenFood.UnitLabel}";
            ShowDeletePanel = true;
        }

        private async void OnDeleteConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedEatenFood == null)
            {
                ShowDeletePanel = false;
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var deleted = await _databaseService.DeleteEatenFoodAsync(SelectedEatenFood.EatenId);
            if (!deleted)
            {
                await DisplayAlertAsync("Error", "Unable to delete the eaten log.", "OK");
                return;
            }

            ShowDeletePanel = false;
            SelectedEatenFood = null;
            await LoadEatenFoodsAsync();
        }

        private void OnDeleteCancelClicked(object? sender, EventArgs e)
        {
            ShowDeletePanel = false;
            SelectedEatenFood = null;
        }

        private void OnDeleteBackdropTapped(object? sender, TappedEventArgs e)
        {
            ShowDeletePanel = false;
            SelectedEatenFood = null;
        }

        private async void OnExportCsvClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = await GetAndroidExchangeFolderAsync();
            if (folderUri == null)
            {
                return;
            }

            _exportCsvTargetFolderUri = folderUri;
            var targetPath = BuildAndroidFileDisplay(folderUri, DailyCsvFileName);
            _exportCsvTargetPath = targetPath;
            if (ExportCsvTargetPathLabel != null)
            {
                ExportCsvTargetPathLabel.Text = targetPath;
            }

            ShowExportCsvPanel = true;
#else
            var directory = GetExchangeDirectory();
            var targetPath = Path.Combine(directory, DailyCsvFileName);
            _exportCsvTargetPath = targetPath;
            if (ExportCsvTargetPathLabel != null)
            {
                ExportCsvTargetPathLabel.Text = targetPath;
            }

            ShowExportCsvPanel = true;
#endif
        }

        private async void OnChangeExportCsvFolderClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = await PickAndStoreAndroidExchangeFolderAsync();
            if (folderUri == null)
            {
                return;
            }

            _exportCsvTargetFolderUri = folderUri;
            var targetPath = BuildAndroidFileDisplay(folderUri, DailyCsvFileName);
            _exportCsvTargetPath = targetPath;
            if (ExportCsvTargetPathLabel != null)
            {
                ExportCsvTargetPathLabel.Text = targetPath;
            }
#elif WINDOWS
            var directory = await PickAndStoreWindowsExchangeDirectoryAsync();
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var targetPath = Path.Combine(directory, DailyCsvFileName);
            _exportCsvTargetPath = targetPath;
            if (ExportCsvTargetPathLabel != null)
            {
                ExportCsvTargetPathLabel.Text = targetPath;
            }
#endif
        }

        private async void OnExportCsvConfirmClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = _exportCsvTargetFolderUri;
            _exportCsvTargetFolderUri = null;
            _exportCsvTargetPath = null;
            ShowExportCsvPanel = false;

            if (folderUri == null)
            {
                return;
            }

            try
            {
                await ExportCsvToAndroidFolderAsync(folderUri);
            }
            catch (Exception)
            {
                // Suppress export errors to avoid additional dialogs after confirmation.
            }
            return;
#else
            var targetPath = _exportCsvTargetPath;
            _exportCsvTargetPath = null;
            ShowExportCsvPanel = false;

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
                await DatabaseInitializer.EnsureDatabaseAsync();
                var eatenFoods = (await _databaseService.GetEatenFoodsAsync()).ToList();
                var weightEntries = (await _databaseService.GetWeightEntriesAsync()).ToList();
                var csv = BuildEatenDailyAllCsv(eatenFoods, weightEntries);
                await File.WriteAllTextAsync(targetPath, csv, new UTF8Encoding(false));
            }
            catch (Exception)
            {
                // Suppress export errors to avoid additional dialogs after confirmation.
            }
#endif
        }

        private void OnExportCsvCancelClicked(object? sender, EventArgs e)
        {
            _exportCsvTargetPath = null;
#if ANDROID
            _exportCsvTargetFolderUri = null;
#endif
            ShowExportCsvPanel = false;
        }

        private void OnExportCsvBackdropTapped(object? sender, TappedEventArgs e)
        {
            _exportCsvTargetPath = null;
#if ANDROID
            _exportCsvTargetFolderUri = null;
#endif
            ShowExportCsvPanel = false;
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

        private static string GetExchangeDirectory()
        {
#if WINDOWS
            var storedDirectory = Preferences.Default.Get(ExchangeFolderPathKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(storedDirectory) && Directory.Exists(storedDirectory))
            {
                return storedDirectory;
            }

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
            var totals = AggregateDailyTotalsForCsv(eatenFoods, weightByDate);
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

        private static IReadOnlyList<DailyTotals> AggregateDailyTotalsForCsv(
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

#if ANDROID
        private static bool CanReadAndroidTree(Android.Net.Uri folderUri)
        {
            try
            {
                var resolver = Platform.AppContext.ContentResolver;
                if (resolver == null)
                {
                    return false;
                }

                var treeId = DocumentsContract.GetTreeDocumentId(folderUri);
                var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(folderUri, treeId);
                if (childrenUri == null)
                {
                    return false;
                }
                using var cursor = resolver.Query(
                    childrenUri,
                    new[] { DocumentsContract.Document.ColumnDocumentId },
                    null,
                    null,
                    null);
                return cursor != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Android.Net.Uri? FindAndroidFileUri(Android.Net.Uri folderUri, string fileName)
        {
            try
            {
                var resolver = Platform.AppContext.ContentResolver;
                if (resolver == null)
                {
                    return null;
                }

                var treeId = DocumentsContract.GetTreeDocumentId(folderUri);
                var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(folderUri, treeId);
                if (childrenUri == null)
                {
                    return null;
                }
                using var cursor = resolver.Query(
                    childrenUri,
                    new[]
                    {
                        DocumentsContract.Document.ColumnDocumentId,
                        DocumentsContract.Document.ColumnDisplayName
                    },
                    null,
                    null,
                    null);

                if (cursor == null)
                {
                    return null;
                }

                var idIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
                var nameIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
                if (idIndex < 0 || nameIndex < 0)
                {
                    return null;
                }

                while (cursor.MoveToNext())
                {
                    var name = cursor.GetString(nameIndex);
                    if (!string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var documentId = cursor.GetString(idIndex);
                    return DocumentsContract.BuildDocumentUriUsingTree(folderUri, documentId);
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Android.Net.Uri? CreateAndroidFileUri(
            Android.Net.Uri folderUri,
            string fileName,
            string mimeType)
        {
            try
            {
                var resolver = Platform.AppContext.ContentResolver;
                if (resolver == null)
                {
                    return null;
                }

                var treeId = DocumentsContract.GetTreeDocumentId(folderUri);
                var parentUri = DocumentsContract.BuildDocumentUriUsingTree(folderUri, treeId);
                if (parentUri == null)
                {
                    return null;
                }

                return DocumentsContract.CreateDocument(resolver, parentUri, mimeType, fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string BuildAndroidDirectoryDisplay(Android.Net.Uri uri)
        {
            const string fallback = "Selected folder";
            try
            {
                var docId = DocumentsContract.GetTreeDocumentId(uri);
                if (string.IsNullOrWhiteSpace(docId))
                {
                    return fallback;
                }

                var parts = docId.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return docId;
                }

                if (parts.Length == 1)
                {
                    return string.Equals(parts[0], "primary", StringComparison.OrdinalIgnoreCase)
                        ? "Internal storage"
                        : parts[0];
                }

                var volume = parts[0];
                var path = parts[1].Trim('/');
                if (string.Equals(volume, "primary", StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrWhiteSpace(path)
                        ? "Internal storage"
                        : $"Internal storage/{path}";
                }

                return string.IsNullOrWhiteSpace(path) ? volume : $"{volume}/{path}";
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static string BuildAndroidFileDisplay(Android.Net.Uri uri, string fileName)
        {
            var directory = BuildAndroidDirectoryDisplay(uri);
            return string.IsNullOrWhiteSpace(directory) ? fileName : $"{directory}/{fileName}";
        }

        private static async Task<Android.Net.Uri?> GetAndroidExchangeFolderAsync()
        {
            Android.Net.Uri? storedUri = null;
            var stored = Preferences.Default.Get(ExchangeFolderUriKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                try
                {
                    storedUri = Android.Net.Uri.Parse(stored);
                }
                catch (Exception)
                {
                    storedUri = null;
                }
            }

            if (storedUri != null)
            {
                if (CanReadAndroidTree(storedUri))
                {
                    return storedUri;
                }
            }

            var picked = await PickAndStoreAndroidExchangeFolderAsync();
            if (picked == null)
            {
                return null;
            }
            return picked;
        }

        private static async Task<Android.Net.Uri?> PickAndroidFolderAsync()
        {
            var tcs = new TaskCompletionSource<Android.Net.Uri?>();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var uri = await MainActivity.PickFolderAsync();
                    tcs.TrySetResult(uri);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return await tcs.Task.ConfigureAwait(false);
        }

        private static async Task<Android.Net.Uri?> PickAndStoreAndroidExchangeFolderAsync()
        {
            var picked = await PickAndroidFolderAsync();
            if (picked == null)
            {
                return null;
            }

            Preferences.Default.Set(ExchangeFolderUriKey, picked.ToString());
            return picked;
        }

        private async Task ExportCsvToAndroidFolderAsync(Android.Net.Uri folderUri)
        {
            var fileUri = FindAndroidFileUri(folderUri, DailyCsvFileName)
                ?? CreateAndroidFileUri(folderUri, DailyCsvFileName, "text/csv");
            if (fileUri == null)
            {
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var eatenFoods = (await _databaseService.GetEatenFoodsAsync()).ToList();
            var weightEntries = (await _databaseService.GetWeightEntriesAsync()).ToList();
            var csv = BuildEatenDailyAllCsv(eatenFoods, weightEntries);
            await using var targetStream = Platform.AppContext.ContentResolver?.OpenOutputStream(fileUri, "w");
            if (targetStream == null)
            {
                return;
            }

            await using var writer = new StreamWriter(targetStream, new UTF8Encoding(false));
            await writer.WriteAsync(csv);
        }
#endif

#if WINDOWS
        private static async Task<string?> PickAndStoreWindowsExchangeDirectoryAsync()
        {
            var picked = await PickWindowsExchangeDirectoryAsync();
            if (string.IsNullOrWhiteSpace(picked))
            {
                return null;
            }

            Preferences.Default.Set(ExchangeFolderPathKey, picked);
            return picked;
        }

        private static Task<string?> PickWindowsExchangeDirectoryAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var window = Application.Current?.Windows.FirstOrDefault();
                if (window?.Handler?.PlatformView is not MauiWinUIWindow nativeWindow)
                {
                    return null;
                }

                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(picker, nativeWindow.WindowHandle);
                var folder = await picker.PickSingleFolderAsync();
                return folder?.Path;
            });
        }
#endif

        private const string DailyCsvFileName = "EatenDailyAll.csv";
#if ANDROID
        private const string ExchangeFolderUriKey = "exchange_folder_uri";
#endif
#if WINDOWS
        private const string ExchangeFolderPathKey = "exchange_folder_path";
#endif

        private enum NutritionDisplayMode
        {
            Min,
            Nip,
            All
        }
    }
}
