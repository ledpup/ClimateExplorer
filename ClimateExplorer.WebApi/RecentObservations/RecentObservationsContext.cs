namespace ClimateExplorer.WebApi.RecentObservations;

/// <summary>
/// Resolves, for a single location and source, which recent-observation series
/// are available and how to fetch each. A series is <c>null</c> when it is not
/// available for the location.
/// </summary>
internal sealed record RecentObservationsContext(
    RecentObservationStationSource Source,
    RecentObservationSeriesContext TempMax,
    RecentObservationSeriesContext TempMin,
    RecentObservationSeriesContext Precipitation)
{
    /// <summary>A location is supported for temperature only when both max and min are available.</summary>
    public bool IsTemperatureSupported => TempMax is not null && TempMin is not null;

    public bool HasAnySeries => TempMax is not null || TempMin is not null || Precipitation is not null;
}
