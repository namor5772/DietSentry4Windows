using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DietSentry
{
    [QueryProperty(nameof(EditRecipeFoodId), "editRecipeFoodId")]
    [QueryProperty(nameof(CopyRecipeFoodId), "copyRecipeFoodId")]
    public partial class AddRecipePage : ContentPage, IQueryAttributable
    {
        private readonly FoodDatabaseService _databaseService = new();
        private Food? _selectedFood;
        private RecipeItem? _selectedRecipe;
        private bool _showAddIngredientPanel;
        private bool _showEditIngredientPanel;
        private bool _showEditNotesPanel;
        private string _ingredientsHeaderText = "Ingredients 0.0 (g) Total";
        private string _recipeNotes = string.Empty;
        private string _screenTitle = "Add Recipe";
        private int? _editingFoodId;
        private int? _copySourceFoodId;
        private bool _recipesPrepared;

        public ObservableCollection<Food> Foods { get; } = new();
        public ObservableCollection<RecipeItem> RecipeItems { get; } = new();

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

        public string? EditRecipeFoodId
        {
            get => _editingFoodId?.ToString(CultureInfo.InvariantCulture);
            set
            {
                var previousId = _editingFoodId;
                if (string.IsNullOrWhiteSpace(value))
                {
                    _editingFoodId = null;
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    _editingFoodId = id;
                }
                else
                {
                    _editingFoodId = null;
                }

                if (previousId != _editingFoodId)
                {
                    _recipesPrepared = false;
                }

                if (_editingFoodId.HasValue)
                {
                    _copySourceFoodId = null;
                }

                UpdateScreenTitle();
            }
        }

        public string? CopyRecipeFoodId
        {
            get => _copySourceFoodId?.ToString(CultureInfo.InvariantCulture);
            set
            {
                var previousId = _copySourceFoodId;
                if (string.IsNullOrWhiteSpace(value))
                {
                    _copySourceFoodId = null;
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    _copySourceFoodId = id;
                }
                else
                {
                    _copySourceFoodId = null;
                }

                if (previousId != _copySourceFoodId)
                {
                    _recipesPrepared = false;
                }

                if (_copySourceFoodId.HasValue)
                {
                    _editingFoodId = null;
                }

                UpdateScreenTitle();
            }
        }

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
                OnPropertyChanged(nameof(SelectedFoodDescription));
                OnPropertyChanged(nameof(SelectedFoodUnitLabel));
            }
        }

        public RecipeItem? SelectedRecipe
        {
            get => _selectedRecipe;
            private set
            {
                if (_selectedRecipe == value)
                {
                    return;
                }

                _selectedRecipe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowRecipeSelectionPanel));
                OnPropertyChanged(nameof(SelectedRecipeDescription));
                OnPropertyChanged(nameof(SelectedRecipeAmountDisplay));
                OnPropertyChanged(nameof(SelectedRecipeUnitLabel));
            }
        }

        public bool ShowRecipeSelectionPanel => SelectedRecipe != null;

        public string SelectedFoodDescription => SelectedFood?.FoodDescription ?? string.Empty;

        public string SelectedFoodUnitLabel => SelectedFood == null
            ? "g"
            : FoodDescriptionFormatter.GetUnit(SelectedFood.FoodDescription);

        public string SelectedRecipeDescription => SelectedRecipe?.FoodDescription ?? string.Empty;

        public string SelectedRecipeUnitLabel => SelectedRecipe == null
            ? "g"
            : SelectedRecipe.UnitLabel;

        public string SelectedRecipeAmountDisplay => SelectedRecipe == null
            ? string.Empty
            : $"Amount: {SelectedRecipe.Amount.ToString("N1", CultureInfo.InvariantCulture)} {SelectedRecipe.UnitLabel}";

        public string IngredientsHeaderText
        {
            get => _ingredientsHeaderText;
            private set
            {
                if (_ingredientsHeaderText == value)
                {
                    return;
                }

                _ingredientsHeaderText = value;
                OnPropertyChanged();
            }
        }

        public bool ShowAddIngredientPanel
        {
            get => _showAddIngredientPanel;
            private set
            {
                if (_showAddIngredientPanel == value)
                {
                    return;
                }

                _showAddIngredientPanel = value;
                OnPropertyChanged();
            }
        }

        public bool ShowEditIngredientPanel
        {
            get => _showEditIngredientPanel;
            private set
            {
                if (_showEditIngredientPanel == value)
                {
                    return;
                }

                _showEditIngredientPanel = value;
                OnPropertyChanged();
            }
        }

        public bool ShowEditNotesPanel
        {
            get => _showEditNotesPanel;
            private set
            {
                if (_showEditNotesPanel == value)
                {
                    return;
                }

                _showEditNotesPanel = value;
                OnPropertyChanged();
            }
        }

        public AddRecipePage()
        {
            InitializeComponent();
            BindingContext = this;
            UpdateScreenTitle();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("editRecipeFoodId", out var value))
            {
                EditRecipeFoodId = value?.ToString();
            }
            else
            {
                EditRecipeFoodId = null;
            }

            if (query.TryGetValue("copyRecipeFoodId", out var copyValue))
            {
                CopyRecipeFoodId = copyValue?.ToString();
            }
            else
            {
                CopyRecipeFoodId = null;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateScreenTitle();
            if (!_editingFoodId.HasValue && !_copySourceFoodId.HasValue && _recipesPrepared)
            {
                _recipesPrepared = false;
                DescriptionEntry.Text = string.Empty;
                _recipeNotes = string.Empty;
            }
            await DatabaseInitializer.EnsureDatabaseAsync();
            await PrepareRecipeAsync();
            await LoadFoodsAsync(FoodFilterEntry?.Text);
            await LoadRecipesAsync();
            DescriptionEntry?.Focus();
        }

        private async Task LoadFoodsAsync(string? query)
        {
            IReadOnlyList<Food> foods;
            if (!string.IsNullOrWhiteSpace(query))
            {
                foods = await _databaseService.SearchFoodsAsync(query);
            }
            else
            {
                foods = await _databaseService.GetFoodsAsync();
            }

            Foods.Clear();
            foreach (var food in foods)
            {
                Foods.Add(food);
            }
        }

        private async Task LoadRecipesAsync()
        {
            var recipes = _editingFoodId.HasValue
                ? await _databaseService.GetCopiedRecipeItemsAsync(_editingFoodId.Value)
                : await _databaseService.GetRecipeItemsAsync();
            RecipeItems.Clear();
            foreach (var recipe in recipes)
            {
                RecipeItems.Add(recipe);
            }

            UpdateIngredientsHeader();
        }

        private void UpdateIngredientsHeader()
        {
            var total = RecipeItems.Sum(item => item.Amount);
            IngredientsHeaderText =
                $"Ingredients {total.ToString("N1", CultureInfo.InvariantCulture)} (g) Total";
        }

        private async void OnFoodFilterCompleted(object? sender, EventArgs e)
        {
            await LoadFoodsAsync(FoodFilterEntry?.Text);
        }

        private async void OnClearFilterClicked(object? sender, EventArgs e)
        {
            FoodFilterEntry.Text = string.Empty;
            await LoadFoodsAsync(string.Empty);
        }

        private async void OnFoodSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
            {
                return;
            }

            SelectedFood = e.CurrentSelection[0] as Food;
            FoodsCollectionView.SelectedItem = null;
            SelectedRecipe = null;
            if (SelectedFood == null)
            {
                return;
            }

            var unit = FoodDescriptionFormatter.GetUnit(SelectedFood.FoodDescription);
            if (unit.Equals("mL", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlertAsync(
                    "CANNOT ADD THIS FOOD",
                    "Only foods measured in grams can be added to a recipe",
                    "OK");
                SelectedFood = null;
                return;
            }

            AddIngredientAmountEntry.Text = string.Empty;
            ShowAddIngredientPanel = true;
        }

        private async void OnAddIngredientConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedFood == null)
            {
                ShowAddIngredientPanel = false;
                return;
            }

            if (!TryParsePositiveDouble(AddIngredientAmountEntry.Text, out var amount))
            {
                await DisplayAlertAsync("Invalid amount", "Enter a valid amount.", "OK");
                return;
            }

            var inserted = await _databaseService.InsertRecipeItemFromFoodAsync(
                SelectedFood,
                amount,
                _editingFoodId ?? 0,
                _editingFoodId.HasValue ? 1 : 0);
            if (!inserted)
            {
                await DisplayAlertAsync("Error", "Unable to add item to recipe.", "OK");
            }

            ShowAddIngredientPanel = false;
            SelectedFood = null;
            await LoadRecipesAsync();
        }

        private void OnAddIngredientCancelClicked(object? sender, EventArgs e)
        {
            ShowAddIngredientPanel = false;
            SelectedFood = null;
        }

        private void OnAddIngredientBackdropTapped(object? sender, EventArgs e)
        {
            ShowAddIngredientPanel = false;
            SelectedFood = null;
        }

        private void OnRecipeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedRecipe = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as RecipeItem : null;
        }

        private void OnEditRecipeClicked(object? sender, EventArgs e)
        {
            if (SelectedRecipe == null)
            {
                return;
            }

            EditIngredientAmountEntry.Text = SelectedRecipe.Amount.ToString("N1", CultureInfo.InvariantCulture);
            ShowEditIngredientPanel = true;
        }

        private async void OnDeleteRecipeClicked(object? sender, EventArgs e)
        {
            if (SelectedRecipe == null)
            {
                return;
            }

            var deleted = await _databaseService.DeleteRecipeItemAsync(SelectedRecipe.RecipeId);
            if (!deleted)
            {
                await DisplayAlertAsync("Error", "Unable to delete recipe item.", "OK");
            }

            SelectedRecipe = null;
            await LoadRecipesAsync();
        }

        private async void OnEditIngredientConfirmClicked(object? sender, EventArgs e)
        {
            if (SelectedRecipe == null)
            {
                ShowEditIngredientPanel = false;
                return;
            }

            if (!TryParsePositiveDouble(EditIngredientAmountEntry.Text, out var newAmount))
            {
                await DisplayAlertAsync("Invalid amount", "Enter a valid amount.", "OK");
                return;
            }

            var factor = SelectedRecipe.Amount <= 0 ? 0 : newAmount / SelectedRecipe.Amount;
            var updatedRecipe = SelectedRecipe with
            {
                Amount = newAmount,
                Energy = SelectedRecipe.Energy * factor,
                Protein = SelectedRecipe.Protein * factor,
                FatTotal = SelectedRecipe.FatTotal * factor,
                SaturatedFat = SelectedRecipe.SaturatedFat * factor,
                TransFat = SelectedRecipe.TransFat * factor,
                PolyunsaturatedFat = SelectedRecipe.PolyunsaturatedFat * factor,
                MonounsaturatedFat = SelectedRecipe.MonounsaturatedFat * factor,
                Carbohydrate = SelectedRecipe.Carbohydrate * factor,
                Sugars = SelectedRecipe.Sugars * factor,
                DietaryFibre = SelectedRecipe.DietaryFibre * factor,
                Sodium = SelectedRecipe.Sodium * factor,
                CalciumCa = SelectedRecipe.CalciumCa * factor,
                PotassiumK = SelectedRecipe.PotassiumK * factor,
                ThiaminB1 = SelectedRecipe.ThiaminB1 * factor,
                RiboflavinB2 = SelectedRecipe.RiboflavinB2 * factor,
                NiacinB3 = SelectedRecipe.NiacinB3 * factor,
                Folate = SelectedRecipe.Folate * factor,
                IronFe = SelectedRecipe.IronFe * factor,
                MagnesiumMg = SelectedRecipe.MagnesiumMg * factor,
                VitaminC = SelectedRecipe.VitaminC * factor,
                Caffeine = SelectedRecipe.Caffeine * factor,
                Cholesterol = SelectedRecipe.Cholesterol * factor,
                Alcohol = SelectedRecipe.Alcohol * factor
            };

            var updated = await _databaseService.UpdateRecipeItemAsync(updatedRecipe);
            if (!updated)
            {
                await DisplayAlertAsync("Error", "Unable to update recipe item.", "OK");
            }

            ShowEditIngredientPanel = false;
            SelectedRecipe = null;
            await LoadRecipesAsync();
        }

        private void OnEditIngredientCancelClicked(object? sender, EventArgs e)
        {
            ShowEditIngredientPanel = false;
        }

        private void OnEditIngredientBackdropTapped(object? sender, EventArgs e)
        {
            ShowEditIngredientPanel = false;
        }

        private void OnSetNotesClicked(object? sender, EventArgs e)
        {
            _recipeNotes = string.Join(
                Environment.NewLine,
                RecipeItems.Select(recipe =>
                {
                    var rounded = (int)Math.Round(recipe.Amount, MidpointRounding.AwayFromZero);
                    return $"{rounded} g : {recipe.FoodDescription}";
                }));
        }

        private void OnEditNotesClicked(object? sender, EventArgs e)
        {
            RecipeNotesEditor.Text = _recipeNotes;
            ShowEditNotesPanel = true;
        }

        private void OnEditNotesConfirmClicked(object? sender, EventArgs e)
        {
            _recipeNotes = RecipeNotesEditor.Text?.Trim() ?? string.Empty;
            ShowEditNotesPanel = false;
        }

        private void OnEditNotesCancelClicked(object? sender, EventArgs e)
        {
            ShowEditNotesPanel = false;
        }

        private void OnEditNotesBackdropTapped(object? sender, EventArgs e)
        {
            ShowEditNotesPanel = false;
        }

        private async Task PrepareRecipeAsync()
        {
            if (_recipesPrepared)
            {
                return;
            }

            if (_editingFoodId.HasValue)
            {
                var food = await _databaseService.GetFoodByIdAsync(_editingFoodId.Value);
                if (food == null)
                {
                    await DisplayAlertAsync("Not found", "The selected recipe could not be loaded.", "OK");
                    await Shell.Current.GoToAsync("//foodSearch");
                    return;
                }

                DescriptionEntry.Text = FoodDescriptionFormatter.GetDisplayName(food.FoodDescription);
                _recipeNotes = food.Notes ?? string.Empty;

                var copied = await _databaseService.CopyRecipesForFoodAsync(_editingFoodId.Value);
                if (!copied)
                {
                    await DisplayAlertAsync("Error", "Unable to prepare recipe items for editing.", "OK");
                }

                _recipesPrepared = true;
                return;
            }

            if (_copySourceFoodId.HasValue)
            {
                var food = await _databaseService.GetFoodByIdAsync(_copySourceFoodId.Value);
                if (food == null)
                {
                    await DisplayAlertAsync("Not found", "The selected recipe could not be loaded.", "OK");
                    await Shell.Current.GoToAsync("//foodSearch");
                    return;
                }

                DescriptionEntry.Text = FoodDescriptionFormatter.GetDisplayName(food.FoodDescription);
                _recipeNotes = food.Notes ?? string.Empty;

                var duplicated = await _databaseService.DuplicateRecipesToFoodIdZeroAsync(_copySourceFoodId.Value);
                if (!duplicated)
                {
                    await DisplayAlertAsync("Error", "Unable to prepare recipe items for copying.", "OK");
                }

                _recipesPrepared = true;
            }
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            var description = DescriptionEntry.Text?.Trim() ?? string.Empty;
            var sanitizedDescription = SanitizeRecipeDescription(description);
            if (string.IsNullOrWhiteSpace(sanitizedDescription))
            {
                await DisplayAlertAsync("Missing description", "Please enter a description.", "OK");
                return;
            }

            var totalAmount = RecipeItems.Sum(item => item.Amount);
            if (totalAmount <= 0)
            {
                await DisplayAlertAsync("Missing ingredients", "Add at least one ingredient.", "OK");
                return;
            }

            var scale = 100.0 / totalAmount;
            var recipeWeightText = Math.Round(totalAmount, 0, MidpointRounding.AwayFromZero)
                .ToString("0", CultureInfo.InvariantCulture);
            var recipeDescription = $"{sanitizedDescription} {{recipe={recipeWeightText}g}}";

            var totalEnergy = RecipeItems.Sum(item => item.Energy);
            var totalProtein = RecipeItems.Sum(item => item.Protein);
            var totalFat = RecipeItems.Sum(item => item.FatTotal);
            var totalSaturated = RecipeItems.Sum(item => item.SaturatedFat);
            var totalTrans = RecipeItems.Sum(item => item.TransFat);
            var totalPoly = RecipeItems.Sum(item => item.PolyunsaturatedFat);
            var totalMono = RecipeItems.Sum(item => item.MonounsaturatedFat);
            var totalCarb = RecipeItems.Sum(item => item.Carbohydrate);
            var totalSugars = RecipeItems.Sum(item => item.Sugars);
            var totalFibre = RecipeItems.Sum(item => item.DietaryFibre);
            var totalSodium = RecipeItems.Sum(item => item.Sodium);
            var totalCalcium = RecipeItems.Sum(item => item.CalciumCa);
            var totalPotassium = RecipeItems.Sum(item => item.PotassiumK);
            var totalThiamin = RecipeItems.Sum(item => item.ThiaminB1);
            var totalRiboflavin = RecipeItems.Sum(item => item.RiboflavinB2);
            var totalNiacin = RecipeItems.Sum(item => item.NiacinB3);
            var totalFolate = RecipeItems.Sum(item => item.Folate);
            var totalIron = RecipeItems.Sum(item => item.IronFe);
            var totalMagnesium = RecipeItems.Sum(item => item.MagnesiumMg);
            var totalVitaminC = RecipeItems.Sum(item => item.VitaminC);
            var totalCaffeine = RecipeItems.Sum(item => item.Caffeine);
            var totalCholesterol = RecipeItems.Sum(item => item.Cholesterol);
            var totalAlcohol = RecipeItems.Sum(item => item.Alcohol);

            var recipeFood = new Food
            {
                FoodId = _editingFoodId ?? 0,
                FoodDescription = recipeDescription,
                Energy = totalEnergy * scale,
                Protein = totalProtein * scale,
                FatTotal = totalFat * scale,
                SaturatedFat = totalSaturated * scale,
                TransFat = totalTrans * scale,
                PolyunsaturatedFat = totalPoly * scale,
                MonounsaturatedFat = totalMono * scale,
                Carbohydrate = totalCarb * scale,
                Sugars = totalSugars * scale,
                DietaryFibre = totalFibre * scale,
                Sodium = totalSodium * scale,
                CalciumCa = totalCalcium * scale,
                PotassiumK = totalPotassium * scale,
                ThiaminB1 = totalThiamin * scale,
                RiboflavinB2 = totalRiboflavin * scale,
                NiacinB3 = totalNiacin * scale,
                Folate = totalFolate * scale,
                IronFe = totalIron * scale,
                MagnesiumMg = totalMagnesium * scale,
                VitaminC = totalVitaminC * scale,
                Caffeine = totalCaffeine * scale,
                Cholesterol = totalCholesterol * scale,
                Alcohol = totalAlcohol * scale,
                Notes = _recipeNotes
            };

            if (_editingFoodId.HasValue)
            {
                var updatedFood = await _databaseService.UpdateFoodAsync(recipeFood);
                if (!updatedFood)
                {
                    await DisplayAlertAsync("Error", "Unable to update recipe food.", "OK");
                    return;
                }

                var replaced = await _databaseService.ReplaceOriginalRecipesWithCopiesAsync(_editingFoodId.Value);
                if (!replaced)
                {
                    await DisplayAlertAsync("Error", "Unable to update recipe items.", "OK");
                    return;
                }
            }
            else
            {
                var insertedId = await _databaseService.InsertFoodReturningIdAsync(recipeFood);
                if (insertedId == null)
                {
                    await DisplayAlertAsync("Error", "Unable to save recipe to Foods table.", "OK");
                    return;
                }

                var updated = await _databaseService.UpdateRecipeFoodIdForTemporaryRecordsAsync(insertedId.Value);
                if (!updated)
                {
                    await DisplayAlertAsync("Error", "Recipe items not linked to new food.", "OK");
                    await _databaseService.DeleteRecipesWithFoodIdZeroAsync();
                }
            }

            await LoadRecipesAsync();
            var route =
                $"//foodSearch?foodInsertedDescription={Uri.EscapeDataString(recipeDescription)}";
            await Shell.Current.GoToAsync(route);
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            if (_editingFoodId.HasValue)
            {
                await _databaseService.DeleteCopiedRecipesAsync(_editingFoodId.Value);
            }
            else
            {
                await _databaseService.DeleteRecipesWithFoodIdZeroAsync();
            }
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
            HelpBodyLabel.Text = body;

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

        private (string Title, string Body) GetHelpContent()
        {
            if (_editingFoodId.HasValue)
            {
                return ("Editing Recipe",
                    "Update ingredients or notes and confirm to save changes. Use Foods Table to return without saving.");
            }

            if (_copySourceFoodId.HasValue)
            {
                return ("Copying Recipe",
                    "Review ingredients, update the description if needed, then confirm to save a new recipe.");
            }

            return ("Add Recipe",
                "Set a description, search foods to add ingredients, and confirm to save the recipe.");
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

        private static string SanitizeRecipeDescription(string description)
        {
            return FoodDescriptionFormatter.GetDisplayName(description).Trim();
        }

        private void UpdateScreenTitle()
        {
            if (_editingFoodId.HasValue)
            {
                ScreenTitle = "Editing Recipe";
            }
            else if (_copySourceFoodId.HasValue)
            {
                ScreenTitle = "Copying Recipe";
            }
            else
            {
                ScreenTitle = "Add Recipe";
            }
        }
    }
}
