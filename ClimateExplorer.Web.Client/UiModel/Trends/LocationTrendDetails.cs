namespace ClimateExplorer.Web.Client.UiModel.Trends;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// Everything the dashboard's temperature trend modal shows: how many complete years there are, every
/// complete year (for the statistical breakdown's comparisons), and the windows a trend was fitted over,
/// in display order. <see cref="Windows"/> is empty when there are too few years for a trend.
/// </summary>
public sealed record LocationTrendDetails(
    int CompleteYearCount,
    IReadOnlyList<DataPoint> FullPeriodPoints,
    IReadOnlyList<LocationTrendWindow> Windows);
