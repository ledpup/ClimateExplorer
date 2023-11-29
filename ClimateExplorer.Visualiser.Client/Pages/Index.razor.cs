using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.UiModel;
using ClimateExplorer.Core.DataPreparation;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using GeoCoordinatePortable;
using ClimateExplorer.Core.Model;
using Blazorise.Snackbar;
using ClimateExplorer.Visualiser.Client.Shared.LocationComponents;
using ClimateExplorer.Visualiser.Client.Shared;

namespace ClimateExplorer.Visualiser.Client.Pages;

public partial class Index : ChartablePage
{
    [Parameter]
    public string? LocationId { get; set; }
    ChangeLocation? changeLocationModal { get; set; }
    MapContainer? mapContainer { get; set; }

    Guid SelectedLocationId { get; set; }
    Location? _selectedLocation { get; set; }
    Location? PreviousLocation { get; set; }
    string? BrowserLocationErrorMessage { get; set; }

    [Inject] Blazored.LocalStorage.ILocalStorageService? LocalStorage { get; set; }

    bool setupDefaultChartSeries;
    Guid oldLocationId = Guid.Empty;

    public Index()
    {
        pageName = "location";
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Locations = (await DataService!.GetLocations(includeNearbyLocations: true, includeWarmingIndex: true, excludeLocationsWithNullWarmingIndex: false)).ToList();

        setupDefaultChartSeries = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Locations == null)
        {
            Logger!.LogError("OnParametersSetAsync(): Locations is null");
            return;
        }    

        Logger!.LogInformation("OnParametersSetAsync() " + NavManager!.Uri + " (NavigateTo)");

        Logger!.LogInformation("OnParametersSetAsync(): " + LocationId);

        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
        if (setupDefaultChartSeries)
        {
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (csd)
            {
                setupDefaultChartSeries = false;
                await UpdateUiStateBasedOnQueryString();
                // Going to assume that the first chart is the primary location
                LocationId = chartView!.ChartSeriesList!.First().SourceSeriesSpecifications!.First().LocationId.ToString();
            }
        }

        GetLocationIdViaNameFromPath(uri);
        
        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (LocationId == null)
            {
                LocationId = (await LocalStorage!.GetItemAsync<string>("lastLocationId"));
                var validGuid = Guid.TryParse(LocationId, out Guid id);
                if (!validGuid || id == Guid.Empty || !Locations!.Any(x => x.Id == id))
                {
                    LocationId = "aed87aa0-1d0c-44aa-8561-cde0fc936395";
                }
            }

            Guid locationId = Guid.Parse(LocationId);

