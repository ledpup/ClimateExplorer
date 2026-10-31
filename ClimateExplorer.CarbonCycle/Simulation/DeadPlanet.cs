namespace ClimateExplorer.CarbonCycle.Simulation;

using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;

// Cars burning petrol in a dead planet's air. One temperature for the whole atmosphere. Something unmodelled, a warm
// sky, holds the planet at its start temperature; as it warms it loses heat to space, and the CO₂ the cars add keeps
// some of it in (a simple energy balance, simulation plan §4.3, with no sun, albedo or ocean). The water the cars make
// rains out at once. The pressure is the weight of the air.
public sealed class DeadPlanet : ISimulation
{
    // W/m² per e-fold of CO₂ (Myhre et al. 1998)
    public const double CarbonDioxideForcingCoefficient = 5.35;

    // J/(kg·K), dry air at constant pressure
    private const double AirHeatCapacity = 1004;

    // The largest share of the O₂ one substep may burn, and the largest rise in CO₂. The temperature is solved exactly
    // over a substep, so it sets no limit.
    private const double MaxOxygenChange = 0.01;
    private const double MaxCarbonDioxideChange = 0.01;

    // Per call to Advance; past it the state stops short of the time asked for
    private const int MaxSubsteps = 10_000;

    private readonly double oxygenPerMole;
    private readonly double carbonDioxidePerMole;
    private readonly double waterPerMole;

    // J/mol of fuel, with the water rained out, so its latent heat is in the air too
    private readonly double heatPerMole;
    private readonly double combustionHeatPerMole;
    private readonly double heatCapacity;

    public DeadPlanet(DeadPlanetSettings settings)
    {
        var errors = new List<ValidationError>();
        settings.Validate(errors);
        InputValidationException.ThrowIfAny(errors);

        Settings = settings;
        Fuel = DefaultFuels.IdealPetrol;

        var octane = SpeciesCatalog.Octane;
        FuelMolesPerSecond = settings.Cars * settings.PetrolPerCar / 1000 * Fuel.Density!.Value / (octane.MolarMass / 1000);

        // One mole of octane through the Tab 1 chemistry engine gives the engines' stoichiometry and heat.
        var perMole = CombustionCalculator.Burn(Fuel, octane.MolarMass / 1000, CarbonSplit.Complete);
        oxygenPerMole = perMole.ReactantMoles(Material.Oxygen);
        carbonDioxidePerMole = perMole.ProductMoles(Material.CarbonDioxide);
        waterPerMole = perMole.ProductMoles(Material.Water);
        combustionHeatPerMole = -perMole.EnthalpyChange;

        var latentHeat = 1000 * (SpeciesCatalog.WaterVapour.EnthalpyOfFormation!.Value - SpeciesCatalog.LiquidWater.EnthalpyOfFormation!.Value);
        heatPerMole = combustionHeatPerMole + (waterPerMole * latentHeat);
        heatCapacity = settings.AtmosphereMass * AirHeatCapacity;
    }

    public DeadPlanetSettings Settings { get; }

    public Fuel Fuel { get; }

    // For display; the engines' numbers come from the chemistry engine
    public Reaction Reaction => ReactionCatalog.OctaneCombustion;

    public double OxygenCutOff => Settings.OxygenCutOff;

    // Of octane, all the cars together
    public double FuelMolesPerSecond { get; }

    // W/m²: the heat the CO₂ added since the start keeps in
    public double CarbonDioxideForcing(SimulationState state) =>
        CarbonDioxideForcingCoefficient * Math.Log(state.CarbonDioxide / state.GasMoles / DryAir.CarbonDioxide);

    // W/m²: the engines' own heat, spread over the planet
    public double EngineHeat(SimulationState state) => BurnRate(state) * heatPerMole / Settings.SurfaceArea;

    public SimulationState Start()
    {
        var settings = Settings;
        var dryAirMolarMass = (DryAir.Nitrogen * SpeciesCatalog.Nitrogen.MolarMass)
            + (DryAir.Oxygen * SpeciesCatalog.Oxygen.MolarMass)
            + (DryAir.Argon * SpeciesCatalog.Argon.MolarMass)
            + (DryAir.CarbonDioxide * SpeciesCatalog.CarbonDioxide.MolarMass)
            + (DryAir.Methane * SpeciesCatalog.Methane.MolarMass);
        var gasMoles = settings.AtmosphereMass / (dryAirMolarMass / 1000);

        var state = new SimulationState
        {
            Temperature = settings.StartTemperature,
            Nitrogen = gasMoles * DryAir.Nitrogen,
            Oxygen = gasMoles * DryAir.Oxygen,
            Argon = gasMoles * DryAir.Argon,
            CarbonDioxide = gasMoles * DryAir.CarbonDioxide,
            Methane = gasMoles * DryAir.Methane,
            FlameOn = FuelMolesPerSecond > 0 && settings.OxygenCutOff <= DryAir.Oxygen,
        };

        return state with { Pressure = PressureOf(state) };
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

    // The weight of the air on each square metre
    private double PressureOf(SimulationState state) =>
        state.Gases.Sum(gas => gas.Moles * gas.Species.MolarMass / 1000) * Settings.Gravity / Settings.SurfaceArea;

    private double BurnRate(SimulationState state) => state.FlameOn ? FuelMolesPerSecond : 0;

    private double StepLimit(SimulationState state)
    {
        var burnRate = BurnRate(state);

        if (burnRate == 0)
        {
            return double.PositiveInfinity;
        }

        var limit = Math.Min(
            MaxOxygenChange * state.Oxygen / (burnRate * oxygenPerMole),
            MaxCarbonDioxideChange * state.CarbonDioxide / (burnRate * carbonDioxidePerMole));

        return Math.Max(limit, 1e-6);
    }

    private SimulationState Step(SimulationState state, double seconds)
    {
        // The engines can't burn more than the O₂ that's there (simulation plan §5.2)
        var burnt = Math.Min(BurnRate(state) * seconds, state.Oxygen / oxygenPerMole);
        var water = burnt * waterPerMole;

        // Heat in from the engines and the CO₂, held through the substep. Heat out to space grows with the warming, so
        // the temperature relaxes towards where they balance.
        var heating = (burnt * heatPerMole / seconds) + (CarbonDioxideForcing(state) * Settings.SurfaceArea);
        var lossPerKelvin = Settings.ClimateFeedback * Settings.SurfaceArea;
        double temperature;

        if (lossPerKelvin > 0)
        {
            var balance = Settings.StartTemperature + (heating / lossPerKelvin);
            temperature = balance + ((state.Temperature - balance) * Math.Exp(-seconds * lossPerKelvin / heatCapacity));
        }
        else
        {
            temperature = state.Temperature + (heating * seconds / heatCapacity);
        }

        var next = state with
        {
            Time = state.Time + seconds,
            Temperature = temperature,
            Oxygen = Math.Max(state.Oxygen - (burnt * oxygenPerMole), 0),
            CarbonDioxide = state.CarbonDioxide + (burnt * carbonDioxidePerMole),
            LiquidWater = state.LiquidWater + water,
            FuelBurnt = state.FuelBurnt + burnt,
            OxygenUsed = state.OxygenUsed + (burnt * oxygenPerMole),
            CarbonDioxideMade = state.CarbonDioxideMade + (burnt * carbonDioxidePerMole),
            WaterMade = state.WaterMade + water,
            HeatReleased = state.HeatReleased + (burnt * combustionHeatPerMole),
        };

        return next with { Pressure = PressureOf(next) };
    }
}
