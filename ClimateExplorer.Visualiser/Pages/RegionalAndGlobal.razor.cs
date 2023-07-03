using Blazorise;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Services;
using ClimateExplorer.Visualiser.Shared;
using ClimateExplorer.Visualiser.UiLogic;
using ClimateExplorer.Visualiser.UiModel;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ClimateExplorer.Visualiser.Pages;
public partial class RegionalAndGlobal : ChartablePage
{
    public RegionalAndGlobal()
    {
        baseUrl = "/regionalandglobal";
    }
    
    Modal addDataSetModal { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (base.DataService == null)
        {
            throw new NullReferenceException(nameof(base.DataService));
        }
        DataSetDefinitions = (await base.DataService.GetDataSetDefinitions()).ToList();
    }


    public void Dispose()
    {
        //Logger.LogInformation("Instance " + _componentInstanceId + " disposing");
        //   NavManager.LocationChanged -= HandleLocationChanged;
    }

    async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        await base.chartView.OnAddDataSet(dle, DataSetDefinitions);
    }

    async Task OnChartPresetSelected(List<ChartSeriesDefinition> chartSeriesDefinitions)
    {
        await base.chartView.OnChartPresetSelected(chartSeriesDefinitions);
    }

    string GetPageTitle()
    {
        //var locationText = SelectedLocation == null ? "" : " - " + SelectedLocation.Name;

        string title = $"ClimateExplorer";// {locationText}";

        base.Logger.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    private Task ShowAddDataSetModal()
    {
        return addDataSetModal.Show();
    }

    protected async Task OnDownloadDataClicked(DataDownloadPackage dataDownloadPackage)
    {
        var fileStream = Exporter.ExportChartData(Logger, new List<Location>(), dataDownloadPackage, NavManager.Uri.ToString());

        var locationNames = dataDownloadPackage.ChartSeriesWithData.SelectMany(x => x.ChartSeries.SourceSeriesSpecifications).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : "";

        fileName = $"Export-{fileName}-{dataDownloadPackage.BinGranularity}-{dataDownloadPackage.Bins.First().Label}-{dataDownloadPackage.Bins.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await base.JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }
}
