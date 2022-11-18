using ClimateExplorer.Core;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Shared;
using ClimateExplorer.Visualiser.UiLogic;
using ClimateExplorer.Visualiser.UiModel;
using Blazorise;
using Blazorise.Charts;
using Blazorise.Charts.Trendline;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Visualiser.Services;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using System.Dynamic;
using GeoCoordinatePortable;
using BlazorCurrentDevice;

namespace ClimateExplorer.Visualiser.Pages;

public partial class Index : IDisposable
{
    [Parameter]
    public string LocationId { get; set; }

    LocationInfo locationInfoComponent { get; set; }
    BinGranularities SelectedBinGranularity { get; set; } = BinGranularities.ByYear;
    List<ChartSeriesDefinition> ChartSeriesList { get; set; } = new List<ChartSeriesDefinition>();
    List<SeriesWithData> ChartSeriesWithData { get; set; }
    BinIdentifier[] ChartBins { get; set; }
    float InternalGroupingThreshold { get; set; } = .7f;
    string GroupingThresholdText { get; set; }
    bool UserOverridePresetAggregationSettings { get; set; }
    short SelectedDayGrouping { get; set; } = 14;
    short SelectingDayGrouping { get; set; }
    Modal addDataSetModal { get; set; }
    Modal optionsModal { get; set; }
    SelectLocation selectLocationModal { get; set; }
    MapContainer mapContainer { get; set; }
    Filter filter { get; set; }

    /// <summary>
    /// The chart type applied to the chart control. If any series is in "Bar" mode, we switch
    /// the entire chart to Bar type to ensure it renders, at the cost of a small misalignment
    /// between grid lines and datapoints for any line series that are being displayed.
    /// Otherwise, we display in "Line" mode to avoid that cost.
    /// </summary>
    ChartType InternalChartType { get; set; }

    /// <summary>
    /// The chart type selected by the user on the options page
    /// </summary>
    ChartType SelectedChartType { get; set; }
    List<short>? DatasetYears { get; set; }
    List<short>? SelectedYears { get; set; }
    List<short> StartYears { get; set; }
    short EndYear { get; set; }
    bool UseMostRecentStartYear { get; set; } = true;
    string? SelectedStartYear { get; set; }
    string? SelectedEndYear { get; set; }
    Guid SelectedLocationId { get; set; }
    Location _selectedLocation { get; set; }
    Location PreviousLocation { get; set; }
    IEnumerable<DataSetDefinitionViewModel> DataSetDefinitions { get; set; }
    IEnumerable<Location> Locations { get; set; }
    ColourServer colours { get; set; } = new ColourServer();
    Guid _componentInstanceId = Guid.NewGuid();
    Chart<float?> chart;
    ChartTrendline<float?> chartTrendline;
    BinIdentifier ChartStartBin, ChartEndBin;
    bool _haveCalledResizeAtLeastOnce = false;

    bool? EnableRangeSlider { get; set; }
    int SliderMin { get; set; }
    int SliderMax { get; set; }
    int? SliderStart { get; set; }
    int? SliderEnd { get; set; }

    string? BrowserLocationErrorMessage { get; set; }

    bool IsMobileDevice { get; set; }

    [Inject] IDataService DataService { get; set; }
    [Inject] NavigationManager NavManager { get; set; }
    [Inject] IExporter Exporter { get; set; }
    [Inject] IJSRuntime JsRuntime { get; set; }
    [Inject] ILogger<Index> Logger { get; set; }

    [Inject] IBlazorCurrentDeviceService BlazorCurrentDeviceService { get; set; }

