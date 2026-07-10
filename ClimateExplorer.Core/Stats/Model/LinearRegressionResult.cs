namespace ClimateExplorer.Core.Stats.Model;

public sealed record LinearRegressionResult(
    RegressionInputSummary Input,
    RegressionLine Line,
    RegressionFit Fit,
    RegressionSignificance Significance);
