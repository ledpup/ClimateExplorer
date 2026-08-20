namespace ClimateExplorer.Core.Stats.Model;

/// <summary>
/// The standard error, t-statistic and 95%-style confidence interval for one fitted coefficient -
/// the per-term breakdown a single "Slope"/"Y-intercept" pair can't give once a fit has more than two
/// terms (quadratic/cubic). <see cref="Power"/> identifies which term of
/// <see cref="PolynomialCurve.Coefficients"/> this describes.
/// </summary>
public sealed record CoefficientStatistics(
    int Power,
    double Value,
    double StandardError,
    double TStatistic,
    ConfidenceInterval ConfidenceInterval);
