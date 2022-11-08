using Blazorise;
using ClimateExplorer.Visualiser.UiModel;
using Microsoft.AspNetCore.Components;

namespace ClimateExplorer.Visualiser.Shared
{
    public partial class WarmingStripe
    {
        [Parameter]
        public string? LocationName { get; set; }

        [Parameter]
        public float? LocationTemperatureMean { get; set; }

        [Parameter]
        public List<YearAndValue> DataRecords { get; set; }

        List<YearAndValue> PreviouslySeenDataRecords { get; set; }

        [Inject]
        public ILogger<WarmingStripe> Logger { get; set; }

        [Parameter]
        public EventCallback<short> OnYearFilterChange { get; set; }

        float Min;
        float Max;
        float NormalisedMin;
        float NormalisedMax;

        public string PopupText { get; set; }

        private Modal popup;
        private Task ShowClimateStripeInfo()
        {
            if (!string.IsNullOrWhiteSpace(PopupText))
            {
                return popup.Show();
            }
            return Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (LocationTemperatureMean == null)
            {
                return;
            }

            if (YearAndValueListsAreEqual(PreviouslySeenDataRecords, DataRecords))
            {
                return;
            }

            if (DataRecords != null)
            {
                Min = DataRecords.Min(x => x.Value);
                Max = DataRecords.Max(x => x.Value);

                // If the max and min are not above or below 1, set them to 1
                // This will wash out the colours on a stripe where the values don't deviate much from the average
                // This is okay because we don't want it to look like extreme heating/cooling unless there are larger variations
                NormalisedMin = Min < -1 ? Min : -1;
                NormalisedMax = Max > 1 ? Max : 1;
            }

            PreviouslySeenDataRecords = DataRecords;

            var url = "\"https://en.wikipedia.org/wiki/Warming_stripes\"";

            PopupText = $@"<p>A <a href={url} target=""_blank"">climate stripe</a> is a simplified graph of coloured stripes, ordered by year, representing the average temperate for each year. They visually portray long-term temperature trends.</p>
<p>Climate stripe colours are calculated by the following algorithm.</p>
<ol>
<li>Calculate the average temperature for the whole series (e.g., {LocationName} {DataRecords.First().Year}-{DataRecords.Last().Year} mean is {Math.Round(LocationTemperatureMean.Value, 1)}°C)</li>
<li>For each year in the series, subtract the average temperature for the <strong>year</strong> from the average for the <strong>series</strong>. Note:</li>
    <ul>
        <li>This value in step 2 is often called the temperature anomaly.</li>
        <li>If the anomaly is above 0°C, we consider it a warmer than average year.</li>
        <li>If the anomaly is below 0°C, we consider it a colder than average year.</li>
    </ul>
<li>Find the coldest anomaly (e.g., {LocationName}'s is {Math.Round(Min, 1)}°C) and assign it the strongest colour of blue</li>
<li>Find the warmest anomaly (e.g., {LocationName}'s is {Math.Round(Max, 1)}°C) and assign it the strongest colour of red</li>
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
            if ((a == null) || (b == null)) return false;

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
            if (value > 0)
            {
                return $"rgba(255, {255 - ((Math.Abs(value / NormalisedMax)) * 255)}, {255 - ((Math.Abs(value / NormalisedMax)) * 255)}, 75%)";
            }
            else
            {
                return $"rgba({255 - ((Math.Abs(value / NormalisedMin)) * 255)}, {255 - ((Math.Abs(value / NormalisedMin)) * 255)}, 255, 75%)";
            }
        }

        string GetRelativeTemp(float v) => $"{(v >= 0 ? "+" : "")}{MathF.Round(v, 1)}°C";

        string GetTitle(float value)
        {
            var aboveOrBelow = value > 0 ? "above" : "below";
            return $"{MathF.Round(value, 1)}°C {aboveOrBelow} average";
        }

        string GetTextColour(float value, string lightTextColour, string darkTextColour)
        {
            return MathF.Round(value, 1) <= Min / 2 ? lightTextColour : darkTextColour;
        }

        async Task FilterToYear(short year)
        {
            await OnYearFilterChange.InvokeAsync(year);
        }
    }
}
