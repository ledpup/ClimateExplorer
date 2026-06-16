namespace ClimateExplorer.Web.Client.UiModel;

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

        var note = CreateCompletenessNote();
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
            Tone = RecentObservationTileTone.Unavailable,
            Note = note,
        };
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
}
