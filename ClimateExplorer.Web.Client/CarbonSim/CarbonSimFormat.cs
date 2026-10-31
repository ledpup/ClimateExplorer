namespace ClimateExplorer.Web.Client.CarbonSim;

using System.Globalization;

/// <summary>Number and time text for the simulator's readouts.</summary>
public static class CarbonSimFormat
{
    private const int SignificantFigures = 3;

    // Shares below 0.1% (1,000 ppm) are given in parts per million, as trace gases usually are
    private const double PercentFrom = 0.001;

    /// <summary>
    /// At least three significant figures, with thousands separators: "2,183", "20.9", "0.000190".
    /// </summary>
    public static string Number(double value)
    {
        if (value == 0 || !double.IsFinite(value))
        {
            return "0";
        }

        return Format(value, Math.Clamp(SignificantFigures - 1 - Magnitude(value), 0, 15));
    }

    /// <summary>
    /// A share of the air: "78.1%" from 0.1% up, "420 ppm" and "1.9 ppm" below. The starting CO₂ and CH₄ are only
    /// known to two or three figures, so neither form shows more than three figures or more than one decimal place.
    /// </summary>
    public static string Share(double fraction) =>
        Math.Abs(fraction) >= PercentFrom
            ? Format(fraction * 100, 1) + "%"
            : Ppm(fraction * 1e6);

    /// <summary>A signed change in a share of the air: "-0.4 pp" (percentage points), "+35 ppm", "0".</summary>
    public static string ShareChange(double change)
    {
        string text;

        if (Math.Abs(change) >= PercentFrom)
        {
            text = Format(change * 100, 1) + " pp";
        }
        else if (Math.Abs(change * 1e6) < 0.05)
        {
            return "0";
        }
        else
        {
            text = Ppm(change * 1e6);
        }

        return (change > 0 ? "+" : string.Empty) + text;
    }

    /// <summary>"45.0 s", "12 min 05 s", "3 h 07 min", "2 d 4 h".</summary>
    public static string Duration(double seconds)
    {
        if (seconds < 60)
        {
            return seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }

        var total = (long)Math.Floor(seconds);

        return total switch
        {
            < 3600 => $"{total / 60} min {total % 60:00} s",
            < 86400 => $"{total / 3600} h {(total % 3600) / 60:00} min",
            _ => $"{total / 86400} d {(total % 86400) / 3600} h",
        };
    }

    // Whole numbers, with one decimal place under 10 ppm (CH₄'s 1.9)
    private static string Ppm(double ppm)
    {
        if (ppm == 0 || !double.IsFinite(ppm))
        {
            return "0 ppm";
        }

        return Math.Abs(ppm) < 0.05
            ? "< 0.1 ppm"
            : Format(ppm, Math.Abs(ppm) < 9.95 ? 1 : 0) + " ppm";
    }

    private static int Magnitude(double value) => (int)Math.Floor(Math.Log10(Math.Abs(value)));

    private static string Format(double value, int decimals) =>
        Math.Round(value, decimals).ToString("N" + decimals, CultureInfo.InvariantCulture);
}
