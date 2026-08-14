namespace ClimateExplorer.Core.Stats.Model;

/// <summary>
/// The fitted polynomial itself: <c>Coefficients[k]</c> is the coefficient of X^k, in the same units
/// as X (a calendar year, throughout this codebase) - so <c>Predict</c> and <c>Derivative</c> take
/// and return values in that same scale directly, with no separate "which year was this centred on"
/// bookkeeping required by the caller.
/// </summary>
/// <param name="Coefficients">
/// Ascending power order: <c>Coefficients[0]</c> is the constant term, <c>Coefficients[1]</c> the
/// coefficient of X, and so on up to <c>Coefficients[Coefficients.Count - 1]</c> for X^(Count - 1).
/// </param>
public sealed record PolynomialCurve(IReadOnlyList<double> Coefficients)
{
    /// <summary>
    /// The coefficient of X^1 - always well-defined for any degree, but only describes the *whole*
    /// curve's rate of change when <c>Coefficients.Count == 2</c> (a straight line). For a curve, use
    /// <see cref="Derivative"/> at a specific X for "the rate of change at that point" instead.
    /// </summary>
    public double Slope => Coefficients[1];

    /// <summary>The fitted value at X = 0 - well-defined for any degree.</summary>
    public double Intercept => Coefficients[0];

    /// <summary>The fitted value at <paramref name="x"/>.</summary>
    public double Predict(double x) => Evaluate(Coefficients, x);

    /// <summary>
    /// The curve's instantaneous rate of change at <paramref name="x"/> - the slope of the tangent
    /// line at that point. Equal to <see cref="Slope"/> everywhere for a straight line (degree 1);
    /// for a quadratic/cubic curve this is the honest generalisation of "the slope" to a shape that
    /// doesn't have one constant rate.
    /// </summary>
    public double Derivative(double x) => Evaluate(DerivativeCoefficients(), x);

    private static double Evaluate(IReadOnlyList<double> coefficients, double x)
    {
        // Horner's method: evaluate highest power first, folding in each lower term.
        var result = 0.0;

        for (var i = coefficients.Count - 1; i >= 0; i--)
        {
            result = (result * x) + coefficients[i];
        }

        return result;
    }

    private double[] DerivativeCoefficients()
    {
        if (Coefficients.Count == 1)
        {
            return [0];
        }

        var derivative = new double[Coefficients.Count - 1];

        for (var power = 1; power < Coefficients.Count; power++)
        {
            derivative[power - 1] = Coefficients[power] * power;
        }

        return derivative;
    }
}
