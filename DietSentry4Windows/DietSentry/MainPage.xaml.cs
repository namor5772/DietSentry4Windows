using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private bool _showExportPanel;
        private bool _showImportPanel;
        private string? _insertedFoodDescription;
        private string? _exportTargetPath;
        private string? _importSourcePath;
#if ANDROID
        private Android.Net.Uri? _exportTargetFolderUri;
        private Android.Net.Uri? _importSourceFileUri;
#endif

        public ObservableCollection<Food> Foods { get; } = new();
        public bool ShowMinOnly => _nutritionDisplayMode == NutritionDisplayMode.Min;
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

                _insertedFoodDescription = System.Uri.UnescapeDataString(value);
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
#if ANDROID
            var folderUri = await GetAndroidExchangeFolderAsync();
            if (folderUri == null)
            {
                return;
            }

            _exportTargetFolderUri = folderUri;
            var targetPath = BuildAndroidFileDisplay(folderUri, DatabaseFileName);
            _exportTargetPath = targetPath;
            if (ExportTargetPathLabel != null)
            {
                ExportTargetPathLabel.Text = targetPath;
            }

            ShowExportPanel = true;
            return;
#else
            var directory = GetExchangeDirectory();
            var targetPath = Path.Combine(directory, DatabaseFileName);
            _exportTargetPath = targetPath;
            if (ExportTargetPathLabel != null)
            {
                ExportTargetPathLabel.Text = targetPath;
            }

            ShowExportPanel = true;
#endif
        }

        private async void OnChangeExportFolderClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = await PickAndStoreAndroidExchangeFolderAsync();
            if (folderUri == null)
            {
                return;
            }

            _exportTargetFolderUri = folderUri;
            var targetPath = BuildAndroidFileDisplay(folderUri, DatabaseFileName);
            _exportTargetPath = targetPath;
            if (ExportTargetPathLabel != null)
            {
                ExportTargetPathLabel.Text = targetPath;
            }
#elif WINDOWS
            var directory = await PickAndStoreWindowsExchangeDirectoryAsync();
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var targetPath = Path.Combine(directory, DatabaseFileName);
            _exportTargetPath = targetPath;
            if (ExportTargetPathLabel != null)
            {
                ExportTargetPathLabel.Text = targetPath;
            }
#endif
        }

        private async void OnExportDbConfirmClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = _exportTargetFolderUri;
            _exportTargetFolderUri = null;
            _exportTargetPath = null;
            ShowExportPanel = false;

            if (folderUri == null)
            {
                return;
            }

            try
            {
                await ExportDatabaseToAndroidFolderAsync(folderUri);
            }
            catch (Exception)
            {
                // Suppress export errors to avoid additional dialogs after confirmation.
            }
            return;
#else
            var targetPath = _exportTargetPath;
            _exportTargetPath = null;
            ShowExportPanel = false;

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
                var sourcePath = DatabaseInitializer.GetDatabasePath();
                File.Copy(sourcePath, targetPath, true);
            }
            catch (Exception)
            {
                // Suppress export errors to avoid additional dialogs after confirmation.
            }
#endif
        }

        private void OnExportDbCancelClicked(object? sender, EventArgs e)
        {
            _exportTargetPath = null;
#if ANDROID
            _exportTargetFolderUri = null;
#endif
            ShowExportPanel = false;
        }

        private void OnExportDbBackdropTapped(object? sender, TappedEventArgs e)
        {
            _exportTargetPath = null;
#if ANDROID
            _exportTargetFolderUri = null;
#endif
            ShowExportPanel = false;
        }

        private async void OnImportDbClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = await GetAndroidExchangeFolderAsync();
            if (folderUri == null)
            {
                return;
            }

            var sourceUri = FindAndroidFileUri(folderUri, DatabaseFileName);
            if (sourceUri == null)
            {
                var displayDirectory = BuildAndroidDirectoryDisplay(folderUri);
                await DisplayAlertAsync(
                    "Import db",
                    $"No {DatabaseFileName} found.\n\nPlace it in:\n{displayDirectory}\n\nThen try again.",
                    "OK");
                return;
            }

            _importSourceFileUri = sourceUri;
            var sourcePath = BuildAndroidFileDisplay(folderUri, DatabaseFileName);
            _importSourcePath = sourcePath;
            if (ImportSourcePathLabel != null)
            {
                ImportSourcePathLabel.Text = sourcePath;
            }

            ShowImportPanel = true;
#else
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

            _importSourcePath = sourcePath;
            if (ImportSourcePathLabel != null)
            {
                ImportSourcePathLabel.Text = sourcePath;
            }

            ShowImportPanel = true;
#endif
        }

        private async void OnChangeImportFolderClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var folderUri = await PickAndStoreAndroidExchangeFolderAsync();
            if (folderUri == null)
            {
                return;
            }

            var sourceUri = FindAndroidFileUri(folderUri, DatabaseFileName);
            _importSourceFileUri = sourceUri;
            var sourcePath = BuildAndroidFileDisplay(folderUri, DatabaseFileName);
            _importSourcePath = sourcePath;
            if (ImportSourcePathLabel != null)
            {
                ImportSourcePathLabel.Text = sourcePath;
            }

            if (sourceUri == null)
            {
                var displayDirectory = BuildAndroidDirectoryDisplay(folderUri);
                await DisplayAlertAsync(
                    "Import db",
                    $"Folder updated.\n\nNo {DatabaseFileName} found.\n\nPlace it in:\n{displayDirectory}\n\nThen try again.",
                    "OK");
            }
