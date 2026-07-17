namespace ClimateExplorer.Data.Downloading.Extenders;

using ClimateExplorer.Core.Model;

public sealed record AcornSatRecordExtensionResult
{
    public required AcornSatExtensionDecision Decision { get; init; }

    /// <summary>The latest year for which ACORN-SAT has complete 31-December coverage, when known.</summary>
    public int? ComparisonYear { get; init; }

    /// <summary>The latest date present anywhere in the supplied ACORN-SAT series, when known.</summary>
    public DateOnly? LatestAcornSatDate { get; init; }

    /// <summary>
    /// A stable signature of the ACORN-SAT eligibility inputs (comparison-year coverage and values).
    /// Null when there was not enough ACORN-SAT data to compute one.
    /// </summary>
    public string? ComparisonSignature { get; init; }

    public string? AdjustedStationId { get; init; }

    public string? OpenCdoStationId { get; init; }

    /// <summary>
    /// Non-null CDO records strictly after <see cref="LatestAcornSatDate"/> through the supplied "today",
    /// present only when <see cref="Decision"/> is <see cref="AcornSatExtensionDecision.Eligible"/>. May span
    /// more than one calendar year when ACORN-SAT is more than one year stale.
    /// </summary>
    public IReadOnlyList<DataRecord> OverlayRecords { get; init; } = [];

    public bool CdoContributed => OverlayRecords.Count > 0;
}
