namespace ClimateExplorer.Web.Client.Pages;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Regions is null)
        {
            Regions = (await DataService!.GetRegions()).ToList();
            StateHasChanged();
        }
    }
}
