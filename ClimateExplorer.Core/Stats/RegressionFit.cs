namespace ClimateExplorer.Core.Stats;

public sealed record RegressionFit(
    double RSquared,
    double ResidualStandardError,
    double ResidualSumOfSquares,
    double TotalSumOfSquares,
    double RegressionSumOfSquares);
