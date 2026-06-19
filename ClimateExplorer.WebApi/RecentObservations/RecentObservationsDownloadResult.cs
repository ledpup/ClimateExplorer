namespace ClimateExplorer.WebApi.RecentObservations;

/// <summary>The downloaded records for all available series from a single source.</summary>
internal sealed record RecentObservationsDownloadResult(
    RecentObservationSeriesDownload TempMax,
    RecentObservationSeriesDownload TempMin,
    RecentObservationSeriesDownload Precipitation)
{
    public bool HasAnySeries => TempMax is not null || TempMin is not null || Precipitation is not null;
}
