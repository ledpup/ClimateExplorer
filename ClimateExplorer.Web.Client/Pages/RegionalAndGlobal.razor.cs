namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Web.Client.Services.Chart;
using Microsoft.AspNetCore.Components;

public partial class RegionalAndGlobal : ChartablePage
{
    public RegionalAndGlobal()
    {
        PageName = "regionalandglobal";
    }

    protected override string PageTitle
    {
        get
        {
            return $"ClimateExplorer - Regional and global long-term climate trends";
        }
    }

    protected override string PageDescription
    {
        get
        {
            return "Explore regional and global long-term climate trends. View CO₂ levels, sea ice extent, sea level rise, and global temperature anomalies.";
        }
    }

    protected override string PageUrl
    {
        get
        {
            return $"https://climateexplorer.net/regionalandglobal";
        }
    }

    [Inject]
    private IRegionalAndGlobalDefaultChartProvider? RegionalAndGlobalDefaultChartProvider { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Regions is null)
        {
            Regions = (await DataService!.GetRegions()).ToList();
            StateHasChanged();
        }

        if (await EnsureInitialChartStateAsync(location: null, CreateDefaultRegionalAndGlobalChartState))
        {
            StateHasChanged();
        }
    }

    private ChartState CreateDefaultRegionalAndGlobalChartState()
    {
        return RegionalAndGlobalDefaultChartProvider!.CreateDefault(
            new RegionalAndGlobalDefaultChartContext
            {
                DataSetDefinitions = DataSetDefinitions!.ToList(),
            });
    }
}
