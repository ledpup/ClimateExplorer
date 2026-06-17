namespace ClimateExplorer.Core.Calculators;

/// <summary>
/// How a current-period value compares to the historical record in the metric's
/// record direction.
/// </summary>
public enum RecentObservationRecordStatus
{
    /// <summary>No historical comparison was available.</summary>
    None,

    /// <summary>The current value sets a new record.</summary>
    NewRecord,

    /// <summary>The current value equals the existing record.</summary>
    EqualRecord,

    /// <summary>The current value has not reached the record.</summary>
    BelowRecord,
}