#elif WINDOWS
            var directory = await PickAndStoreWindowsExchangeDirectoryAsync();
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var sourcePath = Path.Combine(directory, DatabaseFileName);
            _importSourcePath = sourcePath;
            if (ImportSourcePathLabel != null)
            {
                ImportSourcePathLabel.Text = sourcePath;
            }

            if (!File.Exists(sourcePath))
            {
                await DisplayAlertAsync(
                    "Import db",
                    $"Folder updated.\n\nNo {DatabaseFileName} found.\n\nPlace it in:\n{directory}\n\nThen try again.",
                    "OK");
            }
#endif
        }

        private async void OnImportDbConfirmClicked(object? sender, EventArgs e)
        {
#if ANDROID
            var sourceUri = _importSourceFileUri;
            _importSourceFileUri = null;
            _importSourcePath = null;
            ShowImportPanel = false;

            if (sourceUri == null)
            {
                return;
            }

            try
            {
                await ImportDatabaseFromAndroidUriAsync(sourceUri);
                await LoadFoodsAsync(FoodFilterEntry?.Text);
            }
            catch (Exception)
            {
                // Suppress import errors to avoid additional dialogs after confirmation.
            }
            return;
#else
            var sourcePath = _importSourcePath;
            _importSourcePath = null;
            ShowImportPanel = false;

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                var targetPath = DatabaseInitializer.GetDatabasePath();
                File.Copy(sourcePath, targetPath, true);
                await LoadFoodsAsync(FoodFilterEntry?.Text);
            }
            catch (Exception)
            {
                // Suppress import errors to avoid additional dialogs after confirmation.
            }
#endif
        }

        private void OnImportDbCancelClicked(object? sender, EventArgs e)
        {
            _importSourcePath = null;
#if ANDROID
            _importSourceFileUri = null;
#endif
            ShowImportPanel = false;
        }

        private void OnImportDbBackdropTapped(object? sender, TappedEventArgs e)
        {
            _importSourcePath = null;
#if ANDROID
            _importSourceFileUri = null;
#endif
            ShowImportPanel = false;
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
                ShowNotAvailableOverlay("Convert is only available for liquid foods.");
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

        public bool ShowExportPanel
        {
            get => _showExportPanel;
            private set
            {
                if (_showExportPanel == value)
                {
                    return;
                }

                _showExportPanel = value;
                OnPropertyChanged();
            }
        }

        public bool ShowImportPanel
        {
            get => _showImportPanel;
            private set
            {
                if (_showImportPanel == value)
                {
                    return;
                }

                _showImportPanel = value;
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
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return density.ToString("G", CultureInfo.InvariantCulture);
            }

            if (cleaned.Contains('.', StringComparison.Ordinal))
            {
                var parts = cleaned.Split('.', 2);
                var fractional = parts[1].TrimEnd('0');
                return string.IsNullOrEmpty(fractional)
                    ? parts[0]
                    : $"{parts[0]}.{fractional}";
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

        private void ShowNotAvailableOverlay(string message)
        {
            if (NotAvailableOverlay == null || NotAvailableMessageLabel == null)
            {
                return;
            }

            NotAvailableMessageLabel.Text = message;
            NotAvailableOverlay.IsVisible = true;
        }

        private void OnNotAvailableOkClicked(object? sender, EventArgs e)
        {
            if (NotAvailableOverlay == null)
            {
                return;
            }

            NotAvailableOverlay.IsVisible = false;
        }

        private void OnNotAvailableBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (NotAvailableOverlay == null)
            {
                return;
            }

            NotAvailableOverlay.IsVisible = false;
        }

        private void ShowInvalidDensityOverlay(string message)
        {
            if (InvalidDensityOverlay == null || InvalidDensityMessageLabel == null)
            {
                return;
            }

            InvalidDensityMessageLabel.Text = message;
            InvalidDensityOverlay.IsVisible = true;
        }

        private void OnInvalidDensityOkClicked(object? sender, EventArgs e)
        {
            if (InvalidDensityOverlay == null)
            {
                return;
            }

            InvalidDensityOverlay.IsVisible = false;
        }

        private void OnInvalidDensityBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (InvalidDensityOverlay == null)
            {
                return;
            }

            InvalidDensityOverlay.IsVisible = false;
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
                ShowInvalidDensityOverlay("Enter a valid density value.");
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

#if ANDROID
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

        private static async Task ExportDatabaseToAndroidFolderAsync(Android.Net.Uri folderUri)
        {
            var fileUri = FindAndroidFileUri(folderUri, DatabaseFileName)
                ?? CreateAndroidFileUri(folderUri, DatabaseFileName, "application/octet-stream");
            if (fileUri == null)
            {
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var sourcePath = DatabaseInitializer.GetDatabasePath();
            await using var sourceStream = File.OpenRead(sourcePath);
            await using var targetStream = Platform.AppContext.ContentResolver?.OpenOutputStream(fileUri, "w");
            if (targetStream == null)
            {
                return;
            }

            await sourceStream.CopyToAsync(targetStream);
        }

        private static async Task ImportDatabaseFromAndroidUriAsync(Android.Net.Uri sourceUri)
        {
            var inputStream = Platform.AppContext.ContentResolver?.OpenInputStream(sourceUri);
            if (inputStream == null)
            {
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var targetPath = DatabaseInitializer.GetDatabasePath();
            await using var targetStream = File.Create(targetPath);
            await inputStream.CopyToAsync(targetStream);
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

        private enum NutritionDisplayMode
        {
            Min,
            Nip,
            All
        }

#if ANDROID
        private const string ExchangeFolderUriKey = "exchange_folder_uri";
#endif
#if WINDOWS
        private const string ExchangeFolderPathKey = "exchange_folder_path";
#endif
        private const string DatabaseFileName = "foods.db";
    }
}
