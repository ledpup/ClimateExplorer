using Blazorise;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;
using ClimateExplorer.Core;

namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

public partial class ClimateStripe
{
    [Parameter]
    public string? LocationName { get; set; }

    [Parameter]
    public float? LocationMean { get; set; }

    [Parameter]
    public List<YearAndValue>? DataRecords { get; set; }

    [Parameter]
    public UnitOfMeasure UnitOfMeasure { get; set; }

    [Parameter]
    public bool ShowInfo { get; set; }

    List<YearAndValue>? PreviouslySeenDataRecords { get; set; }

    [Inject]
    public ILogger<ClimateStripe>? Logger { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    float Min;
    float Max;

    string? UomString;
    int UomRounding;

    public string? PopupText { get; set; }

    private Modal? popup;
    private Task ShowClimateStripeInfo()
    {
        if (!string.IsNullOrWhiteSpace(PopupText))
        {
            return popup!.Show();
        }
        return Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        UomString = UnitOfMeasureLabelShort(UnitOfMeasure);
        UomRounding = UnitOfMeasureRounding(UnitOfMeasure);

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
            Min = DataRecords.Min(x => x.Value);
            Max = DataRecords.Max(x => x.Value);
        }

        PreviouslySeenDataRecords = DataRecords;

        var url = "\"https://en.wikipedia.org/wiki/Warming_stripes\"";

        PopupText = $@"<p><a href={url} target=""_blank"">Climate stripes</a> are a simplified bar chart of average weather phenomena, ordered by year, from the earliest year in the record until the most recent. Each coloured stripe represents a single year of data. A blue stripe is a year where the value is below the average of the whole series. A red stripe represents an above average value.</p>
<p>Climate stripe colours are calculated by the following algorithm.</p>
<ol>
<li>Calculate the average for the whole series (e.g., {LocationName} {DataRecords!.First().Year}-{DataRecords!.Last().Year} mean is {Math.Round(LocationMean.Value, UomRounding)}{UomString})</li>
<li>For each year in the series, subtract the average for the <strong>series</strong> from the average for the <strong>year</strong> (e.g., if series average is 15{UomString} and the year average is 14{UomString}, the result is -1{UomString}). Note:</li>
    <ul>
        <li>This value in step 2 is often called the anomaly</li>
        <li>If the anomaly is above 0{UomString}, we consider it a warmer than average year</li>
        <li>If the anomaly is below 0{UomString}, we consider it a colder than average year</li>
    </ul>
<li>Find the coldest temperature anomaly (e.g., {LocationName}'s is {Math.Round(Min, UomRounding)}{UomString}) and assign it the strongest colour of blue</li>
<li>Find the warmest temperature anomaly (e.g., {LocationName}'s is {Math.Round(Max, UomRounding)}{UomString}) and assign it the strongest colour of red</li>
<li>All anomalies between the extremes are lighter shades of blue or red</li>
</ol>
<p>Climate Explorer's stripe is interactive. Hover over any year in the series then click. The chart will update with a monthly view of the selected year.";

        await base.OnParametersSetAsync();
    }

    bool YearAndValueListsAreEqual(List<YearAndValue> a, List<YearAndValue> b)
    {
        // If they're both null, the lists are the same
        if (a == null && b == null) return true;

        // If one is null, the lists are different
        if (a == null || b == null) return false;

        // If length is different, the lists are different
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            // If a year is different, the lists are different
            if (a[i].Year != b[i].Year) return false;

            // If a value is different, the lists are different
            if (a[i].Value != b[i].Value) return false;
        }

        return true;
    }

    string GetColour(float value)
    {
        Colour colour = new Colour();
        if (UnitOfMeasure == UnitOfMeasure.DegreesCelsius)
        {
            if (value > 0)
            {
                colour = Colour.Blend(new Colour { R = 255, G = 0, B = 0 }, new Colour { R = 255, G = 245, B = 240 }, value / Max);
            }
            else
            {
                colour = Colour.Blend(new Colour { R = 0, G = 0, B = 255 }, new Colour { R = 240, G = 245, B = 255 }, value / Min);
            }
        }
        if (UnitOfMeasure == UnitOfMeasure.Millimetres)
        {
            if (value > 0)
            {
                colour = Colour.Blend(new Colour { R = 18, G = 140, B = 74 }, new Colour { R = 240, G = 255, B = 245 }, value / Max);
            }
            else
            {
                colour = Colour.Blend(new Colour { R = 138, G = 51, B = 36 }, new Colour { R = 255, G = 245, B = 245 }, value / Min);
            }
        }

        return $"rgba({colour.R}, {colour.G}, {colour.B}, 80%)";

        throw new NotImplementedException();
    }



    string GetRelativeTemp(float v) => $"{(v >= 0 ? "+" : "")}{MathF.Round(v, UomRounding)}{UomString}";

    string GetTitle(float value)
    {
        var aboveOrBelow = value > 0 ? "above" : "below";
        return $"{MathF.Round(value, UomRounding)}{UomString} {aboveOrBelow} average";
    }

    string GetTextColour(float value, string lightTextColour, string darkTextColour)
    {
        if (UnitOfMeasure == UnitOfMeasure.DegreesCelsius)
        {
            return MathF.Round(value, UomRounding) <= Min / 2 ? lightTextColour : darkTextColour;
        }

        if (UnitOfMeasure == UnitOfMeasure.Millimetres)
        {
            return value > 0 ? MathF.Round(value, UomRounding) > Max / 2 ? lightTextColour : darkTextColour
                : MathF.Round(value, UomRounding) < Min / 2 ? lightTextColour : darkTextColour;
        }

        throw new NotImplementedException();
    }

    async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}
