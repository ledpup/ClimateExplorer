namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

/// <summary>
/// What a scenario feeds into the input tabs (simulation scenarios plan §4), one member per input tab, and the setting's
/// own inputs. A tab or setting the scenario doesn't use is null. The land and waste tabs get a member each when they're
/// built.
/// </summary>
public sealed record ScenarioInputs
{
    public static ScenarioInputs None { get; } = new();

    public FossilFuelInputs? FossilFuels { get; init; }

    // A planet setting's sunlight and albedo
    public PlanetInputs? Planet { get; init; }
}
