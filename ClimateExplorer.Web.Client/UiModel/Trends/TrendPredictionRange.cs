namespace ClimateExplorer.Web.Client.UiModel.Trends;

using System.Globalization;

/// <summary>
/// How far a trend may be projected past the end of the record.
/// </summary>
/// <remarks>
/// The ceiling is a century because the statistics panel already frames extrapolation at that
/// scale, and because the prediction interval widens with distance from the middle of the fitted
/// window - past a hundred years the band is wide enough that a single projected line would imply
/// precision the data cannot support. The default is deliberately short of that ceiling, so the
/// projection stays visually secondary to the record it came from.
/// </remarks>
public static class TrendPredictionRange
{
    public const int Default = 50;
    public const int Minimum = 1;
    public const int Maximum = 100;

    public static int Clamp(int years) => Math.Clamp(years, Minimum, Maximum);

    public static bool IsValid(int years) => years >= Minimum && years <= Maximum;

    /// <summary>
    /// Parses a typed "predict until" calendar year, which is valid when it lies within
    /// <see cref="Minimum"/>-<see cref="Maximum"/> years after <paramref name="anchorYear"/>.
    /// </summary>
    public static bool TryParsePredictUntilYear(string? text, int anchorYear, out int targetYear)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
            && IsValid(year - anchorYear))
        {
            targetYear = year;
            return true;
        }

        targetYear = 0;
        return false;
    }
}
