namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Builds the "Average"/"Variation" tab: how the current value compares to the historical
// average and typical (standard-deviation) spread for each metric in the domain.
public sealed partial class RecentObservationsCalculator
{
    private static IReadOnlyList<RecentObservationVariationViewModel> BuildVariationMetrics(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions)
    {
        var metrics = period.Kind == PeriodKind.Daily ? domain.DailyVariationMetrics : domain.VariationMetrics;
        var result = new List<RecentObservationVariationViewModel>();
        var currentPeriodLabel = CreateCurrentPeriodLabel(period);

        foreach (var metric in metrics)
        {
            if (period.MetricValues.TryGetValue(metric.Key, out var currentValue))
            {
                result.Add(BuildVariationMetric(metric, currentValue, distributions.GetValueOrDefault(metric.Key), currentPeriodLabel));
            }
        }

        return result;
    }

    private static RecentObservationVariationViewModel BuildVariationMetric(
        Metric metric,
        MetricObservationValue currentValue,
        HistoricalValues? distribution,
        string currentPeriodLabel)
    {
        var values = distribution?.FiniteValues ?? [];
        if (distribution is null || values.Count == 0 || !currentValue.Value.HasValue || !double.IsFinite(currentValue.Value.Value))
        {
            return new RecentObservationVariationViewModel
            {
                Label = metric.VariationLabel,
                Unit = metric.Unit,
                UnavailableReason = distribution?.UnavailableReason ?? "No comparable historical data is available for this comparison.",
                ComparablePeriodCount = distribution?.ComparablePeriodCount ?? 0,
            };
        }

        var average = values.Average();
        var standardDeviation = StandardDeviationCalculator.PopulationStandardDeviation(values);
        var score = standardDeviation is > 0d
            ? (currentValue.Value.Value - average) / standardDeviation.Value
            : (double?)null;
        var anomaly = currentValue.Value.Value - average;

        return new RecentObservationVariationViewModel
        {
            Label = metric.VariationLabel,
            HistoricalMinimum = values.Min(),
            HistoricalMaximum = values.Max(),
            HistoricalAverage = average,
            CurrentValue = currentValue.Value,
            TypicalVariation = standardDeviation,
            StandardScore = score,
            Unit = metric.Unit,
            HistoricalRangeText = $"Historical range: {metric.Format(values.Min())} to {metric.Format(values.Max())}",
            HistoricalAverageText = $"Historical average: {metric.Format(average)}",
            TypicalVariationText = standardDeviation is null ? null : $"Typical variation: ±{metric.Format(standardDeviation.Value)}",
            CurrentPeriodText = $"{currentPeriodLabel}: {metric.Format(currentValue.Value.Value)}",
            StandardScoreLabel = score.HasValue && double.IsFinite(score.Value) ? "standard score" : null,
            StandardScoreValue = score.HasValue && double.IsFinite(score.Value) ? FormatStandardScore(score.Value) : null,
            Anomaly = anomaly,
            AnomalyText = FormatAnomaly(anomaly, metric),
            AnomalyDirectionText = anomaly >= 0 ? "above average" : "below average",
            ComparablePeriodCount = distribution.ComparablePeriodCount,
        };
    }
}
