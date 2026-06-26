namespace ClimateExplorer.Web.Client.Pages;

using Blazorise;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.Services.Notifications;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using CurrentDevice;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using UserNotificationType = ClimateExplorer.Web.Client.UiModel.NotificationType;

public abstract partial class ChartablePage : ComponentBase, IDisposable
{
    private readonly Guid componentInstanceId = Guid.NewGuid();
    private bool initialChartStateResolved;
    private long chartBuildVersion;

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

    [Inject]
    protected ICurrentDeviceService? CurrentDeviceService { get; set; }

    [Inject]
    protected IChartStateUrlService? ChartStateUrlService { get; set; }

    [Inject]
    protected IChartDataBuilder? ChartDataBuilder { get; set; }

    [Inject]
    protected IUserNotificationService? NotificationService { get; set; }

    protected IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    protected Dictionary<Guid, Location>? LocationDictionary { get; set; }

    protected IEnumerable<Region>? Regions { get; set; }

    protected ChartState? CurrentChartState { get; set; }

    protected ChartDataBuildResult? CurrentChartData { get; set; }

    protected bool ChartDataLoading { get; set; } = true;

    protected bool ChartDataLoadingErrored { get; set; }

    protected string? PageName { get; set; }

    protected Modal? AddDataSetModal { get; set; }

    protected bool? IsMobileDevice { get; private set; }

    protected virtual string? PageTitle { get; }
    protected virtual string? PageDescription { get; }
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
        DataSetDefinitions ??= [.. await DataService!.GetDataSetDefinitions()];

        if (IsMobileDevice is null)
        {
            IsMobileDevice ??= await CurrentDeviceService!.Mobile();
            StateHasChanged();
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

    protected async Task OnChartStateChanged(ChartState state)
    {
        await ApplyChartStateAsync(state);
    }

    protected async Task ApplyChartStateAsync(ChartState state, bool updateUrl = true)
    {
        // The chart raised a state change. Reflect the current chart state in the URL bar so the chart is
        // shareable/bookmarkable. We do NOT rely on this navigation re-triggering a render; ChartView renders
        // directly. Same-path replace:true navigation is unreliable in deployed Blazor Server/WASM.
        CurrentChartState = state;
        CurrentChartData = null;
        ChartDataLoading = true;
        ChartDataLoadingErrored = false;
        var buildVersion = ++chartBuildVersion;

        if (updateUrl)
        {
            ReflectChartStateInUrl(state);
        }

        StateHasChanged();

        try
        {
            var buildResult = await ChartDataBuilder!.BuildAsync(state);

            if (buildVersion != chartBuildVersion)
            {
                return;
            }

            CurrentChartData = buildResult;

            foreach (var message in buildResult.Messages)
            {
                AddNotification(message);
            }
        }
        catch (Exception ex)
        {
            if (buildVersion != chartBuildVersion)
            {
                return;
            }

            Logger!.LogError(ex, "Failed to build chart datasets.");
            ChartDataLoadingErrored = true;
            CurrentChartData = new ChartDataBuildResult();
            AddNotification(new UserNotification { Message = "Failed to create the chart with the current settings.", Type = UserNotificationType.Error });
        }
        finally
        {
            if (buildVersion == chartBuildVersion)
            {
                ChartDataLoading = false;
                StateHasChanged();
            }
        }
    }

    protected void ReflectChartStateInUrl(ChartState state)
    {
        var url = ChartStateUrlService!.BuildRelativeUrl(PageName!, state);
        var currentUri = NavManager!.Uri;
        var newUri = NavManager.BaseUri + url;

        if (currentUri != newUri)
        {
            Logger!.LogInformation("Because the URI reflecting current UI state is different to the URI we're currently at, updating the URL bar.");

            var shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds = currentUri.IndexOf("csd=") == -1;
            NavManager.NavigateTo(url, false, shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds);
        }
    }

    protected async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        Logger!.LogInformation("Adding dle " + dle.Name);

        var state = CurrentChartState ?? new ChartState();
        List<ChartSeriesDefinition> series =
            [
                .. state.Series,
                new ChartSeriesDefinition
                {
                    SeriesDerivationType = dle.SeriesDerivationType,
                    SourceSeriesSpecifications = dle.SourceSeriesSpecifications!.Select(x => BuildSourceSeriesSpecification(x, DataSetDefinitions!)).ToArray(),
                    Aggregation = dle.SeriesAggregation,
                    BinGranularity = GetSelectedBinGranularity(state),
                    Smoothing = SeriesSmoothingOptions.None,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null,
                },
            ];

        await ApplyChartStateAsync(state with { Series = series });
    }