            if (setupDefaultChartSeries)
            {
                SetUpDefaultCharts(locationId);
                setupDefaultChartSeries = false;
                await SelectedLocationChanged(locationId);
            }
            else if (oldLocationId != locationId)
            {
                await SelectedLocationChangedInternal(locationId);
                oldLocationId = locationId;
            }
        }
    }

    private void GetLocationIdViaNameFromPath(Uri uri)
    {
        if (uri.Segments.Length > 2 && !Guid.TryParse(uri.Segments[2], out Guid locationGuid))
        {
            var locatioName = uri.Segments[2];
            var originalName = locatioName;
            locatioName = locatioName.Replace("-", " ");
            var location = Locations!.SingleOrDefault(x => string.Equals(x.Name, locatioName, StringComparison.OrdinalIgnoreCase));
            // Will match for Kalgoorlie-Boulder
            if (location == null)
            {
                location = Locations!.SingleOrDefault(x => string.Equals(x.Name, originalName, StringComparison.OrdinalIgnoreCase));
            }
            if (location != null)
            {
                LocationId = location.Id.ToString();
            }
        }
    }

    private void SetUpDefaultCharts(Guid? locationId)
    {
        // Use Hobart as the basis for building the default chart. We want temperature and precipitation on the default chart, whether it's the first time the user has arrived
        // at the website or when they return. Some locations won't have precipitation but we use the DataAvailable field to cope with that situation.
        // Doing it this way, when the user navigates to another location that *does* have precipitation (without making any other changes to the selected data), we will detect it and put it on the chart.
        var location = Locations!.Single(x => x.Id == Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395"));

        var tempMaxOrMean = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, location.Id, DataSubstitute.StandardTemperatureDataMatches(), throwIfNoMatch: true)!;
        var rainfall = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, location.Id, DataType.Rainfall, null, throwIfNoMatch: true)!;

        if (chartView!.ChartSeriesList == null)
        {
            chartView.ChartSeriesList = new List<ChartSeriesDefinition>();
        }

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
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMaxOrMean),
                Aggregation = SeriesAggregationOptions.Mean,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 20,
                Value = SeriesValueOptions.Value,
                Year = null
            }
        );

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

    string GetPageTitle()
    {
        var locationText = SelectedLocation == null ? "" : " - " + SelectedLocation.FullTitle;

        string title = $"ClimateExplorer{locationText}";

        Logger!.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await chartView!.HandleOnYearFilterChange(yearAndDataTypeFilter);
    }

    private async Task OnOverviewShowHide(bool isOverviewVisible)
    {
        await JsRuntime!.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
    }

    private Task ShowChangeLocationModal()
    {
        return changeLocationModal!.Show();
    }

    Location SelectedLocation
    {
        get
        {
            return _selectedLocation!;
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

    async Task SelectedLocationChanged(Guid locationId)
    {
        if (locationId == Guid.Empty)
        {
            return;
        }
        await NavigateTo($"/{pageName}/" + locationId.ToString());
    }

    void IDisposable.Dispose()
    {
        Dispose();
    }

    async Task SelectedLocationChangedInternal(Guid newValue)
    {
        Logger!.LogInformation("SelectedLocationChangedInternal(): " + newValue);

        if (!Locations!.Any(x => x.Id == newValue))
        {
            Logger!.LogError($"{newValue} doesn't exist in the list of locations. Exiting SelectedLocationChangedInternal()");
            return;
        }

        SelectedLocationId = newValue;
        SelectedLocation = Locations!.Single(x => x.Id == SelectedLocationId);

        await LocalStorage!.SetItemAsync("lastLocationId", SelectedLocationId.ToString());

        var additionalCsds = new List<ChartSeriesDefinition>();

        // Update data series to reflect new location
        foreach (var csd in chartView!.ChartSeriesList!.ToArray())
        {
            foreach (var sss in csd.SourceSeriesSpecifications!)
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

                        var dataMatches = new List<DataSubstitute>
                        {
                            new DataSubstitute
                            {
                                DataType = sss.MeasurementDefinition!.DataType,
                                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
                            }
                        };

                        // If the data type is max or mean temperature, pass through an accepted list of near matching data
                        if (sss.MeasurementDefinition!.DataType == DataType.TempMax || sss.MeasurementDefinition!.DataType == DataType.TempMean)
                        {
                            if (sss.MeasurementDefinition!.DataAdjustment == DataAdjustment.Unadjusted)
                            {
                                dataMatches = DataSubstitute.UnadjustedTemperatureDataMatches();
                            }
                            else
                            {
                                dataMatches = DataSubstitute.StandardTemperatureDataMatches();
                            }
                        }

                        // But: the new location may not have data of the requested type. Let's see if there is any.
                        var dsd =
                            DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                                DataSetDefinitions!,
                                SelectedLocationId,
                                dataMatches,
                                throwIfNoMatch: false);

                        if (dsd == null)
                        {
                            var dataType = ChartSeriesDefinition.MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType);
                            await snackbar!.PushAsync($"{dataType} data is not available at {SelectedLocation.Name}", SnackbarColor.Warning);
                            csd.DataAvailable = false;

                            break;
                        }
                        else
                        {
                            // This data IS available at the new location. Now, update the series accordingly.
                            csd.DataAvailable = true;

                            sss.DataSetDefinition = dsd.DataSetDefinition!;

                            // Next, update the MeasurementDefinition. Look for a match on DataType and DataAdjustment
                            var oldMd = sss.MeasurementDefinition;

                            var candidateMds =
                                sss.DataSetDefinition!.MeasurementDefinitions!
                                .Where(x => x.DataType == oldMd.DataType && x.DataAdjustment == oldMd.DataAdjustment)
                                .ToArray();

                            switch (candidateMds.Length)
                            {
                                case 0:
                                    // There was no exact match. It's possible that the new location has data of the requested type, but not the specified adjustment type.
                                    // If so, try defaulting.
                                    candidateMds = sss.DataSetDefinition.MeasurementDefinitions!.Where(x => x.DataType == oldMd.DataType).ToArray();

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
                    var newDsd = DataSetDefinitions!.Single(x => x.Id == sss.DataSetDefinition!.Id);
                    var newMd =
                        newDsd.MeasurementDefinitions!
                        .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition!.DataType && x.DataAdjustment == sss.MeasurementDefinition.DataAdjustment);

                    if (newMd == null)
                    {
                        newMd =
                            newDsd.MeasurementDefinitions!
                            .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition!.DataType && x.DataAdjustment == null);
                    }

                    if (newMd != null)
                    {
                        additionalCsds.Add(
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications =
                                    [
                                    new SourceSeriesSpecification
                                    {
                                        DataSetDefinition = DataSetDefinitions!.Single(x => x.Id == sss.DataSetDefinition!.Id),
                                        LocationId = newValue,
                                        LocationName = SelectedLocation.Name,
                                        MeasurementDefinition = newMd,
                                    }
                                    ],
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

        Logger!.LogInformation("Adding items to list inside SelectedLocationChangedInternal()");

        var draftList = chartView.ChartSeriesList.Concat(additionalCsds).ToList();

        chartView.ChartSeriesList = draftList.CreateNewListWithoutDuplicates();

        await BuildDataSets();
    }

    protected override async Task UpdateComponents()
    {
        if (SelectedLocation != null && mapContainer != null)
        {
            await mapContainer.ScrollToPoint(new LatLng(SelectedLocation.Coordinates.Latitude, SelectedLocation.Coordinates.Longitude));
        }
    }

    class GetLocationResult
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public float ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
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
            Logger!.LogError(BrowserLocationErrorMessage);
            return null;
        }

        var geoCoord = new GeoCoordinate(getLocationResult.Latitude, getLocationResult.Longitude);

        var distances = Location.GetDistances(geoCoord, Locations!);
        var closestLocation = distances.OrderBy(x => x.Distance).First();

        return closestLocation.LocationId;
    }

    async Task SetCurrentLocation()
    {
        var locationId = await GetCurrentLocation();
        if (locationId != null)
        {
            await SelectedLocationChanged(locationId.Value);
        }
    }
}
