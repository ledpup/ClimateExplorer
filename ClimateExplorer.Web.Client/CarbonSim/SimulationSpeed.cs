namespace ClimateExplorer.Web.Client.CarbonSim;

/// <summary>A stop on the clock's speed control: simulated seconds per real second (simulation plan §5.1).</summary>
public sealed record SimulationSpeed(int SecondsPerSecond, string Label)
{
    public static IReadOnlyList<SimulationSpeed> All { get; } =
    [
        new(1, "Real time"),
        new(60, "1 minute per second"),
        new(600, "10 minutes per second"),
        new(3600, "1 hour per second"),
        new(86400, "1 day per second"),
    ];
}
