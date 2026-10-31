namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

/// <summary>What a scenario puts in the fossil fuels tab: the sealed house's methane burner, or the dead planet's cars.</summary>
public sealed record FossilFuelInputs
{
    // m³/s of methane at 0 °C and 101.325 kPa, as SealedHouseSettings.BurnerRate
    public double MethaneBurnerRate { get; init; }

    // As DeadPlanetSettings.Cars
    public double Cars { get; init; }
}
