namespace ClimateExplorer.Core.Stats.Model;

/// <param name="Since1961Trend">
/// The fit over the points from 1961; null when that window isn't eligible (see
/// <see cref="TrendWindows.GetSince1961Points"/>).
/// </param>
public sealed record TrendWindowSet(
    PolynomialRegressionResult HistoricalTrend,
    PolynomialRegressionResult RecentTrend,
    PolynomialRegressionResult FirstHalfTrend,
    PolynomialRegressionResult? Since1961Trend,
    int CompletePointCount);
