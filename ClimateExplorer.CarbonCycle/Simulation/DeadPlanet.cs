namespace ClimateExplorer.CarbonCycle.Simulation;

using ClimateExplorer.CarbonCycle.Calculation;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Fuels;

// Cars burning petrol in a dead planet's air. One temperature for the whole planet. The sun warms it, and it starts where
// the heat it sends to space balances the sunlight it absorbs. The CO₂ the cars add keeps more heat in, so it warms until
// it balances again (energy balance plan §2 and §5). The water the cars make falls out at once, as frost below freezing.
// The pressure is the weight of the air.
public sealed class DeadPlanet : IPlanetSimulation
{
    // J/(kg·K), dry air at constant pressure
    private const double AirHeatCapacity = 1004;

    // K
    private const double FreezingPoint = 273.15;

    // The largest share of the O₂ one substep may burn, the largest rise in CO₂, and the largest change in temperature,
    // which keeps the temperature's linearisation accurate (planet forcing implementation plan §1.3)
    private const double MaxOxygenChange = 0.01;
    private const double MaxCarbonDioxideChange = 0.01;
    private const double MaxTemperatureChange = 0.5;

    // Per call to Advance; past it the state stops short of the time asked for
    private const int MaxSubsteps = 10_000;

    private readonly double oxygenPerMole;
    private readonly double carbonDioxidePerMole;
    private readonly double waterPerMole;
    private readonly double combustionHeatPerMole;

    // J/mol of water given up as it falls out of the air, as rain or as frost
    private readonly double condensationHeat;
    private readonly double depositionHeat;

    // J/(m²·K), the air's
    private readonly double heatCapacity;

    public DeadPlanet(DeadPlanetSettings settings)
    {
        var errors = new List<ValidationError>();
        settings.Validate(errors);
        InputValidationException.ThrowIfAny(errors);

        Settings = settings;
        Fuel = DefaultFuels.IdealPetrol;
        EnergyBalance = new PlanetEnergyBalance(settings.Energy);

        var octane = SpeciesCatalog.Octane;
        FuelMolesPerSecond = settings.Cars * settings.PetrolPerCar / 1000 * Fuel.Density!.Value / (octane.MolarMass / 1000);

        // One mole of octane through the Tab 1 chemistry engine gives the engines' stoichiometry and heat.
        var perMole = CombustionCalculator.Burn(Fuel, octane.MolarMass / 1000, CarbonSplit.Complete);
        oxygenPerMole = perMole.ReactantMoles(Material.Oxygen);
        carbonDioxidePerMole = perMole.ProductMoles(Material.CarbonDioxide);
        waterPerMole = perMole.ProductMoles(Material.Water);
        combustionHeatPerMole = -perMole.EnthalpyChange;

        var vapour = SpeciesCatalog.WaterVapour.EnthalpyOfFormation!.Value;
        condensationHeat = 1000 * (vapour - SpeciesCatalog.LiquidWater.EnthalpyOfFormation!.Value);
        depositionHeat = 1000 * (vapour - SpeciesCatalog.Ice.EnthalpyOfFormation!.Value);
        heatCapacity = settings.AtmosphereMass * AirHeatCapacity / settings.SurfaceArea;
    }

    public DeadPlanetSettings Settings { get; }

    public PlanetEnergySettings Energy => Settings.Energy;

    public PlanetEnergyBalance EnergyBalance { get; }

    public Fuel Fuel { get; }

    // For display; the engines' numbers come from the chemistry engine
    public Reaction Reaction => ReactionCatalog.OctaneCombustion;

    public double OxygenCutOff => Settings.OxygenCutOff;

    // Of octane, all the cars together
    public double FuelMolesPerSecond { get; }

    public double WarmingPerDoubling => EnergyBalance.WarmingPerDoubling;

    // The engines' heat, with the water's as it falls out, is non-radiative heat beside the forcings
    public ForcingBreakdown Forcing(SimulationState state) =>
        ForcingWith(state, BurnRate(state) * HeatPerMole(state.Temperature) / Settings.SurfaceArea);

    public EnergyBudget Budget(SimulationState state) => EnergyBalance.Budget(state.Temperature, Forcing(state));

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
            Temperature = EnergyBalance.StartTemperature,
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

