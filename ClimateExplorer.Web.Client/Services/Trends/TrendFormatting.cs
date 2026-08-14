namespace ClimateExplorer.Web.Client.Services.Trends;

using System.Globalization;
using ClimateExplorer.Core.Stats.Model;

// Shared between the Recent Observations tile's trend rows, the About-trends breakdown and the
// chart's trend module, so every surface describes the same trend the same way.
internal static class TrendFormatting
{
    // Units the site has always formatted a particular way; anything else falls back to
    // DefaultDecimalPlaces with a space before the unit, which reads better for word-ish units
    // ("420.5 ppm") than the tight form the temperature and rainfall units use ("14.00°C").
    private const int DefaultDecimalPlaces = 2;

    private static readonly Dictionary<string, int> DecimalPlacesByUnit = new(StringComparer.OrdinalIgnoreCase)
    {
        ["°C"] = 2,
        ["°C anomaly"] = 2,
        ["mm"] = 0,
        ["ppm"] = 1,
        ["ppb"] = 1,
    };

    private static readonly HashSet<string> UnitsWithoutLeadingSpace = new(StringComparer.OrdinalIgnoreCase)
    {
        "°C",
        "mm",
    };

    public static bool IsTrendPositive(PolynomialRegressionResult trend, double atX)
    {
        return trend.Significance.IsSlopeSignificant && trend.Curve.Derivative(atX) > 0;
    }

    public static string FormatPerDecade(PolynomialRegressionResult trend, double atX, string unit)
    {
        return trend.Significance.IsSlopeSignificant
            ? FormatPerDecadeValue(trend, atX, unit)
            : "No significant trend";
    }

    /// <summary>
    /// The per-decade rate regardless of significance, for use where the fitted value itself needs
    /// to be shown or discussed even though it isn't headlined as a trend.
    /// </summary>
    /// <param name="atX">
    /// Where to read the rate - the curve's instantaneous rate of change at this X
    /// (<see cref="Model.PolynomialCurve.Derivative"/>). For a degree-1 fit this is the same number
    /// everywhere (the one constant slope), so any X gives an identical result; for a quadratic/cubic
    /// fit, callers pass the window's most recent year, so "the rate" reads as "the current rate",
    /// not an average over a shape that visibly bent.
    /// </param>
    public static string FormatPerDecadeValue(PolynomialRegressionResult trend, double atX, string unit)
    {
        var perDecade = trend.Curve.Derivative(atX) * 10;
        var sign = perDecade >= 0 ? "+" : string.Empty;

        return $"{sign}{FormatNumber(perDecade, unit)}{FormatUnitSuffix(unit)} /decade";
    }

    public static string FormatValue(double value, string unit)
    {
        return $"{FormatNumber(value, unit)}{FormatUnitSuffix(unit)}";
    }

    /// <summary>
    /// The value on its own, with no unit - for places that already state the unit separately,
    /// such as the "±" halves of a "value ± standard error" pair.
    /// </summary>
    public static string FormatNumber(double value, string unit)
    {
        return value.ToString(NumberFormat(DecimalPlaces(unit)), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A per-year rate at higher precision than <see cref="FormatValue"/>, because per-year rates
    /// are typically two or three orders of magnitude smaller than the values they describe.
    /// </summary>
    public static string FormatPerYearRate(double value, string unit)
    {
        var sign = value >= 0 ? "+" : string.Empty;

        return $"{sign}{FormatPerYearMagnitude(value, unit)}";
    }

    /// <summary>
    /// The same precision as <see cref="FormatPerYearRate"/> but with no explicit "+" - for
    /// quantities that are inherently non-negative, such as a standard error.
    /// </summary>
    public static string FormatPerYearMagnitude(double value, string unit)
    {
        var text = value.ToString(NumberFormat(DecimalPlaces(unit) + 3), CultureInfo.InvariantCulture);

        return $"{text}{FormatUnitSuffix(unit)}/year";
    }

    /// <summary>
    /// A whole-number quantity of a unit, for prose like "1°C of change" or "1 ppm of change",
    /// where the decimal places <see cref="FormatValue"/> would add are just noise.
    /// </summary>
    public static string FormatUnitAmount(int amount, string unit)
    {
        return $"{amount.ToString(CultureInfo.InvariantCulture)}{FormatUnitSuffix(unit)}";
    }

    public static string FormatPValue(double pValue)
    {
        return pValue < 0.0001
            ? "< 0.0001"
            : pValue.ToString("0.0000", CultureInfo.InvariantCulture);
    }

    public static int DecimalPlaces(string unit)
    {
        return DecimalPlacesByUnit.TryGetValue(unit, out var places) ? places : DefaultDecimalPlaces;
    }

    private static string FormatUnitSuffix(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return string.Empty;
        }

        return UnitsWithoutLeadingSpace.Contains(unit) ? unit : $" {unit}";
    }

    private static string NumberFormat(int decimalPlaces)
    {
        return decimalPlaces <= 0 ? "0" : "0." + new string('0', decimalPlaces);
    }
}
