namespace ClimateExplorer.Core.Stats;

/// <summary>
/// Point-estimate real roots of a fitted polynomial - "where does the curve cross zero", the
/// question a linear fit's X-intercept answers via Fieller's theorem
/// (<see cref="PolynomialRegressionCalculator.CalculateXIntercept"/>), generalised to a quadratic or
/// cubic curve, which can cross zero more than once. Deliberately point estimates only: Fieller's
/// argument is specific to a ratio of two correlated coefficients, and doesn't extend to three or
/// four without a materially different derivation (a delta-method or bootstrap approach), which is
/// out of scope here - see the design doc.
/// </summary>
public static class PolynomialRootFinder
{
    /// <param name="coefficients">
    /// Ascending power order, as in <see cref="Model.PolynomialCurve.Coefficients"/> - <c>[c0, c1]</c>
    /// for a line, <c>[c0, c1, c2]</c> for a quadratic, <c>[c0, c1, c2, c3]</c> for a cubic.
    /// </param>
    /// <returns>Every distinct real root, ascending. Empty if the curve never crosses zero.</returns>
    public static IReadOnlyList<double> FindRealRoots(IReadOnlyList<double> coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);

        return (coefficients.Count - 1) switch
        {
            1 => FindLinearRoots(coefficients[0], coefficients[1]),
            2 => FindQuadraticRoots(coefficients[0], coefficients[1], coefficients[2]),
            3 => FindCubicRoots(coefficients[0], coefficients[1], coefficients[2], coefficients[3]),
            _ => throw new ArgumentOutOfRangeException(nameof(coefficients), "Only degree 1-3 polynomials are supported."),
        };
    }

    private static IReadOnlyList<double> FindLinearRoots(double c0, double c1)
    {
        return c1 == 0 ? [] : [-c0 / c1];
    }

    private static IReadOnlyList<double> FindQuadraticRoots(double c0, double c1, double c2)
    {
        if (c2 == 0)
        {
            return FindLinearRoots(c0, c1);
        }

        var discriminant = (c1 * c1) - (4 * c2 * c0);

        if (discriminant < 0)
        {
            return [];
        }

        if (discriminant == 0)
        {
            return [-c1 / (2 * c2)];
        }

        var sqrtDiscriminant = Math.Sqrt(discriminant);
        var root1 = (-c1 - sqrtDiscriminant) / (2 * c2);
        var root2 = (-c1 + sqrtDiscriminant) / (2 * c2);

        return Order(root1, root2);
    }

    /// <summary>
    /// Cardano's method via the depressed cubic (<c>x = t - a2/3</c>, giving <c>t³ + pt + q = 0</c>),
    /// with the standard trigonometric case for three distinct real roots.
    /// </summary>
    private static IReadOnlyList<double> FindCubicRoots(double c0, double c1, double c2, double c3)
    {
        if (c3 == 0)
        {
            return FindQuadraticRoots(c0, c1, c2);
        }

        var a2 = c2 / c3;
        var a1 = c1 / c3;
        var a0 = c0 / c3;
        var shift = a2 / 3;

        var p = a1 - (a2 * a2 / 3);
        var q = (2 * a2 * a2 * a2 / 27) - (a2 * a1 / 3) + a0;
        var discriminant = (q * q / 4) + (p * p * p / 27);

        if (discriminant > 1e-12)
        {
            // One real root.
            var sqrtDiscriminant = Math.Sqrt(discriminant);
            var u = Math.Cbrt((-q / 2) + sqrtDiscriminant);
            var v = Math.Cbrt((-q / 2) - sqrtDiscriminant);

            return [u + v - shift];
        }

        if (discriminant > -1e-12)
        {
            // A repeated root - one or two distinct real roots.
            if (Math.Abs(p) < 1e-12)
            {
                return [-shift];
            }

            var u = Math.Cbrt(-q / 2);

            return Order((2 * u) - shift, -u - shift);
        }

        // Three distinct real roots - trigonometric case.
        var r = Math.Sqrt(-p * p * p / 27);
        var phi = Math.Acos(Math.Clamp(-q / (2 * r), -1, 1));
        var magnitude = 2 * Math.Sqrt(-p / 3);

        double[] roots =
        [
            (magnitude * Math.Cos(phi / 3)) - shift,
            (magnitude * Math.Cos((phi + (2 * Math.PI)) / 3)) - shift,
            (magnitude * Math.Cos((phi + (4 * Math.PI)) / 3)) - shift,
        ];

        Array.Sort(roots);

        return roots;
    }

    private static IReadOnlyList<double> Order(double a, double b) => a <= b ? [a, b] : [b, a];
}
