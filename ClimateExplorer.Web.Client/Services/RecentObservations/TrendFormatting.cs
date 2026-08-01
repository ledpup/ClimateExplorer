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
        return trend.Significance.IsSlopeSignificant
            ? FormatPerDecadeValue(trend, unit)
            : "No significant trend";
    }

    // The per-decade rate regardless of significance, for use where the fitted value itself
    // needs to be shown or discussed even though it isn't headlined as a trend.
    public static string FormatPerDecadeValue(LinearRegressionResult trend, string unit)
    {
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
