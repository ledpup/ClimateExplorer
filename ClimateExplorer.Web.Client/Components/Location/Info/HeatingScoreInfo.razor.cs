namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class HeatingScoreInfo
{
    [Inject]
    public IDataService? DataService { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [PersistentState]
    public IEnumerable<HeatingScoreRow>? HeatingScoreTable { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private static int StartYear => TrendDefaults.FixedPeriodStartYear;

    private static int LatestFirstYear => TrendDefaults.FixedPeriodStartYear + TrendDefaults.FixedPeriodStartToleranceYears - 1;

    private static int MinimumYears => TrendDefaults.MinimumYearsForTrend;

    private static int MaximumYearsBehindNewest => Location.HeatingScoreMaximumYearsBehindNewest;

    private string RateUnitLabel => $"{Units.ShortLabel(UnitOfMeasure.DegreesCelsius)}/decade";

    protected override async Task OnInitializedAsync()
    {
        if (HeatingScoreTable is not null)
        {
            return;
        }

        HeatingScoreTable = await DataService!.GetHeatingScoreTable();
    }

    // The middle score bands are only a few hundredths of a degree per decade wide, so the rates are shown to
    // three decimal places (two more than a temperature difference) for neighbouring bands to be told apart.
    private string FormatLocationRate(Location location) =>
        ApiMeasures.HeatingScoreRate(location) is { } rate ? $"{Units.Format(rate, signed: true, extraDecimals: 1)}/decade" : "NA";

    private string FormatBound(double bound) => Units.FormatNumber(ApiMeasures.HeatingScoreRateBound(bound), extraDecimals: 1);
}
