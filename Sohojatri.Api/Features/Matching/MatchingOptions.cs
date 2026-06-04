namespace Sohojatri.Api.Features.Matching;

public class MatchingOptions
{
    public const string SectionName = "Matching";
    public double CurrentLocationRadiusMeters { get; set; } = 100;
    public double DestinationRadiusMeters { get; set; } = 500;
    public int TimeWindowMinutes { get; set; } = 10;
    public int MaxGroupSize { get; set; } = 4;
    public int GroupLifetimeMinutes { get; set; } = 20;
}
