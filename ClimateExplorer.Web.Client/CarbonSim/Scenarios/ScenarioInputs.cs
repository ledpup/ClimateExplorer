namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

/// <summary>
/// What a scenario feeds into the input tabs (simulation plan §2.4), one member per tab. A tab the scenario doesn't
/// use is null. The land and waste tabs get a member each when they're built.
/// </summary>
public sealed record ScenarioInputs
{
    public static ScenarioInputs None { get; } = new();

    public FossilFuelInputs? FossilFuels { get; init; }
}
