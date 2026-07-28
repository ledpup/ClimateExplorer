namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using static ClimateExplorer.Core.Enums;

public static class ObservationDomainCatalog
{
    public const string TemperatureKey = "temperature";
    public const string PrecipitationKey = "precipitation";
    public const string Co2Key = "co2";

    public static readonly ObservationDomain Temperature = new(
        TemperatureKey,
        "Temperature",
        [DataType.TempMax, DataType.TempMin, DataType.TempMean],
        SupportsAdjustment: true,
        SupportsSeasonTiles: true);

    public static readonly ObservationDomain Precipitation = new(
        PrecipitationKey,
        "Precipitation",
        [DataType.Precipitation],
        SupportsAdjustment: false,
        SupportsSeasonTiles: true);

    public static readonly ObservationDomain Co2 = new(
        Co2Key,
        "CO₂",
        [DataType.CO2],
        SupportsAdjustment: false,
        SupportsSeasonTiles: false);

    public static readonly IReadOnlyList<ObservationDomain> DefaultLocationDomains = [Temperature, Precipitation];

    public static readonly IReadOnlyList<ObservationDomain> AtmosphereDomains = [Co2];
}
