namespace ClimateExplorer.Web.Client.Pages;

using Blazorise.Snackbar;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Shared;
using ClimateExplorer.Web.Client.Shared.LocationComponents;
using ClimateExplorer.Web.UiModel;
using CurrentDevice;
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
    public string? LocationString { get; set; }

    public bool? IsMobileDevice { get; private set; }

    protected override string PageTitle
    {
        get
        {
            var title = Location == null ? $"Local long-term climate trends" : $"ClimateExplorer - {Location.FullTitle}";

            return title;
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
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    private ChangeLocation? ChangeLocationModal { get; set; }
    private MapContainer? MapContainer { get; set; }
    private Location? PreviousLocation { get; set; }
    private string? BrowserLocationErrorMessage { get; set; }
    private Location? Location { get; set; }

    private bool LocationChangeEventOccurring { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        // Check to see if a named location is being requested. That will look like /location/<location-name>
        Location = await GetNamedLocation();

        if (Location is not null)
        {
            LocationString = Location?.Id.ToString();
        }

        await base.OnInitializedAsync();
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

            // Location may have been set in OnInitializedAsync
            if (Location is null)
            {
                // Get location from local storage. If not in local storage, use Hobart as default.
                // If we have URL that is a csd, override with the location specified in the csd.
                Location = await GetLocation();
            }

            // If there is no "csd" query string parameter, set up default charts for the location
            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (!csd && Location != null)
            {
                SetUpDefaultCharts(Location.Id);
                await SelectedLocationChanged(Location.Id);
            }

            StateHasChanged();
        }

        // Handle location change event (coming from the map or the Change Location modal)
        if (LocationChangeEventOccurring && LocationString is not null)
        {
            LocationChangeEventOccurring = false;
            var locationId = Guid.Parse(LocationString!);
            await SelectedLocationChangedInternal(locationId);
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task UpdateComponents()
    {
        if (Location != null && MapContainer != null)
        {
            await MapContainer.ScrollToPoint(Location.Coordinates);
        }
    }

    private async Task<Location?> GetNamedLocation()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
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

        return location;
    }

    private async Task<Location?> GetLocation()
    {
        var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
        var locationString = await LocalStorage!.GetItemAsync<string>("lastLocationId");
        var validGuid = Guid.TryParse(locationString, out Guid guid);
        Guid? locationId = null;
        if (validGuid && guid != Guid.Empty && LocationDictionary!.ContainsKey(guid))
        {
            locationId = guid;
        }
        else
        {
            locationId = Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395");
        }

        // Override location with the one in csd, if there is a csd
        var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        if (csd)
        {
            await UpdateUiStateBasedOnQueryString();

            // Going to assume that the first chart is the primary location
            locationId = ChartView!.ChartSeriesList!.First().SourceSeriesSpecifications!.First().LocationId;
        }

        var location = LocationDictionary![locationId!.Value];

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

        LocationChangeEventOccurring = true;

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

        Location = LocationDictionary[newValue];

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
                        sss.LocationName = Location.Name;

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
                                Location.Id,
                                dataMatches,
                                throwIfNoMatch: false);

                        if (dsd == null)
                        {
                            var dataType = ChartSeriesDefinition.MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType);
                            await Snackbar!.PushAsync($"{dataType} data is not available at {Location.Name}", SnackbarColor.Warning);
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
                                        LocationName = Location.Name,
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
}
