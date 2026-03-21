namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using Blazorise;
using ClimateExplorer.Core;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class ClimateStripe
{
    private double min;
    private double max;

    private string? uomString;
    private int uomRounding;

    private Modal? popup;

    [Inject]
    public ILogger<ClimateStripe>? Logger { get; set; }

    [Parameter]
    public string? LocationName { get; set; }

    [Parameter]
    public double? LocationMean { get; set; }

    [Parameter]
    public List<YearlyValues>? DataRecords { get; set; }

    [Parameter]
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    [Parameter]
    public bool ShowInfo { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (UnitOfMeasure == null)
        {
            return;
        }

        uomString = UnitOfMeasureLabelShort(UnitOfMeasure.Value);
        uomRounding = UnitOfMeasureRounding(UnitOfMeasure.Value);

        if (LocationMean == null)
        {
            return;
        }

        if (DataRecords != null)
        {
            min = DataRecords.Min(x => x.Relative);
            max = DataRecords.Max(x => x.Relative);
        }

        await base.OnParametersSetAsync();
    }

    private Task ShowClimateStripeInfo()
    {
        return popup!.Show();
    }

    private Colour GetColourObject(double value)
    {
        Colour colour = default;

        if (UnitOfMeasure.HasValue)
        {
            if (UnitOfMeasure.Value == Enums.UnitOfMeasure.DegreesCelsius)
            {
                if (value > 0)
                {
                    colour = Colour.Blend(new Colour { R = 255, G = 0, B = 0 }, new Colour { R = 255, G = 245, B = 240 }, value / max);
                }
                else
                {
                    colour = Colour.Blend(new Colour { R = 0, G = 0, B = 255 }, new Colour { R = 240, G = 245, B = 255 }, value / min);
                }
            }

            if (UnitOfMeasure!.Value == Enums.UnitOfMeasure.Millimetres)
            {
                if (value > 0)
                {
                    colour = Colour.Blend(new Colour { R = 18, G = 140, B = 74 }, new Colour { R = 240, G = 255, B = 245 }, value / max);
                }
                else
                {
                    colour = Colour.Blend(new Colour { R = 138, G = 51, B = 36 }, new Colour { R = 255, G = 245, B = 245 }, value / min);
                }
            }
        }

        return colour;
    }

    private string GetColour(double value)
    {
        var c = GetColourObject(value);
        return $"rgba({c.R}, {c.G}, {c.B}, 80%)";
    }

    private string GetLabelColour(double value)
    {
        var c = GetColourObject(value);
        return $"rgba({c.R}, {c.G}, {c.B}, 97%)";
    }

    private string GetRelativeValue(YearlyValues values) => UnitOfMeasure == Enums.UnitOfMeasure.Millimetres ? $"{Math.Round(values.PercentageOfAverage, 0)}%" : $"{(values.Relative >= 0 ? "+" : string.Empty)}{Math.Round(values.Relative, uomRounding)}{uomString}";
    private string GetAbsoluteValue(double v) => $"{Math.Round(v, uomRounding)}{uomString}";

    private string GetTitle(YearlyValues values)
    {
        var aboveOrBelow = values.Relative > 0 ? "above" : "below";
        var title = $"Year {values.Year}\r\n";
        if (UnitOfMeasure == Enums.UnitOfMeasure.Millimetres)
        {
            title += $"Precipitation total of {Math.Round(values.Absolute, uomRounding)}{uomString}\r\n";
            title += $"{Math.Abs(Math.Round(values.Relative, uomRounding))}{uomString} {aboveOrBelow} average\r\n";
            title += $"{Math.Round(values.PercentageOfAverage, 0)}% of the average\r\n";
        }
        else
        {
            title += $"Mean temperature average of {Math.Round(values.Absolute, uomRounding)}{uomString}\r\n";
            title += $"{Math.Abs(Math.Round(values.Relative, uomRounding))}{uomString} {aboveOrBelow} average";
        }

        return title;
    }

    private string GetTextColour(double value, string lightTextColour, string darkTextColour)
    {
        // WCAG 2.1 relative luminance — https://www.w3.org/TR/WCAG21/#dfn-relative-luminance
        static double ToLinear(double c)
        {
            c /= 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static double Luminance(double r, double g, double b)
            => (0.2126 * ToLinear(r)) + (0.7152 * ToLinear(g)) + (0.0722 * ToLinear(b));

        var c = GetColourObject(value);
        return Luminance(c.R, c.G, c.B) < 0.179 ? lightTextColour : darkTextColour;
    }

    private async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}