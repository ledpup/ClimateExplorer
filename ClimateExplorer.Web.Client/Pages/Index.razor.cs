namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Components.Location;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class Index : ChartablePage
{
    private const string DefaultLocationId = "aed87aa0-1d0c-44aa-8561-cde0fc936395";

    private Collapsible? suggestedChartsCollapsible;
    private InfoPanel? siteOverviewPanel;
    private LocationDashboard? locationInfoComponent;

    public Index()
    {
        PageName = "location";
    }

    [Parameter]
    public string? LocationString { get; set; }

    protected override string PageTitle
    {
        get
        {
            var title = Location == null ? $"ClimateExplorer - Local long-term climate trends" : $"ClimateExplorer - {Location.FullTitle}";

            return title;
        }
    }

    protected override string PageDescription
    {
        get
        {
            return Location == null
                ? "ClimateExplorer helps you understand climate change trends where you live. Explore long-term temperature and precipitation data for thousands of locations worldwide."
                : $"Explore long-term climate trends for {Location.FullTitle}. View temperature records, warming anomaly, heating score, and climate stripes.";
        }
    }

    protected override string PageUrl
    {
        get
        {
            return Location == null ? $"https://climateexplorer.net" : $"https://climateexplorer.net/location/{Location.UrlReadyName()}";
        }
    }

    [Inject]
    private Blazored.LocalStorage.ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    private ISiteOverviewService? SiteOverviewService { get; set; }

    [Inject]
    private IChartSeriesLocationSubstitutionService? ChartSeriesLocationSubstitutionService { get; set; }

    private ChangeLocation? ChangeLocationModal { get; set; }
    private string? BrowserLocationErrorMessage { get; set; }
    private Location? Location { get; set; }

    private Location? PreviousLocation { get; set; }

    private IEnumerable<Location>? MapLocations
    {
        get
        {
            if (LocationDictionary is not null)
            {
                return LocationDictionary.Values;
            }

            return Location is null ? null : [Location];
        }
    }

    private CancellationTokenSource? DeferredLocationDictionaryLoadCts { get; set; }

    private Task? LocationDictionaryLoadTask { get; set; }

    public override void Dispose()
    {
        SiteOverviewService!.ShowRequested -= HandleShowRequested;
        DeferredLocationDictionaryLoadCts?.Cancel();
        DeferredLocationDictionaryLoadCts?.Dispose();
        base.Dispose();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Resolving the route's location lives here (not OnInitializedAsync) so it runs both on
        // first load AND whenever the router reuses this component across navigations -
        // OnInitializedAsync only runs once per instance, which is why named-URL navigation
        // used to silently fail to update. OnParametersSetAsync is awaited during prerendering,
        // so a location-bearing route is resolved before the prerendered HTML (and therefore
        // the SEO-relevant title/meta/canonical) is produced.
        await ResolveLocationAsync();

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            SiteOverviewService!.ShowRequested += HandleShowRequested;
        }

        var shouldRenderAgain = false;

        if (DataSetDefinitions is null)
        {
            var dataSetDefinitionsTask = DataService!.GetDataSetDefinitions();
            var regionsTask = DataService!.GetRegions();

            await Task.WhenAll(dataSetDefinitionsTask, regionsTask);

            DataSetDefinitions = [.. await dataSetDefinitionsTask];
            Regions = [.. await regionsTask];

            // Resolve the location only when the route itself names none (the home page, or a
            // "?csd=..." deep link). This path needs JS (LocalStorage) and/or the dictionary,
            // neither available while prerendering - and OnAfterRenderAsync never runs during
            // prerendering. That's fine: the home page's canonical URL is location-independent.
            //
            // The LocationString guard is essential. When the route DOES name a location,
            // OnParametersSetAsync owns the resolution and may still be in flight (Location
            // transiently null while GetLocationByPath awaits). Falling back here on "Location is
            // null" alone would race that resolution and load the stale last-viewed location -
            // that's the flip-to-previous-location bug.
            if (Location is null && string.IsNullOrEmpty(LocationString?.TrimEnd('/')))
            {
                Location = await GetLocation();

                if (Location is not null)
                {
                    LocationString = Location.Id.ToString();
                    await PersistLastLocationAsync(Location.Id);

                    var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
                    var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out _);
                    if (!csd)
                    {
                        await NavigateTo($"/{PageName}/{Location.Id}");
                    }
                }
            }

            // DataSetDefinitions and Regions are settled now. Location may arrive from an
            // async route lookup on a later render, so chart initialization is retried below.
            shouldRenderAgain = true;
        }

        if (Location is not null && await EnsureInitialChartStateAsync(Location, CreateDefaultLocationChartState))
        {
            shouldRenderAgain = true;
        }

        if (LocationDictionary is null && Location is not null)
        {
            StartDeferredLocationDictionaryLoad();
        }

        await base.OnAfterRenderAsync(firstRender);

        if (shouldRenderAgain)
        {
            StateHasChanged();
        }
    }

    // Single entry point for turning the current route into the displayed Location. Idempotent:
    // it no-ops when the route already matches the current Location, so the re-runs caused by the
    // prerender->interactive handoff and by the canonical-URL redirect are harmless.
    private async Task ResolveLocationAsync()
    {
        var segment = LocationString?.TrimEnd('/');

        // No location in the route (home page or "?csd=..."). That case depends on JS / the
        // dictionary, so it's handled after the first interactive render in OnAfterRenderAsync.
        if (string.IsNullOrEmpty(segment))
        {
            return;
        }

        if (Guid.TryParse(segment, out var locationGuid))
        {
            if (Location?.Id == locationGuid)
            {
                return;
            }

            var location = LocationDictionary is not null && LocationDictionary.TryGetValue(locationGuid, out var known)
                ? known
                : await DataService!.GetLocationById(locationGuid);

            // Navigation may have moved on while the lookup was in flight; discard stale results.
            if (LocationString?.TrimEnd('/') != segment)
            {
                return;
            }

            await ApplyResolvedLocationAsync(location);
        }
        else
        {
            var name = segment.ToLower();

            if (Location is not null && Location.UrlReadyName() == name)
            {
                return;
            }

            // Prefer the in-memory dictionary when it's loaded (free); otherwise use the
            // dedicated name-lookup endpoint rather than forcing the deferred bulk load.
            var location = LocationDictionary?.Values.FirstOrDefault(x => x.UrlReadyName() == name)
                ?? await DataService!.GetLocationByPath(name);

            if (LocationString?.TrimEnd('/').ToLower() != name)
            {
                return;
            }

            await ApplyResolvedLocationAsync(location);
        }
    }

    private async Task ApplyResolvedLocationAsync(Location? location)
    {
        if (location is null)
        {
            // Genuine not-found. Only redirect once interactive; during prerendering we leave
            // Location null and let the interactive pass make the call, rather than emitting a
            // redirect into the prerendered response.
            if (RendererInfo.IsInteractive)
            {
                NavManager!.NavigateTo("/error", true);
            }

            return;
        }

        if (location.Id != Location?.Id)
        {
            PreviousLocation = Location;
            Location = location;
        }

        // Deliberately NOT rewriting a name URL to its GUID form. The name URL is the canonical
        // one (see PageUrl / <link rel="canonical">), so leaving it in place is correct for SEO -
        // and, crucially, redirecting here would be a re-entrant navigation while OnParametersSet
        // is still resolving, which caused the URL to thrash and Location to be lost on SPA
        // navigations (e.g. clicking a location on the /locations page).
        if (RendererInfo.IsInteractive)
        {
            await PersistLastLocationAsync(location.Id);
        }
    }

    private async Task PersistLastLocationAsync(Guid locationId)
    {
        try
        {
            await LocalStorage!.SetItemAsync("lastLocationId", locationId.ToString());
        }
        catch (JSDisconnectedException)
        {
            // The circuit was torn down (e.g. InteractiveAuto handing the component off from
            // Server to WebAssembly). Persisting the last location is best-effort, so ignore it.
        }
    }

    private async Task<string?> ReadLastLocationAsync()
    {
        try
        {
            return await LocalStorage!.GetItemAsync<string>("lastLocationId");
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    private async Task<Location?> GetLocation()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        var locationString = await ReadLastLocationAsync();
        var validGuid = Guid.TryParse(locationString, out Guid guid);
        Guid? locationId = null;

        if (validGuid && guid != Guid.Empty)
        {
            locationId = guid;
        }

        var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        if (csd)
        {
            await EnsureLocationDictionaryLoadedAsync();
            locationId = GetLocationFromCsd(csdSpecifier);
        }

        if (locationId is not null)
        {
            var location = await DataService!.GetLocationById(locationId.Value);
            if (location is not null)
            {
                return location;
            }
        }

        return await DataService!.GetLocationById(Guid.Parse(DefaultLocationId));
    }

    private void StartDeferredLocationDictionaryLoad()
    {
        if (LocationDictionary is not null || DeferredLocationDictionaryLoadCts is not null)
        {
            return;
        }

        DeferredLocationDictionaryLoadCts = new CancellationTokenSource();
        _ = LoadLocationDictionaryAfterDelayAsync(DeferredLocationDictionaryLoadCts.Token);
    }

    private async Task LoadLocationDictionaryAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            await InvokeAsync(
                async () =>
                {
                    await EnsureLocationDictionaryLoadedAsync();
                    StateHasChanged();
                });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task EnsureLocationDictionaryLoadedAsync()
    {
        if (LocationDictionary is not null)
        {
            return;
        }

        DeferredLocationDictionaryLoadCts?.Cancel();
        LocationDictionaryLoadTask ??= LoadLocationDictionaryAsync();

        await LocationDictionaryLoadTask;
    }

    private async Task LoadLocationDictionaryAsync()
    {
        LocationDictionary = (await DataService!.GetLocations(false)).ToDictionary(x => x.Id, x => x);

        if (Location is not null)
        {
            LocationDictionary.TryAdd(Location.Id, Location);
        }
    }

    private async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        if (CurrentChartState is null)
        {
            return;
        }

        var targetGranularity = GetSelectedBinGranularity(CurrentChartState) == BinGranularities.ByDayOnly
            ? BinGranularities.ByDayOnly
            : BinGranularities.ByMonthOnly;

        var seriesList = CurrentChartState.Series.ToList();
        foreach (var csd in seriesList)
        {
            csd.BinGranularity = targetGranularity;
        }

        seriesList = seriesList.CreateNewListWithoutDuplicates();

        if (yearAndDataTypeFilter.UnitOfMeasure.HasValue)
        {
            seriesList =
                [
                    .. seriesList.Where(x => x.SourceSeriesSpecifications!.Any(y =>
                        IsCompatibleUnitOfMeasure(y.MeasurementDefinition!.UnitOfMeasure, yearAndDataTypeFilter.UnitOfMeasure.Value))),
                ];
        }

        var chartWithData = CurrentChartData?.SeriesWithData
            .FirstOrDefault(x =>
                (x.SourceDataSet!.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
                (x.SourceDataSet.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null));

        SourceSeriesSpecification[]? sourceSeriesSpecifications;
        SeriesAggregationOptions aggregation;

        if (chartWithData != null)
        {
            sourceSeriesSpecifications = chartWithData.ChartSeries!.SourceSeriesSpecifications;

            var chartSeries = seriesList
                .FirstOrDefault(x => x.SourceSeriesSpecifications!.Any(y =>
                   (y.MeasurementDefinition!.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
                   (y.MeasurementDefinition.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null)));

            aggregation = chartSeries?.Aggregation ?? SeriesAggregationOptions.Mean;
        }
        else if (Location is not null && yearAndDataTypeFilter.DataType.HasValue)
        {
            var dataMatches = yearAndDataTypeFilter.UnitOfMeasure == UnitOfMeasure.DegreesCelsius
                ? DataSubstitute.StandardTemperatureDataMatches()
                : [new DataSubstitute { DataType = yearAndDataTypeFilter.DataType.Value, DataAdjustment = yearAndDataTypeFilter.DataAdjustment }];

            var dsd = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                DataSetDefinitions!,
                Location.Id,
                dataMatches,
                throwIfNoMatch: false);

            if (dsd == null)
            {
                return;
            }

            sourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Location, dsd);
            aggregation = yearAndDataTypeFilter.DataType == DataType.Precipitation
                ? SeriesAggregationOptions.Sum
                : SeriesAggregationOptions.Mean;
        }
        else
        {
            return;
        }

        seriesList =
            [
                .. seriesList,
                new ChartSeriesDefinition
                {
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = sourceSeriesSpecifications,
                    Aggregation = aggregation,
                    BinGranularity = targetGranularity,
                    Smoothing = SeriesSmoothingOptions.None,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = yearAndDataTypeFilter.Year,
                },
            ];

        await ApplyChartStateAsync(CurrentChartState with { Series = seriesList });
    }

    private async Task OnOverviewShowHide(bool isOverviewVisible)
    {
        if (JsRuntime is not null)
        {
            await JsRuntime.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
        }
    }

    private async Task ShowChangeLocationModal()
    {
        await EnsureLocationDictionaryLoadedAsync();
        await ChangeLocationModal!.Show();
    }

    private void HandleShowRequested()
    {
        if (siteOverviewPanel is not null)
        {
            _ = siteOverviewPanel.ShowAsync();
        }
    }

    private async Task SelectedLocationChanged(Guid locationId)
    {
        if (locationId == Guid.Empty)
        {
            return;
        }

        // TODO: remove this stale code
        // PreviousLocation = Location;
        // Location = value;
        // await LocalStorage!.SetItemAsync("lastLocationId", newValue.ToString());
        // await ApplyLocationChangeToChartState();
        // StateHasChanged();

        // Map / Change-Location modal selections just navigate to the GUID URL; the resulting
        // OnParametersSetAsync -> ResolveLocationAsync applies the change (one code path for all
        // location switches, whether driven by the URL or by the UI).
        await NavigateTo($"/{PageName}/{locationId}");
    }

    private async Task ApplyLocationChangeToChartState()
    {
        if (PreviousLocation is null || Location is null || PreviousLocation.Id == Location.Id || CurrentChartState is null || DataSetDefinitions is null || Regions is null)
        {
            return;
        }

        var result = ChartSeriesLocationSubstitutionService!.Substitute(
            new ChartLocationSubstitutionContext
            {
                State = CurrentChartState,
                Location = Location,
                Regions = Regions.ToList(),
                DataSetDefinitions = DataSetDefinitions.ToList(),
            });

        foreach (var message in result.Messages)
        {
            await SnackbarMessageEventHandler(message);
        }

        await ApplyChartStateAsync(result.State);
    }

    private void ToggleSuggestedCharts()
    {
        suggestedChartsCollapsible?.CollapserOnClick();
    }

    private Task ShowRecordHighAsync() => locationInfoComponent?.ShowRecordHighAsync() ?? Task.CompletedTask;

    private ChartState CreateDefaultLocationChartState()
    {
        if (Location is null)
        {
            throw new InvalidOperationException("A location is required before creating the default location chart.");
        }

        return CreateDefaultChartState();
    }

    private ChartState CreateDefaultChartState()
    {
        ArgumentNullException.ThrowIfNull(DataSetDefinitions);
        ArgumentNullException.ThrowIfNull(Location);
        ArgumentNullException.ThrowIfNull(IsMobileDevice);

        var series = new List<ChartSeriesDefinition>();
        var temperature = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            DataSetDefinitions.ToList(),
            Location.Id,
            DataSubstitute.StandardTemperatureDataMatches(),
            throwIfNoMatch: true)!;

        series.Add(
            new ChartSeriesDefinition
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Location, temperature),
                Aggregation = SeriesAggregationOptions.Mean,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 20,
                Value = SeriesValueOptions.Value,
                Year = null,
            });

        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            DataSetDefinitions,
            Location.Id,
            DataType.Precipitation,
            null,
            throwIfNoMatch: false);

        if (precipitation is not null && !IsMobileDevice.Value)
        {
            series.Add(
                new ChartSeriesDefinition
                {
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Location, precipitation),
                    Aggregation = SeriesAggregationOptions.Sum,
                    BinGranularity = BinGranularities.ByYear,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null,
                });
        }

        return new ChartState { Series = series };
    }

    private BinGranularities GetSelectedBinGranularity(ChartState state)
    {
        return state.Series.FirstOrDefault()?.BinGranularity ?? BinGranularities.ByYear;
    }

    private bool IsCompatibleUnitOfMeasure(UnitOfMeasure seriesUnitOfMeasure, UnitOfMeasure filterUnitOfMeasure)
    {
        if (filterUnitOfMeasure == UnitOfMeasure.DegreesCelsius)
        {
            return seriesUnitOfMeasure == UnitOfMeasure.DegreesCelsius
                || seriesUnitOfMeasure == UnitOfMeasure.DegreesCelsiusAnomaly;
        }

        return seriesUnitOfMeasure == filterUnitOfMeasure;
    }
}
