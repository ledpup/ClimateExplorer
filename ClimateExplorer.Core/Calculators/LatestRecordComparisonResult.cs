namespace ClimateExplorer.Core.Calculators;

public sealed record LatestRecordComparisonResult
{
    public double Value { get; init; }
    public int HistoricalCount { get; init; }
    public int ComparableCount => HistoricalCount + 1;
    public int HighRank { get; init; }
    public int LowRank { get; init; }
    public double HighPercentile { get; init; }
    public double LowPercentile { get; init; }
    public double HistoricalAverage { get; init; }
    public double Anomaly { get; init; }
    public bool IsNewHighRecord { get; init; }
    public bool IsNewLowRecord { get; init; }
    public bool IsTiedHighRecord { get; init; }
    public bool IsTiedLowRecord { get; init; }
    public LatestRecordComparisonDirection Direction { get; init; }
}
