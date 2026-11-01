namespace ClimateExplorer.CarbonCycle.Simulation;

// A planet with Earth's size, air and sunlight and nothing else: no water, no ocean, no plants, no rock weathering. Cars
// burn petrol as octane in its air. SI units. See docs/design/2026-10-31-01-dead-planet-cars-scenario-plan.md and
// docs/design/2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md.
public sealed record DeadPlanetSettings
{
    public static DeadPlanetSettings Default { get; } = new();

    // m², Earth's
    public double SurfaceArea { get; init; } = 5.10e14;

    // kg of dry air, Earth's (Trenberth and Smith 2005)
    public double AtmosphereMass { get; init; } = 5.135e18;

    // m/s²
    public double Gravity { get; init; } = 9.80665;

    // Sunlight, albedo and greenhouse effect. The start temperature follows from them.
    public PlanetEnergySettings Energy { get; init; } = PlanetEnergySettings.DryPlanet;

    public double Cars { get; init; } = 1e9;

    // L/s of petrol per car: 7 L/100 km at 60 km/h
    public double PetrolPerCar { get; init; } = 4.2 / 3600;

    // Mole fraction of O₂ below which the engines stop, as the house's flame
    public double OxygenCutOff { get; init; } = 0.15;

    internal void Validate(List<ValidationError> errors)
    {
        InputValidation.Positive(errors, "planet.surfaceArea", SurfaceArea);
        InputValidation.Positive(errors, "planet.atmosphereMass", AtmosphereMass);
        InputValidation.Positive(errors, "planet.gravity", Gravity);
        InputValidation.NonNegative(errors, "planet.cars", Cars);
        InputValidation.NonNegative(errors, "planet.petrolPerCar", PetrolPerCar);
        InputValidation.Fraction(errors, "planet.oxygenCutOff", OxygenCutOff);
        Energy.Validate(errors);
    }
}
