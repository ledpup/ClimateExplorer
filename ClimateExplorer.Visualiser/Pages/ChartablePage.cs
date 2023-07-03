using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Services;
using ClimateExplorer.Visualiser.Shared;
using ClimateExplorer.Visualiser.UiLogic;
using ClimateExplorer.Visualiser.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ClimateExplorer.Visualiser.Pages;

public abstract partial class ChartablePage : ComponentBase
{
    [Inject] protected IDataService DataService { get; set; }
    [Inject] protected NavigationManager NavManager { get; set; }
    [Inject] protected IExporter Exporter { get; set; }
    [Inject] protected IJSRuntime JsRuntime { get; set; }
    [Inject] protected ILogger<ChartablePage> Logger { get; set; }

    protected IEnumerable<DataSetDefinitionViewModel> DataSetDefinitions { get; set; }

    protected ChartView chartView;

    protected string baseUrl = "location";

    //protected override async Task OnInitializedAsync()
    //{
    //    if (DataService == null)
    //    {
    //        throw new NullReferenceException(nameof(DataService));
    //    }
    //    DataSetDefinitions = (await DataService.GetDataSetDefinitions()).ToList();
    //}

    protected async Task<bool> BuildDataSets()
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

        var l = new LogAugmenter(Logger, "BuildDataSets");
        l.LogInformation("starting");

        chartView.LogChartSeriesList();

        // Recalculate the URL
        string chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(chartView.ChartSeriesList);

        string url = baseUrl;

        if (chartSeriesUrlComponent.Length > 0) url += "?csd=" + chartSeriesUrlComponent;

        string currentUri = NavManager.Uri;
        string newUri = NavManager.ToAbsoluteUri(url).ToString();

        var updateViews = false;

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

            // Fetch the data required to render the selected data series
            chartView.ChartSeriesWithData = await chartView.RetrieveDataSets(chartView.ChartSeriesList);

            l.LogInformation("Set ChartSeriesWithData after call to RetrieveDataSets(). ChartSeriesWithData now has " + chartView.ChartSeriesWithData.Count + " entries.");

            // Render the series
            await chartView.HandleRedraw();

            updateViews = true;
        }

        l.LogInformation("leaving");

        return updateViews;
    }

    protected async Task NavigateTo(string uri, bool replace = false)
    {
        Logger.LogInformation("NavManager.NavigateTo(uri=" + uri + ", replace=" + replace + ")");

        // Below is a JavaScript hack to stop NavigateTo from scrolling to the top of the page.
        // See: https://github.com/dotnet/aspnetcore/issues/40190 and index.html
        await JsRuntime.InvokeVoidAsync("willSkipScrollTo", true);

        NavManager.NavigateTo(uri, false, replace);
    }

    //protected async Task UpdateUiStateBasedOnQueryString()
    //{
    //    var uri = NavManager.ToAbsoluteUri(NavManager.Uri);

    //    if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
    //    {
    //        try
    //        {
    //            var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger, csdSpecifier, DataSetDefinitions, Locations);

    //            if (csdList.Any())
    //            {
    //                chartView.SelectedBinGranularity = csdList.First().BinGranularity;
    //            }

    //            Logger.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

    //            chartView.ChartSeriesList = csdList.ToList();

    //            await BuildDataSets();

    //            StateHasChanged();
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.LogError(ex.ToString());
    //        }
    //    }
    //}
}
