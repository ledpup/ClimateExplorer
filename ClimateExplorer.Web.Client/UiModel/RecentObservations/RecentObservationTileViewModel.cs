namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using ClimateExplorer.Core.Calculators;

public sealed record RecentObservationTileViewModel
{
    public RecentObservationPeriodKind PeriodKind { get; init; }
    public int? PeriodOffset { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string PeriodTitle { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string PercentileSentence { get; init; } = string.Empty;
    public string PrimaryLabel { get; init; } = string.Empty;
    public string PrimaryValue { get; init; } = string.Empty;
    public RecentObservationRecordStatus PrimaryRecordStatus { get; init; }
    public string? PrimaryRecordStatusText { get; init; }
    public string? HistoricalMaxLabel { get; init; }
    public string? HistoricalMaxValue { get; init; }
    public string? HistoricalMaxOccurred { get; init; }
    public string? HistoricalMinLabel { get; init; }
    public string? HistoricalMinValue { get; init; }
    public string? HistoricalMinOccurred { get; init; }
    public RecentObservationTileTone Tone { get; init; }
    public bool HasComparison { get; init; }
    public string? Note { get; init; }
    public List<RecentObservationStatViewModel> Stats { get; init; } = [];
    public List<RecentObservationStatViewModel> SupportingStats { get; init; } = [];
    public IReadOnlyList<RecentObservationMetricGroupViewModel> MetricGroups { get; init; } = [];
    public IReadOnlyList<RecentObservationExpandedTabViewModel> ExpandedTabs { get; init; } = [];
    public int ComparablePeriodCount { get; init; }
    public bool CanShowHistoricalRecord { get; init; }
    public bool CanShowHistoricalRange { get; init; }
    public bool CanShowRank { get; init; }
    public bool CanShowPercentile { get; init; }
    public int AvailableObservationCount { get; init; } = 1;
    public int ExpectedObservationCount { get; init; } = 1;

    public float Completeness => ExpectedObservationCount <= 0
        ? 1f
        : Math.Clamp((float)AvailableObservationCount / ExpectedObservationCount, 0f, 1f);

    public IReadOnlyList<RecentObservationExpandedTabViewModel> AvailableExpandedTabs =>
        ExpandedTabs.Count > 0 ? ExpandedTabs : MetricGroups;

    public RecentObservationTileViewModel ApplyCompletenessThreshold(float completenessThreshold)
    {
        var threshold = Math.Clamp(completenessThreshold, 0f, 1f);
        if (!IsBelowCompletenessThreshold(threshold))
        {
            return this;
        }

        var note = CreateThresholdNote();
        if (!HasComparison)
        {
            return this with { Note = note };
        }

        return this with
        {
            Headline = "Comparison unavailable",
            PercentileSentence = "Recent observations are below the completeness threshold.",
            HistoricalMaxLabel = null,
            HistoricalMaxValue = null,
            HistoricalMaxOccurred = null,
            HistoricalMinLabel = null,
            HistoricalMinValue = null,
            HistoricalMinOccurred = null,
            HasComparison = false,
            CanShowHistoricalRecord = false,
            CanShowHistoricalRange = false,
            CanShowRank = false,
            CanShowPercentile = false,
            PrimaryRecordStatus = RecentObservationRecordStatus.None,
            PrimaryRecordStatusText = null,
            Tone = RecentObservationTileTone.Unavailable,
            Note = note,
            Stats = StripRecordStatus(Stats),
            SupportingStats = StripRecordStatus(SupportingStats),
            MetricGroups = StripComparisons(MetricGroups),
            ExpandedTabs = StripComparisons(ExpandedTabs),
        };
    }

    private static List<RecentObservationStatViewModel> StripRecordStatus(
        IReadOnlyList<RecentObservationStatViewModel> stats)
    {
        return [.. stats.Select(stat => stat with
        {
            RecordStatus = RecentObservationRecordStatus.None,
            RecordStatusText = null,
        })];
    }

    private static IReadOnlyList<RecentObservationMetricGroupViewModel> StripComparisons(
        IReadOnlyList<RecentObservationMetricGroupViewModel> groups)
    {
        return [.. groups.Select(group => group with
        {
            Metrics = StripRecordMetrics(group.Metrics),
        })];
    }

    private static IReadOnlyList<RecentObservationExpandedTabViewModel> StripComparisons(
        IReadOnlyList<RecentObservationExpandedTabViewModel> tabs)
    {
        return [.. tabs.Select(tab => tab switch
        {
            RecentObservationRecordsTabViewModel records => records with { Metrics = StripRecordMetrics(records.Metrics) },
            RecentObservationVariationTabViewModel variation => variation with { Metrics = StripVariationMetrics(variation.Metrics) },
            RecentObservationTrendTabViewModel trend => trend with { Metrics = StripTrendMetrics(trend.Metrics) },
            _ => tab,
        })];
    }

    private static IReadOnlyList<RecentObservationRecordsViewModel> StripRecordMetrics(
        IReadOnlyList<RecentObservationRecordsViewModel> metrics)
    {
        return [.. metrics.Select(metric => metric with
        {
            RankText = null,
            RecordStatus = Core.Calculators.RecentObservationRecordStatus.None,
            RecordStatusText = null,
            RecordHigh = null,
            RecordLow = null,
            CanShowHistoricalRecord = false,
            CanShowHistoricalRange = false,
            CanShowRank = false,
            CanShowPercentile = false,
        })];
    }

    private static IReadOnlyList<RecentObservationVariationViewModel> StripVariationMetrics(
        IReadOnlyList<RecentObservationVariationViewModel> metrics)
    {
        return [.. metrics.Select(metric => metric with
        {
            HistoricalMinimum = null,
            HistoricalMaximum = null,
            HistoricalAverage = null,
            CurrentValue = null,
            TypicalVariation = null,
            StandardScore = null,
            HistoricalRangeText = null,
            HistoricalAverageText = null,
            TypicalVariationText = null,
            CurrentPeriodText = null,
            StandardScoreLabel = null,
            StandardScoreValue = null,
            UnavailableReason = "Recent observations are below the completeness threshold.",
        })];
    }

    private static IReadOnlyList<RecentObservationTrendViewModel> StripTrendMetrics(
        IReadOnlyList<RecentObservationTrendViewModel> metrics)
    {
        return [.. metrics.Select(metric => metric with
        {
            HeadlineText = null,
            HeadlineCaption = null,
            FullPeriodTooltip = null,
            RecentTrendYearRange = null,
            RecentTrendValueText = null,
            RecentTrendTooltip = null,
            FirstHalfTrendYearRange = null,
            FirstHalfTrendValueText = null,
            FirstHalfTrendTooltip = null,
            UnavailableReason = "Recent observations are below the completeness threshold.",
        })];
    }

    private bool IsBelowCompletenessThreshold(float threshold)
    {
        return ExpectedObservationCount > 0 &&
               AvailableObservationCount < ExpectedObservationCount &&
               Completeness < threshold;
    }

    private string CreateCompletenessNote()
    {
        var percentage = RecentObservationCompletenessThreshold.ToPercentage(Completeness);
        return $"Only {AvailableObservationCount} of {ExpectedObservationCount} days are available (only {percentage}% completeness).";
    }

    private string CreateThresholdNote()
    {
        var completenessNote = CreateCompletenessNote();
        if (string.IsNullOrWhiteSpace(Note) || Note.Contains(completenessNote, StringComparison.Ordinal))
        {
            return string.IsNullOrWhiteSpace(Note) ? completenessNote : Note;
        }

        return $"{Note} {completenessNote}";
    }
}
