namespace DietSentry
{
    public sealed class WeightEntry
    {
        public int WeightId { get; init; }
        public string DateWeight { get; init; } = string.Empty;
        public double Weight { get; init; }
        public string Comments { get; init; } = string.Empty;
    }
}
