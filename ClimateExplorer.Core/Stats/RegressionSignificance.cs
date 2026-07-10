namespace ClimateExplorer.Core.Stats;

public sealed record RegressionSignificance(
    double SlopeStandardError,
    double TStatistic,
    double FStatistic,
    double PValue,
    int DegreesOfFreedom,
    double Alpha,
    ConfidenceInterval SlopeConfidenceInterval,
    bool IsSlopeSignificant);
