namespace ClimateExplorer.Core.Calculators;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// The full-fidelity trend fits behind a location's <see cref="Model.TrendSummary"/>s.
/// </summary>
/// <param name="FullPeriod">The fit over every complete year.</param>
/// <param name="Last30Years">The fit over the last 30 complete years; null below 60 complete years.</param>
/// <param name="OrderedPoints">Every complete year, in year order - the source of each window's points.</param>
public sealed record TemperatureTrendFits(
    PolynomialRegressionResult FullPeriod,
    PolynomialRegressionResult? Last30Years,
    IReadOnlyList<DataPoint> OrderedPoints);
