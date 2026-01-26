using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace DietSentry
{
    [QueryProperty(nameof(EditFoodId), "editFoodId")]
    [QueryProperty(nameof(CopyFoodId), "copyFoodId")]
    public partial class AddSolidFoodPage : ContentPage, IQueryAttributable
    {
        private readonly FoodDatabaseService _databaseService = new();
        private int? _editFoodId;
        private int? _copyFoodId;
        private bool _loadedFoodData;
        private string _screenTitle = "Add Solid Food";

        public string ScreenTitle
        {
            get => _screenTitle;
            private set
            {
                if (_screenTitle == value)
                {
                    return;
                }

                _screenTitle = value;
                OnPropertyChanged();
            }
        }

        public string? EditFoodId
        {
            get => _editFoodId?.ToString(CultureInfo.InvariantCulture);
            set
            {
                var previousId = _editFoodId;
                if (string.IsNullOrWhiteSpace(value))
                {
                    _editFoodId = null;
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    _editFoodId = id;
                }
                else
                {
                    _editFoodId = null;
                }

                if (previousId != _editFoodId)
                {
                    _loadedFoodData = false;
                }

                if (_editFoodId.HasValue)
                {
                    _copyFoodId = null;
                }

                UpdateScreenTitle();
            }
        }

        public string? CopyFoodId
        {
            get => _copyFoodId?.ToString(CultureInfo.InvariantCulture);
            set
            {
                var previousId = _copyFoodId;
                if (string.IsNullOrWhiteSpace(value))
                {
                    _copyFoodId = null;
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    _copyFoodId = id;
                }
                else
                {
                    _copyFoodId = null;
                }

                if (previousId != _copyFoodId)
                {
                    _loadedFoodData = false;
                }

                if (_copyFoodId.HasValue)
                {
                    _editFoodId = null;
                }

                UpdateScreenTitle();
            }
        }

        public AddSolidFoodPage()
        {
            InitializeComponent();
            BindingContext = this;
            UpdateScreenTitle();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("editFoodId", out var value))
            {
                EditFoodId = value?.ToString();
            }
            else
            {
                EditFoodId = null;
            }

            if (query.TryGetValue("copyFoodId", out var copyValue))
            {
                CopyFoodId = copyValue?.ToString();
            }
            else
            {
                CopyFoodId = null;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateScreenTitle();
            if (!_editFoodId.HasValue && !_copyFoodId.HasValue && _loadedFoodData)
            {
                ClearForm();
                _loadedFoodData = false;
            }
            if ((!_editFoodId.HasValue && !_copyFoodId.HasValue) || _loadedFoodData)
            {
                return;
            }

            await DatabaseInitializer.EnsureDatabaseAsync();
            var foodId = _editFoodId ?? _copyFoodId ?? 0;
            var food = await _databaseService.GetFoodByIdAsync(foodId);
            if (food == null)
            {
                await DisplayAlertAsync("Not found", "The selected food could not be loaded.", "OK");
                await Shell.Current.GoToAsync("//foodSearch");
                return;
            }

            DescriptionEntry.Text = FoodDescriptionFormatter.GetDisplayName(food.FoodDescription);
            EnergyEntry.Text = FormatNumber(food.Energy);
            ProteinEntry.Text = FormatNumber(food.Protein);
            FatTotalEntry.Text = FormatNumber(food.FatTotal);
            SaturatedFatEntry.Text = FormatNumber(food.SaturatedFat);
            TransFatEntry.Text = FormatNumber(food.TransFat);
            PolyunsaturatedFatEntry.Text = FormatNumber(food.PolyunsaturatedFat);
            MonounsaturatedFatEntry.Text = FormatNumber(food.MonounsaturatedFat);
            CarbohydrateEntry.Text = FormatNumber(food.Carbohydrate);
            SugarsEntry.Text = FormatNumber(food.Sugars);
            DietaryFibreEntry.Text = FormatNumber(food.DietaryFibre);
            SodiumEntry.Text = FormatNumber(food.Sodium);
            CalciumEntry.Text = FormatNumber(food.CalciumCa);
            PotassiumEntry.Text = FormatNumber(food.PotassiumK);
            ThiaminB1Entry.Text = FormatNumber(food.ThiaminB1);
            RiboflavinB2Entry.Text = FormatNumber(food.RiboflavinB2);
            NiacinB3Entry.Text = FormatNumber(food.NiacinB3);
            FolateEntry.Text = FormatNumber(food.Folate);
            IronEntry.Text = FormatNumber(food.IronFe);
            MagnesiumEntry.Text = FormatNumber(food.MagnesiumMg);
            VitaminCEntry.Text = FormatNumber(food.VitaminC);
            CaffeineEntry.Text = FormatNumber(food.Caffeine);
            CholesterolEntry.Text = FormatNumber(food.Cholesterol);
            AlcoholEntry.Text = FormatNumber(food.Alcohol);
            NotesEditor.Text = food.Notes;
            _loadedFoodData = true;
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            var description = DescriptionEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                ShowMissingDescriptionOverlay("Enter a description.");
                return;
            }

            var energy = await ParseEntryAsync(EnergyEntry, "Energy (kJ)");
            if (energy is null) return;
            var protein = await ParseEntryAsync(ProteinEntry, "Protein (g)");
            if (protein is null) return;
            var fatTotal = await ParseEntryAsync(FatTotalEntry, "Fat, Total (g)");
            if (fatTotal is null) return;
            var saturatedFat = await ParseEntryAsync(SaturatedFatEntry, "Saturated (g)");
            if (saturatedFat is null) return;
            var transFat = await ParseEntryAsync(TransFatEntry, "Trans (mg)");
            if (transFat is null) return;
            var polyunsaturatedFat = await ParseEntryAsync(PolyunsaturatedFatEntry, "Polyunsaturated (g)");
            if (polyunsaturatedFat is null) return;
            var monounsaturatedFat = await ParseEntryAsync(MonounsaturatedFatEntry, "Monounsaturated (g)");
            if (monounsaturatedFat is null) return;
            var carbohydrate = await ParseEntryAsync(CarbohydrateEntry, "Carbohydrate (g)");
            if (carbohydrate is null) return;
            var sugars = await ParseEntryAsync(SugarsEntry, "Sugars (g)");
            if (sugars is null) return;
            var dietaryFibre = await ParseEntryAsync(DietaryFibreEntry, "Dietary Fibre (g)");
            if (dietaryFibre is null) return;
            var sodium = await ParseEntryAsync(SodiumEntry, "Sodium (mg)");
            if (sodium is null) return;
            var calcium = await ParseEntryAsync(CalciumEntry, "Calcium (mg)");
            if (calcium is null) return;
            var potassium = await ParseEntryAsync(PotassiumEntry, "Potassium (mg)");
            if (potassium is null) return;
            var thiaminB1 = await ParseEntryAsync(ThiaminB1Entry, "Thiamin B1 (mg)");
            if (thiaminB1 is null) return;
            var riboflavinB2 = await ParseEntryAsync(RiboflavinB2Entry, "Riboflavin B2 (mg)");
            if (riboflavinB2 is null) return;
            var niacinB3 = await ParseEntryAsync(NiacinB3Entry, "Niacin B3 (mg)");
            if (niacinB3 is null) return;
            var folate = await ParseEntryAsync(FolateEntry, "Folate (ug)");
            if (folate is null) return;
            var ironFe = await ParseEntryAsync(IronEntry, "Iron (mg)");
            if (ironFe is null) return;
            var magnesiumMg = await ParseEntryAsync(MagnesiumEntry, "Magnesium (mg)");
            if (magnesiumMg is null) return;
            var vitaminC = await ParseEntryAsync(VitaminCEntry, "Vitamin C (mg)");
            if (vitaminC is null) return;
            var caffeine = await ParseEntryAsync(CaffeineEntry, "Caffeine (mg)");
            if (caffeine is null) return;
            var cholesterol = await ParseEntryAsync(CholesterolEntry, "Cholesterol (mg)");
            if (cholesterol is null) return;
            var alcohol = await ParseEntryAsync(AlcoholEntry, "Alcohol (g)");
            if (alcohol is null) return;

            var processedDescription = EnsureSolidMarker(description);
            var food = new Food
            {
                FoodId = _editFoodId ?? 0,
                FoodDescription = processedDescription,
                Energy = energy.Value,
                Protein = protein.Value,
                FatTotal = fatTotal.Value,
                SaturatedFat = saturatedFat.Value,
                TransFat = transFat.Value,
                PolyunsaturatedFat = polyunsaturatedFat.Value,
                MonounsaturatedFat = monounsaturatedFat.Value,
                Carbohydrate = carbohydrate.Value,
                Sugars = sugars.Value,
                DietaryFibre = dietaryFibre.Value,
                Sodium = sodium.Value,
                CalciumCa = calcium.Value,
                PotassiumK = potassium.Value,
                ThiaminB1 = thiaminB1.Value,
                RiboflavinB2 = riboflavinB2.Value,
                NiacinB3 = niacinB3.Value,
                Folate = folate.Value,
                IronFe = ironFe.Value,
                MagnesiumMg = magnesiumMg.Value,
                VitaminC = vitaminC.Value,
                Caffeine = caffeine.Value,
                Cholesterol = cholesterol.Value,
                Alcohol = alcohol.Value,
                Notes = NotesEditor.Text?.Trim() ?? string.Empty
            };

            await DatabaseInitializer.EnsureDatabaseAsync();
            var saved = _editFoodId.HasValue
                ? await _databaseService.UpdateFoodAsync(food)
                : await _databaseService.InsertFoodAsync(food);
            if (!saved)
            {
                await DisplayAlertAsync("Error", "Unable to save the food.", "OK");
                return;
            }

            var route =
                $"//foodSearch?foodInsertedDescription={Uri.EscapeDataString(processedDescription)}";
            await Shell.Current.GoToAsync(route);
        }

        private Task<double?> ParseEntryAsync(Entry entry, string fieldName)
        {
            if (TryParseOptionalDouble(entry.Text, out var value))
            {
                if (value < 0)
                {
                    ShowInvalidValueOverlay($"Enter a non-negative number for {fieldName}.");
                    return Task.FromResult<double?>(null);
                }

                return Task.FromResult<double?>(value);
            }

            ShowInvalidValueOverlay($"Enter a valid number for {fieldName}.");
            return Task.FromResult<double?>(null);
        }

        private static bool TryParseOptionalDouble(string? input, out double value)
        {
            var normalized = (input ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace(',', '.');
            if (string.IsNullOrEmpty(normalized))
            {
                value = 0;
                return true;
            }

            return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static string EnsureSolidMarker(string description)
        {
            var trimmed = description.Trim();
            if (trimmed.EndsWith("#", StringComparison.Ordinal))
            {
                return trimmed;
            }

            return $"{trimmed} #";
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

            var (title, body) = GetHelpContent();
            HelpTitleLabel.Text = title;
            HelpBodyView.MarkdownText = body;

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

        private void ShowMissingDescriptionOverlay(string message)
        {
            if (MissingDescriptionOverlay == null || MissingDescriptionMessageLabel == null)
            {
                return;
            }

            MissingDescriptionMessageLabel.Text = message;
            MissingDescriptionOverlay.IsVisible = true;
        }

        private void OnMissingDescriptionOkClicked(object? sender, EventArgs e)
        {
            if (MissingDescriptionOverlay == null)
            {
                return;
            }

            MissingDescriptionOverlay.IsVisible = false;
        }

        private void OnMissingDescriptionBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (MissingDescriptionOverlay == null)
            {
                return;
            }

            MissingDescriptionOverlay.IsVisible = false;
        }

        private void ShowInvalidValueOverlay(string message)
        {
            if (InvalidValueOverlay == null || InvalidValueMessageLabel == null)
            {
                return;
            }

            InvalidValueMessageLabel.Text = message;
            InvalidValueOverlay.IsVisible = true;
        }

        private void OnInvalidValueOkClicked(object? sender, EventArgs e)
        {
            if (InvalidValueOverlay == null)
            {
                return;
            }

            InvalidValueOverlay.IsVisible = false;
        }

        private void OnInvalidValueBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (InvalidValueOverlay == null)
            {
                return;
            }

            InvalidValueOverlay.IsVisible = false;
        }

        private (string Title, string Body) GetHelpContent()
        {
            if (_editFoodId.HasValue)
            {
                return (HelpContent.FormatHelpTitle("Editing Solid Food"), HelpContent.EditSolidFoodBody);
            }

            if (_copyFoodId.HasValue)
            {
                return (HelpContent.FormatHelpTitle("Copying Solid Food"), HelpContent.CopySolidFoodBody);
            }

            return (HelpContent.FormatHelpTitle("Add Solid Food"), HelpContent.AddSolidFoodBody);
        }

        private void UpdateScreenTitle()
        {
            if (_editFoodId.HasValue)
            {
                ScreenTitle = "Editing Solid Food";
            }
            else if (_copyFoodId.HasValue)
            {
                ScreenTitle = "Copying Solid Food";
            }
            else
            {
                ScreenTitle = "Add Solid Food";
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void ClearForm()
        {
            DescriptionEntry.Text = string.Empty;
            EnergyEntry.Text = string.Empty;
            ProteinEntry.Text = string.Empty;
            FatTotalEntry.Text = string.Empty;
            SaturatedFatEntry.Text = string.Empty;
            TransFatEntry.Text = string.Empty;
            PolyunsaturatedFatEntry.Text = string.Empty;
            MonounsaturatedFatEntry.Text = string.Empty;
            CarbohydrateEntry.Text = string.Empty;
            SugarsEntry.Text = string.Empty;
            DietaryFibreEntry.Text = string.Empty;
            SodiumEntry.Text = string.Empty;
            CalciumEntry.Text = string.Empty;
            PotassiumEntry.Text = string.Empty;
            ThiaminB1Entry.Text = string.Empty;
            RiboflavinB2Entry.Text = string.Empty;
            NiacinB3Entry.Text = string.Empty;
            FolateEntry.Text = string.Empty;
            IronEntry.Text = string.Empty;
            MagnesiumEntry.Text = string.Empty;
            VitaminCEntry.Text = string.Empty;
            CaffeineEntry.Text = string.Empty;
            CholesterolEntry.Text = string.Empty;
            AlcoholEntry.Text = string.Empty;
            NotesEditor.Text = string.Empty;
        }
    }
}
