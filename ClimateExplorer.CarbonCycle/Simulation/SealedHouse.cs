namespace ClimateExplorer.CarbonCycle.Simulation;

using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;

// The first slice of the simulation engine (simulation plan §5): methane burning in a sealed house. One lumped
// temperature for the air and the house's contents, which lose heat through the walls. Forward Euler on adaptive
// substeps, so the result doesn't depend on the step the caller asks for (§5.3).
public sealed class SealedHouse : ISimulation
{
    // J/(mol·K)
    public const double GasConstant = 8.314462618;

    // m³/mol of an ideal gas at 0 °C and 101.325 kPa
    public const double MolarVolumeAtStandardConditions = 0.022413969;

    // J/(mol·K), air at constant pressure; the vent holds the pressure
    private const double GasHeatCapacity = 29.1;

    // The largest share of the O₂ one substep may burn, and the largest temperature change (K)
    private const double MaxOxygenChange = 0.01;
    private const double MaxTemperatureChange = 0.5;

    // Per call to Advance; past it the state stops short of the time asked for
    private const int MaxSubsteps = 10_000;

    private readonly double oxygenPerMole;
    private readonly double carbonDioxidePerMole;
    private readonly double waterPerMole;
    private readonly double heatPerMole;
    private readonly double latentHeat;

    public SealedHouse(SealedHouseSettings settings)
    {
        var errors = new List<ValidationError>();
        settings.Validate(errors);
        InputValidationException.ThrowIfAny(errors);

        Settings = settings;
        Fuel = DefaultFuels.IdealNaturalGas;
        BurnerMolesPerSecond = settings.BurnerRate / MolarVolumeAtStandardConditions;

        // One mole of methane through the Tab 1 chemistry engine gives the burner's stoichiometry and heat.
        var perMole = CombustionCalculator.Burn(Fuel, SpeciesCatalog.Methane.MolarMass / 1000, CarbonSplit.Complete);
        oxygenPerMole = perMole.ReactantMoles(Material.Oxygen);
        carbonDioxidePerMole = perMole.ProductMoles(Material.CarbonDioxide);
        waterPerMole = perMole.ProductMoles(Material.Water);
        heatPerMole = -perMole.EnthalpyChange;

        // J/mol released when water vapour condenses
        latentHeat = 1000 * (SpeciesCatalog.WaterVapour.EnthalpyOfFormation!.Value - SpeciesCatalog.LiquidWater.EnthalpyOfFormation!.Value);
    }

    public SealedHouseSettings Settings { get; }

    public Fuel Fuel { get; }

    // For display; the burner's numbers come from the chemistry engine
    public Reaction Reaction => ReactionCatalog.MethaneCombustion;

    public double OxygenCutOff => Settings.OxygenCutOff;

    public double BurnerMolesPerSecond { get; }

    // Magnus formula over liquid water (Alduchov and Eskridge 1996), Pa
    public static double SaturationVapourPressure(double temperature)
    {
        var celsius = temperature - 273.15;

        return 610.94 * Math.Exp(17.625 * celsius / (celsius + 243.04));
    }

    public SimulationState Start()
    {
        var settings = Settings;
        var temperature = settings.StartTemperature;
        var gasMoles = MolesAt(settings.TargetPressure, temperature);
        var water = Math.Min(settings.RelativeHumidity * MolesAt(SaturationVapourPressure(temperature), temperature), gasMoles);
        var dry = gasMoles - water;
        var oxygen = dry * DryAir.Oxygen;

        return new SimulationState
        {
            Temperature = temperature,
            Pressure = settings.TargetPressure,
            Nitrogen = dry * DryAir.Nitrogen,
            Oxygen = oxygen,
            Argon = dry * DryAir.Argon,
            CarbonDioxide = dry * DryAir.CarbonDioxide,
            Methane = dry * DryAir.Methane,
            WaterVapour = water,
            FlameOn = BurnerMolesPerSecond > 0 && oxygen / gasMoles >= settings.OxygenCutOff,
        };
    }

