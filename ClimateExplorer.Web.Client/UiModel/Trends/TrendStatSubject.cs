namespace ClimateExplorer.Web.Client.UiModel.Trends;

using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// What a trend is being described *about* - the label and unit used throughout the statistical
/// breakdown. Keeping this separate from any one view model is what lets the Recent Observations
/// tile and a chart series share the trend statistics builder rather than growing a second
/// statistics vocabulary.
/// </summary>
/// <param name="Label">What the trend is of, e.g. "Maximum temperature".</param>
/// <param name="Unit">The metric unit the trend was fitted in, or null when the values aren't a quantity
/// in any unit (e.g. a chart series transformed into a count).</param>
/// <param name="CanConvert">False when the values the trend was fitted to can't be shown in another unit
/// system (see <c>SeriesDisplayUnits.IsConvertible</c>), so the trend stays metric too.</param>
public sealed record TrendStatSubject(string Label, UnitOfMeasure? Unit, bool CanConvert = true)
{
    /// <summary>
    /// The metric unit label. The full regression report (<c>TrendStatSectionBuilder</c>) is always
    /// metric: its intercepts, X-intercepts ("reaches 0°C in ...") and confidence intervals are tied to
    /// the metric fit, and an X-intercept would move to a different year in °F.
    /// </summary>
    public string MetricUnitLabel => Unit is { } unit ? UnitFormatter.Metric.ShortLabel(unit) : string.Empty;

    /// <summary>The formatter to show this trend's rate with, given the reader's.</summary>
    public IUnitFormatter FormatterFor(IUnitFormatter units) => CanConvert ? units : UnitFormatter.Metric;
}
