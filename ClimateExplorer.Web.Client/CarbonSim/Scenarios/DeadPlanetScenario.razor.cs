namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using System.Globalization;
using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>Cars burning petrol on a dead planet: Earth's air and sunlight, with no ocean or plants.</summary>
public partial class DeadPlanetScenario
{
    // The speed DeadPlanetSettings.PetrolPerCar is for
    private const double SpeedKilometresPerHour = 60;
    private const double KilometresPerMile = 1.609344;

    private DeadPlanet Planet => (DeadPlanet)Simulation;

    private ForcingBreakdown Forcing => Planet.Forcing(State);

    private string CarsText => Planet.Settings.Cars.ToString("N0", CultureInfo.InvariantCulture);

    private string SpeedText => Units.System == UnitSystem.Metric
        ? $"{SpeedKilometresPerHour:0} km/h"
        : $"{SpeedKilometresPerHour / KilometresPerMile:0} mph";

    // Per hour reads better than per second for one car
    private string PerCarText => Units.System == UnitSystem.Metric
        ? $"{Planet.Settings.PetrolPerCar * 3600:0.#} litres an hour"
        : $"{Units.ToDisplay(Measure.Level(Planet.Settings.PetrolPerCar, UnitOfMeasure.LiquidLitresPerSecond)):0.#} US gallons an hour";

    private string StartTemperatureText => Units.Format(Measure.Level(Planet.EnergyBalance.StartTemperature - 273.15, UnitOfMeasure.DegreesCelsius));

    private string PerDoublingText =>
        Units.Format(Measure.Change(Planet.WarmingPerDoubling, UnitOfMeasure.DegreesCelsius));

    // Below freezing, the cars' water goes straight to frost
    private string WaterFallsText => Planet.EnergyBalance.StartTemperature < 273.15
        ? "falls as frost"
        : "falls as rain";

    private string FallenOutText =>
        CarbonSimFormat.Quantity(Units, Measure.Level((State.LiquidWater + State.Ice) * SpeciesCatalog.LiquidWater.MolarMass / 1000, UnitOfMeasure.Kilograms));

    private string HeatFluxText(double wattsPerSquareMetre) =>
        CarbonSimFormat.Quantity(Units, Measure.Level(wattsPerSquareMetre, UnitOfMeasure.WattsPerSquareMetre));
}
