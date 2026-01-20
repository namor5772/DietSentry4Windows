namespace DietSentry
{
    public sealed class DailyTotals
    {
        public string Date { get; init; } = string.Empty;
        public string UnitLabel { get; init; } = string.Empty;
        public double AmountEaten { get; init; }
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
        public double SodiumNa { get; init; }
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
        public string WeightDisplay { get; init; } = "NA";
        public string Comments { get; init; } = string.Empty;

        public bool HasComments => !string.IsNullOrWhiteSpace(Comments);

        public string AmountLabel => UnitLabel.ToLowerInvariant() switch
        {
            "ml" => "Amount (mL)",
            "g" => "Amount (g)",
            "mixed units" => "Amount (g or mL)",
            _ => $"Amount ({UnitLabel})"
        };
    }
}
