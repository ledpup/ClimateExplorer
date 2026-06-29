namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using ClimateExplorer.Core.Calculators;

/// <summary>
/// A single statistic shown in an expanded tile: its current-period value, a single
/// rank (toward whichever end of the historical range the value is nearer, or a
/// New/Equal record badge when at an extreme), and the record high and record low
/// for the comparison date shown as plain reference context.
/// </summary>
public sealed record RecentObservationMetricViewModel
{
    public string Label { get; init; } = string.Empty;
    public string CurrentValue { get; init; } = string.Empty;

    /// <summary>
    /// The current value's single rank, e.g. "2nd highest of 102" or "3rd lowest of
    /// 102". null when no history is available, or when the value is at an extreme
    /// (a record) — in which case <see cref="RecordStatusText"/> is shown instead.
    /// </summary>
    public string? RankText { get; init; }

    public RecentObservationRecordStatus RecordStatus { get; init; }
    public string? RecordStatusText { get; init; } // "New record" | "Equal record"

    public RecentObservationMetricRecordViewModel? RecordHigh { get; init; }
    public RecentObservationMetricRecordViewModel? RecordLow { get; init; }

    public int ComparablePeriodCount { get; init; }
    public bool CanShowHistoricalRecord { get; init; }
    public bool CanShowHistoricalRange { get; init; }
    public bool CanShowRank { get; init; }
    public bool CanShowPercentile { get; init; }

    public bool HasRecords => RecordHigh is not null || RecordLow is not null;

    /// <summary>The available record ends in display order (high then low).</summary>
    public IEnumerable<RecentObservationMetricRecordViewModel> Records
    {
        get
        {
            if (RecordHigh is not null)
            {
                yield return RecordHigh;
            }

            if (RecordLow is not null)
            {
                yield return RecordLow;
            }
        }
    }
}
