namespace ClimateExplorer.Core.Stats;

/// <summary>
/// The F-distribution's upper-tail p-value, used to test whether a fitted polynomial as a whole
/// explains significantly more variance than a flat (constant) fit - the generalisation of
/// <see cref="StudentTDistributionCalculator"/>'s two-tailed slope t-test to more than one fitted
/// coefficient. The two are the same test when there is exactly one non-intercept coefficient: an
/// F(1, df) upper-tail p-value is identical to a two-tailed t(df) p-value at
/// <c>F = t²</c> (Fisher's identity), which is why <see cref="PolynomialRegressionCalculator"/>'s
/// degree-1 output is unchanged by using this instead of the t-test directly.
/// </summary>
internal static class FDistributionCalculator
{
    public static double UpperTailPValue(double fStatistic, int numeratorDegreesOfFreedom, int denominatorDegreesOfFreedom)
    {
        if (numeratorDegreesOfFreedom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numeratorDegreesOfFreedom), "Numerator degrees of freedom must be positive.");
        }

        if (denominatorDegreesOfFreedom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominatorDegreesOfFreedom), "Denominator degrees of freedom must be positive.");
        }

        if (double.IsPositiveInfinity(fStatistic))
        {
            return 0;
        }

        if (fStatistic <= 0)
        {
            return 1;
        }

        var dfn = (double)numeratorDegreesOfFreedom;
        var dfd = (double)denominatorDegreesOfFreedom;
        var x = dfd / (dfd + (dfn * fStatistic));

        return RegularizedIncompleteBetaFunction.Calculate(x, dfd / 2.0, dfn / 2.0);
    }
}
