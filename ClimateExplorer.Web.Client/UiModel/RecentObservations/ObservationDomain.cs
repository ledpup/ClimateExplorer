namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using static ClimateExplorer.Core.Enums;

public sealed record ObservationDomain(
    string Key,
    string TabLabel,
    IReadOnlyList<DataType> DataTypeRequests,
    bool SupportsAdjustment,
    bool SupportsSeasonTiles);
