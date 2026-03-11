namespace ClimateExplorer.Web.Client.Pages;

using Blazorise;
using Blazorise.Snackbar;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Shared;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Services;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Primitives;
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

    protected IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    protected Dictionary<Guid, Location>? LocationDictionary { get; set; }

    protected IEnumerable<Region>? Regions { get; set; }

    protected SnackbarStack? Snackbar { get; set; }

    protected ChartView? ChartView { get; set; }

    protected string? PageName { get; set; }

    protected Modal? AddDataSetModal { get; set; }
    protected virtual string? PageTitle { get; }
    protected virtual string? PageUrl { get; }

    public virtual void Dispose()
    {
        Logger!.LogInformation("Instance " + componentInstanceId + " disposing");
        NavManager!.LocationChanged -= HandleNavigationLocationChanged!;
    }

    protected override async Task OnInitializedAsync()
    {
        NavManager!.LocationChanged += HandleNavigationLocationChanged!;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (DataSetDefinitions is null)
        {
            DataSetDefinitions = (await DataService!.GetDataSetDefinitions()).ToList();
        }
    }

    protected async Task NavigateTo(string uri, bool replace = false)
    {
        Logger!.LogInformation("NavManager.NavigateTo(uri=" + uri + ", replace=" + replace + ")");

        NavManager!.NavigateTo(uri, false, replace);
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
        var geographicalEntities = new List<GeographicalEntity>();
        geographicalEntities.AddRange(LocationDictionary!.Values);
        geographicalEntities.AddRange(Regions!);

        var fileStream = Exporter!.ExportChartData(Logger!, geographicalEntities, dataDownloadPackage, NavManager!.Uri.ToString());

        var locationNames = dataDownloadPackage.ChartSeriesWithData!.SelectMany(x => x.ChartSeries!.SourceSeriesSpecifications!).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : string.Empty;

        fileName = $"Export-{fileName}-{dataDownloadPackage.BinGranularity}-{dataDownloadPackage.Bins!.First().Label}-{dataDownloadPackage.Bins!.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JsRuntime!.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }

    protected async Task SnackbarMessageEventHandler(SnackbarMessage snackbarMessage)
    {
        await Snackbar!.PushAsync(snackbarMessage.Message, snackbarMessage.Type);
    }

    protected Guid GetLocationFromCsd(StringValues csdSpecifier)
    {
        if (DataSetDefinitions is null || LocationDictionary is null || Regions is null)
        {
            throw new InvalidOperationException("DataSetDefinitions, LocationDictionary, and Regions must be loaded before calling GetLocationFromCsd.");
        }

        var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger!, csdSpecifier!, DataSetDefinitions, LocationDictionary, Regions);
        var chartSeriesList = csdList.ToList();
        return chartSeriesList!.First().SourceSeriesSpecifications!.First().LocationId;
    }

    private void HandleNavigationLocationChanged(object sender, LocationChangedEventArgs e)
    {
        Logger!.LogInformation("Instance " + componentInstanceId + " HandleLocationChanged: " + NavManager!.Uri);
    }
}