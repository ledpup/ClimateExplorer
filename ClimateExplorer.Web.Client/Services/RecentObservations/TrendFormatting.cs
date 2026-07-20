namespace ClimateExplorer.Web.Client.Services.RecentObservations;

using System.Globalization;
using ClimateExplorer.Core.Stats.Model;

// Shared between the tile's own trend rows (RecentObservationsCalculator) and the About-trends
// modal's full statistical breakdown, so both surfaces describe the same trend the same way.
internal static class TrendFormatting
{
    public static bool IsTrendPositive(LinearRegressionResult trend)
    {
        return trend.Significance.IsSlopeSignificant && trend.Line.Slope > 0;
    }

    public static string FormatPerDecade(LinearRegressionResult trend, string unit)
    {
        if (!trend.Significance.IsSlopeSignificant)
        {
            return "No significant trend";
        }

        var perDecade = trend.Line.Slope * 10;
        var sign = perDecade >= 0 ? "+" : string.Empty;
        return unit == "°C"
            ? $"{sign}{perDecade.ToString("0.00", CultureInfo.InvariantCulture)}°C /decade"
            : $"{sign}{perDecade.ToString("0", CultureInfo.InvariantCulture)}mm /decade";
    }

    public static string FormatValue(double value, string unit)
    {
        return unit == "°C"
            ? $"{value.ToString("0.00", CultureInfo.InvariantCulture)}°C"
            : $"{value.ToString("0", CultureInfo.InvariantCulture)}mm";
    }
}
