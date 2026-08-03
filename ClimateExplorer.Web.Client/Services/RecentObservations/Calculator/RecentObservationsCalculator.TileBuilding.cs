namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Assembles the top-level RecentObservationTileViewModel for a period: the headline,
// primary value, supporting max/min stats, historical range, and tone - everything above
// the expandable rankings/average/variation/trend tabs (built in MetricGroups.cs).
public sealed partial class RecentObservationsCalculator
{
    private static RecentObservationTileViewModel BuildTile(
        PeriodObservation period,
        MetricDomain domain,
        IReadOnlyDictionary<string, HistoricalValues> distributions)
    {
        var historicalValues = distributions[domain.Primary.Key];
        var primaryValue = period.MetricValues[domain.Primary.Key];
        var ranking = RecentObservationComparison.Rank(primaryValue.Value!.Value, historicalValues.Values);
        var singular = period.Completeness.AvailableObservationCount == 1;
        var stats = new List<RecentObservationStatViewModel>();
        var supportingStats = new List<RecentObservationStatViewModel>();

        foreach (var metric in domain.Supporting)
        {
            if (period.MetricValues.TryGetValue(metric.Key, out var value))
            {
                var status = GetRecordStatus(value.Value!.Value, distributions.GetValueOrDefault(metric.Key));
                supportingStats.Add(new RecentObservationStatViewModel
                {
                    Label = singular ? metric.SingularLabel : metric.PluralLabel,
                    Value = metric.Format(value.Value!.Value),
                    RecordStatus = status,
                    RecordStatusText = FormatCollapsedRecordStatus(status),
                });
            }
        }

        var primaryStatus = ranking is null
            ? RecentObservationRecordStatus.None
            : RecentObservationComparison.DetermineRecordStatus(ranking);

        if (ranking is not null)
        {
            stats.Add(new RecentObservationStatViewModel { Label = "Historical average", Value = domain.Primary.Format(ranking.HistoricalAverage) });
            stats.Add(new RecentObservationStatViewModel { Label = "Anomaly", Value = FormatAnomaly(ranking.Anomaly, domain.Primary), AnomalyValue = ranking.Anomaly });
        }

        var showHistoricalRange = ranking is not null && historicalValues.CanShowHistoricalRange;
        var historicalContext = showHistoricalRange ? CreateHistoricalContextLabel(period) : null;
        var showMin = domain.ShowHistoricalMin;

        var metricGroups = BuildMetricGroups(period, domain, distributions);
        return new RecentObservationTileViewModel
        {
            PeriodKind = ToTilePeriodKind(period.Kind),
            PeriodOffset = period.PeriodOffset,
            PeriodStartDate = period.StartDate,
            PeriodEndDate = period.EndDate,
            PeriodTitle = period.Title,
            Headline = BuildTileHeadline(period, domain, historicalValues, ranking),
            PercentileSentence = BuildPercentileSentence(period, domain, historicalValues, ranking),
            PrimaryLabel = singular ? domain.Primary.SingularLabel : domain.Primary.PluralLabel,
            PrimaryValue = domain.Primary.Format(primaryValue.Value!.Value),
            PrimaryRecordStatus = primaryStatus,
            PrimaryRecordStatusText = FormatCollapsedRecordStatus(primaryStatus),
            HistoricalMaxLabel = showHistoricalRange ? $"{domain.HistoricalMaxWord} {historicalContext}" : null,
            HistoricalMaxValue = showHistoricalRange ? domain.Primary.Format(ranking!.HistoricalMax) : null,
            HistoricalMaxOccurred = showHistoricalRange ? FormatHistoricalOccurrence(historicalValues.MaxValue) : null,
            HistoricalMinLabel = showHistoricalRange && showMin ? $"{domain.HistoricalMinWord} {historicalContext}" : null,
            HistoricalMinValue = showHistoricalRange && showMin ? domain.Primary.Format(ranking!.HistoricalMin) : null,
            HistoricalMinOccurred = showHistoricalRange && showMin ? FormatHistoricalOccurrence(historicalValues.MinValue) : null,
            HasComparison = ranking is not null,
            Tone = domain.GetTone(ranking),
            Note = CombineNotes(period.Note, BuildLimitedHistoryNote(period, historicalValues, ranking)),
            Stats = stats,
            SupportingStats = supportingStats,
            MetricGroups = metricGroups,
            ExpandedTabs = BuildExpandedTabs(period, domain, distributions, metricGroups),
            ComparablePeriodCount = historicalValues.ComparablePeriodCount,
            CanShowHistoricalRecord = historicalValues.CanShowHistoricalRecord,
            CanShowHistoricalRange = historicalValues.CanShowHistoricalRange,
            CanShowRank = historicalValues.CanShowRank,
            CanShowPercentile = historicalValues.CanShowPercentile,
            AvailableObservationCount = period.Completeness.AvailableObservationCount,
            ExpectedObservationCount = period.Completeness.ExpectedObservationCount,
        };
    }

    private static RecentObservationRecordStatus GetRecordStatus(double currentValue, HistoricalValues? distribution)
    {
        var ranking = distribution is null
            ? null
            : RecentObservationComparison.Rank(currentValue, distribution.Values);

        return ranking is null
            ? RecentObservationRecordStatus.None
            : RecentObservationComparison.DetermineRecordStatus(ranking);
    }

