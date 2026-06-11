namespace ClimateExplorer.WebApi;

using System;

internal static class ClimateExplorerApiConstants
{
    internal static string HeatingScoreTable => "HeatingScoreTable";

    internal static string NearbyLocations => "NearbyLocations";

    internal static string LatestRecordsCacheKeyPrefix => "LatestRecords";

    internal static float DefaultCupDataProportion => 0.7f;

    internal static int DefaultCupSize => 14;

    internal static Guid BomDataSetDefinitionId { get; } = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E");
}
