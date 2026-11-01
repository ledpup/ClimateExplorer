namespace ClimateExplorer.CarbonCycle.Simulation;

// The forcing by agent at one moment, and the non-radiative heat beside it, all W/m². Non-radiative heat, such as the
// engines', isn't a forcing, so it's left out of the total (energy balance plan §4).
public sealed record ForcingBreakdown(IReadOnlyList<Forcing> Agents, double NonRadiativeHeat)
{
    public double Total => Agents.Sum(forcing => forcing.Value);

    // F_GHG: the part that lowers the heat reaching space. Solar and albedo act through the absorbed sunlight instead.
    public double GreenhouseGas => Agents.Where(forcing => forcing.Agent.Kind == ForcingKind.GreenhouseGas).Sum(forcing => forcing.Value);

    // 0 for an agent that isn't in the breakdown
    public double Of(ForcingAgent agent) => Agents.FirstOrDefault(forcing => forcing.Agent == agent)?.Value ?? 0;
}
