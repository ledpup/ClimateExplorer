namespace ClimateExplorer.WebApi;

using System;

internal static class ClimateExplorerApiConstants
{
    internal static string HeatingScoreTable => "HeatingScoreTable";

    internal static string NearbyLocations => "NearbyLocations";

    internal static string RecentObservationsCacheKeyPrefix => "RecentObservations";

    internal static float DefaultCupDataProportion => 0.7f;

    internal static int DefaultCupSize => 14;

    internal static Guid BomDataSetDefinitionId { get; } = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E");

    internal static Guid GhcndTemperatureDataSetDefinitionId { get; } = Guid.Parse("87C65C34-C689-4BA1-8061-626E4A63D401");

    internal static Guid GhcndPrecipitationDataSetDefinitionId { get; } = Guid.Parse("5BBEAF4C-B459-410E-9B77-470905CB1E46");
}