    protected async Task OnChartPresetSelected(SuggestedChartPresetModel chartPresetModel)
    {
        if (chartPresetModel.ChartSeriesList == null || !chartPresetModel.ChartSeriesList.Any())
        {
            AddNotification(new UserNotification { Message = $"No data available for the preset '<b>{chartPresetModel.Title}</b>'.", Type = UserNotificationType.Warning });
            return;
        }

        var state = CurrentChartState ?? new ChartState();
        await ApplyChartStateAsync(
            state with
            {
                ChartAllData = chartPresetModel.ChartAllData,
                StartYear = chartPresetModel.StartYear?.ToString(),
                EndYear = chartPresetModel.EndYear?.ToString(),
                Series = chartPresetModel.ChartSeriesList,
            });
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

    protected void AddNotification(UserNotification notification)
    {
        NotificationService!.Add(notification);
    }

    protected async Task<bool> EnsureInitialChartStateAsync(Location? location, Func<ChartState> createDefaultState)
    {
        if (initialChartStateResolved || DataSetDefinitions is null || Regions is null)
        {
            return false;
        }

        if (IsMobileDevice is null)
        {
            IsMobileDevice = await CurrentDeviceService!.Mobile();
        }

        var context = CreateChartUrlStateContext(location);
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        var result = ChartStateUrlService!.Parse(uri, context);

        var initialState = result.Kind switch
        {
            ChartUrlStateKind.Missing => createDefaultState(),
            ChartUrlStateKind.Valid => result.State,
            ChartUrlStateKind.ExplicitEmpty => result.State,
            ChartUrlStateKind.Invalid => null,
            _ => throw new NotImplementedException($"Chart URL state kind {result.Kind}"),
        };

        initialChartStateResolved = true;

        if (result.Kind == ChartUrlStateKind.Invalid)
        {
            Logger!.LogError("Failed to initialize chart state from URL: {ErrorMessage}", result.ErrorMessage);
        }

        if (initialState is null)
        {
            ChartDataLoading = false;
            return false;
        }

        await ApplyChartStateAsync(initialState);

        return true;
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

    private ChartUrlStateContext CreateChartUrlStateContext(Location? location)
    {
        return new ChartUrlStateContext
        {
            Locations = GetKnownLocationDictionary(location),
            Regions = Regions!.ToList(),
            DataSetDefinitions = DataSetDefinitions!.ToList(),
        };
    }

    private Dictionary<Guid, Location>? GetKnownLocationDictionary(Location? location)
    {
        if (LocationDictionary is not null)
        {
            return LocationDictionary;
        }

        return location is null
            ? null
            : new Dictionary<Guid, Location> { [location.Id] = location };
    }

    private void HandleNavigationLocationChanged(object sender, LocationChangedEventArgs e)
    {
        Logger!.LogInformation("Instance " + componentInstanceId + " HandleLocationChanged: " + NavManager!.Uri);
    }

    private BinGranularities GetSelectedBinGranularity(ChartState state)
    {
        return state.Series.FirstOrDefault()?.BinGranularity ?? BinGranularities.ByYear;
    }

    private SourceSeriesSpecification BuildSourceSeriesSpecification(DataSetLibraryEntry.SourceSeriesSpecification sss, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        var dsd = dataSetDefinitions.Single(x => x.Id == sss.SourceDataSetId);
        var md = dsd.MeasurementDefinitions!.Single(x => x.DataType == sss.DataType && x.DataAdjustment == sss.DataAdjustment);

        return new SourceSeriesSpecification
        {
            LocationId = sss.LocationId,
            LocationName = sss.LocationName!,
            DataSetDefinition = dsd,
            MeasurementDefinition = md,
        };
    }
}
