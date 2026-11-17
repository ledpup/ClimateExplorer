namespace ClimateExplorer.Core.Model;

/// <summary>
/// One heating score and the range of since-1961 warming rates it covers. The rates are in °C per decade,
/// the unit the heating score info shows; <see cref="TrendSummary.Slope"/> is per year. The row for the
/// locations that have no score has a null score and no rates.
/// </summary>
/// <remarks>
/// The nullable properties aren't <c>required</c>: the API leaves nulls out of its JSON, and deserializing a row
/// that is missing a required property throws, even when the property is nullable.
/// </remarks>
public sealed record HeatingScoreRow
{
    public double? MinimumRatePerDecade { get; set; }
    public double? MaximumRatePerDecade { get; set; }
    public int? Score { get; set; }

    /// <summary>The number of locations that have this score.</summary>
    public required int LocationCount { get; set; }
}
