namespace ClimateExplorer.Core.Model;

/// <summary>
/// One heating score and the range of since-1961 warming rates it covers. The rates are in °C per decade,
/// the unit the heating score info shows; <see cref="TrendSummary.Slope"/> is per year.
/// </summary>
public sealed record HeatingScoreRow
{
    public required double MinimumRatePerDecade { get; set; }
    public required double MaximumRatePerDecade { get; set; }
    public required int Score { get; set; }
}
