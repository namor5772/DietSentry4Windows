using System.Text.RegularExpressions;

namespace DietSentry
{
    public static class FoodDescriptionFormatter
    {
        private static readonly Regex MlSuffixRegex = new("mL#?$", RegexOptions.IgnoreCase);
        private static readonly Regex TrailingMarkersRegex = new(" #$| mL#?$", RegexOptions.IgnoreCase);
        private static readonly Regex RecipeMarkerRegex = new("\\{recipe=[^}]+\\}", RegexOptions.IgnoreCase);
        private static readonly Regex TrailingRecipeStarRegex = new("\\s*\\*$");
        private static readonly Regex TrailingRecipeHashRegex = new("\\s*#\\s*$");

        public static string GetUnit(string description)
        {
            return MlSuffixRegex.IsMatch(description) ? "mL" : "g";
        }

        public static string GetDisplayName(string description)
        {
            var cleaned = TrailingMarkersRegex.Replace(description, "");
            if (!RecipeMarkerRegex.IsMatch(cleaned))
            {
                return cleaned;
            }

            var withoutRecipe = RecipeMarkerRegex.Replace(cleaned, "").TrimEnd();
            return StripTrailingRecipeSuffix(withoutRecipe);
        }

        private static string StripTrailingRecipeSuffix(string description)
        {
            return TrailingRecipeHashRegex.Replace(
                TrailingRecipeStarRegex.Replace(description, ""),
                string.Empty).TrimEnd();
        }
    }
}
