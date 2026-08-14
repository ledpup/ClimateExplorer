namespace ClimateExplorer.Core.Stats.Model;

public sealed record TrendWindowSet(
    PolynomialRegressionResult HistoricalTrend,
    PolynomialRegressionResult RecentTrend,
    PolynomialRegressionResult FirstHalfTrend,
    int CompletePointCount);
