namespace ClimateExplorer.WebApi.RecentObservations;

using ClimateExplorer.Core.Model;

internal sealed record RecentObservationsContext(
    RecentObservationStationSource Source,
    string StationId,
    MeasurementDefinition MeasurementDefinition);
