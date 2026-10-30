namespace ClimateExplorer.CarbonCycle.Simulation;

using ClimateExplorer.CarbonCycle.Chemistry;

// The sealed house at one moment. Amounts are mol, the temperature K, the pressure Pa and times s since the start.
public sealed record HouseState
{
    public double Time { get; init; }

    public double Temperature { get; init; }

    public double Pressure { get; init; }

    public double Nitrogen { get; init; }

    public double Oxygen { get; init; }

    public double Argon { get; init; }

    public double CarbonDioxide { get; init; }

    public double Methane { get; init; }

    public double WaterVapour { get; init; }

    // Condensed on the walls
    public double LiquidWater { get; init; }

    public bool FlameOn { get; init; }

    public double? FlameOutTime { get; init; }

    // Totals since the start
    public double MethaneBurnt { get; init; }

    public double OxygenUsed { get; init; }

    public double CarbonDioxideMade { get; init; }

    public double WaterMade { get; init; }

    // J
    public double HeatReleased { get; init; }

    public double NitrogenVented { get; init; }

    public double GasMoles => Nitrogen + Oxygen + Argon + CarbonDioxide + Methane + WaterVapour;

    public double OxygenFraction => Oxygen / GasMoles;

    // Largest first, as air is usually listed
    public IReadOnlyList<GasAmount> Gases =>
    [
        new(SpeciesCatalog.Nitrogen, Nitrogen),
        new(SpeciesCatalog.Oxygen, Oxygen),
        new(SpeciesCatalog.Argon, Argon),
        new(SpeciesCatalog.WaterVapour, WaterVapour),
        new(SpeciesCatalog.CarbonDioxide, CarbonDioxide),
        new(SpeciesCatalog.Methane, Methane),
    ];
}