    // The reference state is the start: today's CO₂, and the planet's own sunlight and albedo, which don't change here.
    // So the solar and albedo forcings stay 0, and the reader sees the sun isn't the cause.
    private ForcingBreakdown ForcingWith(SimulationState state, double nonRadiativeHeat)
    {
        var energy = Settings.Energy;

        return new(
            [
                new(ForcingAgents.CarbonDioxide, RadiativeForcing.CarbonDioxide(state.CarbonDioxide / state.GasMoles, DryAir.CarbonDioxide)),
                new(ForcingAgents.Solar, RadiativeForcing.Solar(energy.SolarConstant, energy.SolarConstant, energy.Albedo)),
                new(ForcingAgents.Albedo, RadiativeForcing.Albedo(energy.SolarConstant, energy.Albedo, energy.Albedo)),
            ],
            nonRadiativeHeat);
    }

    // J/mol of fuel, with the water fallen out, so its latent heat is in the air too
    private double HeatPerMole(double temperature) =>
        combustionHeatPerMole + (waterPerMole * (temperature < FreezingPoint ? depositionHeat : condensationHeat));

    // The weight of the air on each square metre
    private double PressureOf(SimulationState state) =>
        state.Gases.Sum(gas => gas.Moles * gas.Species.MolarMass / 1000) * Settings.Gravity / Settings.SurfaceArea;

    private double BurnRate(SimulationState state) => state.FlameOn ? FuelMolesPerSecond : 0;

    private double StepLimit(SimulationState state)
    {
        var limit = TemperatureStepLimit(state);
        var burnRate = BurnRate(state);

        if (burnRate > 0)
        {
            limit = Math.Min(
                limit,
                Math.Min(
                    MaxOxygenChange * state.Oxygen / (burnRate * oxygenPerMole),
                    MaxCarbonDioxideChange * state.CarbonDioxide / (burnRate * carbonDioxidePerMole)));
        }

        return Math.Max(limit, 1e-6);
    }

    // Close to balance, a step of any length lands on it. Further away, the step is cut to the time the current rate of
    // warming takes to move the temperature by MaxTemperatureChange.
    private double TemperatureStepLimit(SimulationState state)
    {
        var distance = Math.Abs(EnergyBalance.DistanceFromBalance(state.Temperature, Forcing(state)));

        if (distance <= MaxTemperatureChange)
        {
            return double.PositiveInfinity;
        }

        return MaxTemperatureChange * heatCapacity / (distance * EnergyBalance.Feedback(state.Temperature));
    }

    private SimulationState Step(SimulationState state, double seconds)
    {
        // The engines can't burn more than the O₂ that's there (simulation engine plan §3)
        var burnt = Math.Min(BurnRate(state) * seconds, state.Oxygen / oxygenPerMole);
        var water = burnt * waterPerMole;
        var frozen = state.Temperature < FreezingPoint;

        // Water that has already fallen stays as it fell
        var next = state with
        {
            Time = state.Time + seconds,
            Oxygen = Math.Max(state.Oxygen - (burnt * oxygenPerMole), 0),
            CarbonDioxide = state.CarbonDioxide + (burnt * carbonDioxidePerMole),
            LiquidWater = frozen ? state.LiquidWater : state.LiquidWater + water,
            Ice = frozen ? state.Ice + water : state.Ice,
            FuelBurnt = state.FuelBurnt + burnt,
            OxygenUsed = state.OxygenUsed + (burnt * oxygenPerMole),
            CarbonDioxideMade = state.CarbonDioxideMade + (burnt * carbonDioxidePerMole),
            WaterMade = state.WaterMade + water,
            HeatReleased = state.HeatReleased + (burnt * combustionHeatPerMole),
        };

        // The engines' heat is held through the substep, and the CO₂'s forcing rises steadily with the CO₂
        var engineHeat = burnt * HeatPerMole(state.Temperature) / seconds / Settings.SurfaceArea;
        var temperature = EnergyBalance.Temperature(
            state.Temperature, ForcingWith(state, engineHeat), ForcingWith(next, engineHeat), heatCapacity, seconds);

        return next with { Temperature = temperature, Pressure = PressureOf(next) };
    }
}
