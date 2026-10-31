namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using ClimateExplorer.CarbonCycle.Chemistry;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>The house's readouts and the share of each gas in its air, now and against the start.</summary>
public partial class GasTable
{
    [Parameter]
    [EditorRequired]
    public HouseState State { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public HouseState Start { get; set; } = default!;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    // Argon takes no part in the reaction, so it's left out of the rows; the note and the total cover it
    private IEnumerable<Row> Rows =>
        State.Gases.Zip(Start.Gases, (now, then) =>
        {
            var fraction = now.Moles / State.GasMoles;

            return new Row(
                now.Species,
                now.Moles,
                fraction,
                fraction - (then.Moles / Start.GasMoles));
        })
        .Where(row => row.Species != SpeciesCatalog.Argon);

    private string ArgonShareText => CarbonSimFormat.Share(State.Argon / State.GasMoles);

    private string TemperatureText => Units.Format(Measure.Level(State.Temperature - 273.15, UnitOfMeasure.DegreesCelsius));

    private string PressureText => Units.Format(Measure.Level(State.Pressure / 1000, UnitOfMeasure.Kilopascals));

    private string FlameText => State.FlameOn
        ? "Burning"
        : State.FlameOutTime is double time
            ? $"Out at {CarbonSimFormat.Duration(time)}"
            : "Not lit";

    private sealed record Row(Species Species, double Moles, double Fraction, double Change);
}
