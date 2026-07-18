namespace ClimateExplorer.Data.Downloading.Extenders;

using ClimateExplorer.Core.Interface;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The small, per location/data-type cached result of an ACORN-SAT extension decision: enough to reuse a
/// conclusive decision without repeating the comparison, and enough to detect when a new annual ACORN-SAT
/// release invalidates it. Deliberately does not cache the full ACORN-SAT series - only the append-range
/// overlay (see <see cref="AcornSatRecordExtensionResult.OverlayRecords"/>) plus the decision inputs.
/// </summary>
public sealed class AcornSatExtensionCacheEntry : ICachedData
{
    public required Guid LocationId { get; set; }

    public required DataType DataType { get; set; }

    public required string AdjustedStationId { get; set; }

    public required string OpenCdoStationId { get; set; }

    public required int ComparisonYear { get; set; }

    public required AcornSatExtensionDecision Decision { get; set; }

    public required DateOnly LatestAcornSatDate { get; set; }

    public required string ComparisonSignature { get; set; }

    public required List<DataRecord> OverlayRecords { get; set; }

    /// <summary>The successful CDO retrieval this entry's overlay/decision reflects, when known.</summary>
    public DateTimeOffset? RetrievedDate { get; set; }

    /// <summary>The latest CDO source record date at the time of that retrieval, when known.</summary>
    public DateOnly? LatestCdoSourceRecordDate { get; set; }

    /// <summary>
    /// Only <see cref="AcornSatExtensionDecision.Eligible"/> and
    /// <see cref="AcornSatExtensionDecision.AdjustmentsDetected"/> are conclusive for a matching annual
    /// ACORN-SAT comparison signature; every other decision reflects missing data or a transient failure and
    /// must be eligible for a later retry rather than being reused as-is.
    /// </summary>
    public bool IsConclusive =>
        Decision is AcornSatExtensionDecision.Eligible or AcornSatExtensionDecision.AdjustmentsDetected;
}
