namespace ClimateExplorer.CarbonCycle.Simulation;

// A planet with one temperature, the global mean (energy balance plan §2). It absorbs a quarter of the solar constant,
// less what its albedo reflects, and sends (1 − g) σT⁴ − F_GHG to space. It starts in balance, so its start temperature
// follows from the settings. No state of its own.
public sealed class PlanetEnergyBalance
{
    // W/(m²·K⁴)
    public const double StefanBoltzmann = 5.670374419e-8;

    public PlanetEnergyBalance(PlanetEnergySettings settings)
    {
        var errors = new List<ValidationError>();
        settings.Validate(errors);
        InputValidationException.ThrowIfAny(errors);

        Settings = settings;
        IncomingSunlight = settings.SolarConstant / 4;
        AbsorbedSunlight = (1 - settings.Albedo) * IncomingSunlight;
        StartTemperature = Math.Pow(AbsorbedSunlight / ((1 - settings.GreenhouseEffect) * StefanBoltzmann), 0.25);
    }

    public PlanetEnergySettings Settings { get; }

    // W/m², averaged over the sphere: it catches sunlight on a disc a quarter of its area
    public double IncomingSunlight { get; }

    public double AbsorbedSunlight { get; }

    // K, where the heat to space balances the absorbed sunlight with no forcing
    public double StartTemperature { get; }

    // K of equilibrium warming per doubling of CO₂, from the climate feedback at the start
    public double WarmingPerDoubling => RadiativeForcing.CarbonDioxidePerDoubling / Feedback(StartTemperature);

    // g(T): grows with the warming when there's water vapour to feed back
    public double GreenhouseEffect(double temperature) =>
        Settings.GreenhouseEffect + (Settings.WaterVapourFeedback * (temperature - StartTemperature));

    // W/m²; the greenhouse gases' forcing lowers it
    public double HeatToSpace(double temperature, double greenhouseGasForcing) =>
        ((1 - GreenhouseEffect(temperature)) * StefanBoltzmann * Math.Pow(temperature, 4)) - greenhouseGasForcing;

    // λ, W/(m²·K): how much more heat reaches space per kelvin of warming. 4 (1 − g₀) σT³ with no water vapour, the Planck
    // response alone.
    public double Feedback(double temperature) =>
        (4 * (1 - GreenhouseEffect(temperature)) * StefanBoltzmann * Math.Pow(temperature, 3))
        - (Settings.WaterVapourFeedback * StefanBoltzmann * Math.Pow(temperature, 4));

    public EnergyBudget Budget(double temperature, ForcingBreakdown forcing) =>
        new(
            IncomingSunlight,
            IncomingSunlight - AbsorbedSunlight,
            AbsorbedSunlight,
            HeatToSpace(temperature, forcing.GreenhouseGas),
            forcing.NonRadiativeHeat);

    // K after the given seconds with the forcing held, for heat capacity C in J/(m²·K)
    public double Temperature(double temperature, ForcingBreakdown forcing, double heatCapacity, double seconds) =>
        Temperature(temperature, forcing, forcing, heatCapacity, seconds);

    // K after the given seconds with the forcing changing steadily from start to end. Linearised about the starting
    // temperature, it follows the moving balance, lagging it by the response time C/λ, so it's stable at any step and
    // keeps the lag even when the step is longer; keep the change small for accuracy.
    public double Temperature(double temperature, ForcingBreakdown start, ForcingBreakdown end, double heatCapacity, double seconds)
    {
        if (seconds <= 0)
        {
            return temperature;
        }

        // W/m², and its rate of change, s⁻¹
        var heating = Heating(temperature, start);
        var rate = (Heating(temperature, end) - heating) / seconds;
        var feedback = Feedback(temperature);

        if (feedback <= 0)
        {
            return temperature + (((heating * seconds) + (rate * seconds * seconds / 2)) / heatCapacity);
        }

        // C dx/dt = heating + rate·t − λx, from x = 0
        var lag = rate * heatCapacity / (feedback * feedback);

        return temperature
            + ((heating + (rate * seconds)) / feedback)
            - lag
            + ((lag - (heating / feedback)) * Math.Exp(-feedback * seconds / heatCapacity));
    }

    // K: how far the temperature has to go to balance, at the current climate feedback
    public double DistanceFromBalance(double temperature, ForcingBreakdown forcing)
    {
        var feedback = Feedback(temperature);

        return feedback > 0 ? Heating(temperature, forcing) / feedback : double.PositiveInfinity;
    }

    // W/m²: N + H, what warms the planet
    private double Heating(double temperature, ForcingBreakdown forcing) =>
        AbsorbedSunlight - HeatToSpace(temperature, forcing.GreenhouseGas) + forcing.NonRadiativeHeat;
}
