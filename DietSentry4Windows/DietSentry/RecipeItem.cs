namespace DietSentry
{
    public sealed record RecipeItem
    {
        public int RecipeId { get; init; }
        public int FoodId { get; init; }
        public int CopyFg { get; init; }
        public double Amount { get; init; }
        public string FoodDescription { get; init; } = string.Empty;
        public double Energy { get; init; }
        public double Protein { get; init; }
        public double FatTotal { get; init; }
        public double SaturatedFat { get; init; }
        public double TransFat { get; init; }
        public double PolyunsaturatedFat { get; init; }
        public double MonounsaturatedFat { get; init; }
        public double Carbohydrate { get; init; }
        public double Sugars { get; init; }
        public double DietaryFibre { get; init; }
        public double Sodium { get; init; }
        public double CalciumCa { get; init; }
        public double PotassiumK { get; init; }
        public double ThiaminB1 { get; init; }
        public double RiboflavinB2 { get; init; }
        public double NiacinB3 { get; init; }
        public double Folate { get; init; }
        public double IronFe { get; init; }
        public double MagnesiumMg { get; init; }
        public double VitaminC { get; init; }
        public double Caffeine { get; init; }
        public double Cholesterol { get; init; }
        public double Alcohol { get; init; }

        public string UnitLabel => FoodDescriptionFormatter.GetUnit(FoodDescription);
    }
}
