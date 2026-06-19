namespace ClimateExplorer.Core.Model;

public sealed record RecentObservationsResponse
{
    /// <summary>
    /// True when the location is supported for recent-observation temperature,
    /// i.e. both <see cref="TempMax"/> and <see cref="TempMin"/> are available.
    /// </summary>
    public bool IsSupported { get; set; }

    public DateTimeOffset? RetrievedDate { get; set; }

    public RecentObservationSeries? TempMax { get; set; }
    public RecentObservationSeries? TempMin { get; set; }
    public RecentObservationSeries? Precipitation { get; set; }
}
