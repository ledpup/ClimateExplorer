namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

/// <summary>What a scenario puts in the fossil fuels tab. So far the only fuel is the sealed house's methane burner.</summary>
public sealed record FossilFuelInputs
{
    // m³/s of methane at 0 °C and 101.325 kPa, as SealedHouseSettings.BurnerRate
    public double MethaneBurnerRate { get; init; }
}
