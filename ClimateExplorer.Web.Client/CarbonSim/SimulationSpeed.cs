namespace ClimateExplorer.Web.Client.CarbonSim;

/// <summary>A stop on the clock's speed control: simulated seconds per real second (simulation plan §5.1).</summary>
public sealed record SimulationSpeed(double SecondsPerSecond, string Label)
{
    private const double Day = 86400;

    // Julian year, as CarbonSimFormat.Duration
    private const double Year = 365.25 * Day;

    public static IReadOnlyList<SimulationSpeed> All { get; } =
    [
        new(1, "Real time"),
        new(10, "10 seconds per second"),
        new(60, "1 minute per second"),
        new(600, "10 minutes per second"),
        new(3600, "1 hour per second"),
        new(Day, "1 day per second"),
        new(7 * Day, "1 week per second"),
        new(Year, "1 year per second"),
        new(10 * Year, "10 years per second"),
        new(100 * Year, "100 years per second"),
    ];
}
