
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ClimateExplorer.Visualiser.Pages;
public partial class RegionalAndGlobal : IDisposable
{
    [Inject] IDataService DataService { get; set; }
    [Inject] ILogger<RegionalAndGlobal> Logger { get; set; }

    IEnumerable<DataSetDefinitionViewModel> DataSetDefinitions { get; set; }

    ChartView chartView;
    Modal addDataSetModal { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (DataService == null)
        {
            throw new NullReferenceException(nameof(DataService));
        }
        DataSetDefinitions = (await DataService.GetDataSetDefinitions()).ToList();
    }

    public void Dispose()
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " disposing");
        //   NavManager.LocationChanged -= HandleLocationChanged;
    }
    string GetPageTitle()
    {
        //var locationText = SelectedLocation == null ? "" : " - " + SelectedLocation.Name;

        string title = $"ClimateExplorer";// {locationText}";

        Logger.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }
}
