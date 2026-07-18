namespace ClimateExplorer.Core.Stats.Model;

public sealed record TrendWindowSet(
    LinearRegressionResult HistoricalTrend,
    LinearRegressionResult RecentTrend,
    LinearRegressionResult FirstHalfTrend,
    int CompletePointCount);
