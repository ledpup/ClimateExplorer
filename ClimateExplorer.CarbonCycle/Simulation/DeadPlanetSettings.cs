namespace ClimateExplorer.CarbonCycle.Simulation;

// A planet with Earth's size and air and nothing else: no sun, no ocean, no plants, no rock weathering. Cars burn
// petrol as octane in its air. SI units. See docs/design/2026-10-31-01-dead-planet-cars-scenario-plan.md.
public sealed record DeadPlanetSettings
{
    public static DeadPlanetSettings Default { get; } = new();

    // m², Earth's
    public double SurfaceArea { get; init; } = 5.10e14;

    // kg of dry air, Earth's (Trenberth and Smith 2005)
    public double AtmosphereMass { get; init; } = 5.135e18;

    // m/s²
    public double Gravity { get; init; } = 9.80665;

    // K, Earth's mean surface temperature. Heat to space is balanced at this temperature and the starting CO₂.
    public double StartTemperature { get; init; } = 288.15;

    public double Cars { get; init; } = 1e9;

    // L/s of petrol per car: 7 L/100 km at 60 km/h
    public double PetrolPerCar { get; init; } = 4.2 / 3600;

    // Mole fraction of O₂ below which the engines stop, as the house's flame
    public double OxygenCutOff { get; init; } = 0.15;

    // W/(m²·K): the extra heat lost to space per kelvin of warming. The Planck response alone (AR6: 3.2), since the
    // planet has no water vapour, ice or clouds to feed back. 0 makes the planet lose no heat at all.
    public double ClimateFeedback { get; init; } = 3.2;

    internal void Validate(List<ValidationError> errors)
    {
        InputValidation.Positive(errors, "planet.surfaceArea", SurfaceArea);
        InputValidation.Positive(errors, "planet.atmosphereMass", AtmosphereMass);
        InputValidation.Positive(errors, "planet.gravity", Gravity);
        InputValidation.Positive(errors, "planet.startTemperature", StartTemperature);
        InputValidation.NonNegative(errors, "planet.cars", Cars);
        InputValidation.NonNegative(errors, "planet.petrolPerCar", PetrolPerCar);
        InputValidation.Fraction(errors, "planet.oxygenCutOff", OxygenCutOff);
        InputValidation.NonNegative(errors, "planet.climateFeedback", ClimateFeedback);
    }
}
