namespace ClimateExplorer.Core.Stats.Model;

/// <param name="SlopeStandardError">The standard error of <c>Curve.Coefficients[1]</c> specifically -
/// exactly "the slope's" standard error for a degree-1 fit, and the linear term's standard error
/// among several for a degree-2/3 fit.</param>
/// <param name="TStatistic">The t-statistic for <c>Curve.Coefficients[1]</c> against zero.</param>
/// <param name="FStatistic">
/// The overall-model F-statistic: does this fit, as a whole, explain significantly more variance
/// than a flat (constant) fit. For a degree-1 fit this is mathematically identical to the slope
/// t-test (<c>FStatistic == TStatistic²</c>) - the same question asked two equivalent ways - so
/// <see cref="IsSlopeSignificant"/> is unchanged from a pre-polynomial degree-1 result. For degree 2/3
/// it is the test that actually applies: no single coefficient's t-test can answer "is this curve
/// significant" on its own.
/// </param>
/// <param name="PValue">The p-value for <see cref="FStatistic"/>.</param>
/// <param name="DegreesOfFreedom">Residual (denominator) degrees of freedom: <c>n - degree - 1</c>.</param>
/// <param name="SlopeConfidenceInterval">The confidence interval for <c>Curve.Coefficients[1]</c> - see <see cref="SlopeStandardError"/>.</param>
/// <param name="IsSlopeSignificant">
/// Whether <see cref="PValue"/> (the overall-model F-test) is below <see cref="Alpha"/> - "is this
/// trend, as a whole, distinguishable from no trend at all".
/// </param>
public sealed record RegressionSignificance(
    double SlopeStandardError,
    double TStatistic,
    double FStatistic,
    double PValue,
    int DegreesOfFreedom,
    double Alpha,
    ConfidenceInterval SlopeConfidenceInterval,
    bool IsSlopeSignificant);
