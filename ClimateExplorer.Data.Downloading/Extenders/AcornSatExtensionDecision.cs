namespace ClimateExplorer.Data.Downloading.Extenders;

public enum AcornSatExtensionDecision
{
    /// <summary>ACORN-SAT and CDO agreed over the comparison year; the overlay may be appended.</summary>
    Eligible,

    /// <summary>At least one comparison-year pair differed by more than the tolerance.</summary>
    AdjustmentsDetected,

    /// <summary>ACORN-SAT already has a date in the request year, or has no complete prior year at all.</summary>
    AcornNotThroughPreviousYear,

    /// <summary>The comparison year had no usable overlap: an empty, all-null, or one-sided date set.</summary>
    InsufficientComparisonData,

    /// <summary>The location has no single open, non-blank CDO station to compare against.</summary>
    NoOpenCdoStation,

    /// <summary>ACORN-SAT or CDO input could not be read at all.</summary>
    SourceUnavailable,
}
