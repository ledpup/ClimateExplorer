namespace ClimateExplorer.Web.Client.CarbonSim;

using System.Globalization;

/// <summary>Number and time text for the simulator's readouts.</summary>
public static class CarbonSimFormat
{
    private const int SignificantFigures = 3;

    /// <summary>
    /// At least three significant figures, with thousands separators: "2,183", "20.9", "0.000190".
    /// </summary>
    public static string Number(double value)
    {
        if (value == 0 || !double.IsFinite(value))
        {
            return "0";
        }

        var magnitude = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        var decimals = Math.Clamp(SignificantFigures - 1 - magnitude, 0, 15);

        return Math.Round(value, decimals).ToString("N" + decimals, CultureInfo.InvariantCulture);
    }

    public static string Percent(double fraction) => Number(fraction * 100) + "%";

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
}
