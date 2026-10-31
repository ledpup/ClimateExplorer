namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using System.Globalization;
using ClimateExplorer.Web.Client.CarbonSim.Scenarios;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The methane burner's rate, typed in the reader's units. A valid rate is raised through <see cref="ValueChanged"/>
/// in metric; text that can't be used shows an error and raises <see cref="Invalid"/>.
/// </summary>
public partial class BurnerRateInput
{
    private const UnitOfMeasure Unit = UnitOfMeasure.GasLitresPerSecond;

    // In litres per second
    private const double MaxRate = 10_000;
    private const double HouseFireRate = 100;
    private const double HobRate = 0.07;

    private readonly string id = $"burner-rate-{Guid.NewGuid():N}";
    private string? text;
    private string? error;
    private IUnitFormatter? shownUnits;
    private FossilFuelInputs? shownValue;

    [Parameter]
    [EditorRequired]
    public FossilFuelInputs Value { get; set; } = default!;

    [Parameter]
    public EventCallback<FossilFuelInputs> ValueChanged { get; set; }

    [Parameter]
    public EventCallback Invalid { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private string HouseFireRateText => CarbonSimFormat.Approximate(Units, Measure.Level(HouseFireRate, Unit));

    private string HobRateText => CarbonSimFormat.Approximate(Units, Measure.Level(HobRate, Unit));

    // A change of units, or a rate from outside, rewrites the text from the rate in use. That also drops any invalid
    // text, which was typed in the old units. The rate this raised comes back unchanged and leaves the text alone.
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(Units, shownUnits) && Value == shownValue)
        {
            return;
        }

        shownUnits = Units;
        shownValue = Value;
        text = CarbonSimFormat.Number(Units.ToDisplay(Measure.Level(Value.MethaneBurnerRate * 1000, Unit)));
        error = null;
    }

    private async Task OnTextChangedAsync(string? value)
    {
        text = value;

        var parsed = double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var number);
        var litresPerSecond = Units.FromDisplay(number, Unit);

        if (!parsed
            || !double.IsFinite(litresPerSecond)
            || litresPerSecond <= 0
            || litresPerSecond > MaxRate)
        {
            error = $"Enter a rate more than 0 and up to {CarbonSimFormat.Quantity(Units, Measure.Level(MaxRate, Unit))}.";
            await Invalid.InvokeAsync();
            return;
        }

        error = null;
        shownValue = Value with { MethaneBurnerRate = litresPerSecond / 1000 };
        await ValueChanged.InvokeAsync(shownValue);
    }
}
