namespace ClimateExplorer.Web.Client.Services.Trends;

using System.Globalization;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

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
    /// <summary>
    /// The notifications for every trend on one series, without repeats. All of a series' trends are
    /// fitted to the same points, and which windows are significant depends only on the regression
    /// type, not on the window a trend shows or its projection. So trends that share a regression type
    /// would produce the same notification, and only the first of them is reported. When too few years
    /// are plotted for any trend, that's true whatever the regression type, so it's reported once.
    /// </summary>
    /// <param name="units">The reader's unit system for the fitted rates. Defaults to metric.</param>
    public static List<UserNotification> BuildForSeries(
        string seriesTitle,
        IReadOnlyList<ChartSeriesTrend> trends,
        string? locationName,
        Guid? locationId,
        IUnitFormatter? units = null)
    {
        ArgumentNullException.ThrowIfNull(trends);

        if (trends.FirstOrDefault(x => x.UnavailableReason is not null) is { } unavailable)
        {
            return Build(seriesTitle, unavailable, locationName, locationId, units: units) is { } notification ? [notification] : [];
        }

        var byRegressionType = trends.DistinctBy(x => x.RegressionType).ToList();
        var nameRegressionType = byRegressionType.Count > 1;

        return
        [
            .. byRegressionType
                .Select(trend => Build(
                    seriesTitle,
                    trend,
                    locationName,
                    locationId,
                    nameRegressionType ? trend.RegressionType : null,
                    units))
                .OfType<UserNotification>(),
        ];
    }

    /// <param name="regressionType">
    /// Named in the message prefix (e.g. "quadratic trend") when a series has trends of more than one
    /// regression type, so their notifications don't read as duplicates of each other. Null otherwise,
    /// keeping the single-trend wording unchanged.
    /// </param>
    /// <param name="units">The reader's unit system for the fitted rates. Defaults to metric.</param>
    public static UserNotification? Build(
        string seriesTitle,
        ChartSeriesTrend trend,
        string? locationName,
        Guid? locationId,
        TrendRegressionType? regressionType = null,
        IUnitFormatter? units = null)
    {
        ArgumentNullException.ThrowIfNull(trend);

        var subjectLabel = regressionType is { } type
            ? $"{Escape(seriesTitle)}, <b>{type.ToString().ToLowerInvariant()} trend</b>"
            : Escape(seriesTitle);

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

        // The summary, each rejected window and the closing note are sibling <div>s, so each starts
        // on its own line.
        var detail = string.Concat(
            rejected.Select(window => DescribeRejectedWindow(window, trend.Subject.Unit, trend.Subject.FormatterFor(units ?? UnitFormatter.Metric))));

        var footer = $"<div>A period is only offered when its p-value is below {ThresholdText(trend)}. "
            + "Open <b>About trends</b> for the full statistics on every period.</div>";

        if (!trend.HasSignificantWindow)
        {
            return Create(
                $"<div>{subjectLabel}: none of the trend periods produce a statistically significant trend, so no trend line was added.</div>{detail}{footer}",
                NotificationType.Warning,
                locationName,
                locationId);
        }

        var subject = rejected.Count == 1
            ? "one of the trend periods isn't statistically significant, so it isn't offered as an option"
            : $"{NumberWord(rejected.Count)} of the trend periods aren't statistically significant, so they aren't offered as options";

        return Create(
            $"<div>{subjectLabel}: {subject}.</div>{detail}{footer}",
            NotificationType.Info,
            locationName,
            locationId);
    }

    private static string DescribeRejectedWindow(ChartSeriesTrendWindowResult window, UnitOfMeasure? unit, IUnitFormatter units)
    {
        var label = TrendWindowLabel.Get(window.Window);
        var years = $"{window.FirstYear.ToString(CultureInfo.InvariantCulture)}-{window.LastYear.ToString(CultureInfo.InvariantCulture)}";
        var rate = TrendFormatting.FormatPerDecadeValue(window.Regression, window.Regression.Input.MaximumX, unit, units);
        var pValue = TrendFormatting.FormatPValue(window.Regression.Significance.PValue);

        return $"<div><b>{label}</b> ({years}): the fitted rate is {rate}, but p = {pValue} - the year-to-year scatter is too large, "
            + "relative to the number of years, to tell this apart from no trend at all.</div>";
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
