namespace ClimateExplorer.WebApi.RecentObservations;

using ClimateExplorer.Core.Model;

/// <summary>Everything needed to fetch one recent-observation series: the station and its measurement definition.</summary>
internal sealed record RecentObservationSeriesContext(
    string StationId,
    MeasurementDefinition MeasurementDefinition);
