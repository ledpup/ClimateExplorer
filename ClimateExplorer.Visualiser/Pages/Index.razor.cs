using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Shared;
using ClimateExplorer.Visualiser.UiLogic;
using ClimateExplorer.Visualiser.UiModel;
using Blazorise;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Visualiser.Services;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using GeoCoordinatePortable;

namespace ClimateExplorer.Visualiser.Pages;

public partial class Index : IDisposable
{
    [Parameter]
    public string LocationId { get; set; }

    LocationInfo locationInfoComponent { get; set; }
    SelectLocation selectLocationModal { get; set; }
    MapContainer mapContainer { get; set; }

    Guid SelectedLocationId { get; set; }
    Location _selectedLocation { get; set; }
    Location PreviousLocation { get; set; }
    IEnumerable<DataSetDefinitionViewModel> DataSetDefinitions { get; set; }
    IEnumerable<Location> Locations { get; set; }
    Guid _componentInstanceId = Guid.NewGuid();

    string? BrowserLocationErrorMessage { get; set; }

    [Inject] IDataService DataService { get; set; }
    [Inject] NavigationManager NavManager { get; set; }
    [Inject] IExporter Exporter { get; set; }
    [Inject] IJSRuntime JsRuntime { get; set; }
    [Inject] ILogger<Index> Logger { get; set; }

    [Inject] Blazored.LocalStorage.ILocalStorageService? LocalStorage { get; set; }

    bool setupDefaultChartSeries;
    bool ShowLocation { get; set; }

    Modal addDataSetModal { get; set; }

