namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.Client.Units;

// Builds the "Average"/"Variation" tab: how the current value compares to the historical
// average and typical (standard-deviation) spread for each metric in the domain.
public sealed partial class RecentObservationsCalculator
{
    private static RecentObservationVariationViewModel BuildVariationMetric(
        Metric metric,
        MetricObservationValue currentValue,
        HistoricalValues? distribution,
        string currentPeriodLabel,
        IUnitFormatter units)
    {
        var values = distribution?.FiniteValues ?? [];
        if (distribution is null || values.Count == 0 || !currentValue.Value.HasValue || !double.IsFinite(currentValue.Value.Value))
        {
            return new RecentObservationVariationViewModel
            {
                Label = metric.VariationLabel,
                Unit = units.ShortLabel(metric.Unit),
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
            Unit = units.ShortLabel(metric.Unit),
            HistoricalRangeText = $"Historical range: {metric.FormatLevel(values.Min(), units)} to {metric.FormatLevel(values.Max(), units)}",
            HistoricalAverageText = $"Historical average: {metric.FormatLevel(average, units)}",
            TypicalVariationText = standardDeviation is null ? null : $"Typical variation: ±{metric.FormatChange(standardDeviation.Value, units)}",
            CurrentPeriodText = $"{currentPeriodLabel}: {metric.FormatLevel(currentValue.Value.Value, units)}",
            StandardScoreLabel = score.HasValue && double.IsFinite(score.Value) ? "standard score" : null,
            StandardScoreValue = score.HasValue && double.IsFinite(score.Value) ? FormatStandardScore(score.Value) : null,
            Anomaly = anomaly,
            AnomalyText = FormatAnomaly(anomaly, metric, units),
            AnomalyDirectionText = anomaly >= 0 ? "above average" : "below average",
            ComparablePeriodCount = distribution.ComparablePeriodCount,
        };
    }

    private IReadOnlyList<RecentObservationVariationViewModel> BuildVariationMetrics(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions,
        IUnitFormatter units)
    {
        var metrics = period.Kind == RecentObservationPeriodKind.Daily ? domain.DailyVariationMetrics : domain.VariationMetrics;
        var result = new List<RecentObservationVariationViewModel>();
        var currentPeriodLabel = CreateCurrentPeriodLabel(period);

        foreach (var metric in metrics)
        {
            if (period.MetricValues.TryGetValue(metric.Key, out var currentValue))
            {
                result.Add(BuildVariationMetric(metric, currentValue, distributions.GetValueOrDefault(metric.Key), currentPeriodLabel, units));
            }
        }

        return result;
    }
}