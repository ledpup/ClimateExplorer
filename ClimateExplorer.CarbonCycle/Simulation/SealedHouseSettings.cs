namespace ClimateExplorer.CarbonCycle.Simulation;

// The sealed house setting (simulation plan §3.1): opaque walls, no sunlight, no gas exchange apart from the N₂ vent,
// and a methane burner. SI units. The heat defaults are provisional, sized for a house (simulation plan §4.3: to source).
public sealed record SealedHouseSettings
{
    public static SealedHouseSettings Default { get; } = new();

    // m³
    public double Volume { get; init; } = 250;

    // K; the house starts at the outside temperature, so any change comes from the flame
    public double StartTemperature { get; init; } = 293.15;

    // K
    public double OutsideTemperature { get; init; } = 293.15;

    // Pa. The N₂ vent holds the house at or below this (simulation plan §4.4).
    public double TargetPressure { get; init; } = 101_325;

    // Fraction of the water vapour the air could hold at the start temperature
    public double RelativeHumidity { get; init; } = 0.5;

    // m³/s of methane at 0 °C and 101.325 kPa, the fuel table's gas basis (simulation plan §2.1)
    public double BurnerRate { get; init; } = 0.1;

    // Mole fraction of O₂ below which the flame goes out (simulation plan §4.4)
    public double OxygenCutOff { get; init; } = 0.15;

    // W/K: heat lost through the walls per kelvin above the outside
    public double WallHeatLoss { get; init; } = 200;

    // J/K: the walls, floor and contents, taken to warm with the air
    public double ContentsHeatCapacity { get; init; } = 10e6;

    internal void Validate(List<ValidationError> errors)
    {
        InputValidation.Positive(errors, "house.volume", Volume);
        InputValidation.Positive(errors, "house.startTemperature", StartTemperature);
        InputValidation.Positive(errors, "house.outsideTemperature", OutsideTemperature);
        InputValidation.Positive(errors, "house.targetPressure", TargetPressure);
        InputValidation.Fraction(errors, "house.relativeHumidity", RelativeHumidity);
        InputValidation.NonNegative(errors, "house.burnerRate", BurnerRate);
        InputValidation.Fraction(errors, "house.oxygenCutOff", OxygenCutOff);
        InputValidation.NonNegative(errors, "house.wallHeatLoss", WallHeatLoss);
        InputValidation.NonNegative(errors, "house.contentsHeatCapacity", ContentsHeatCapacity);
    }
}
