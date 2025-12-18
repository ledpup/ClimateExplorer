namespace ClimateExplorer.Web.Client.Pages;

using Blazorise.Snackbar;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Shared;
using ClimateExplorer.Web.Client.Shared.LocationComponents;
using ClimateExplorer.Web.UiModel;
using CurrentDevice;
using GeoCoordinatePortable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class Index : ChartablePage
{
    private Collapsible? suggestedChartsCollapsible;

    public Index()
    {
        PageName = "location";
    }

    [Parameter]
    public Guid? LocationId { get; set; }

    public bool? IsMobileDevice { get; private set; }

    protected override string PageTitle
    {
        get
        {
            var title = InternalLocation == null ? $"Local long-term climate trends" : $"ClimateExplorer - {InternalLocation.FullTitle}";

            return title;
        }
    }

    protected override string PageUrl
    {
        get
        {
            return InternalLocation == null ? $"https://climateexplorer.net" : $"https://climateexplorer.net/location/{InternalLocation.UrlReadyName()}";
        }
    }

    [Inject]
    private Blazored.LocalStorage.ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    private ChangeLocation? ChangeLocationModal { get; set; }
    private MapContainer? MapContainer { get; set; }
    private Location? PreviousLocation { get; set; }
    private string? BrowserLocationErrorMessage { get; set; }

    private Location? InternalLocation { get; set; }

    void IDisposable.Dispose()
    {
        Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        var location = await GetLocation(uri, true);
        InternalLocation = location;

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsMobileDevice is null)
        {
            IsMobileDevice = await CurrentDeviceService!.Mobile();
        }

        if (LocationDictionary is null)
        {
            var dataSetDefinitionsTask = DataService!.GetDataSetDefinitions();
            var locationsTask = DataService!.GetLocations(false);
            var regionsTask = DataService!.GetRegions();

            await Task.WhenAll(dataSetDefinitionsTask, locationsTask, regionsTask);

            DataSetDefinitions = (await dataSetDefinitionsTask).ToList();
            LocationDictionary = (await locationsTask).ToDictionary(x => x.Id, x => x);
            Regions = (await regionsTask).ToList();

            var geographicalEntities = new List<GeographicalEntity>();
            geographicalEntities.AddRange(LocationDictionary.Values);
            geographicalEntities.AddRange(Regions);
            GeographicalEntities = geographicalEntities;

            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            InternalLocation = await GetLocation(uri, false);

            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (!csd && InternalLocation != null)
            {
                SetUpDefaultCharts(InternalLocation.Id);
                await SelectedLocationChangedInternal(InternalLocation.Id);
                StateHasChanged();
            }
        }

        if (LocationId != InternalLocation?.Id)
        {
            Logger!.LogInformation("New location detected in OnAfterRenderAsync()");
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task UpdateComponents()
    {
        if (InternalLocation != null && MapContainer != null)
        {
            await MapContainer.ScrollToPoint(InternalLocation.Coordinates);
        }
    }

    private async Task<Location?> GetLocation(Uri uri, bool onlyCheckUri)
    {
        Location? location = null;
        if (uri.Segments.Length > 2 && !Guid.TryParse(uri.Segments[2], out Guid locationGuid))
        {
            var locationName = uri.Segments[2];

            location = await DataService!.GetLocationByPath(locationName.ToLower());

            if (location == null)
            {
                NavManager!.NavigateTo("/error", true);
            }
        }

        if (onlyCheckUri)
        {
            return location;
        }

        LocationId = location?.Id;
        if (location == null)
        {
            var locationId = await LocalStorage!.GetItemAsync<string>("lastLocationId");
            var validGuid = Guid.TryParse(locationId, out Guid guid);
            if (validGuid && guid != Guid.Empty && LocationDictionary!.ContainsKey(guid))
            {
                LocationId = guid;
            }
            else
            {
                LocationId = Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395");
            }
        }

        var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        if (csd)
        {
            await UpdateUiStateBasedOnQueryString();

            // Going to assume that the first chart is the primary location
            LocationId = ChartView!.ChartSeriesList!.First().SourceSeriesSpecifications!.First().LocationId;
        }

        location = LocationDictionary![LocationId!.Value];

        return location;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    private void SetUpDefaultCharts(Guid? locationId)
    {
        // Use Hobart as the basis for building the default chart. We want temperature and precipitation on the default chart, whether it's the first time the user has arrived
        // at the website or when they return. Some locations won't have precipitation but we use the DataAvailable field to cope with that situation.
        // Doing it this way, when the user navigates to another location that *does* have precipitation (without making any other changes to the selected data), we will detect it and put it on the chart.
        var location = LocationDictionary![Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395")];

        var tempMaxOrMean = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, location.Id, DataSubstitute.StandardTemperatureDataMatches(), throwIfNoMatch: true)!;
        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, location.Id, DataType.Precipitation, null, throwIfNoMatch: true)!;

        if (ChartView!.ChartSeriesList == null)
        {
            ChartView.ChartSeriesList = new List<ChartSeriesDefinition>();
        }

        ChartView.ChartSeriesList.Add(
            new ChartSeriesDefinition()
            {
                // TODO: remove if we're not going to default to average temperature
                // SeriesDerivationType = SeriesDerivationTypes.AverageOfMultipleSeries,
                // SourceSeriesSpecifications = new SourceSeriesSpecification[]
                // {
                //    SourceSeriesSpecification.BuildArray(location, tempMax)[0],
                //    SourceSeriesSpecification.BuildArray(location, tempMin)[0],
                // },
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMaxOrMean),
                Aggregation = SeriesAggregationOptions.Mean,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 20,
                Value = SeriesValueOptions.Value,
                Year = null,
            });

        ChartView.ChartSeriesList.Add(
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, precipitation),
                Aggregation = SeriesAggregationOptions.Sum,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 20,
                Value = SeriesValueOptions.Value,
                Year = null,
            });
    }

    private async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await ChartView!.HandleOnYearFilterChange(yearAndDataTypeFilter);
    }

    private async Task OnOverviewShowHide(bool isOverviewVisible)
    {
        await JsRuntime!.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
    }

    private Task ShowChangeLocationModal()
    {
        return ChangeLocationModal!.Show();
    }

    private async Task SelectedLocationChanged(Guid locationId)
    {
        if (locationId == Guid.Empty)
        {
            return;
        }

        await NavigateTo($"/{PageName}/" + locationId.ToString());
    }

    private async Task SelectedLocationChangedInternal(Guid newValue)
    {
        Logger!.LogInformation("SelectedLocationChangedInternal(): " + newValue);

        if (!LocationDictionary!.ContainsKey(newValue))
        {
            Logger!.LogError($"{newValue} doesn't exist in the list of locations. Exiting SelectedLocationChangedInternal()");
            return;
        }

        InternalLocation = LocationDictionary[newValue];

        await LocalStorage!.SetItemAsync("lastLocationId", newValue.ToString());

        var additionalCsds = new List<ChartSeriesDefinition>();

        // Update data series to reflect new location
        foreach (var csd in ChartView!.ChartSeriesList!.ToArray())
        {
            foreach (var sss in csd.SourceSeriesSpecifications!)
            {
                if (!csd.IsLocked)
                {
                    // If this source series is
                    // 1) a simple series (only one data source), or
                    // 2) we're not changing location, or
                    // 3) this series belongs to the location we were previously on.
                    // (This check is to ensure that when the user changes location, when we update compound series that are comparing
                    // across locations, we don't update both source series to the same location, which would be nonsense.)
                    // Furthermore, we only run location change substition for geographical entities that are locations. If it is a region, we skip this.
                    if ((csd.SourceSeriesSpecifications.Length == 1 || PreviousLocation == null || sss.LocationId == PreviousLocation.Id)
                            && !Regions!.Any(x => x.Id == sss.LocationId))
                    {
                        sss.LocationId = newValue;
                        sss.LocationName = InternalLocation.Name;

                        var dataMatches = new List<DataSubstitute>
                        {
                            new DataSubstitute
                            {
                                DataType = sss.MeasurementDefinition!.DataType,
                                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
                            },
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
                                InternalLocation.Id,
                                dataMatches,
                                throwIfNoMatch: false);

                        if (dsd == null)
                        {
                            var dataType = ChartSeriesDefinition.MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType);
                            await Snackbar!.PushAsync($"{dataType} data is not available at {InternalLocation.Name}", SnackbarColor.Warning);
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
                                        LocationName = InternalLocation.Name,
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
                                MinimumDataResolution = csd.MinimumDataResolution,
                            });
                    }
                }
            }
        }

        Logger!.LogInformation("Adding items to list inside SelectedLocationChangedInternal()");

        var draftList = ChartView.ChartSeriesList.Concat(additionalCsds).ToList();

        ChartView.ChartSeriesList = draftList.CreateNewListWithoutDuplicates();

        await BuildDataSets();
    }

    private async Task<Guid?> GetCurrentLocation()
    {
        if (JsRuntime == null)
        {
            return null;
        }

        var getLocationResult = await JsRuntime.InvokeAsync<GetLocationResult>("getLocation");

        BrowserLocationErrorMessage = null;
        if (getLocationResult.ErrorCode > 0)
        {
            BrowserLocationErrorMessage = "Unable to determine your location" + (!string.IsNullOrWhiteSpace(getLocationResult.ErrorMessage) ? $" ({getLocationResult.ErrorMessage})" : string.Empty);
            Logger!.LogError(BrowserLocationErrorMessage);
            return null;
        }

        var geoCoord = new GeoCoordinate(getLocationResult.Latitude, getLocationResult.Longitude);

        var distances = Location.GetDistances(geoCoord, LocationDictionary!.Values!);
        var closestLocation = distances.OrderBy(x => x.Distance).First();

        return closestLocation.LocationId;
    }

    private async Task SetCurrentLocation()
    {
        var locationId = await GetCurrentLocation();
        if (locationId != null)
        {
            await SelectedLocationChanged(locationId.Value);
        }
    }

    private class GetLocationResult
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public float ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
