namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.Client.Units;

// Builds the "Ranking"/"Daily ranking" metric groups shown collapsed on the tile, and the
// full set of expandable tabs (rankings + Average/Variation/Trend) shown when a tile expands.
public sealed partial class RecentObservationsCalculator
{
    private static IReadOnlyList<RecentObservationMetricGroupViewModel> BuildMetricGroups(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions,
        IUnitFormatter units)
    {
        var groups = new List<RecentObservationMetricGroupViewModel>();

        // A daily tile is a single day's observation - max/min/mean, not aggregates
        // across days, so it uses its own Ranking group backed by daily metrics.
        var groupDefinitions = period.Kind == RecentObservationPeriodKind.Daily ? domain.DailyGroups : domain.Groups;

        foreach (var group in groupDefinitions)
        {
            var metrics = new List<RecentObservationRankingsViewModel>();
            foreach (var metric in group.Metrics)
            {
                if (period.MetricValues.TryGetValue(metric.Key, out var value))
                {
                    metrics.Add(BuildMetric(metric, value, distributions.GetValueOrDefault(metric.Key), units));
                }
            }

            if (metrics.Count > 0)
            {
                groups.Add(new RecentObservationMetricGroupViewModel
                {
                    Key = group.Key,
                    Title = group.Title,
                    Metrics = metrics,
                });
            }
        }

        return groups;
    }

    private static RecentObservationRankingsViewModel BuildMetric(Metric metric, MetricObservationValue currentValue, HistoricalValues? distribution, IUnitFormatter units)
    {
        var ranking = distribution is null
            ? null
            : RecentObservationComparison.Rank(currentValue.Value!.Value, distribution.Values);

        if (ranking is null || distribution is null)
        {
            return new RecentObservationRankingsViewModel
            {
                Label = metric.DetailLabel,
                CurrentValue = metric.FormatLevel(currentValue.Value!.Value, units),
                CurrentValueDate = currentValue.OccurredOn,
                ComparablePeriodCount = distribution?.ComparablePeriodCount ?? 0,
                CanShowHistoricalRecord = distribution?.CanShowHistoricalRecord ?? false,
                CanShowHistoricalRange = distribution?.CanShowHistoricalRange ?? false,
                CanShowRank = distribution?.CanShowRank ?? false,
                CanShowPercentile = distribution?.CanShowPercentile ?? false,
            };
        }

        // One rank for the observed value (toward whichever end it is nearer, or a
        // New/Equal record badge at an extreme), plus the record high and record low
        // for the comparison date as plain reference context (no rank of their own).
        var status = RecentObservationComparison.DetermineRecordStatus(ranking);

        return new RecentObservationRankingsViewModel
        {
            Label = metric.DetailLabel,
            CurrentValue = metric.FormatLevel(currentValue.Value!.Value, units),
            CurrentValueDate = currentValue.OccurredOn,
            RecordStatus = status,
            RecordStatusText = FormatRecordStatus(status, ranking, distribution),
            RankText = distribution.CanShowRank ? BuildRankText(ranking, status) : null,
            RecordHigh = distribution.CanShowHistoricalRange ? BuildRecordReference(metric, "Record high", ranking.HistoricalMax, distribution.MaxValue, units) : null,
            RecordLow = distribution.CanShowHistoricalRange ? BuildRecordReference(metric, "Record low", ranking.HistoricalMin, distribution.MinValue, units) : null,
            ComparablePeriodCount = distribution.ComparablePeriodCount,
            CanShowHistoricalRecord = distribution.CanShowHistoricalRecord,
            CanShowHistoricalRange = distribution.CanShowHistoricalRange,
            CanShowRank = distribution.CanShowRank,
            CanShowPercentile = distribution.CanShowPercentile,
        };
    }

    private static string? BuildRankText(RecentObservationComparisonResult ranking, RecentObservationRecordStatus status)
    {
        // At an extreme the value is shown as a "New record" / "Equal record" badge.
        if (status != RecentObservationRecordStatus.None)
        {
            return null;
        }

        // Otherwise rank toward whichever end the value is nearer (the smaller rank).
        var high = ranking.HighRank <= ranking.LowRank;
        var rank = high ? ranking.HighRank : ranking.LowRank;
        return $"{RecentObservationComparison.FormatOrdinal(rank)} {(high ? "highest" : "lowest")} of {ranking.ComparableCount}";
    }

    private static RecentObservationMetricRecordViewModel BuildRecordReference(
        Metric metric,
        string label,
        double value,
        HistoricalPeriodValue? occurrence,
        IUnitFormatter units)
    {
        return new RecentObservationMetricRecordViewModel
        {
            Label = label,
            Value = metric.FormatLevel(value, units),
            Year = FormatHistoricalOccurrence(occurrence),
            Date = occurrence?.OccurredOn,
        };
    }

    private static string? FormatRecordStatus(
        RecentObservationRecordStatus status,
        RecentObservationComparisonResult ranking,
        HistoricalValues distribution)
    {
        if (status == RecentObservationRecordStatus.None)
        {
            return null;
        }

        if (!distribution.CanShowRank)
        {
            var direction = GetRecordDirectionWord(ranking);
            return status switch
            {
                RecentObservationRecordStatus.NewRecord => $"New {direction} of {ranking.ComparableCount}",
                RecentObservationRecordStatus.EqualRecord => $"Equal {direction} of {ranking.ComparableCount}",
                _ => null,
            };
        }

        return status switch
        {
            RecentObservationRecordStatus.NewRecord => "New record",
            RecentObservationRecordStatus.EqualRecord => "Equal record",
            _ => null,
        };
    }

    private static string GetRecordDirectionWord(RecentObservationComparisonResult ranking)
    {
        if (ranking.IsNewHighRecord || (ranking.IsTiedHighRecord && ranking.HighRank == 1))
        {
            return "high";
        }

        return "low";
    }

    private IReadOnlyList<RecentObservationExpandedTabViewModel> BuildExpandedTabs(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions,
        IReadOnlyList<RecentObservationMetricGroupViewModel> recordGroups,
        IUnitFormatter units)
    {
        var tabs = new List<RecentObservationExpandedTabViewModel>();

        foreach (var group in recordGroups)
        {
            tabs.Add(new RecentObservationRankingsTabViewModel
            {
                Key = group.Key,
                Title = group.Title,
                Metrics = group.Metrics,
            });
        }

        var variationMetrics = BuildVariationMetrics(period, domain, distributions, units);
        if (variationMetrics.Count > 0)
        {
            tabs.Add(new RecentObservationAverageTabViewModel
            {
                Key = MetricGroupKey.Average,
                Title = "Average",
                Metrics = variationMetrics,
            });

            tabs.Add(new RecentObservationVariationTabViewModel
            {
                Key = MetricGroupKey.Variation,
                Title = "Variation",
                Metrics = variationMetrics,
            });
        }

        var trendMetricKeys = period.Kind == RecentObservationPeriodKind.Daily ? domain.DailyVariationMetrics : domain.VariationMetrics;
        if (trendMetricKeys.Any(metric => period.MetricValues.ContainsKey(metric.Key)))
        {
            tabs.Add(new RecentObservationTrendTabViewModel
            {
                Key = MetricGroupKey.Trend,
                Title = "Trend",
                MetricsFactory = new Lazy<IReadOnlyList<RecentObservationTrendViewModel>>(
                    () => BuildTrendMetrics(period, domain, distributions, units)),
            });
        }

        return tabs;
    }
}