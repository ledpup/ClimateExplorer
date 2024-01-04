using Blazorise;
using Blazorise.Snackbar;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Shared;
using ClimateExplorer.Web.Services;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace ClimateExplorer.Web.Client.Pages;

public abstract partial class ChartablePage : ComponentBase, IDisposable
{
    [Inject] protected IDataService? DataService { get; set; }
    [Inject] protected NavigationManager? NavManager { get; set; }
    [Inject] protected IExporter? Exporter { get; set; }
    [Inject] protected IJSRuntime? JsRuntime { get; set; }
    [Inject] protected ILogger<ChartablePage>? Logger { get; set; }

    protected IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    protected ChartView? chartView;

    protected string? pageName;

    Guid _componentInstanceId = Guid.NewGuid();

    protected IEnumerable<Location>? Locations { get; set; }
    protected IEnumerable<Region>? Regions { get; set; }
    protected IEnumerable<GeographicalEntity>? GeographicalEntities { get; set; }

    protected Modal? addDataSetModal { get; set; }

    protected SnackbarStack? snackbar;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            NavManager!.LocationChanged += HandleNavigationLocationChanged!;

            if (DataService == null)
            {
                throw new NullReferenceException(nameof(DataService));
            }
            DataSetDefinitions = (await DataService.GetDataSetDefinitions()).ToList();
        }
    }

    protected async Task BuildDataSets()
    {
        // This method is called whenever anything has occurred that may require the chart to
        // be re-rendered.
        //
        // Examples:
        //     - User navigates to /locations/{anything}?csd={anythingelse} page for the first time
        //     - User updates URL manually while already at /locations
        //     - User chooses a preset or otherwise updates ChartSeriesList
        //     - User changes another setting that influences chart rendering (e.g. year filtering)
        //
        // Some, but not all, of those changes/events are reflected directly in the URL (e.g. location is in the
        // URL, and CSDs are in the URL).
        //
        // Others currently are not, but probably should be (e.g. year filtering).
        //
        // Our strategy here is:
        //
        // This method has been called because something has happened that may require the chart to be
        // re-rendered. We calculate the URI reflecting the current UI state. If we're already at that
        // URI, then we conclude that one of the properties has changed that does NOT impact the URI,
        // so we just immediately re-render the chart. If we are NOT already at that URI, then we just
        // trigger navigation to that URI, and DO NOT RE-RENDER THE CHART YET. Instead, as part of that
        // navigation process, methods will trigger that will re render the chart based on what's in the
        // updated URI.
        //
        // This is all to avoid re-rendering the chart more than once (bad for performance) or, even worse,
        // re-rendering the chart on two different async call chains at the same time (bad for correctness -
        // this was leading to the same series being rendered more than once, and the year labels on the
        // X axis being added more than once).

        var l = new LogAugmenter(Logger!, "BuildDataSets");
        l.LogInformation("Starting");

        chartView!.ChartLoadingIndicatorVisible = true;
        chartView!.ChartLoadingErrored = false;
        chartView.LogChartSeriesList();

        // Recalculate the URL
        string chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(chartView.ChartSeriesList!);

        string url = pageName!;

        if (chartSeriesUrlComponent.Length > 0) url += "?csd=" + chartSeriesUrlComponent;

        string currentUri = NavManager!.Uri;
        string newUri = NavManager.ToAbsoluteUri(url).ToString();

        if (currentUri != newUri)
        {
            l.LogInformation("Because the URI reflecting current UI state is different to the URI we're currently at, triggering navigation. After navigation occurs, the UI state will update accordingly.");

            bool shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds = currentUri.IndexOf("csd=") == -1;

            // Just let the navigation process trigger the UI updates
            await NavigateTo(url, shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds);
        }
        else
        {
            l.LogInformation("Not calling NavigationManager.NavigateTo().");

            var usableChartSeries = chartView.ChartSeriesList!.Where(x => x.DataAvailable);

            // Fetch the data required to render the selected data series
            chartView.ChartSeriesWithData = await chartView.RetrieveDataSets(usableChartSeries);

            l.LogInformation("Set ChartSeriesWithData after call to RetrieveDataSets(). ChartSeriesWithData now has " + usableChartSeries.Count() + " entries.");

            // Render the series
            await chartView.HandleRedraw();

            await UpdateComponents();
        }

        l.LogInformation("Leaving");
    }

    public void Dispose()
    {
        Logger!.LogInformation("Instance " + _componentInstanceId + " disposing");
        NavManager!.LocationChanged -= HandleNavigationLocationChanged!;
    }

    protected abstract Task UpdateComponents();

    protected async Task NavigateTo(string uri, bool replace = false)
    {
        Logger!.LogInformation("NavManager.NavigateTo(uri=" + uri + ", replace=" + replace + ")");

        // Below is a JavaScript hack to stop NavigateTo from scrolling to the top of the page.
        // See: https://github.com/dotnet/aspnetcore/issues/40190 and index.html
        await JsRuntime!.InvokeVoidAsync("willSkipScrollTo", true);

        NavManager!.NavigateTo(uri, false, replace);
    }

    void HandleNavigationLocationChanged(object sender, LocationChangedEventArgs e)
    {
        Logger!.LogInformation("Instance " + _componentInstanceId + " HandleLocationChanged: " + NavManager!.Uri);

        // The URL changed. Update UI state to reflect what's in the URL.
        InvokeAsync(UpdateUiStateBasedOnQueryString);
    }

    protected async Task UpdateUiStateBasedOnQueryString()
    {
        await UpdateUiStateBasedOnQueryString(true);
    }

    protected async Task UpdateUiStateBasedOnQueryString(bool stateChanged)
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
        {
            try
            {
                var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger!, csdSpecifier!, DataSetDefinitions!, GeographicalEntities!);

                if (csdList.Any())
                {
                    chartView!.SelectedBinGranularity = csdList.First().BinGranularity;
                }

                Logger!.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

                chartView!.ChartSeriesList = csdList.ToList();

                try
                {
                    await BuildDataSets();
                }
                catch (Exception)
                {
                    await snackbar!.PushAsync($"Failed to create the chart with the current settings", SnackbarColor.Danger);
                    chartView!.ChartLoadingErrored = true;
                    await chartView!.HandleRedraw();
                }

                if (stateChanged)
                {
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger!.LogError(ex.ToString());
            }
        }
    }

    protected Task ShowAddDataSetModal()
    {
        return addDataSetModal!.Show();
    }

    protected async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        await chartView!.OnAddDataSet(dle, DataSetDefinitions!);
    }

    protected async Task OnChartPresetSelected(List<ChartSeriesDefinition> chartSeriesDefinitions)
    {
        await chartView!.OnChartPresetSelected(chartSeriesDefinitions);
    }

    protected async Task OnDownloadDataClicked(DataDownloadPackage dataDownloadPackage)
    {
        var fileStream = Exporter!.ExportChartData(Logger!, GeographicalEntities!, dataDownloadPackage, NavManager!.Uri.ToString());

        var locationNames = dataDownloadPackage.ChartSeriesWithData!.SelectMany(x => x.ChartSeries!.SourceSeriesSpecifications!).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : "";

        fileName = $"Export-{fileName}-{dataDownloadPackage.BinGranularity}-{dataDownloadPackage.Bins!.First().Label}-{dataDownloadPackage.Bins!.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JsRuntime!.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }
}
