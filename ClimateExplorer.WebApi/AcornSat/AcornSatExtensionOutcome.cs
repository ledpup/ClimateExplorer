#nullable enable
namespace ClimateExplorer.WebApi.AcornSat;

using System;
using ClimateExplorer.Data.Downloading.Extenders;

/// <summary>
/// The result of <see cref="AcornSatClimateRecordService.ResolveAsync"/>: the extension decision (and overlay,
/// when eligible) plus the retrieval time to surface on the composed response, matching how
/// <c>DataSetEndpoints.PostDataSets</c> treats a null retrieval time as "not newly retrieved".
/// </summary>
internal sealed record AcornSatExtensionOutcome(AcornSatRecordExtensionResult Extension, DateTimeOffset? RetrievedDate)
{
    public static AcornSatExtensionOutcome NotEligible(AcornSatExtensionDecision decision, AcornSatStationResolution stations)
    {
        return new AcornSatExtensionOutcome(
            new AcornSatRecordExtensionResult
            {
                Decision = decision,
                AdjustedStationId = stations.AdjustedStationId,
                OpenCdoStationId = stations.OpenCdoStationId,
            },
            null);
    }

    public static AcornSatExtensionOutcome FromCacheEntry(AcornSatExtensionCacheEntry entry)
    {
        return new AcornSatExtensionOutcome(
            new AcornSatRecordExtensionResult
            {
                Decision = entry.Decision,
                ComparisonYear = entry.ComparisonYear,
                LatestAcornSatDate = entry.LatestAcornSatDate,
                ComparisonSignature = entry.ComparisonSignature,
                AdjustedStationId = entry.AdjustedStationId,
                OpenCdoStationId = entry.OpenCdoStationId,
                OverlayRecords = entry.OverlayRecords,
            },
            entry.RetrievedDate);
    }
}
