namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>Methane burning in a sealed house (simulation plan §3.1).</summary>
public partial class SealedHouseScenario
{
    private SealedHouse House => (SealedHouse)Simulation;

    private string HouseVolumeText => CarbonSimFormat.Quantity(Units, Measure.Level(House.Settings.Volume, UnitOfMeasure.CubicMetres));

    private string StartTemperatureText => Units.Format(Measure.Level(House.Settings.StartTemperature - 273.15, UnitOfMeasure.DegreesCelsius));

    private string CondensedText =>
        CarbonSimFormat.Quantity(Units, Measure.Level(State.LiquidWater * SpeciesCatalog.LiquidWater.MolarMass / 1000, UnitOfMeasure.Kilograms));
}
