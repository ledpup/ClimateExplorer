namespace ClimateExplorer.Core.Stats;

public sealed record RegressionInputSummary(
    int Count,
    double MinimumX,
    double MaximumX,
    double MeanX,
    double MeanY,
    double SumSquaredXDeviations,
    double SumSquaredYDeviations,
    bool HasRepeatedXValues);
