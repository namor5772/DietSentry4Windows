using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

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
        private List<EatenFood> _allEatenFoods = new();
        private List<WeightEntry> _weightEntries = new();

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

        private enum NutritionDisplayMode
        {
            Min,
            Nip,
            All
        }
    }
}