    ChartView chartView;
    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " OnInitializedAsync");

        NavManager.LocationChanged += HandleLocationChanged;

        if (DataService == null)
        {
            throw new NullReferenceException(nameof(DataService));
        }
        DataSetDefinitions = (await DataService.GetDataSetDefinitions()).ToList();

        // A cheat: register some 'derived' measurement types. Could be done better.
        var acornSatDsd = DataSetDefinitions.Single(x => x.Id == Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"));

        acornSatDsd.MeasurementDefinitions
            .Add(
                new MeasurementDefinitionViewModel
                {
                    DataAdjustment = DataAdjustment.Difference,
                    DataType = DataType.TempMax,
                    UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                }
            );

        acornSatDsd.MeasurementDefinitions
            .Add(
                new MeasurementDefinitionViewModel
                {
                    DataAdjustment = DataAdjustment.Difference,
                    DataType = DataType.TempMin,
                    UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                }
            );

        Locations = (await DataService.GetLocations(includeNearbyLocations: true, includeWarmingMetrics: true)).ToList();

        setupDefaultChartSeries = true;
        ShowLocation = true;

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogInformation("OnParametersSetAsync() " + NavManager.Uri + " (NavigateTo)");

        Logger.LogInformation("OnParametersSetAsync(): " + LocationId);

        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
        if (setupDefaultChartSeries)
        {
            setupDefaultChartSeries = (LocationId == null && chartView.ChartSeriesList.Count == 0) || !QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        }

        GetLocationIdViaNameFromPath(uri);

        if (LocationId == null)
        {
            LocationId = (await LocalStorage.GetItemAsync<string>("lastLocationId"));
            var validGuid = Guid.TryParse(LocationId, out Guid id);
            if (!validGuid || id == Guid.Empty)
            {
                LocationId = "aed87aa0-1d0c-44aa-8561-cde0fc936395";
            }
        }

        Guid locationId = Guid.Parse(LocationId);

        if (setupDefaultChartSeries)
        {
            SetUpDefaultCharts(locationId);
            setupDefaultChartSeries = false;
        }

        // Pick up parameters from querystring
        await UpdateUiStateBasedOnQueryString();

        await SelectedLocationChangedInternal(locationId);

        await base.OnParametersSetAsync();
    }

    private void GetLocationIdViaNameFromPath(Uri uri)
    {
        if (uri.Segments.Length > 2 && !Guid.TryParse(uri.Segments[2], out Guid locationGuid))
        {
            var locatioName = uri.Segments[2];
            locatioName = locatioName.Replace("-", " ");
            var location = Locations.SingleOrDefault(x => string.Equals(x.Name, locatioName, StringComparison.OrdinalIgnoreCase));
            if (location != null)
            {
                LocationId = location.Id.ToString();
            }
        }
    }

    private void SetUpDefaultCharts(Guid? locationId)
    {
        var location = Locations.Single(x => x.Id == locationId);

        var tempMax = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions, location.Id, DataType.TempMax, DataAdjustment.Adjusted, true);
        var tempMin = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions, location.Id, DataType.TempMin, DataAdjustment.Adjusted, true);
        var rainfall = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions, location.Id, DataType.Rainfall, null, true, false);

        if (chartView.ChartSeriesList == null)
        {
            chartView.ChartSeriesList = new List<ChartSeriesDefinition>();
        }

        if (tempMax != null)
        {
            chartView.ChartSeriesList.Add(
                new ChartSeriesDefinition()
                {
                    // TODO: remove if we're not going to default to average temperature
                    //SeriesDerivationType = SeriesDerivationTypes.AverageOfMultipleSeries,
                    //SourceSeriesSpecifications = new SourceSeriesSpecification[]
                    //{
                    //    SourceSeriesSpecification.BuildArray(location, tempMax)[0],
                    //    SourceSeriesSpecification.BuildArray(location, tempMin)[0],
                    //},
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
                    Aggregation = SeriesAggregationOptions.Mean,
                    BinGranularity = BinGranularities.ByYear,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null
                }
            );
        }

        if (rainfall != null)
        {
            chartView.ChartSeriesList.Add(
                new ChartSeriesDefinition()
                {
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, rainfall),
                    Aggregation = SeriesAggregationOptions.Sum,
                    BinGranularity = BinGranularities.ByYear,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null
                }
            );
        }
    }



    public void Dispose()
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " disposing");
        NavManager.LocationChanged -= HandleLocationChanged;
    }

    string GetPageTitle()
    {
        var locationText = SelectedLocation == null ? "" : " - " + SelectedLocation.Name;

        string title = $"ClimateExplorer{locationText}";

        Logger.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await chartView.HandleOnYearFilterChange(yearAndDataTypeFilter);
    }

    private Task ShowAddDataSetModal()
    {
        return addDataSetModal.Show();
    }

    async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        Logger.LogInformation("Adding dle " + dle.Name);

        chartView.ChartSeriesList =
            chartView.ChartSeriesList
            .Concat(
                new List<ChartSeriesDefinition>()
                {
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = dle.SeriesDerivationType,
                        SourceSeriesSpecifications = dle.SourceSeriesSpecifications.Select(BuildSourceSeriesSpecification).ToArray(),
                        Aggregation = dle.SeriesAggregation,
                        BinGranularity = chartView.SelectedBinGranularity,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 5,
                        Value = SeriesValueOptions.Value,
                        Year = null
                    }
                }
            )
            .ToList();

        await BuildDataSets();
    }

    private async Task OnOverviewShowHide(bool isOverviewVisible)
    {
        await JsRuntime.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
    }

    private Task ShowSelectLocationModal()
    {
        return selectLocationModal.Show();
    }

    public async Task OnChartPresetSelected(List<ChartSeriesDefinition> chartSeriesDefinitions)
    {
        chartView.SelectedBinGranularity = chartSeriesDefinitions.First().BinGranularity;

        chartView.ChartSeriesList = chartSeriesDefinitions.ToList();

        await BuildDataSets();
    }

    SourceSeriesSpecification BuildSourceSeriesSpecification(DataSetLibraryEntry.SourceSeriesSpecification sss)
    {
        var dsd = DataSetDefinitions.Single(x => x.Id == sss.SourceDataSetId);

        var md = dsd.MeasurementDefinitions.Single(x => x.DataType == sss.DataType && x.DataAdjustment == sss.DataAdjustment);

        return
            new SourceSeriesSpecification
            {
                LocationId = sss.LocationId,
                LocationName = sss.LocationName,
                DataSetDefinition = dsd,
                MeasurementDefinition = md
            };
    }

    Location SelectedLocation
    {
        get
        {
            return _selectedLocation;
        }
        set
        {
            if (value != _selectedLocation)
            {
                PreviousLocation = _selectedLocation;
                _selectedLocation = value;
                LocationCoordinates = _selectedLocation.Coordinates;
            }
        }
    }
    Coordinates LocationCoordinates;

    void HandleLocationChanged(object sender, LocationChangedEventArgs e)
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " HandleLocationChanged: " + NavManager.Uri);

        // The URL changed. Update UI state to reflect what's in the URL.
        base.InvokeAsync(UpdateUiStateBasedOnQueryString);
    }

    async Task UpdateUiStateBasedOnQueryString()
    {
        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
        {
            try
            {
                var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger, csdSpecifier, DataSetDefinitions, Locations);

                if (csdList.Any())
                {
                    chartView.SelectedBinGranularity = csdList.First().BinGranularity;
                }

                Logger.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

                chartView.ChartSeriesList = csdList.ToList();

                await BuildDataSets();

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
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

        var l = new LogAugmenter(Logger, "BuildDataSets");

        l.LogInformation("starting");

        chartView.LogChartSeriesList();

        // Recalculate the URL
        string chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(chartView.ChartSeriesList);

        string url = "/location/" + LocationId;

        if (chartSeriesUrlComponent.Length > 0) url += "?csd=" + chartSeriesUrlComponent;

        string currentUri = NavManager.Uri;
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

            // Fetch the data required to render the selected data series
            chartView.ChartSeriesWithData = await RetrieveDataSets(chartView.ChartSeriesList);

            l.LogInformation("Set ChartSeriesWithData after call to RetrieveDataSets(). ChartSeriesWithData now has " + chartView.ChartSeriesWithData.Count + " entries.");

            ShowLocation = chartView.ChartSeriesWithData != null && chartView.ChartSeriesWithData.Any(x => x.SourceDataSet.DataType == DataType.TempMax || x.SourceDataSet.DataType == DataType.TempMin || x.SourceDataSet.DataType == DataType.Rainfall || x.SourceDataSet.DataType == DataType.SolarRadiation);

            // Render the series
            await chartView.HandleRedraw();

            if (SelectedLocation != null && mapContainer != null)
            {
                await mapContainer.ScrollToPoint(new LatLng(SelectedLocation.Coordinates.Latitude, SelectedLocation.Coordinates.Longitude));
            }
        }

        l.LogInformation("leaving");
    }

    static ContainerAggregationFunctions MapSeriesAggregationOptionToBinAggregationFunction(SeriesAggregationOptions a)
    {
        switch (a)
        {
            case SeriesAggregationOptions.Mean: return ContainerAggregationFunctions.Mean;
            case SeriesAggregationOptions.Minimum: return ContainerAggregationFunctions.Min;
            case SeriesAggregationOptions.Maximum: return ContainerAggregationFunctions.Max;
            case SeriesAggregationOptions.Median: return ContainerAggregationFunctions.Median;
            case SeriesAggregationOptions.Sum: return ContainerAggregationFunctions.Sum;
            default: throw new NotImplementedException($"SeriesAggregationOptions {a}");
        }
    }

    SeriesSpecification BuildDataPrepSeriesSpecification(SourceSeriesSpecification sss)
    {
        return
            new SeriesSpecification
            {
                DataSetDefinitionId = sss.DataSetDefinition.Id,
                DataType = sss.MeasurementDefinition.DataType,
                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
                LocationId = sss.LocationId
            };
    }

    async Task<List<SeriesWithData>> RetrieveDataSets(List<ChartSeriesDefinition> chartSeriesList)
    {
        var datasetsToReturn = new List<SeriesWithData>();

        Logger.LogInformation("RetrieveDataSets: starting enumeration");

        foreach (var csd in chartSeriesList)
        {
            var cupAggregationFunction = MapSeriesAggregationOptionToBinAggregationFunction(csd.Aggregation);
            var bucketAggregationFunction = cupAggregationFunction;

            // If we're doing modular binning and the aggregation function is sum, then force mean aggregation at
            // top level
            var binAggregationFunction =
                chartView.SelectedBinGranularity.IsModular() && cupAggregationFunction == ContainerAggregationFunctions.Sum
                ? ContainerAggregationFunctions.Mean
                : cupAggregationFunction;
            
            DataSet dataSet =
                await DataService.PostDataSet(
                    chartView.SelectedBinGranularity,
                    binAggregationFunction,
                    bucketAggregationFunction,
                    cupAggregationFunction,
                    csd.Value,
                    csd.SourceSeriesSpecifications.Select(BuildDataPrepSeriesSpecification).ToArray(),
                    csd.SeriesDerivationType,
                    chartView.GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    chartView.GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    chartView.GetGroupingThreshold(csd.GroupingThreshold),
                    chartView.SelectedDayGrouping,
                    csd.SeriesTransformation,
                    csd.Year
                );

            datasetsToReturn.Add(
                new SeriesWithData() { ChartSeries = csd, SourceDataSet = dataSet }
            );
        }

        Logger.LogInformation("RetrieveDataSets: completed enumeration");

        return datasetsToReturn;
    }

    async Task SelectedLocationChanged(Guid locationId)
    {
        await NavigateTo("/location/" + locationId.ToString());
    }

    public async Task NavigateTo(string uri, bool replace = false)
    {
        Logger.LogInformation("NavManager.NavigateTo(uri=" + uri + ", replace=" + replace + ")");

        // Below is a JavaScript hack to stop NavigateTo from scrolling to the top of the page.
        // See: https://github.com/dotnet/aspnetcore/issues/40190 and index.html
        await JsRuntime.InvokeVoidAsync("willSkipScrollTo", true);

        NavManager.NavigateTo(uri, false, replace);
    }

    async Task SelectedLocationChangedInternal(Guid newValue)
    {
        Logger.LogInformation("SelectedLocationChangedInternal(): " + newValue);

        SelectedLocationId = newValue;
        SelectedLocation = Locations.Single(x => x.Id == SelectedLocationId);

        await LocalStorage.SetItemAsync("lastLocationId", SelectedLocationId.ToString());

        List<ChartSeriesDefinition> additionalCsds = new List<ChartSeriesDefinition>();

        // Update data series to reflect new location
        foreach (var csd in chartView.ChartSeriesList.ToArray())
        {
            foreach (var sss in csd.SourceSeriesSpecifications)
            {
                if (!csd.IsLocked)
                {
                    // If this source series is location-specific
                    if (sss.LocationId != null &&
                        // and this is a simple series (only one data source), or we're not changing location, or this series belongs
                        // to the location we were previously on. (this check is to ensure that when the user changes location, when
                        // we update compound series that are comparing across locations, we don't update both source series to the
                        // same location, which would be nonsense.)
                        (csd.SourceSeriesSpecifications.Length == 1 || PreviousLocation == null || sss.LocationId == PreviousLocation.Id))
                    {
                        sss.LocationId = newValue;
                        sss.LocationName = SelectedLocation.Name;

                        // But: the new location may not have data of the requested type. Let's see if there is any.
                        DataSetAndMeasurementDefinition dsd =
                            DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                                DataSetDefinitions,
                                SelectedLocationId,
                                sss.MeasurementDefinition.DataType,
                                sss.MeasurementDefinition.DataAdjustment,
                                allowNullDataAdjustment: true,
                                throwIfNoMatch: false);

                        if (dsd == null)
                        {
                            // This data is not available for the new location. For now, just leave this series as is.
                            // Probably kinder to the user if we show a warning of some kind.
                            chartView.ChartSeriesList.Remove(csd);

                            break;
                        }
                        else
                        {
                            // This data IS available at the new location. Now, update the series accordingly.
                            sss.DataSetDefinition = dsd.DataSetDefinition;

                            // Next, update the MeasurementDefinition. Look for a match on DataType and DataAdjustment
                            var oldMd = sss.MeasurementDefinition;

                            var candidateMds =
                                sss.DataSetDefinition.MeasurementDefinitions
                                .Where(x => x.DataType == oldMd.DataType && x.DataAdjustment == oldMd.DataAdjustment)
                                .ToArray();

                            switch (candidateMds.Length)
                            {
                                case 0:
                                    // There was no exact match. It's possible that the new location has data of the requested type, but not the specified adjustment type.
                                    // If so, try defaulting.
                                    candidateMds = sss.DataSetDefinition.MeasurementDefinitions.Where(x => x.DataType == oldMd.DataType).ToArray();

                                    if (candidateMds.Length == 1)
                                    {
                                        // If only one is available, just use it
                                        sss.MeasurementDefinition = candidateMds.Single();
                                    }
                                    else
                                    {
                                        // Otherwise, use "Adjusted" if available
                                        var adjustedMd = candidateMds.SingleOrDefault(x => x.DataAdjustment == DataAdjustment.Adjusted);

                                        if (adjustedMd != null)
                                        {
                                            sss.MeasurementDefinition = adjustedMd;
                                        }
                                    }

                                    break;

                                case 1:
                                    sss.MeasurementDefinition = candidateMds.Single();
                                    break;

                                default:
                                    // There were multiple matches. That's unexpected.
                                    throw new Exception("Unexpected condition: after changing location, while updating ChartSeriesDefinitions, there were multiple compatible MeasurementDefinitions for one CSD.");
                            }
                        }
                    }
                }
                else
                {
                    // It's locked, so duplicate it & set the location on the duplicate to the new location
                    var newDsd = DataSetDefinitions.Single(x => x.Id == sss.DataSetDefinition.Id);
                    var newMd =
                        newDsd.MeasurementDefinitions
                        .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition.DataType && x.DataAdjustment == sss.MeasurementDefinition.DataAdjustment);

                    if (newMd == null)
                    {
                        newMd =
                            newDsd.MeasurementDefinitions
                            .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition.DataType && x.DataAdjustment == null);
                    }

                    if (newMd != null)
                    {
                        additionalCsds.Add(
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications =
                                    new SourceSeriesSpecification[]
                                    {
                                    new SourceSeriesSpecification
                                    {
                                        DataSetDefinition = DataSetDefinitions.Single(x => x.Id == sss.DataSetDefinition.Id),
                                        LocationId = newValue,
                                        LocationName = SelectedLocation.Name,
                                        MeasurementDefinition = newMd,
                                    }
                                    },
                                Aggregation = csd.Aggregation,
                                BinGranularity = csd.BinGranularity,
                                DisplayStyle = csd.DisplayStyle,
                                IsLocked = false,
                                ShowTrendline = csd.ShowTrendline,
                                Smoothing = csd.Smoothing,
                                SmoothingWindow = csd.SmoothingWindow,
                                Value = csd.Value,
                                Year = csd.Year,
                                SeriesTransformation = csd.SeriesTransformation,
                                GroupingThreshold = csd.GroupingThreshold,
                            }
                        );
                    }
                }
            }
        }

        Logger.LogInformation("Adding items to list inside SelectedLocationChangedInternal()");

        var draftList = chartView.ChartSeriesList.Concat(additionalCsds).ToList();

        chartView.ChartSeriesList = draftList.CreateNewListWithoutDuplicates();

        await BuildDataSets();
    }

  



    class GetLocationResult
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public float ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    async Task<Guid?> GetCurrentLocation()
    {
        if (JsRuntime == null)
        {
            return null;
        }

        var getLocationResult = await JsRuntime.InvokeAsync<GetLocationResult>("getLocation");

        BrowserLocationErrorMessage = null;
        if (getLocationResult.ErrorCode > 0)
        {
            BrowserLocationErrorMessage = "Unable to determine your location" + (!string.IsNullOrWhiteSpace(getLocationResult.ErrorMessage) ? $" ({getLocationResult.ErrorMessage})" : "");
            Logger.LogError(BrowserLocationErrorMessage);
            return null;
        }

        var geoCoord = new GeoCoordinate(getLocationResult.Latitude, getLocationResult.Longitude);

        var distances = Location.GetDistances(geoCoord, Locations);
        var closestLocation = distances.OrderBy(x => x.Distance).First();

        return closestLocation.LocationId;
    }

    async Task SetCurrentLocation()
    {
        var locationId = await GetCurrentLocation();
        if (locationId != null)
        {
            await SelectedLocationChangedInternal(locationId.Value);
        }
    }

    private async Task OnDownloadDataClicked(DataDownloadPackage dataDownloadPackage)
    { 
        var fileStream = Exporter.ExportChartData(Logger, Locations, dataDownloadPackage, NavManager.Uri.ToString());

        var locationNames = dataDownloadPackage.ChartSeriesWithData.SelectMany(x => x.ChartSeries.SourceSeriesSpecifications).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : "";

        fileName = $"Export-{fileName}-{chartView.SelectedBinGranularity}-{dataDownloadPackage.Bins.First().Label}-{dataDownloadPackage.Bins.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }
}