    private static string? FormatCollapsedRecordStatus(RecentObservationRecordStatus status)
    {
        return status switch
        {
            RecentObservationRecordStatus.NewRecord => "NEW RECORD",
            RecentObservationRecordStatus.EqualRecord => "EQUAL RECORD",
            _ => null,
        };
    }

    private static string BuildTileHeadline(
        PeriodObservation period,
        MetricDomain domain,
        HistoricalValues historicalValues,
        RecentObservationComparisonResult? ranking)
    {
        if (ranking is null)
        {
            return "Comparison unavailable";
        }

        return historicalValues.CanShowRank
            ? domain.BuildHeadline(period.ComparisonLabel, ranking)
            : BuildLimitedSampleHeadline(period, domain, ranking);
    }

    private static string BuildLimitedSampleHeadline(
        PeriodObservation period,
        MetricDomain domain,
        RecentObservationComparisonResult ranking)
    {
        var sampleLabel = CreateComparableSampleLabel(period);

        if (ranking.IsNewHighRecord)
        {
            return $"{domain.HistoricalMaxWord} of {ranking.ComparableCount} {sampleLabel}";
        }

        if (ranking.IsNewLowRecord)
        {
            return $"{domain.HistoricalMinWord} of {ranking.ComparableCount} {sampleLabel}";
        }

        if (ranking.IsTiedHighRecord && ranking.HighRank == 1)
        {
            return $"Equal {LowerFirst(domain.HistoricalMaxWord)} of {ranking.ComparableCount} {sampleLabel}";
        }

        if (ranking.IsTiedLowRecord && ranking.LowRank == 1)
        {
            return $"Equal {LowerFirst(domain.HistoricalMinWord)} of {ranking.ComparableCount} {sampleLabel}";
        }

        return "Limited historical comparison";
    }

    private static string BuildPercentileSentence(
        PeriodObservation period,
        MetricDomain domain,
        HistoricalValues historicalValues,
        RecentObservationComparisonResult? ranking)
    {
        if (ranking is null)
        {
            return historicalValues.UnavailableReason ?? "No comparable historical data is available for this comparison.";
        }

        return historicalValues.CanShowPercentile
            ? domain.BuildPercentileSentence(period.ComparisonLabelPlural, historicalValues.StartYear, ranking)
            : $"Ranking unavailable: only {FormatHistoricalSampleCount(historicalValues.ComparablePeriodCount, period)}.";
    }

    private static string? BuildLimitedHistoryNote(
        PeriodObservation period,
        HistoricalValues historicalValues,
        RecentObservationComparisonResult? ranking)
    {
        return ranking is not null && !historicalValues.CanShowRank
            ? $"Limited history: comparison based on {FormatHistoricalSampleCount(historicalValues.ComparablePeriodCount, period)}."
            : null;
    }

    private static string? CombineNotes(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second;
        }

        if (string.IsNullOrWhiteSpace(second) || first.Contains(second, StringComparison.Ordinal))
        {
            return first;
        }

        return $"{first} {second}";
    }

    private static string FormatHistoricalSampleCount(int count, PeriodObservation period)
    {
        var noun = period.ComparisonMode == PeriodComparisonMode.DailyDate ? "year" : "period";
        return $"{count} comparable {Pluralize(noun, count)}";
    }

    private static RecentObservationPeriodKind ToTilePeriodKind(PeriodKind periodKind)
    {
        return periodKind switch
        {
            PeriodKind.Daily => RecentObservationPeriodKind.Daily,
            PeriodKind.LatestSevenDays => RecentObservationPeriodKind.LatestSevenDays,
            PeriodKind.CurrentMonth => RecentObservationPeriodKind.CurrentMonth,
            PeriodKind.PreviousMonth => RecentObservationPeriodKind.PreviousMonth,
            PeriodKind.CurrentSeason => RecentObservationPeriodKind.CurrentSeason,
            PeriodKind.PreviousSeason => RecentObservationPeriodKind.PreviousSeason,
            PeriodKind.YearToDate => RecentObservationPeriodKind.YearToDate,
            PeriodKind.PreviousYear => RecentObservationPeriodKind.PreviousYear,
            _ => throw new NotImplementedException(),
        };
    }

    private static RecentObservationTileTone GetTemperatureTone(RecentObservationComparisonResult? ranking)
    {
        return ranking?.Direction switch
        {
            RecentObservationComparisonDirection.High => RecentObservationTileTone.TemperatureWarm,
            RecentObservationComparisonDirection.Low => RecentObservationTileTone.TemperatureCool,
            null => RecentObservationTileTone.Unavailable,
            _ => RecentObservationTileTone.Neutral,
        };
    }

    private static RecentObservationTileTone GetPrecipitationTone(RecentObservationComparisonResult? ranking)
    {
        return ranking?.Direction switch
        {
            RecentObservationComparisonDirection.High => RecentObservationTileTone.PrecipitationWet,
            RecentObservationComparisonDirection.Low => RecentObservationTileTone.PrecipitationDry,
            null => RecentObservationTileTone.Unavailable,
            _ => RecentObservationTileTone.Neutral,
        };
    }

    private static RecentObservationTileTone GetCo2Tone(RecentObservationComparisonResult? ranking)
    {
        return ranking is null ? RecentObservationTileTone.Unavailable : RecentObservationTileTone.Neutral;
    }
}
