namespace ClimateExplorer.Web.Client.CarbonSim;

using System.Globalization;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>Number and time text for the simulator's readouts.</summary>
public static class CarbonSimFormat
{
    private const int SignificantFigures = 3;

    // Shares below 0.1% (1,000 ppm) are given in parts per million, as trace gases usually are
    private const double PercentFrom = 0.001;

    // From a trillion up, numbers are given as a power of ten: a planet's air is about 10²⁰ mol
    private const double PowerOfTenFrom = 1e12;

    // Julian year
    private const long SecondsPerYear = 31_557_600;

    private const string SuperscriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

    /// <summary>
    /// At least three significant figures, with thousands separators: "2,183", "20.9", "0.000190". From a trillion up,
    /// three significant figures and a power of ten: "1.78 × 10²⁰".
    /// </summary>
    public static string Number(double value)
    {
        if (value == 0 || !double.IsFinite(value))
        {
            return "0";
        }

        if (Math.Abs(value) >= PowerOfTenFrom)
        {
            return PowerOfTen(value);
        }

        return Format(value, Math.Clamp(SignificantFigures - 1 - Magnitude(value), 0, 15));
    }

    /// <summary>A measure in the reader's units, as <see cref="Number"/> gives it: "250 m³", "8,829 ft³".</summary>
    public static string Quantity(IUnitFormatter units, Measure measure) =>
        WithLabel(units, measure.Unit, Number(units.ToDisplay(measure)));

    /// <summary>
    /// A measure in the reader's units, to two significant figures and without trailing zeros, for a rough example:
    /// "0.07 L/s", "8.9 ft³/h", "13,000 ft³/h".
    /// </summary>
    public static string Approximate(IUnitFormatter units, Measure measure)
    {
        var value = units.ToDisplay(measure);

        if (value != 0 && double.IsFinite(value))
        {
            var places = 1 - Magnitude(value);
            var step = Math.Pow(10, -places);

            // Round to a whole number of steps, then to the places, so 0.07 doesn't come out as 0.07000000000000001
            value = Math.Round(Math.Round(value / step) * step, Math.Max(places, 0));
        }

        return WithLabel(units, measure.Unit, value.ToString("#,##0.###############", CultureInfo.InvariantCulture));
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

    /// <summary>"45.0 s", "12 min 05 s", "3 h 07 min", "2 d 4 h", "12 yr 45 d", "3,934 yr".</summary>
    public static string Duration(double seconds)
    {
        if (seconds < 60)
        {
            return seconds.ToString("0", CultureInfo.InvariantCulture) + " s";
        }

        var total = (long)Math.Floor(seconds);

        return total switch
        {
            < 3600 => $"{total / 60} min {total % 60:00} s",
            < 86400 => $"{total / 3600} h {(total % 3600) / 60:00} min",
            < SecondsPerYear => $"{total / 86400} d {(total % 86400) / 3600} h",
            < 100 * SecondsPerYear => $"{total / SecondsPerYear} yr {(total % SecondsPerYear) / 86400} d",
            _ => (total / SecondsPerYear).ToString("N0", CultureInfo.InvariantCulture) + " yr",
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

    private static string WithLabel(IUnitFormatter units, UnitOfMeasure unit, string number) =>
        units.IsTight(unit) ? number + units.ShortLabel(unit) : $"{number} {units.ShortLabel(unit)}";

    private static string PowerOfTen(double value)
    {
        var exponent = Magnitude(value);
        var mantissa = Math.Round(value / Math.Pow(10, exponent), SignificantFigures - 1);

        // 9.996 × 10¹² rounds up to 10.0 × 10¹²
        if (Math.Abs(mantissa) >= 10)
        {
            mantissa /= 10;
            exponent++;
        }

        var superscript = string.Concat(exponent.ToString(CultureInfo.InvariantCulture).Select(digit => SuperscriptDigits[digit - '0']));

        return $"{mantissa.ToString("F" + (SignificantFigures - 1), CultureInfo.InvariantCulture)} × 10{superscript}";
    }

    private static int Magnitude(double value) => (int)Math.Floor(Math.Log10(Math.Abs(value)));

    private static string Format(double value, int decimals) =>
        Math.Round(value, decimals).ToString("N" + decimals, CultureInfo.InvariantCulture);
}
