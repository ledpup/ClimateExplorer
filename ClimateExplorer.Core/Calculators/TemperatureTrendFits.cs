namespace ClimateExplorer.Core.Calculators;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// The full-fidelity trend fits behind a location's <see cref="Model.TrendSummary"/>s, and behind the
/// dashboard's temperature trend modal.
/// </summary>
/// <param name="FullPeriod">The fit over every complete year.</param>
/// <param name="Last30Years">The fit over the last 30 complete years; null below 60 complete years.</param>
/// <param name="FirstHalf">
/// The fit over the early period (the first half of the complete years); null below 60 complete years.
/// Not summarised onto <see cref="Model.Location"/> - only the modal shows it.
/// </param>
/// <param name="OrderedPoints">Every complete year, in year order - the source of each window's points.</param>
public sealed record TemperatureTrendFits(
    PolynomialRegressionResult FullPeriod,
    PolynomialRegressionResult? Last30Years,
    PolynomialRegressionResult? FirstHalf,
    IReadOnlyList<DataPoint> OrderedPoints);