    public string PopupText { get; set; } = @"<div style=""padding-bottom: 24px;""><img style=""max-width: 100%;"" src=""images/ChartOptions.png"" alt=""Chart Options image"" /></div>
<p><strong>Year filtering</strong>: allows you to change the start and end years for the chart. For example, if you want to see the change in temperature for the 20th century, you could set the end year to 2000.</p>
<p><strong>Clear filter</strong>: the clear filter button is only displayed when there is a start or end year filter applied to the chart. Clicking this button will reset the chart back to the default filter and remove the range slider (if it has been turned on).</p>
<p><strong>Grouping</strong>: the grouping option allows you to look at the data from another point of view. The default view is ""Yearly""; i.e., each point on the graph represents a single year in the series. To represent daily data at the yearly level, ClimateExplorer applies rules and aggregations to average (or sum) the data together. If you select ""Year + Month"" the data will be re-processed, starting with the daily data for the particular year, to present twelve points on the chart per year. This view works best in combination with the range slider. If you select ""Month"", the data will be sliced, again starting with the lowest level of the data (usually daily), into only twelve points, one point for every month of the year. The value for each point will be an average (or sum) of the data across all years. This will give you a climatic view of the data for the location, it will not be as useful for viewing the change in the climate over time.</p>
<p><strong>Download data</strong>: the download data button allows you to download, as a csv file, the data for the chart you are currently looking at. The button is context sensitive; it'll download data that applies to the current view. For example, if you are looking at the data as a ""Year + Month"" grouping, you will get twelve records for each year.</p>
<p><strong>Aggregation options</strong> (*advanced* feature): the aggregation options allow you to change the underlying grouping parameters for the chart. The default values will group the daily data into 14 day (i.e., fortnightly) sub-bins. If each of those sub-bins has records for 70% of those days (i.e., 10 days of the 14 days will need to have records) then the whole year is considered valid. This means that you can still have substantial data loss for the year (e.g., the meteorologist was unwilling to come in on weekends to record the min and max temperatures)</p>
<p><strong>Add data set</strong> (*advanced* feature): the suggested charts at the bottom of the screen provide the user with a number of predefined and recommended charts that can be viewed within ClimateExplorer. Other datasets can be added in an ad-hoc manner with the ""Add data set"" button. The list on the ""Add data set"" dialog contains data for your current location, such as solar radiation and the diurnal range for temperature. The list also contains reference data sets that can be added, such as CO₂, ENSO indexes and data from the cryosphere (the cryosphere comprises the parts of the planet that are frozen most of the year).</p>";

    public string PopupAggregationOptionsInfoText { get; set; } = @"<p>The aggregation options are an advanced feature that allows you to change the underlying aggregation process. To calculate a single aggregated value for data for a year, from daily or monthly series, data is bundled together. If each bundle of data does not have enough records, the bundle is rejected as being unreliable.</p>
<p>By default, the bundles are groups of 14 days (fortnights) and each bundle requires 70% (10 days of the 14) of the records to be present for the year to be considered reliable enough for an average mean to be calculated. This means that a number of records can be missing for the year, so long as not too many consecutive days are missing. As temperature (and other climate data) follows cyclic patterns, missing data from a consecutive block is considered to be more untrustworthy than sporadic data missing throughout the year.</p>
<p>Some presets (specifically, the cryosphere reference data – sea ice extent and melt) have a lower threshold applied to them because the data has been curated and considered to be trustworthy enough that more of it can be missing while still not corrupting the results.</p>
<p>If you make changes to these settings and apply them, your settings will take precedence and override any preset specific settings. You can clear this by clicking “Clear override” which will appear after you apply your changes.</p>
<p><strong>Day grouping</strong>: select groups from weekly, fortnightly, monthly, and half-yearly, amongst other options.</p>
<p><strong>Threshold required to form a valid group (% percentage)</strong>: this is a percentage of how many records is considered sufficient to form a valid bundle of data.</p>
<p><strong>Apply</strong>: save your changes and apply them to the chart. These settings will persist as you change locations and datasets within the application.</p>
<p><strong>Clear overide</strong>: this will reset the settings back to their default (14 days at 70% threshold). Only appears after applying your settings.</p>";

    private Modal popup, popupAggregationOptionsInfoText;
    private Task ShowChartOptionsInfo()
    {
        if (!string.IsNullOrWhiteSpace(PopupText))
        {
            return popup.Show();
        }
        return Task.CompletedTask;
    }

    private Task ShowAggregationOptionsInfo()
    {
        if (!string.IsNullOrWhiteSpace(PopupText))
        {
            return popupAggregationOptionsInfoText.Show();
        }
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " OnInitializedAsync");

        IsMobileDevice = await BlazorCurrentDeviceService.Mobile();

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

        SelectedYears = new List<short>();

        var datasetYears = new List<short>();
        for (short i = 1800; i <= (short)DateTime.Now.Year; i++)
        {
            datasetYears.Add(i);
        }
        DatasetYears = datasetYears;

        SliderMax = DateTime.Now.Year;

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogInformation("OnParametersSetAsync() " + NavManager.Uri + " (NavigateTo)");

        Logger.LogInformation("OnParametersSetAsync(): " + LocationId);

