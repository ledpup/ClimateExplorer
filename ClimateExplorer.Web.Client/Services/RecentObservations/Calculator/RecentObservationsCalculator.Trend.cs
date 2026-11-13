namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

// Builds the "Trend" tab: the last-30-years, since-1961, first-half and full-period linear
// trends for each metric, sharing TrendFormatting with the About-trends modal so both describe
// a trend the same way. The tile shows the first three; About trends shows all four.
public sealed partial class RecentObservationsCalculator
{
    private static IReadOnlyList<RecentObservationTrendViewModel> BuildTrendMetrics(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions,
        IUnitFormatter units)
    {
        var metrics = period.Kind == RecentObservationPeriodKind.Daily ? domain.DailyVariationMetrics : domain.VariationMetrics;
        var result = new List<RecentObservationTrendViewModel>();

        foreach (var metric in metrics)
        {
            if (period.MetricValues.TryGetValue(metric.Key, out var currentValue))
            {
                result.Add(BuildTrendMetric(metric, currentValue, period.StartDate.Year, distributions.GetValueOrDefault(metric.Key), units));
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
        HistoricalValues? distribution,
        IUnitFormatter units)
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
                    + $"Recent Observations trends use the same {AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly}-year minimum "
                    + "as the warming anomaly, so they're comparable with it.",
            };
        }

        var ordered = points.OrderBy(x => x.X).ToList();
        var recentCount = Math.Min(RecentTrendWindowYears, ordered.Count);
        var firstHalfCount = ordered.Count / 2;
        var recentPoints = ordered.TakeLast(recentCount).ToList();
        var firstHalfPoints = ordered.Take(firstHalfCount).ToList();
        var since1961Trend = trendSet.Since1961Trend;
        var since1961Points = since1961Trend is null ? [] : TrendWindows.GetSince1961Points(ordered) ?? [];

        return new RecentObservationTrendViewModel
        {
            Label = metric.VariationLabel,
            Unit = metric.Unit,
            CompleteYearCount = trendSet.CompletePointCount,
            RecentTrendYearRange = FormatYearRange(trendSet.RecentTrend),
            RecentTrendValueText = TrendFormatting.FormatPerDecade(trendSet.RecentTrend, trendSet.RecentTrend.Input.MaximumX, metric.Unit, units),
            IsRecentTrendPositive = TrendFormatting.IsTrendPositive(trendSet.RecentTrend, trendSet.RecentTrend.Input.MaximumX),
            RecentTrendTooltip = BuildTrendTooltip(recentPoints, trendSet.RecentTrend, metric.Unit, units),
            Since1961TrendYearRange = since1961Trend is null ? null : FormatYearRange(since1961Trend),
            Since1961TrendValueText = since1961Trend is null ? null : TrendFormatting.FormatPerDecade(since1961Trend, since1961Trend.Input.MaximumX, metric.Unit, units),
            IsSince1961TrendPositive = since1961Trend is not null && TrendFormatting.IsTrendPositive(since1961Trend, since1961Trend.Input.MaximumX),
            Since1961TrendTooltip = since1961Trend is null ? null : BuildTrendTooltip(since1961Points, since1961Trend, metric.Unit, units),
            FirstHalfTrendYearRange = FormatYearRange(trendSet.FirstHalfTrend),
            FirstHalfTrendValueText = TrendFormatting.FormatPerDecade(trendSet.FirstHalfTrend, trendSet.FirstHalfTrend.Input.MaximumX, metric.Unit, units),
            IsFirstHalfTrendPositive = TrendFormatting.IsTrendPositive(trendSet.FirstHalfTrend, trendSet.FirstHalfTrend.Input.MaximumX),
            FirstHalfTrendTooltip = BuildTrendTooltip(firstHalfPoints, trendSet.FirstHalfTrend, metric.Unit, units),
            FullPeriodTrend = trendSet.HistoricalTrend,
            RecentTrend = trendSet.RecentTrend,
            Since1961Trend = since1961Trend,
            FirstHalfTrend = trendSet.FirstHalfTrend,
            FullPeriodPoints = ordered,
            RecentTrendPoints = recentPoints,
            Since1961TrendPoints = since1961Points,
            FirstHalfTrendPoints = firstHalfPoints,
        };
    }

    private static string FormatYearRange(PolynomialRegressionResult trend)
    {
        return $"{trend.Input.MinimumX.ToString("0", CultureInfo.InvariantCulture)}-{trend.Input.MaximumX.ToString("0", CultureInfo.InvariantCulture)}";
    }

    private static string BuildTrendTooltip(IEnumerable<DataPoint> segmentPoints, PolynomialRegressionResult trend, UnitOfMeasure unit, IUnitFormatter units)
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
            notSignificantText = $"The fitted rate is {TrendFormatting.FormatPerDecadeValue(trend, trend.Input.MaximumX, unit, units)}, but the year-to-year scatter is too "
                                + "large relative to the number of years for this to be distinguished from no trend at all.";
        }

        return $"<p>{statsText}</p>"
                + (string.IsNullOrEmpty(notSignificantText) ? string.Empty : $"<p>{notSignificantText}</p>")
                + $"<p>Years {minYear}-{maxYear} ({years.Count} of {yearSpan}).</p>"
                + (string.IsNullOrEmpty(missingText) ? string.Empty : $"<p>{missingText}</p>");
    }
}