    public SimulationState Advance(SimulationState state, double seconds)
    {
        var errors = new List<ValidationError>();
        InputValidation.NonNegative(errors, "seconds", seconds);
        InputValidationException.ThrowIfAny(errors);

        var remaining = seconds;

        for (var i = 0; i < MaxSubsteps && remaining > 0; i++)
        {
            var step = Math.Min(remaining, StepLimit(state));
            var next = Step(state, step);

            if (state.FlameOn && next.OxygenFraction < Settings.OxygenCutOff)
            {
                // Land on the cut-off. O₂ falls almost linearly within a substep, so one estimate is close enough.
                step *= (state.OxygenFraction - Settings.OxygenCutOff) / (state.OxygenFraction - next.OxygenFraction);
                next = Step(state, step) with { FlameOn = false, FlameOutTime = state.Time + step };
            }

            state = next;
            remaining -= step;
        }

        return state;
    }

    private double MolesAt(double pressure, double temperature) => pressure * Settings.Volume / (GasConstant * temperature);

    private double HeatCapacity(SimulationState state) => Settings.ContentsHeatCapacity + (state.GasMoles * GasHeatCapacity);

    private double BurnRate(SimulationState state) => state.FlameOn ? BurnerMolesPerSecond : 0;

    // W into the house, before condensation
    private double HeatFlow(SimulationState state, double burnRate) =>
        (burnRate * heatPerMole) - (Settings.WallHeatLoss * (state.Temperature - Settings.OutsideTemperature));

    private double StepLimit(SimulationState state)
    {
        var limit = double.PositiveInfinity;
        var capacity = HeatCapacity(state);
        var burnRate = BurnRate(state);

        // Keeps the wall loss stable, however close the house is to the outside temperature
        if (Settings.WallHeatLoss > 0)
        {
            limit = 0.1 * capacity / Settings.WallHeatLoss;
        }

        if (burnRate > 0)
        {
            limit = Math.Min(limit, MaxOxygenChange * state.Oxygen / (burnRate * oxygenPerMole));
        }

        var warming = Math.Abs(HeatFlow(state, burnRate) / capacity);

        if (warming > 0)
        {
            limit = Math.Min(limit, MaxTemperatureChange / warming);
        }

        return Math.Max(limit, 1e-6);
    }

    private SimulationState Step(SimulationState state, double seconds)
    {
        var burnRate = BurnRate(state);

        // The flame can't burn more than the O₂ that's there (simulation plan §5.2)
        var burnt = Math.Min(burnRate * seconds, state.Oxygen / oxygenPerMole);
        var heat = burnt * heatPerMole;
        var oxygen = Math.Max(state.Oxygen - (burnt * oxygenPerMole), 0);
        var carbonDioxide = state.CarbonDioxide + (burnt * carbonDioxidePerMole);
        var vapour = state.WaterVapour + (burnt * waterPerMole);
        var capacity = HeatCapacity(state);
        var heatLost = Settings.WallHeatLoss * (state.Temperature - Settings.OutsideTemperature) * seconds;
        var temperature = state.Temperature + ((heat - heatLost) / capacity);

        // Water above saturation condenses on the walls at once, giving out its latent heat; below it, the liquid evaporates.
        var water = vapour + state.LiquidWater;
        var newVapour = Math.Min(water, MolesAt(SaturationVapourPressure(temperature), temperature));
        temperature += (vapour - newVapour) * latentHeat / capacity;

        // The vent only removes, and only N₂ (simulation plan §4.4)
        var nitrogen = state.Nitrogen;
        var gasMoles = nitrogen + oxygen + state.Argon + carbonDioxide + state.Methane + newVapour;
        var vented = Math.Clamp(gasMoles - MolesAt(Settings.TargetPressure, temperature), 0, nitrogen);
        nitrogen -= vented;
        gasMoles -= vented;

        return state with
        {
            Time = state.Time + seconds,
            Temperature = temperature,
            Pressure = gasMoles * GasConstant * temperature / Settings.Volume,
            Nitrogen = nitrogen,
            Oxygen = oxygen,
            CarbonDioxide = carbonDioxide,
            WaterVapour = newVapour,
            LiquidWater = water - newVapour,
            FuelBurnt = state.FuelBurnt + burnt,
            OxygenUsed = state.OxygenUsed + (burnt * oxygenPerMole),
            CarbonDioxideMade = state.CarbonDioxideMade + (burnt * carbonDioxidePerMole),
            WaterMade = state.WaterMade + (burnt * waterPerMole),
            HeatReleased = state.HeatReleased + heat,
            NitrogenVented = state.NitrogenVented + vented,
        };
    }
}
