namespace ClimateExplorer.Core.Calculators;

/// <summary>
/// Whether a current-period value sits at an extreme of its historical range. A
/// value in between (or with no comparison available) is <see cref="None"/> and is
/// shown as a rank instead.
/// </summary>
public enum RecentObservationRecordStatus
{
    /// <summary>No record: the value is between the extremes, or no comparison was available.</summary>
    None,

    /// <summary>The current value sets a new record at the high or low extreme.</summary>
    NewRecord,

    /// <summary>The current value equals the existing record (at either extreme).</summary>
    EqualRecord,
}
