namespace ClimateExplorer.Web.Client.Pages;

using Blazorise;
using Blazorise.Snackbar;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Shared;
using ClimateExplorer.Web.Services;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

public abstract partial class ChartablePage : ComponentBase, IDisposable
{
    private readonly Guid componentInstanceId = Guid.NewGuid();

    [Inject]
    protected IDataService? DataService { get; set; }

    [Inject]
    protected NavigationManager? NavManager { get; set; }

    [Inject]
    protected IExporter? Exporter { get; set; }

    [Inject]
    protected IJSRuntime? JsRuntime { get; set; }

    [Inject]
    protected ILogger<ChartablePage>? Logger { get; set; }

    protected SnackbarStack? Snackbar { get; set; }

    protected IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    protected ChartView? ChartView { get; set; }

    protected string? PageName { get; set; }

    protected IEnumerable<Location>? Locations { get; set; }
    protected IEnumerable<Region>? Regions { get; set; }
    protected IEnumerable<GeographicalEntity>? GeographicalEntities { get; set; }

    protected Modal? AddDataSetModal { get; set; }
    protected virtual string? PageTitle { get; }
    protected virtual string? PageUrl { get; }

    public void Dispose()
    {
        Logger!.LogInformation("Instance " + componentInstanceId + " disposing");
        NavManager!.LocationChanged -= HandleNavigationLocationChanged!;
    }

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

        ChartView!.LoadingChart();

        // Recalculate the URL
        string chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(ChartView.ChartSeriesList!);

        string url = PageName!;

        if (chartSeriesUrlComponent.Length > 0)
        {
            var chartAllData = ChartView.ChartAllData.ToString() !.ToLower();
            var startYear = ChartView.SelectedStartYear;
            var endYear = ChartView.SelectedEndYear;

            var queryString = new Uri(NavManager!.Uri).Query;
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(queryString);

            url += "?chartAllData=" + chartAllData;
            if (!string.IsNullOrWhiteSpace(startYear))
            {
                url += $"&startYear={startYear}";
            }

            if (!string.IsNullOrWhiteSpace(endYear))
            {
                url += $"&endYear={endYear}";
            }

            var groupingDays = ChartView.SelectedGroupingDays;
            if (ChartView.SelectedGroupingDays > 0)
            {
                url += $"&groupingDays={groupingDays}";
            }

            var groupingThresholdText = ChartView.GroupingThresholdText;
            if (!string.IsNullOrWhiteSpace(groupingThresholdText))
            {
                url += $"&groupingThreshold={groupingThresholdText}";
            }

            url += "&csd=" + chartSeriesUrlComponent;
        }
        else
        {
            ChartView.ChartSeriesWithData = null;
            await ChartView.HandleRedraw();
        }

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

            var usableChartSeries = ChartView.ChartSeriesList!.Where(x => x.DataAvailable);

            // Fetch the data required to render the selected data series
            ChartView.ChartSeriesWithData = await ChartView.RetrieveDataSets(usableChartSeries);

            l.LogInformation("Set ChartSeriesWithData after call to RetrieveDataSets(). ChartSeriesWithData now has " + usableChartSeries.Count() + " entries.");

            // Render the series
            await ChartView.HandleRedraw();

            await UpdateComponents();
        }

        l.LogInformation("Leaving");
    }

    protected abstract Task UpdateComponents();

    protected async Task NavigateTo(string uri, bool replace = false)
    {
        Logger!.LogInformation("NavManager.NavigateTo(uri=" + uri + ", replace=" + replace + ")");

        NavManager!.NavigateTo(uri, false, replace);
    }

    protected async Task UpdateUiStateBasedOnQueryString()
    {
        await UpdateUiStateBasedOnQueryString(true);
    }

    protected async Task UpdateUiStateBasedOnQueryString(bool stateChanged)
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return;
        }

        var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);

        ChartView!.ChartAllData = queryDictionary["chartAllData"] == null ? false : bool.Parse(queryDictionary["chartAllData"] !);
        ChartView!.SelectedStartYear = queryDictionary["startYear"];
        ChartView!.SelectedEndYear = queryDictionary["endYear"];
        ChartView!.SelectedGroupingDays = queryDictionary["groupingDays"] == null ? (short)14 : short.Parse(queryDictionary["groupingDays"] !);
        ChartView!.GroupingThresholdText = string.IsNullOrWhiteSpace(queryDictionary["groupingThreshold"]) ? "70" : queryDictionary["groupingThreshold"];

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
        {
            try
            {
                var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger!, csdSpecifier!, DataSetDefinitions!, GeographicalEntities!);

                if (csdList.Any())
                {
                    ChartView!.SelectedBinGranularity = csdList.First().BinGranularity;
                }

                Logger!.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

                ChartView!.ChartSeriesList = csdList.ToList();

                try
                {
                    await BuildDataSets();
                }
                catch (Exception)
                {
                    await Snackbar!.PushAsync($"Failed to create the chart with the current settings", SnackbarColor.Danger);
                    ChartView!.ChartLoadingErrored = true;
                    await ChartView!.HandleRedraw();
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
        return AddDataSetModal!.Show();
    }

    protected async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        await ChartView!.OnAddDataSet(dle, DataSetDefinitions!);
    }

    protected async Task OnChartPresetSelected(SuggestedChartPresetModel chartPresetModel)
    {
        await ChartView!.OnChartPresetSelected(chartPresetModel);
    }

    protected async Task OnDownloadDataClicked(DataDownloadPackage dataDownloadPackage)
    {
        var fileStream = Exporter!.ExportChartData(Logger!, GeographicalEntities!, dataDownloadPackage, NavManager!.Uri.ToString());

        var locationNames = dataDownloadPackage.ChartSeriesWithData!.SelectMany(x => x.ChartSeries!.SourceSeriesSpecifications!).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : string.Empty;

        fileName = $"Export-{fileName}-{dataDownloadPackage.BinGranularity}-{dataDownloadPackage.Bins!.First().Label}-{dataDownloadPackage.Bins!.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JsRuntime!.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }

    private void HandleNavigationLocationChanged(object sender, LocationChangedEventArgs e)
    {
        Logger!.LogInformation("Instance " + componentInstanceId + " HandleLocationChanged: " + NavManager!.Uri);

        // The URL changed. Update UI state to reflect what's in the URL.
        InvokeAsync(UpdateUiStateBasedOnQueryString);
    }
}