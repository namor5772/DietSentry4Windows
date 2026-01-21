using System;
using System.Globalization;
using System.Threading.Tasks;

namespace DietSentry
{
    public partial class AddLiquidFoodPage : ContentPage
    {
        private readonly FoodDatabaseService _databaseService = new();

        public AddLiquidFoodPage()
        {
            InitializeComponent();
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            var description = DescriptionEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                await DisplayAlertAsync("Missing description", "Enter a description.", "OK");
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

            var processedDescription = EnsureLiquidMarker(description);
            var food = new Food
            {
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
            var inserted = await _databaseService.InsertFoodAsync(food);
            if (!inserted)
            {
                await DisplayAlertAsync("Error", "Unable to save the food.", "OK");
                return;
            }

            var route =
                $"//foodSearch?foodInsertedDescription={Uri.EscapeDataString(processedDescription)}";
            await Shell.Current.GoToAsync(route);
        }

        private async Task<double?> ParseEntryAsync(Entry entry, string fieldName)
        {
            if (TryParseOptionalDouble(entry.Text, out var value))
            {
                return value;
            }

            await DisplayAlertAsync("Invalid value", $"Enter a valid number for {fieldName}.", "OK");
            return null;
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

        private static string EnsureLiquidMarker(string description)
        {
            var trimmed = description.Trim();
            if (trimmed.EndsWith("mL#", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return $"{trimmed} mL#";
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//foodSearch");
        }
    }
}
