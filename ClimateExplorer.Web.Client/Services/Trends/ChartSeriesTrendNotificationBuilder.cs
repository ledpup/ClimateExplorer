namespace ClimateExplorer.Web.Client.Services.Trends;

using System.Globalization;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// Explains, in one notification per series, why any trend period is missing from the trend
/// dropdown.
/// </summary>
/// <remarks>
/// Every window is fitted in a single pass, so this is where that pass reports itself: a period
/// that isn't statistically significant is not offered as an option, and this is the only place the
/// user is told why. One notification per series rather than one per window, because the results
/// are produced together and read together.
/// </remarks>
public static class ChartSeriesTrendNotificationBuilder
{
    /// <param name="trendOrdinal">
    /// The trend's 1-based position on its series (e.g. 2 for the second of three), or null when
    /// the series has only one trend. Included in the message prefix only when a series has more
    /// than one trend - so three notifications about one series don't read as duplicates of each
    /// other - and omitted otherwise, keeping the single-trend wording byte-identical to before
    /// multiple trends per series existed.
    /// </param>
    public static UserNotification? Build(
        string seriesTitle,
        ChartSeriesTrend trend,
        string? locationName,
        Guid? locationId,
        int? trendOrdinal = null)
    {
        ArgumentNullException.ThrowIfNull(trend);

        var subjectLabel = trendOrdinal is { } ordinal ? $"{Escape(seriesTitle)}, trend {ordinal}" : Escape(seriesTitle);

        if (trend.UnavailableReason is { } unavailableReason)
        {
            return Create(
                $"{subjectLabel}: no trend could be fitted. {unavailableReason}",
                NotificationType.Warning,
                locationName,
                locationId);
        }

        var rejected = trend.Windows.Where(x => !x.IsSignificant).ToList();

        if (rejected.Count == 0)
        {
            return null;
        }

        var detail = string.Join(
            " ",
            rejected.Select(window => DescribeRejectedWindow(window, trend.Subject.Unit)));

        var threshold = ThresholdText(trend);

        if (!trend.HasSignificantWindow)
        {
            return Create(
                $"{subjectLabel}: none of the trend periods produce a statistically significant trend, so no trend line was added. "
                + $"{detail} A period is only offered when its p-value is below {threshold}. "
                + "Open <b>About trends</b> for the full statistics on every period.",
                NotificationType.Warning,
                locationName,
                locationId);
        }

        var subject = rejected.Count == 1
            ? "one of the trend periods isn't statistically significant, so it isn't offered as an option"
            : $"{NumberWord(rejected.Count)} of the trend periods aren't statistically significant, so they aren't offered as options";

        return Create(
            $"{subjectLabel}: {subject}. {detail} A period is only offered when its p-value is below {threshold}. "
            + "Open <b>About trends</b> for the full statistics on every period.",
            NotificationType.Info,
            locationName,
            locationId);
    }

    private static string DescribeRejectedWindow(ChartSeriesTrendWindowResult window, string unit)
    {
        var label = TrendWindowLabel.Get(window.Window);
        var years = $"{window.FirstYear.ToString(CultureInfo.InvariantCulture)}-{window.LastYear.ToString(CultureInfo.InvariantCulture)}";
        var rate = TrendFormatting.FormatPerDecadeValue(window.Regression, window.Regression.Input.MaximumX, unit);
        var pValue = TrendFormatting.FormatPValue(window.Regression.Significance.PValue);

        return $"<b>{label}</b> ({years}): the fitted rate is {rate}, but p = {pValue} - the year-to-year scatter is too large, "
            + "relative to the number of years, to tell this apart from no trend at all.";
    }

    private static string ThresholdText(ChartSeriesTrend trend)
    {
        var alpha = trend.Windows.Count > 0
            ? trend.Windows[0].Regression.Significance.Alpha
            : 0.05;

        return alpha.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string NumberWord(int count)
    {
        return count switch
        {
            2 => "Two",
            3 => "Three",
            _ => count.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static UserNotification Create(string message, NotificationType type, string? locationName, Guid? locationId)
    {
        return new UserNotification
        {
            Message = message,
            Type = type,
            LocationName = locationName,
            LocationId = locationId,
        };
    }

    private static string Escape(string value)
    {
        return value.Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
