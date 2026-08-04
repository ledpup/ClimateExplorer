namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Builds the "Trend" tab: the full-period, recent-window and first-half linear trends for
// each metric, sharing TrendFormatting with the About-trends modal so both describe a trend
// the same way.
public sealed partial class RecentObservationsCalculator
{
    private static IReadOnlyList<RecentObservationTrendViewModel> BuildTrendMetrics(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions)
    {
        var metrics = period.Kind == PeriodKind.Daily ? domain.DailyVariationMetrics : domain.VariationMetrics;
        var result = new List<RecentObservationTrendViewModel>();

        foreach (var metric in metrics)
        {
            if (period.MetricValues.TryGetValue(metric.Key, out var currentValue))
            {
                result.Add(BuildTrendMetric(metric, currentValue, period.StartDate.Year, distributions.GetValueOrDefault(metric.Key)));
            }
        }

        return result;
    }

    private static List<DataPoint> BuildTrendPoints(HistoricalValues distribution)
    {
        return distribution.PeriodValues
            .Where(x => x.Year.HasValue && x.Value.HasValue && double.IsFinite(x.Value.Value))
            .Select(x => new DataPoint(x.Year!.Value, x.Value!.Value))
            .ToList();
    }

    private static RecentObservationTrendViewModel BuildTrendMetric(
        Metric metric,
        MetricObservationValue currentValue,
        int currentPeriodYear,
        HistoricalValues? distribution)
    {
        // The tile's own current period is sliced identically to every historical
        // comparable (GetHistoricalDailyRangeDistributions/GetHistoricalDailyDateDistributions
        // apply the same template start/end to every year), but is deliberately excluded
        // from `distribution` since that population exists to rank the current value
        // against history. A trend describes the whole time series though, so the
        // current (possibly year-to-date) value belongs back in as its most recent point.
        var points = distribution is null ? [] : BuildTrendPoints(distribution);
        if (currentValue.Value.HasValue && double.IsFinite(currentValue.Value.Value))
        {
            points.Add(new DataPoint(currentPeriodYear, currentValue.Value.Value));
        }

        var trendSet = points.Count >= AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly
            ? TrendWindowCalculator.Calculate(points, AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly, RecentTrendWindowYears)
            : null;

        if (trendSet is null)
        {
            return new RecentObservationTrendViewModel
            {
                Label = metric.VariationLabel,
                Unit = metric.Unit,
                CompleteYearCount = points.Count,
                MinimumRequiredYears = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly,
                UnavailableReason = $"Less than {AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly} complete years of data. "
                    + $"A minimum of {AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly} years is used across the site "
                    + "(for example the warming anomaly and heating score) so long-term trends aren't skewed by short records.",
            };
        }

        var ordered = points.OrderBy(x => x.X).ToList();
        var recentCount = Math.Min(RecentTrendWindowYears, ordered.Count);
        var firstHalfCount = ordered.Count / 2;
        var recentPoints = ordered.TakeLast(recentCount).ToList();
        var firstHalfPoints = ordered.Take(firstHalfCount).ToList();

        return new RecentObservationTrendViewModel
        {
            Label = metric.VariationLabel,
            Unit = metric.Unit,
            CompleteYearCount = trendSet.CompletePointCount,
            HeadlineText = TrendFormatting.FormatPerDecade(trendSet.HistoricalTrend, metric.Unit),
            IsHeadlinePositive = TrendFormatting.IsTrendPositive(trendSet.HistoricalTrend),
            HeadlineCaption = FormatYearRange(trendSet.HistoricalTrend),
            FullPeriodTooltip = BuildTrendTooltip(ordered, trendSet.HistoricalTrend, metric.Unit),
            RecentTrendYearRange = FormatYearRange(trendSet.RecentTrend),
            RecentTrendValueText = TrendFormatting.FormatPerDecade(trendSet.RecentTrend, metric.Unit),
            IsRecentTrendPositive = TrendFormatting.IsTrendPositive(trendSet.RecentTrend),
            RecentTrendTooltip = BuildTrendTooltip(recentPoints, trendSet.RecentTrend, metric.Unit),
            FirstHalfTrendYearRange = FormatYearRange(trendSet.FirstHalfTrend),
            FirstHalfTrendValueText = TrendFormatting.FormatPerDecade(trendSet.FirstHalfTrend, metric.Unit),
            IsFirstHalfTrendPositive = TrendFormatting.IsTrendPositive(trendSet.FirstHalfTrend),
            FirstHalfTrendTooltip = BuildTrendTooltip(firstHalfPoints, trendSet.FirstHalfTrend, metric.Unit),
            FullPeriodTrend = trendSet.HistoricalTrend,
            RecentTrend = trendSet.RecentTrend,
            FirstHalfTrend = trendSet.FirstHalfTrend,
            FullPeriodPoints = ordered,
            RecentTrendPoints = recentPoints,
            FirstHalfTrendPoints = firstHalfPoints,
        };
    }

    private static string FormatYearRange(LinearRegressionResult trend)
    {
        return $"{trend.Input.MinimumX.ToString("0", CultureInfo.InvariantCulture)}-{trend.Input.MaximumX.ToString("0", CultureInfo.InvariantCulture)}";
    }

    private static string BuildTrendTooltip(IEnumerable<DataPoint> segmentPoints, LinearRegressionResult trend, string unit)
    {
        var years = segmentPoints.Select(x => (int)Math.Round(x.X)).OrderBy(x => x).ToList();
        var minYear = years[0];
        var maxYear = years[^1];
        var yearSpan = maxYear - minYear + 1;
        var missingYears = Enumerable.Range(minYear, yearSpan).Except(years).ToList();

        var missingText = missingYears.Count == 0
            ? string.Empty
            : $"Missing years: {string.Join(", ", missingYears)}.";

        var pValueText = TrendFormatting.FormatPValue(trend.Significance.PValue);
        var rSquaredText = trend.Fit.RSquared.ToString("0.00", CultureInfo.InvariantCulture);

        var statsText = $"p = {pValueText}, R² = {rSquaredText}, method = ordinary least squares.";

        string? notSignificantText = null;
        if (!trend.Significance.IsSlopeSignificant)
        {
            notSignificantText = $"The fitted rate is {TrendFormatting.FormatPerDecadeValue(trend, unit)}, but the year-to-year scatter is too "
                                + "large relative to the number of years for this to be distinguished from no trend at all.";
        }

        return $"<p>{statsText}</p>"
                + (string.IsNullOrEmpty(notSignificantText) ? string.Empty : $"<p>{notSignificantText}</p>")
                + $"<p>Years {minYear}-{maxYear} ({years.Count} of {yearSpan}).</p>"
                + (string.IsNullOrEmpty(missingText) ? string.Empty : $"<p>{missingText}</p>");
    }
}
