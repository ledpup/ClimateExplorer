namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using System.Globalization;
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

    private IEnumerable<Row> Rows =>
        State.Gases.Zip(Start.Gases, (now, then) =>
        {
            var fraction = now.Moles / State.GasMoles;

            return new Row(
                now.Species.ToDisplayString(),
                now.Species.Name,
                now.Moles,
                fraction,
                fraction - (then.Moles / Start.GasMoles));
        });

    private string TemperatureText => Units.Format(Measure.Level(State.Temperature - 273.15, UnitOfMeasure.DegreesCelsius));

    private string PressureText => (State.Pressure / 1000).ToString("N2", CultureInfo.InvariantCulture) + " kPa";

    private string FlameText => State.FlameOn
        ? "Burning"
        : State.FlameOutTime is double time
            ? $"Out at {CarbonSimFormat.Duration(time)}"
            : "Not lit";

    // Percentage points, signed
    private static string ChangeText(double change)
    {
        var points = change * 100;

        if (Math.Abs(points) < 5e-7)
        {
            return "0";
        }

        return (points > 0 ? "+" : string.Empty) + CarbonSimFormat.Number(points);
    }

    private sealed record Row(string Formula, string Name, double Moles, double Fraction, double Change);
}
