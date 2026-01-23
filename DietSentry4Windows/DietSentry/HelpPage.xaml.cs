using System;
using System.Collections.Generic;

namespace DietSentry
{
    public partial class HelpPage : ContentPage, IQueryAttributable
    {
        private readonly Dictionary<string, View> _sectionTargets;
        private string? _pendingSection;

        public HelpPage()
        {
            InitializeComponent();
            _sectionTargets = new Dictionary<string, View>(StringComparer.OrdinalIgnoreCase)
            {
                { "help-overview", HelpOverviewSection },
                { "foods-table", FoodsTableSection },
                { "nutrition-modes", NutritionModesSection },
                { "filters", FiltersSection },
                { "eaten-table", EatenTableSection },
                { "weight-table", WeightTableSection },
                { "adding-foods", AddFoodsSection },
                { "add-solid", AddSolidSection },
                { "add-liquid", AddLiquidSection },
                { "add-recipe", AddRecipeSection },
                { "add-json", AddJsonSection },
                { "edit-food", EditFoodSection },
                { "edit-recipe", EditRecipeSection },
                { "copy-food", CopyFoodSection },
                { "copy-recipe", CopyRecipeSection },
                { "import-export", ImportExportSection },
                { "logging", LoggingSection },
                { "palette", PaletteSection }
            };
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("section", out var value) && value != null)
            {
                _pendingSection = Uri.UnescapeDataString(value.ToString() ?? string.Empty);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (string.IsNullOrWhiteSpace(_pendingSection))
            {
                return;
            }

            if (_sectionTargets.TryGetValue(_pendingSection, out var target))
            {
                await HelpScrollView.ScrollToAsync(target, ScrollToPosition.Start, true);
            }

            _pendingSection = null;
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

            await Shell.Current.GoToAsync("help?section=help-overview");
        }
    }
}