        bool setupDefaultChartSeries = LocationId == null && ChartSeriesList.Count == 0;

        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
        if (!QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
        {
            setupDefaultChartSeries = true;
        }

        GetLocationIdViaNameFromPath(uri);

        if (LocationId == null)
        {
            LocationId = (await GetCurrentLocation())?.ToString();

            if (LocationId == null)
            {
                // Not sure whether we're allowed to set parameters this way, but it's short-lived - we'll immediately navigate away after
                // preparing querystring
                LocationId = "aed87aa0-1d0c-44aa-8561-cde0fc936395";
            }
        }

        Guid locationId = Guid.Parse(LocationId);

        if (setupDefaultChartSeries)
        {
            SetUpDefaultCharts(locationId);
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

        if (ChartSeriesList == null)
        {
            ChartSeriesList = new List<ChartSeriesDefinition>();
        }

        if (tempMax != null)
        {
            ChartSeriesList.Add(
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
            ChartSeriesList.Add(
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await HandleRedraw();
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

        string title = $"Climate explorer{locationText}";

        Logger.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    string DayGroupingText(int dayGrouping)
    {
        switch (dayGrouping)
        {
            case 5:
                return "Groups of 5 days (73 groups)";
            case 7:
                return "Groups of 7 days (52 groups)";
            case 13:
                return "Groups of 13 days (28 groups)";
            case 14:
                return "Groups of 14 days (26 groups)";
            case 26:
                return "Groups of 26 days (14 groups)";
            case 28:
                return "Groups of 28 days (13 groups)";
            case 73:
                return "Groups of 73 days (5 groups)";
            case 91:
                return "Groups of 91 days (4 groups)";
            case 182:
                return "Groups of 182 days (2 groups)";
        }
        throw new NotImplementedException(dayGrouping.ToString());
    }

    private async Task OnDownloadDataClicked()
    {
        var fileStream = Exporter.ExportChartData(Logger, ChartSeriesWithData, Locations, ChartBins, NavManager.Uri.ToString());

        var locationNames = ChartSeriesWithData.SelectMany(x => x.ChartSeries.SourceSeriesSpecifications).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : "";

        fileName = $"Export-{fileName}-{SelectedBinGranularity}-{ChartBins.First().Label}-{ChartBins.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }

    async Task OnSelectedDayGroupingChanged(short value)
    {
        SelectingDayGrouping = value;
    }

    async Task OnDayGroupThresholdTextChanged(string value)
    {
        GroupingThresholdText = value;
    }

    async Task ApplyYearlyAverageParameters()
    {
        UserOverridePresetAggregationSettings = true;
        InternalGroupingThreshold = float.Parse(GroupingThresholdText) / 100;
        SelectedDayGrouping = SelectingDayGrouping == 0 ? SelectedDayGrouping : SelectingDayGrouping;
        await BuildDataSets();
    }

    async Task ClearUserAggregationOverride()
    {
        UserOverridePresetAggregationSettings = false;
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

    private Task ShowAddDataSetModal()
    {
        return addDataSetModal.Show();
    }

    private Task ShowOptionsModal()
    {
        GroupingThresholdText = MathF.Round(InternalGroupingThreshold * 100, 0).ToString();
        return optionsModal.Show();
    }

    async Task ShowFilterModal()
    {
        await filter.Show();
    }

    public async Task OnChartPresetSelected(List<ChartSeriesDefinition> chartSeriesDefinitions)
    {
        SelectedBinGranularity = chartSeriesDefinitions.First().BinGranularity;

        ChartSeriesList = chartSeriesDefinitions.ToList();

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

    async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        Logger.LogInformation("Adding dle " + dle.Name);

        ChartSeriesList =
            ChartSeriesList
            .Concat(
                new List<ChartSeriesDefinition>()
                {
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = dle.SeriesDerivationType,
                        SourceSeriesSpecifications = dle.SourceSeriesSpecifications.Select(BuildSourceSeriesSpecification).ToArray(),
                        Aggregation = dle.SeriesAggregation,
                        BinGranularity = SelectedBinGranularity,
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

    async Task OnSelectedYearsChanged(List<short> values)
    {
        if (!SelectedYears.Any() && values.Count == 0)
        {
            SelectedBinGranularity = BinGranularities.ByYear;
            await InvokeAsync(StateHasChanged);

            RebuildChartSeriesListToReflectSelectedYears();

            await BuildDataSets();
            return;
        }

        var validValues = new List<short>();
        foreach (var value in values)
        {
            if (DatasetYears.Any(x => x == value))
            {
                validValues.Add(value);
            }
        }
        SelectedYears = validValues;

        SelectedBinGranularity = BinGranularities.ByMonthOnly;

        await InvokeAsync(StateHasChanged);
        RebuildChartSeriesListToReflectSelectedYears();

        await BuildDataSets();
    }

    async Task OnSelectedBinGranularityChanged(BinGranularities value, bool rebuildDataSets = true)
    {
        SelectedBinGranularity = value;

        foreach (var csd in ChartSeriesList)
        {
            csd.BinGranularity = value;
        }

        ChartSeriesList = ChartSeriesList.CreateNewListWithoutDuplicates();

        if (EnableRangeSlider == null && SelectedBinGranularity == BinGranularities.ByYearAndMonth)
        {
            await ShowRangeSliderChanged(true);
        }

        if (rebuildDataSets)
        {
            await BuildDataSets();
        }
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
                    SelectedBinGranularity = csdList.First().BinGranularity;
                }

                Logger.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

                ChartSeriesList = csdList.ToList();

                await BuildDataSets();

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }
    }

    void LogChartSeriesList()
    {
        Logger.LogInformation("ChartSeriesList: (SelectedBinGranularity is " + SelectedBinGranularity + ")");

        foreach (var csd in ChartSeriesList)
        {
            Logger.LogInformation("    " + csd.ToString());
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

        LogChartSeriesList();

        // Recalculate the URL
        string chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(ChartSeriesList);

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
            ChartSeriesWithData = await RetrieveDataSets(ChartSeriesList);

            l.LogInformation("Set ChartSeriesWithData after call to RetrieveDataSets(). ChartSeriesWithData now has " + ChartSeriesWithData.Count + " entries.");

            // Render the series
            await HandleRedraw();

            if (SelectedLocation != null && mapContainer != null)
            {
                await mapContainer.ScrollToPoint(new LatLng(SelectedLocation.Coordinates.Latitude, SelectedLocation.Coordinates.Longitude));
            }
        }

        l.LogInformation("leaving");
    }

    public void RebuildChartSeriesListToReflectSelectedYears()
    {
        var years = SelectedYears.Any() ? SelectedYears.Select(x => (short?)x).ToList() : new List<short?>() { null };

        List<ChartSeriesDefinition> newCsds = new List<ChartSeriesDefinition>();

        var uniqueChartSeriesList = ChartSeriesList.Distinct(new ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked()).ToArray();

        foreach (var csd in uniqueChartSeriesList)
        {
            foreach (var year in years)
            {
                newCsds.Add(
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = csd.SourceSeriesSpecifications,
                        Aggregation = csd.Aggregation,
                        BinGranularity = year == null ? BinGranularities.ByYear : BinGranularities.ByYearAndMonth,
                        DisplayStyle = csd.DisplayStyle,
                        IsLocked = csd.IsLocked,
                        ShowTrendline = csd.ShowTrendline,
                        Smoothing = csd.Smoothing,
                        SmoothingWindow = csd.SmoothingWindow,
                        Value = csd.Value,
                        Year = year,
                        SeriesTransformation = csd.SeriesTransformation,
                        GroupingThreshold = csd.GroupingThreshold,
                    }
                );
            }
        }

        Logger.LogInformation("RebuildChartSeriesListToReflectSelectedYears() setting ChartSeriesList");
        ChartSeriesList = newCsds;
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
                SelectedBinGranularity.IsModular() && cupAggregationFunction == ContainerAggregationFunctions.Sum
                ? ContainerAggregationFunctions.Mean
                : cupAggregationFunction;
            
            DataSet dataSet =
                await DataService.PostDataSet(
                    SelectedBinGranularity,
                    binAggregationFunction,
                    bucketAggregationFunction,
                    cupAggregationFunction,
                    csd.Value,
                    csd.SourceSeriesSpecifications.Select(BuildDataPrepSeriesSpecification).ToArray(),
                    csd.SeriesDerivationType,
                    GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold),
                    SelectedDayGrouping,
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

    private float GetGroupingThreshold(float? groupingThreshold, bool binGranularityIsLinear = false)
    {
        // If we're in linear time, all buckets in a bin must have passed the data completeness test.
        // Otherwise, we apply GroupingThreshold from either user input or ChartSeriesDefinition
        return binGranularityIsLinear
            ? 1.0f 
            : (UserOverridePresetAggregationSettings || groupingThreshold == null) 
                ? InternalGroupingThreshold
                : groupingThreshold.Value;
    }

    private string GetGroupingThresholdText()
    {
        var groupingThreshold = ChartSeriesList.FirstOrDefault() == null ? null : ChartSeriesList.First().GroupingThreshold;

        return UserOverridePresetAggregationSettings
            ? $"{InternalGroupingThreshold * 100}% (user override)"
            : groupingThreshold == null
                    ? $"{InternalGroupingThreshold * 100}%"
                    : $"{groupingThreshold * 100}% (preset defined)";
    }

    async Task OnSelectedYearsChanged(ExtentValues extentValues)
    {
        await ChangeStartYear(extentValues.FromValue, false);
        await ChangeEndYear(extentValues.ToValue, false);
        await HandleRedraw();
    }

    async Task OnStartYearTextChanged(string text)
    {
        await ChangeStartYear(text, true);
    }

    private async Task ChangeStartYear(string text, bool redraw)
    {
        SelectedStartYear = text;
        UseMostRecentStartYear = false;
        SliderStart = Convert.ToInt32(SelectedStartYear);
        if (redraw)
        {
            await HandleRedraw();
        }
    }

    async Task OnEndYearTextChanged(string text)
    {
        await ChangeEndYear(text, true);
    }

    private async Task ChangeEndYear(string text, bool redraw)
    {
        SelectedEndYear = text;
        SliderEnd = Convert.ToInt32(SelectedEndYear);
        if (redraw)
        {
            await HandleRedraw();
        }
    }

    async Task OnUseMostRecentStartYearChanged(bool value)
    {
        UseMostRecentStartYear = value;
        if (value)
        {
            SliderStart = null;
            SelectedStartYear = null;
        }

        await HandleRedraw();
    }

    async Task HandleRedraw()
    {
        var l = new LogAugmenter(Logger, "HandleRedraw");

        l.LogInformation("Entering");

        // This can happen at startup, or if the user switches off all data series
        if (ChartSeriesWithData == null || ChartSeriesWithData.Count == 0 || chart == null || chartTrendline == null)
        {
            l.LogInformation("Bailing early as no chart data available");

            return;
        }

        LogChartSeriesList();

        await chart.Clear();

        // We used to choose set ChartType to Bar if the user's selected chart type was bar or difference or rainfall,
        // and line otherwise.
        //
        // Since v2, we now set ChartType to Bar if any series is of type Bar, and Line otherwise.
        var newInternalChartType =
            ChartSeriesWithData.Any(x => x.ChartSeries.DisplayStyle == SeriesDisplayStyle.Bar)
            ? ChartType.Bar
            : ChartType.Line;

        if (newInternalChartType != InternalChartType)
        {
            InternalChartType = newInternalChartType;

            await chart.ChangeType(newInternalChartType);
        }

        colours = new ColourServer();

        var subtitle = string.Empty;

        List<ChartTrendlineData> trendlines = null;

        var title = ChartLogic.BuildChartTitle(ChartSeriesWithData);

        // Data sets sometimes have internal gaps in data (i.e. years which have no data even though earlier
        // and later years have data). Additionally, they may have external gaps in data if the overall period
        // to be charted goes beyond the range of the available data in one particular data set.
        //
        // To ensure these gaps are handled correctly in the plotted chart, we build a new dataset that includes
        // records for each missing year. Value is set to null for those records.

        l.LogInformation("Calling BuildProcessedDataSets");

        BuildProcessedDataSets(ChartSeriesWithData, UseMostRecentStartYear);

        subtitle =
            (ChartStartBin != null & ChartEndBin != null)
            ? $"({ChartStartBin.Label}-{ChartEndBin.Label})"
            : SelectedBinGranularity.ToFriendlyString();

        l.LogInformation("Calling AddDataSetsToGraph");

        trendlines = await AddDataSetsToChart();

        l.LogInformation("Trendlines count: " + trendlines.Count);

        l.LogInformation("Calling AddLabels");

        var labels = ChartBins.Select(x => x.Label).ToArray();
        await chart.AddLabels(labels);
        
        dynamic scales = BuildChartScales();

        object chartOptions = new
        {
            Animation = false,
            Responsive = true,
            MaintainAspectRatio = false,
            SpanGaps = false,
            Plugins = new
            {
                Title = new
                {
                    Text = title,
                    Display = true
                },
                Subtitle = new
                {
                    Text = subtitle,
                    Display = true
                },
            },
            Scales = scales,
            //Parsing = false
        };

        await chart.SetOptionsObject(chartOptions);

        if (trendlines != null && trendlines.Count > 0)
        {
            await chartTrendline.AddTrendLineOptions(trendlines);
        }

        await chart.Update();

        // The below line is required to get the chart.js component to honour the styling applied on the parent div
        // If you don't call resize, the chart will apply the styling only after you resize the window,
        // but it does not apply the style on the initial load of the page.
        // See https://www.chartjs.org/docs/latest/configuration/responsive.html for more information
        if (!_haveCalledResizeAtLeastOnce)
        {
            await chart.Resize();
            _haveCalledResizeAtLeastOnce = true;
        }

        l.LogInformation("Leaving");
    }

    private dynamic BuildChartScales()
    {
        dynamic scales = new ExpandoObject();

        var xLabel = ChartLogic.GetXAxisLabel(SelectedBinGranularity);

        // The casing on the names of the scales is case-sensitive.
        // The x axis needs to be x (not X) or the chart will not register the scale correctly.
        // This will at least break the trendlines component
        scales.x = new
        {
            Title = new
            {
                Text = xLabel,
                Display = true,
                Color = "blue",
            },
        };

        var axes = new List<string>();
        foreach (var s in ChartSeriesList)
        {
            var uom = s.SourceSeriesSpecifications.First().MeasurementDefinition.UnitOfMeasure;
            var axisId = ChartLogic.GetYAxisId(s.SeriesTransformation, uom);
            if (!axes.Contains(axisId))
            {
                ((IDictionary<string, object>)scales).Add(
                    axisId,
                    new
                    {
                        Display = true,
                        Axis = "y",
                        Position = axes.Count % 2 == 0 ? "left" : "right",
                        Grid = new { DrawOnChartArea = false },
                        Title = new
                        {
                            Text = UnitOfMeasureLabel(s.SeriesTransformation, uom),
                            Display = true,
                            Color = "blue",
                        },
                    });
                axes.Add(axisId);
            }
        }

        return scales;
    }

    async Task<List<ChartTrendlineData>> AddDataSetsToChart()
    {
        var dataSetIndex = 0;

        colours = new ColourServer();

        var trendlines = new List<ChartTrendlineData>();

        var requestedColours = ChartSeriesWithData
            .Where(x => x.ChartSeries.RequestedColour != Colours.AutoAssigned)
            .Select(x => x.ChartSeries.RequestedColour)
            .ToList();

        foreach (var chartSeries in ChartSeriesWithData)
        {
            var dataSet = chartSeries.ProcessedDataSet;
            var htmlColourCode = colours.GetNextColour(chartSeries.ChartSeries.RequestedColour, requestedColours);
            var renderSmallPoints = IsMobileDevice || dataSet.DataRecords.Count > 400;
            var defaultLabel = IsMobileDevice 
                ? chartSeries.ChartSeries.GetFriendlyTitleShort() 
                : $"{chartSeries.ChartSeries.FriendlyTitle} | {UnitOfMeasureLabelShort(dataSet.MeasurementDefinition.UnitOfMeasure)}";
            

            await ChartLogic.AddDataSetToChart(
                chart,
                chartSeries,
                dataSet,
                GetChartLabel(chartSeries.ChartSeries.SeriesTransformation, defaultLabel, chartSeries.ChartSeries.Aggregation),
                htmlColourCode,
                renderSmallPoints: renderSmallPoints);

            if (chartSeries.ChartSeries.ShowTrendline)
            {
                trendlines.Add(ChartLogic.CreateTrendline(dataSetIndex, ChartColor.FromHtmlColorCode(htmlColourCode)));
            }

            dataSetIndex++;
        }

        return trendlines;
    }

    private string GetChartLabel(SeriesTransformations seriesTransformation, string defaultLabel, SeriesAggregationOptions seriesAggregationOptions)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.IsFrosty                      => "Number of days of frost",
            SeriesTransformations.DayOfYearIfFrost              => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "Last day of frost" : "First day of frost",
            SeriesTransformations.EqualOrAbove35                => "Number of days 35°C or above",
            SeriesTransformations.EqualOrAbove1                 => "Number of days with 1mm of rain or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10    => "Number of days between 1mm and 10mm of rain",
            SeriesTransformations.EqualOrAbove10                => "Number of days with 10mm of rain or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25   => "Number of days between 10mm and 25mm of rain",
            SeriesTransformations.EqualOrAbove25                => "Number of days with 25mm of rain or more",
            _ => defaultLabel,
        };
    }

    void BuildProcessedDataSets(List<SeriesWithData> chartSeriesWithData, bool useMostRecentStartYear = true)
    {
        var l = new LogAugmenter(Logger, "BuildProcessedDataSets");

        l.LogInformation("entering");

        // If we're doing smoothing via the moving average, precalculate these data and add them to PreProcessedDataSets.
        // We do this because the SimpleMovingAverage calculate function will remove some years from the start of the data set.
        // It removes these years because it doesn't have a good enough average to present it to the user.
        // Therefore, we need to calculate the smoothing before we calculate the start year - the basis for labelling the chart
        // If we're not calculating a moving average, PreProcessedDataSets = SourceDataSets
        foreach (var cs in chartSeriesWithData)
        {
            // We only support moving averages on linear bin granularities (e.g. Year, YearAndMonth) - not modular ones like MonthOnly
            if (SelectedBinGranularity.IsLinear() && cs.ChartSeries.Smoothing == SeriesSmoothingOptions.MovingAverage)
            {
                var movingAverageValues =
                    cs.SourceDataSet.DataRecords
                    .Select(x => x.Value)
                    .CalculateCentredMovingAverage(cs.ChartSeries.SmoothingWindow, 0.75f);

                // Now, join back to the original DataRecord set
                var newDataRecords =
                    movingAverageValues
                    .Zip(
                        cs.SourceDataSet.DataRecords,
                        (val, dr) => new DataRecord
                        {
                            Label = dr.Label,
                            BinId = dr.BinId,
                            Value = val
                        }
                    )
                    .ToList();

                cs.PreProcessedDataSet =
                    new DataSet
                    {
                        Location = cs.SourceDataSet.Location,
                        MeasurementDefinition = cs.SourceDataSet.MeasurementDefinition,
                        DataRecords = newDataRecords
                    };
            }
            else
            {
                cs.PreProcessedDataSet = cs.SourceDataSet;
            }
        }

        l.LogInformation("done with moving average calculation");

        // There must be exactly one bin granularity or else something odd's going on.
        var binGranularity = chartSeriesWithData.Select(x => x.ChartSeries.BinGranularity).Distinct().Single();

        if (binGranularity != SelectedBinGranularity)
        {
            throw new Exception($"BinGranularity selected for series ({binGranularity}) doesn't match overall selected granularity {SelectedBinGranularity}");
        }

        BinIdentifier[] chartBins = null;

        switch (binGranularity)
        {
            case BinGranularities.ByYear:
            case BinGranularities.ByYearAndMonth:
                // Calculate first and last year which we have a data record for, across all data sets underpinning all chart series
                var preProcessedDataSets = chartSeriesWithData.Select(x => x.PreProcessedDataSet);
                var allDataRecords = preProcessedDataSets.SelectMany(x => x.DataRecords);

                (ChartStartBin, ChartEndBin) =
                    ChartLogic.GetBinRangeToPlotForGaplessRange(
                        // Pass in the data available for plotting
                        preProcessedDataSets,
                        // and the user's preferences about what x axis range they'd like plotted
                        useMostRecentStartYear,
                        SelectedStartYear,
                        SelectedEndYear);

                chartBins = BinHelpers.EnumerateBinsInRange(ChartStartBin, ChartEndBin).ToArray();

                // build a list of all the years in which data sets start, used by the UI to allow the user to conveniently select from them
                StartYears = preProcessedDataSets.Select(x => x.GetStartYearForDataSet()).Distinct().OrderBy(x => x).ToList();
                SliderMin = StartYears.Min();
                if (SliderStart < SliderMin)
                {
                    SliderStart = SliderMin;
                }

                var lastYears = preProcessedDataSets.Select(x => x.GetEndYearForDataSet()).Distinct().OrderBy(x => x).ToList();
                SliderMax = EndYear = lastYears.Max();
                if (SliderEnd > SliderMax)
                {
                    SliderEnd = SliderMax;
                }

                break;

            case BinGranularities.ByMonthOnly:
            case BinGranularities.BySouthernHemisphereTemperateSeasonOnly:
            case BinGranularities.BySouthernHemisphereTropicalSeasonOnly:
                ChartStartBin = null;
                ChartEndBin = null;
                chartBins = BinHelpers.GetBinsForModularGranularity(binGranularity);
                break;

            default:
                throw new NotImplementedException($"binGranularity {binGranularity}");
        }

        foreach (var cs in chartSeriesWithData)
        {
            l.LogInformation("constructing ProcessedDataSet");

            var recordsByBinId = cs.PreProcessedDataSet.DataRecords.ToLookup(x => x.BinId);

            l.LogInformation("First chart bin: " + chartBins.First() + ", last chart: " + chartBins.Last());

            // Create new datasets, same as the source, but with any gaps filled with null records
            cs.ProcessedDataSet =
                new DataSet
                {
                    Location = cs.PreProcessedDataSet.Location,
                    MeasurementDefinition = cs.PreProcessedDataSet.MeasurementDefinition,
                    DataRecords =
                        chartBins
                        .Select(
                            bin =>
                            // If there's a record in the source dataset, use it
                            recordsByBinId[bin.Id].SingleOrDefault()
                            // Otherwise, create a null record
                            ?? new DataRecord { BinId = bin.Id, Value = null }
                        )
                        .ToList()
                };
        }

        ChartBins = chartBins;

        // Now, we cut down the processed datasets to just the bins that we intend to display on the chart.
        // This should only affect linear (gapless) BinGranularities, but executes either way, in case we
        // later allow users to say "just give me month-ignoring-year, but only for months after 4 and before 7",
        // for example.
        HashSet<string> binIdsToPlot = new HashSet<string>(ChartBins.Select(x => x.Id));
        foreach (var cswd in ChartSeriesWithData)
        {
            cswd.ProcessedDataSet.DataRecords =
                cswd.ProcessedDataSet.DataRecords
                .Where(x => binIdsToPlot.Contains(x.BinId))
                .ToList();
        }

        l.LogInformation("leaving");
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

        List<ChartSeriesDefinition> additionalCsds = new List<ChartSeriesDefinition>();

        // Update data series to reflect new location
        foreach (var csd in ChartSeriesList.ToArray())
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
                            ChartSeriesList.Remove(csd);

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

        var draftList = ChartSeriesList.Concat(additionalCsds).ToList();

        ChartSeriesList = draftList.CreateNewListWithoutDuplicates();

        await BuildDataSets();
    }

    async Task OnLineChartClicked(ChartMouseEventArgs e)
    {
        if (SelectedBinGranularity != BinGranularities.ByYear)
        {
            // TODO: Add support for SelectedBinGranularity != BinGranularities.ByYear
            return;
        }

        var startYear = UseMostRecentStartYear 
            ? StartYears.Last()
            : SelectedStartYear != null 
                ? Convert.ToInt16(SelectedStartYear) 
                : throw new NotImplementedException();

        var year = (short)(startYear + e.Index);

        var dataType = ChartSeriesWithData[e.DatasetIndex].SourceDataSet.DataType;
        var dataAdjustment = ChartSeriesWithData[e.DatasetIndex].SourceDataSet.DataAdjustment;

        await HandleOnYearFilterChange(new YearAndDataTypeFilter(year) { DataType = dataType, DataAdjustment = dataAdjustment });
    }

    async Task ShowRangeSliderChanged(bool? value)
    {
        EnableRangeSlider = value;
        if (EnableRangeSlider.GetValueOrDefault() && SliderStart == null)
        {
            var rangeStart = (int)((EndYear - StartYears.Max()) * .2);
            await OnStartYearTextChanged((EndYear - rangeStart).ToString());
        }
    }

    async Task OnClearFilter()
    {
        UseMostRecentStartYear = true;
        SelectedStartYear = null;
        SelectedEndYear = null;
        SliderStart = null;
        SliderEnd = null;
        EnableRangeSlider = false;

        await HandleRedraw();
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

    async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await OnSelectedBinGranularityChanged(BinGranularities.ByMonthOnly, false);

        var chartWithData = ChartSeriesWithData
            .First(x => 
            (x.SourceDataSet.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
            (x.SourceDataSet.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null));

        var chartSeries = ChartSeriesList
            .First(x => x.SourceSeriesSpecifications.Any(y => 
               (y.MeasurementDefinition.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
               (y.MeasurementDefinition.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null)));

        ChartSeriesList =
            ChartSeriesList
            .Concat(
                new List<ChartSeriesDefinition>()
                {
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = chartWithData.ChartSeries.SourceSeriesSpecifications,
                        Aggregation = chartSeries.Aggregation,
                        BinGranularity = SelectedBinGranularity,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 5,
                        Value = SeriesValueOptions.Value,
                        Year = yearAndDataTypeFilter.Year,
                    }
                }
            )
            .ToList();

        await BuildDataSets();
    }
}
