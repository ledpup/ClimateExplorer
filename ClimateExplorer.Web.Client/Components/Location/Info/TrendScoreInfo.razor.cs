namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class TrendScoreInfo
{
    private SidePanel? locationsSidePanel;
    private TrendScoreRow? selectedRow;

    [Inject]
    public IDataService? DataService { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [PersistentState]
    public IEnumerable<TrendScoreRow>? TrendScoreTable { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private static int StartYear => TrendDefaults.FixedPeriodStartYear;

    private static int LatestFirstYear => TrendDefaults.FixedPeriodStartYear + TrendDefaults.FixedPeriodStartToleranceYears - 1;

    private static int MinimumYears => TrendDefaults.MinimumYearsForTrend;

    private static int MiddlePercent => (int)Math.Round((Location.TrendScoreHighPercentile - Location.TrendScoreLowPercentile) * 100);

    private static int MaximumYearsBehindNewest => Location.TrendScoreMaximumYearsBehindNewest;

    // The popup this is shown in, closed when a location in the side panel is followed
    [CascadingParameter]
    private Modal? Modal { get; set; }

    private string RateUnitLabel => $"{Units.ShortLabel(UnitOfMeasure.DegreesCelsius)}/decade";

    protected override async Task OnInitializedAsync()
    {
        if (TrendScoreTable is not null)
        {
            return;
        }

        TrendScoreTable = await DataService!.GetTrendScoreTable();
    }

    private static string GetLocationsTitle(TrendScoreRow row) => row.Score switch
    {
        null => "Locations without a trend score",
        < 0 => $"Locations that have cooled (trend score {row.Score})",
        _ => $"Locations with trend score {row.Score}",
    };

    // The score steps are only a few hundredths of a degree per decade wide, so the rates are shown to three decimal
    // places (two more than a temperature difference) for neighbouring steps to be told apart.
    private string FormatLocationRate(Location location) =>
        ApiMeasures.TrendScoreRate(location) is { } rate ? $"{Units.Format(rate, signed: true, extraDecimals: 1)}/decade" : "NA";

    private string FormatBound(double bound) => Units.FormatNumber(ApiMeasures.TrendScoreRateBound(bound), extraDecimals: 1);

    private string? GetRowClass(TrendScoreRow row)
    {
        var highlighted = Location?.TrendScore == row.Score;
        var clickable = row.LocationCount > 0;

        return (highlighted, clickable) switch
        {
            (true, true) => "is-highlighted is-clickable",
            (true, false) => "is-highlighted",
            (false, true) => "is-clickable",
            _ => null,
        };
    }

    private async Task ShowLocationsAsync(TrendScoreRow row)
    {
        if (row.LocationCount == 0)
        {
            return;
        }

        selectedRow = row;
        await (locationsSidePanel?.ShowAsync() ?? Task.CompletedTask);
    }

    // The link goes to that location's page behind the modal, so close the modal (and the side panel in it) to show it
    private Task CloseModalAsync() => Modal?.HideAsync() ?? Task.CompletedTask;
}
