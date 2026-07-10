namespace ClimateExplorer.Core.Stats;

public sealed record LinearRegressionResult(
    RegressionInputSummary Input,
    RegressionLine Line,
    RegressionFit Fit,
    RegressionSignificance Significance);
