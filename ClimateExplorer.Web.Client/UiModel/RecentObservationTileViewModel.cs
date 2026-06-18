namespace ClimateExplorer.Web.Client.UiModel;

public sealed record RecentObservationTileViewModel
{
    public RecentObservationPeriodKind PeriodKind { get; init; }
    public int? PeriodOffset { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string PeriodTitle { get; init; } = string.Empty;
    public string MetricGroupLabel { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string PercentileSentence { get; init; } = string.Empty;
    public string PrimaryLabel { get; init; } = string.Empty;
    public string PrimaryValue { get; init; } = string.Empty;
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
    public IReadOnlyList<RecentObservationMetricGroupViewModel> MetricGroups { get; init; } = [];
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
            PercentileSentence = "Recent observations are below the Completeness Threshold for this period.",
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
            Tone = RecentObservationTileTone.Unavailable,
            Note = note,
            MetricGroups = StripComparisons(MetricGroups),
        };
    }

    private static IReadOnlyList<RecentObservationMetricGroupViewModel> StripComparisons(
        IReadOnlyList<RecentObservationMetricGroupViewModel> groups)
    {
        return [.. groups.Select(group => group with
        {
            Metrics = [.. group.Metrics.Select(metric => metric with
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
            })],
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
