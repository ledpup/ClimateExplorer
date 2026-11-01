namespace ClimateExplorer.CarbonCycle.Simulation;

// A planet's sunlight and how it holds heat (energy balance plan §1 and §2). Its start temperature follows from these:
// the one where the heat it sends to space balances the sunlight it absorbs.
public sealed record PlanetEnergySettings
{
    // Dry air with CO₂ as its only greenhouse gas, so no water vapour to feed back
    public static PlanetEnergySettings DryPlanet { get; } = new() { GreenhouseEffect = 0.10 };

    // W/m² arriving on a surface facing the sun at the planet's distance (Sources.SolarConstant)
    public double SolarConstant { get; init; } = 1361;

    // The share of the sunlight reflected straight back to space. Earth's, about half of it clouds.
    public double Albedo { get; init; } = 0.30;

    // g₀: the share of the surface's heat radiation the air keeps from reaching space, at the start (Raval and Ramanathan
    // 1989). The dry planet's is provisional (Sources.DryPlanetGreenhouseEffect).
    public double GreenhouseEffect { get; init; }

    // γ, per K: how much the greenhouse effect grows as the planet warms, as warmer air holds more water vapour. 0 with no
    // water.
    public double WaterVapourFeedback { get; init; }

    internal void Validate(List<ValidationError> errors)
    {
        InputValidation.Positive(errors, "planet.solarConstant", SolarConstant);
        InputValidation.FractionBelowOne(errors, "planet.albedo", Albedo);
        InputValidation.FractionBelowOne(errors, "planet.greenhouseEffect", GreenhouseEffect);
        InputValidation.NonNegative(errors, "planet.waterVapourFeedback", WaterVapourFeedback);
    }
}
