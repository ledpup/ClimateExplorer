namespace ClimateExplorer.CarbonCycle.Simulation;

// Something that changes a planet's energy budget other than its temperature (energy balance plan §3). Catalog agents are
// singletons, so equality is by reference.
public sealed class ForcingAgent
{
    public ForcingAgent(string id, string name, ForcingKind kind, string source)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Source = source;
    }

    // Stable key, e.g. "co2"
    public string Id { get; }

    public string Name { get; }

    public ForcingKind Kind { get; }

    public string Source { get; }
}
