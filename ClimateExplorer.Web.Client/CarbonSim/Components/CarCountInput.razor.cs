namespace ClimateExplorer.Web.Client.CarbonSim.Components;

using System.Globalization;
using ClimateExplorer.Web.Client.CarbonSim.Scenarios;
using Microsoft.AspNetCore.Components;

/// <summary>
/// The number of cars on the dead planet. A valid count is raised through <see cref="ValueChanged"/>; text that can't
/// be used shows an error and raises <see cref="Invalid"/>.
/// </summary>
public partial class CarCountInput
{
    // A trillion
    private const double MaxCars = 1e12;

    private readonly string id = $"car-count-{Guid.NewGuid():N}";
    private string? text;
    private string? error;
    private FossilFuelInputs? shownValue;

    [Parameter]
    [EditorRequired]
    public FossilFuelInputs Value { get; set; } = default!;

    [Parameter]
    public EventCallback<FossilFuelInputs> ValueChanged { get; set; }

    [Parameter]
    public EventCallback Invalid { get; set; }

    // A count from outside rewrites the text and drops any invalid text. The count this raised comes back unchanged
    // and leaves the text alone.
    protected override void OnParametersSet()
    {
        if (Value == shownValue)
        {
            return;
        }

        shownValue = Value;
        text = Value.Cars.ToString("N0", CultureInfo.InvariantCulture);
        error = null;
    }

    private async Task OnTextChangedAsync(string? value)
    {
        text = value;

        var parsed = double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var cars);

        if (!parsed
            || !double.IsFinite(cars)
            || cars < 1
            || cars > MaxCars
            || cars != Math.Floor(cars))
        {
            error = $"Enter a whole number of cars from 1 up to {MaxCars.ToString("N0", CultureInfo.InvariantCulture)}.";
            await Invalid.InvokeAsync();
            return;
        }

        error = null;
        shownValue = Value with { Cars = cars };
        await ValueChanged.InvokeAsync(shownValue);
    }
}
