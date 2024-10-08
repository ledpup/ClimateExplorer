namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using Blazorise;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;
using ClimateExplorer.Core;

public partial class ClimateStripe
{
    private double min;
    private double max;

    private string? uomString;
    private int uomRounding;

    private Modal? popup;

    [Parameter]
    public string? LocationName { get; set; }

    [Parameter]
    public double? LocationMean { get; set; }

    [Parameter]
    public List<YearlyValues>? DataRecords { get; set; }

    [Parameter]
    public UnitOfMeasure UnitOfMeasure { get; set; }

    [Parameter]
    public bool ShowInfo { get; set; }

    [Inject]
    public ILogger<ClimateStripe>? Logger { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    public string? PopupText { get; set; }

    private List<YearlyValues>? PreviouslySeenDataRecords { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        uomString = UnitOfMeasureLabelShort(UnitOfMeasure);
        uomRounding = UnitOfMeasureRounding(UnitOfMeasure);

        var weatherPhenomenon = UnitOfMeasure == UnitOfMeasure.Millimetres ? "precipitation" : "temperature";

        if (LocationMean == null)
        {
            return;
        }

        if (YearAndValueListsAreEqual(PreviouslySeenDataRecords!, DataRecords!))
        {
            return;
        }

        if (DataRecords != null)
        {
            min = DataRecords.Min(x => x.Relative);
            max = DataRecords.Max(x => x.Relative);
        }

        PreviouslySeenDataRecords = DataRecords;

        var url = "\"https://en.wikipedia.org/wiki/Warming_stripes\"";

        PopupText = $@"<p><a href={url} target=""_blank"">Climate stripes</a> are a simplified bar chart of average weather phenomena, ordered by year, from the earliest year in the record until the most recent. Each coloured stripe represents a single year of data. A blue stripe is a year where the value is below the average of the whole series. A red stripe represents an above average value.</p>
<p>Climate stripe colours are calculated by the following algorithm.</p>
<ol>
<li>Calculate the average for the whole series (e.g., {LocationName} {DataRecords!.First().Year}-{DataRecords!.Last().Year} mean is {Math.Round(LocationMean.Value, uomRounding)}{uomString})</li>
<li>For each year in the series, subtract the average for the <strong>series</strong> from the average for the <strong>year</strong> (e.g., if series average is 15{uomString} and the year average is 14{uomString}, the result is -1{uomString}). Note:</li>
    <ul>
        <li>This value in step 2 is often called the anomaly</li>
        <li>If the anomaly is above 0{uomString}, we consider it a warmer than average year</li>
        <li>If the anomaly is below 0{uomString}, we consider it a colder than average year</li>
    </ul>
<li>Find the coldest temperature anomaly (e.g., {LocationName}'s is {Math.Round(min, uomRounding)}{uomString}) and assign it the strongest colour of blue</li>
<li>Find the warmest temperature anomaly (e.g., {LocationName}'s is {Math.Round(max, uomRounding)}{uomString}) and assign it the strongest colour of red</li>
<li>All anomalies between the extremes are lighter shades of blue or red</li>
</ol>
<p>Climate Explorer's stripe is interactive. Hover over any year in the series then click. The chart will update with a monthly view of the selected year.";

        await base.OnParametersSetAsync();
    }

    private Task ShowClimateStripeInfo()
    {
        if (!string.IsNullOrWhiteSpace(PopupText))
        {
            return popup!.Show();
        }

        return Task.CompletedTask;
    }

    private bool YearAndValueListsAreEqual(List<YearlyValues> a, List<YearlyValues> b)
    {
        // If they're both null, the lists are the same
        if (a == null && b == null)
        {
            return true;
        }

        // If one is null, the lists are different
        if (a == null || b == null)
        {
            return false;
        }

        // If length is different, the lists are different
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            // If a year is different, the lists are different
            if (a[i].Year != b[i].Year)
            {
                return false;
            }

            // If a value is different, the lists are different
            if (a[i].Absolute != b[i].Absolute)
            {
                return false;
            }
        }

        return true;
    }

    private string GetColour(double value)
    {
        Colour colour = default(Colour);
        if (UnitOfMeasure == UnitOfMeasure.DegreesCelsius)
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

        if (UnitOfMeasure == UnitOfMeasure.Millimetres)
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

        return $"rgba({colour.R}, {colour.G}, {colour.B}, 80%)";

        throw new NotImplementedException();
    }

    private string GetRelativeValue(YearlyValues values) => UnitOfMeasure == UnitOfMeasure.Millimetres ? $"{Math.Round(values.PercentageOfAverage, 0)}%" : $"{(values.Relative >= 0 ? "+" : string.Empty)}{Math.Round(values.Relative, uomRounding)}{uomString}";
    private string GetAbsoluteValue(double v) => $"{Math.Round(v, uomRounding)}{uomString}";

    private string GetTitle(YearlyValues values)
    {
        var aboveOrBelow = values.Relative > 0 ? "above" : "below";
        var title = $"Year {values.Year}\r\n";
        if (UnitOfMeasure == UnitOfMeasure.Millimetres)
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
        if (UnitOfMeasure == UnitOfMeasure.DegreesCelsius)
        {
            return Math.Round(value, uomRounding) <= min / 2 ? lightTextColour : darkTextColour;
        }

        if (UnitOfMeasure == UnitOfMeasure.Millimetres)
        {
            return value > 0 ? Math.Round(value, uomRounding) > max / 1.4 ? lightTextColour : darkTextColour
                             : Math.Round(value, uomRounding) < min / 1.4 ? lightTextColour : darkTextColour;
        }

        throw new NotImplementedException();
    }

    private async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}