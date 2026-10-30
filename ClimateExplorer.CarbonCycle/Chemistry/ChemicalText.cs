namespace ClimateExplorer.CarbonCycle.Chemistry;

using System.Globalization;
using System.Text;

internal static class ChemicalText
{
    private const int SignificantFigures = 3;

    // "C8H18" → "C₈H₁₈". A decimal point is kept as is: "CH1.96" → "CH₁.₉₆".
    public static string Subscript(string formula)
    {
        var builder = new StringBuilder(formula.Length);

        foreach (var c in formula)
        {
            builder.Append(char.IsAsciiDigit(c) ? (char)('₀' + (c - '0')) : c);
        }

        return builder.ToString();
    }

    // A whole number as an integer; otherwise three significant figures in fixed notation (12.5, 1.96, 0.00000436).
    public static string FormatNumber(double value)
    {
        var rounded = Math.Round(value);

        if (Math.Abs(value - rounded) <= Tolerances.Relative * Math.Max(1.0, Math.Abs(value)))
        {
            return rounded.ToString("0", CultureInfo.InvariantCulture);
        }

        var magnitude = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        var decimals = Math.Clamp(SignificantFigures - 1 - magnitude, 1, 15);

        return Math.Round(value, decimals).ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
    }
}
