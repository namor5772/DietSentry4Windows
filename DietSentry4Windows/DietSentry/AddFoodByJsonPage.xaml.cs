using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DietSentry
{
    public partial class AddFoodByJsonPage : ContentPage
    {
        private readonly FoodDatabaseService _databaseService = new();

        public AddFoodByJsonPage()
        {
            InitializeComponent();
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            var rawText = JsonEditor?.Text ?? string.Empty;
            if (!TryExtractJsonPayload(rawText, out var jsonPayload))
            {
                await DisplayAlertAsync("Invalid JSON", "Please paste valid JSON.", "OK");
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(jsonPayload);
                var root = document.RootElement;

                var description = GetRequiredString(root, "FoodDescription").Trim();
                if (string.IsNullOrWhiteSpace(description))
                {
                    await DisplayAlertAsync("Missing description", "FoodDescription is required.", "OK");
                    return;
                }

                var notes = GetOptionalString(root, "notes").Trim();
                var food = new Food
                {
                    FoodId = 0,
                    FoodDescription = description,
                    Energy = GetRequiredDouble(root, "Energy"),
                    Protein = GetRequiredDouble(root, "Protein"),
                    FatTotal = GetRequiredDouble(root, "FatTotal"),
                    SaturatedFat = GetRequiredDouble(root, "SaturatedFat"),
                    TransFat = GetRequiredDouble(root, "TransFat"),
                    PolyunsaturatedFat = GetRequiredDouble(root, "PolyunsaturatedFat"),
                    MonounsaturatedFat = GetRequiredDouble(root, "MonounsaturatedFat"),
                    Carbohydrate = GetRequiredDouble(root, "Carbohydrate"),
                    Sugars = GetRequiredDouble(root, "Sugars"),
                    DietaryFibre = GetRequiredDouble(root, "DietaryFibre"),
                    Sodium = GetRequiredDouble(root, "SodiumNa"),
                    CalciumCa = GetRequiredDouble(root, "CalciumCa"),
                    PotassiumK = GetRequiredDouble(root, "PotassiumK"),
                    ThiaminB1 = GetRequiredDouble(root, "ThiaminB1"),
                    RiboflavinB2 = GetRequiredDouble(root, "RiboflavinB2"),
                    NiacinB3 = GetRequiredDouble(root, "NiacinB3"),
                    Folate = GetRequiredDouble(root, "Folate"),
                    IronFe = GetRequiredDouble(root, "IronFe"),
                    MagnesiumMg = GetRequiredDouble(root, "MagnesiumMg"),
                    VitaminC = GetRequiredDouble(root, "VitaminC"),
                    Caffeine = GetRequiredDouble(root, "Caffeine"),
                    Cholesterol = GetRequiredDouble(root, "Cholesterol"),
                    Alcohol = GetRequiredDouble(root, "Alcohol"),
                    Notes = notes
                };

                await DatabaseInitializer.EnsureDatabaseAsync();
                var inserted = await _databaseService.InsertFoodAsync(food);
                if (!inserted)
                {
                    await DisplayAlertAsync("Error", "Failed to insert food.", "OK");
                    return;
                }

                var route =
                    $"//foodSearch?foodInsertedDescription={Uri.EscapeDataString(description)}";
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Invalid JSON", "Invalid JSON or missing fields.", "OK");
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            if (JsonEditor != null)
            {
                JsonEditor.Text = string.Empty;
            }

            await Shell.Current.GoToAsync("//foodSearch");
        }

        private async void OnHelpClicked(object? sender, EventArgs e)
        {
            if (HelpOverlay == null || HelpSheet == null)
            {
                return;
            }

            HelpOverlay.IsVisible = true;
            HelpSheet.TranslationY = 220;
            _ = HelpSheet.TranslateTo(0, 0, 150, Easing.CubicOut);
        }

        private void OnHelpDismissed(object? sender, EventArgs e)
        {
            if (HelpOverlay == null)
            {
                return;
            }

            HelpOverlay.IsVisible = false;
        }

        private async void OnHelpOpenFullClicked(object? sender, EventArgs e)
        {
            if (HelpOverlay != null)
            {
                HelpOverlay.IsVisible = false;
            }

            await Shell.Current.GoToAsync("help?section=add-json");
        }

        private static bool TryExtractJsonPayload(string input, out string payload)
        {
            payload = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var jsonStart = input.IndexOf('{');
            var jsonEnd = input.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                return false;
            }

            payload = input.Substring(jsonStart, jsonEnd - jsonStart + 1);
            return true;
        }

        private static string GetRequiredString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element))
            {
                throw new InvalidOperationException();
            }

            return element.GetString() ?? string.Empty;
        }

        private static string GetOptionalString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element))
            {
                return string.Empty;
            }

            return element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : element.ToString();
        }

        private static double GetRequiredDouble(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element))
            {
                throw new InvalidOperationException();
            }

            if (element.ValueKind != JsonValueKind.Number)
            {
                throw new InvalidOperationException();
            }

            return element.GetDouble();
        }
    }
}
