namespace ClimateExplorer.Web.Client.Components.Info;

using ClimateExplorer.Core.Model;
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

    private string AnomalyUnitLabel => Units.ShortLabel(UnitOfMeasure.DegreesCelsius);

    protected override async Task OnInitializedAsync()
    {
        if (HeatingScoreTable is not null)
        {
            return;
        }

        HeatingScoreTable = await DataService!.GetHeatingScoreTable();
    }

    // The table and the location's anomaly are both differences, shown a place more precisely than usual
    // so neighbouring score bands can be told apart.
    private string FormatLocationAnomaly(Location location) =>
        ApiMeasures.WarmingAnomaly(location) is { } anomaly ? Units.Format(anomaly, extraDecimals: 1) : "NA";

    private string FormatBound(double bound) => Units.FormatNumber(ApiMeasures.HeatingScoreAnomalyBound(bound), extraDecimals: 1);
}
