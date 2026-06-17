namespace ClimateExplorer.Core.Calculators;

/// <summary>
/// Indicates which extreme of a metric's historical distribution is treated as
/// "the record" for record detection and ranking.
/// </summary>
public enum RecentObservationRecordDirection
{
    /// <summary>The highest historical value is the record (e.g. highest daily maximum).</summary>
    High,

    /// <summary>The lowest historical value is the record (e.g. lowest daily minimum).</summary>
    Low,
}
