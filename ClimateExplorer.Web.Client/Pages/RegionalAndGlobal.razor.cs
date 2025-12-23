namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.WebUtilities;
using static ClimateExplorer.Core.Enums;
public partial class RegionalAndGlobal : ChartablePage
{
    private bool finishedSetup;

    public RegionalAndGlobal()
    {
        PageName = "regionalandglobal";
    }

    protected override string PageTitle
    {
        get
        {
            return $"Regional and global long-term climate trends";
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
        }

        if (!finishedSetup)
        {
            finishedSetup = true;
            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (csd)
            {
                await ChartView!.UpdateUiStateBasedOnQueryString();
            }

            StateHasChanged();
        }
    }
}
