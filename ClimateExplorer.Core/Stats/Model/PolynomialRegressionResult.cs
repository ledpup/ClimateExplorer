namespace ClimateExplorer.Core.Stats.Model;

/// <param name="CoefficientCovarianceMatrix">
/// The covariance matrix of <see cref="Curve"/>'s coefficients, in the same ascending-power/original-
/// X-scale basis as <see cref="PolynomialCurve.Coefficients"/> - <c>CoefficientCovarianceMatrix[i][j]</c>
/// is <c>Cov(c_i, c_j)</c>. This is precision plumbing for <see cref="PolynomialRegressionCalculator"/>'s
/// own follow-up calculations (<c>Predict</c>'s confidence/prediction intervals,
/// <c>CalculateCoefficientStatistics</c>'s per-coefficient standard errors) rather than something to
/// display directly - for a per-coefficient breakdown, use <c>CalculateCoefficientStatistics</c>.
/// </param>
public sealed record PolynomialRegressionResult(
    RegressionInputSummary Input,
    int Degree,
    PolynomialCurve Curve,
    RegressionFit Fit,
    RegressionSignificance Significance,
    IReadOnlyList<IReadOnlyList<double>> CoefficientCovarianceMatrix);
