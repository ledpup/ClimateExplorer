namespace ClimateExplorer.Web.Client.CarbonSim.Scenarios;

using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>Methane burning in a sealed house (simulation plan §3.1).</summary>
public partial class SealedHouseScenario
{
    private string HouseVolumeText => CarbonSimFormat.Quantity(Units, Measure.Level(Scenario.Setting.Volume, UnitOfMeasure.CubicMetres));

    private string StartTemperatureText => Units.Format(Measure.Level(Scenario.Setting.StartTemperature - 273.15, UnitOfMeasure.DegreesCelsius));
}
