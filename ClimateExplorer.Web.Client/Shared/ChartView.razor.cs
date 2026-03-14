namespace ClimateExplorer.Web.Client.Shared;

using Blazorise;
using Blazorise.Charts;
using Blazorise.Charts.Trendline;
using Blazorise.Snackbar;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using CurrentDevice;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System;
using System.Dynamic;
using static ClimateExplorer.Core.Enums;

public partial class ChartView
{
    private bool haveCalledResizeAtLeastOnce = false;

    private Chart<double?>? chart;
    private ChartTrendline<double?>? chartTrendline;

    private BinIdentifier? chartStartBin;
    private BinIdentifier? chartEndBin;

    private InfoPanel? aggregationOptionsInfoPanel;
    private InfoPanel? chartOptionsInfoPanel;

    private string? groupingThresholdText;

    private bool updateUiStateInProcess;
    private bool buildDataSetsInProcess;

    [Parameter]
    public string? PageName { get; set; }

    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public Guid? LocationId { get; set; }

    [Parameter]
    public bool ChartAllData { get; set; }

    [Parameter]
    public Dictionary<Guid, Location>? LocationDictionary { get; set; }

    [Parameter]
    public IEnumerable<Region>? Regions { get; set; }

    [Parameter]
    public EventCallback<DataDownloadPackage> DownloadDataEvent { get; set; }

    [Parameter]
    public EventCallback ShowAddDataSetModalEvent { get; set; }

    [Parameter]
    public EventCallback<SnackbarMessage> SnackbarMessageEvent { get; set; }

    [Inject]
    private IDataService? DataService { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    [Inject]
    private ILogger<Index>? Logger { get; set; }

    [Inject]
    private NavigationManager? NavManager { get; set; }

    private bool ChartLoadingIndicatorVisible { get; set; }
    private bool ChartLoadingErrored { get; set; }
    private BinGranularities SelectedBinGranularity { get; set; } = BinGranularities.ByYear;
    private List<SeriesWithData>? ChartSeriesWithData { get; set; }
    private Guid? InternalLocationId { get; set; }
    private string? SelectedStartYear { get; set; }
    private string? SelectedEndYear { get; set; }
    private short SelectedGroupingDays { get; set; }
    private string? GroupingThresholdText
    {
        get
        {
            return groupingThresholdText;
        }
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                groupingThresholdText = value;
                InternalGroupingThreshold = float.Parse(value) / 100;
            }
        }
    }

    private List<ChartSeriesDefinition>? ChartSeriesList { get; set; } = new List<ChartSeriesDefinition>();

    private short SelectingGroupingDays { get; set; }

    private float InternalGroupingThreshold { get; set; } = .7f;

    private bool UserOverridePresetAggregationSettings { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    private BinIdentifier[]? ChartBins { get; set; }

    private bool? IsMobileDevice { get; set; }

    private bool? EnableRangeSlider { get; set; }
    private int SliderMin { get; set; }
    private int SliderMax { get; set; }
    private int? SliderStart { get; set; }
    private int? SliderEnd { get; set; }

    /// <summary>
    /// Gets or sets the chart type selected by the user on the options page.
    /// </summary>
    private ChartType SelectedChartType { get; set; }
    private List<short>? SelectedYears { get; set; }
    private List<short>? StartYears { get; set; }
    private short EndYear { get; set; }

    private ColourServer Colours { get; set; } = new ColourServer();

    private Modal? OptionsModal { get; set; }

    /// <summary>
    /// The chart type applied to the chart control. If any series is in "Bar" mode, we switch
    /// the entire chart to Bar type to ensure it renders, at the cost of a small misalignment
    /// between grid lines and datapoints for any line series that are being displayed.
    /// Otherwise, we display in "Line" mode to avoid that cost.
    /// </summary>
    private ChartType InternalChartType { get; set; }

    public async Task OnAddDataSet(DataSetLibraryEntry dle, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        Logger!.LogInformation("Adding dle " + dle.Name);

        ChartSeriesList =
            ChartSeriesList!
            .Concat(
                [
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = dle.SeriesDerivationType,
                        SourceSeriesSpecifications = dle.SourceSeriesSpecifications!.Select(x => BuildSourceSeriesSpecification(x, dataSetDefinitions)).ToArray(),
                        Aggregation = dle.SeriesAggregation,
                        BinGranularity = SelectedBinGranularity,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                    },
                ])
            .ToList();

        await BuildDataSets();
    }

    public async Task OnChartPresetSelected(SuggestedChartPresetModel chartPresetModel)
    {
        if (chartPresetModel.ChartSeriesList == null || !chartPresetModel.ChartSeriesList.Any())
        {
            throw new Exception("No chart series definition passed as a parameter when preset was selected");
        }

        ChartAllData = chartPresetModel.ChartAllData;
        SelectedStartYear = chartPresetModel.StartYear?.ToString();
        SelectedEndYear = chartPresetModel.EndYear?.ToString();

        SelectedBinGranularity = chartPresetModel.ChartSeriesList.First().BinGranularity;

        ChartSeriesList = chartPresetModel.ChartSeriesList;

        await BuildDataSets();
    }

    public async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await OnSelectedBinGranularityChanged(BinGranularities.ByMonthOnly, false);

        var chartWithData = ChartSeriesWithData!
            .First(x =>
            (x.SourceDataSet!.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
            (x.SourceDataSet.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null));

        var chartSeries = ChartSeriesList!
            .First(x => x.SourceSeriesSpecifications!.Any(y =>
               (y.MeasurementDefinition!.DataType == yearAndDataTypeFilter.DataType || yearAndDataTypeFilter.DataType == null) &&
               (y.MeasurementDefinition.DataAdjustment == yearAndDataTypeFilter.DataAdjustment || yearAndDataTypeFilter.DataAdjustment == null)));

        ChartSeriesList =
            ChartSeriesList!
            .Concat(
                [
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = chartWithData.ChartSeries!.SourceSeriesSpecifications,
                        Aggregation = chartSeries.Aggregation,
                        BinGranularity = SelectedBinGranularity,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = yearAndDataTypeFilter.Year,
                    },
                ])
            .ToList();

        await BuildDataSets();
    }

    protected override void OnInitialized()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;
        updateUiStateInProcess = false;

        SelectedYears = [];

        SelectedGroupingDays = 14;
        GroupingThresholdText = "70";

        SliderMax = DateTime.Now.Year;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        IsMobileDevice ??= await CurrentDeviceService!.Mobile();

        if (DataSetDefinitions is not null && Regions is not null)
        {
            if (LocationId is not null)
            {
                if (InternalLocationId is not null && InternalLocationId != LocationId)
                {
                    InternalLocationId = LocationId;
                    await ChangedLocation();
                }

                if (ChartLoadingIndicatorVisible)
                {
                    var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
                    if (uri.Query.Contains("csd="))
                    {
                        await UpdateUiStateBasedOnQueryString(uri);
                    }
                    else if (InternalLocationId is null)
                    {
                        // First time loading a page with a location in the URL. Just add a default chart with temperatute (and probably precipitation) data, so that the user sees something.
                        InternalLocationId = LocationId;
                        await SetUpLocalDefaultCharts(InternalLocationId.Value);
                    }
                    else if (uri.Query.Contains("chartAllData="))
                    {
                        // We have a chartAllData parameter in the query string, but no chart series definition.
                        // This means that the user has likely removed all charts series.
                        await RenderChart();
                    }
                }
            }
            else
            {
                if (ChartLoadingIndicatorVisible)
                {
                    var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
                    if (uri.Query.Contains("csd="))
                    {
                        await UpdateUiStateBasedOnQueryString(uri);
                    }
                    else if (!uri.Query.Contains("chartAllData="))
                    {
                        // We haven't setup and chart series yet - first time loading a page without a location in the URL.
                        // Just add a default chart with CO2 data, so that the user sees something, and also so that we have some data to work with when demonstrating the various options on the page.
                        await AddDefaultChart();
                        StateHasChanged();
                    }
                    else if (uri.Query.Contains("chartAllData="))
                    {
                        // We have a chartAllData parameter in the query string, but no chart series definition.
                        // This means that the user has likely removed all charts series.
                        await RenderChart();
                    }
                }
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
        var l = new LogAugmenter(Logger!, "BuildDataSets");
        l.LogInformation("Starting");

        if (buildDataSetsInProcess)
        {
            return;
        }

        buildDataSetsInProcess = true;

        LoadingChart();

        // Recalculate the URL
        string chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(ChartSeriesList!);

        var queryString = new Uri(NavManager!.Uri).Query;
        var queryDictionary = System.Web.HttpUtility.ParseQueryString(queryString);

        var url = GetGlobalQueryStringSettings();

        if (chartSeriesUrlComponent.Length > 0)
        {
            url += "&csd=" + chartSeriesUrlComponent;
        }
        else
        {
            ChartSeriesWithData = null;
        }

        string currentUri = NavManager!.Uri;
        string newUri = NavManager.ToAbsoluteUri(url).ToString();

        if (currentUri != newUri)
        {
            l.LogInformation("Because the URI reflecting current UI state is different to the URI we're currently at, triggering navigation. After navigation occurs, the UI state will update accordingly.");

            bool shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds = currentUri.IndexOf("csd=") == -1;

            // Just let the navigation process trigger the UI updates
            // await NavigateTo(url, shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds);
            NavManager!.NavigateTo(url, false, shouldJustReplaceCurrentUrlBecauseWeAreAddingInQueryStringParametersForCsds);
        }
        else
        {
            l.LogInformation("Not calling NavigationManager.NavigateTo().");

            var usableChartSeries = ChartSeriesList!.Where(x => x.DataAvailable);

            // Fetch the data required to render the selected data series
            ChartSeriesWithData = await RetrieveDataSets(usableChartSeries);

            l.LogInformation("Set ChartSeriesWithData after call to RetrieveDataSets(). ChartSeriesWithData now has " + usableChartSeries.Count() + " entries.");

            // Render the series
            await RenderChart();
        }

        buildDataSetsInProcess = false;

        l.LogInformation("Leaving");
    }

    protected async Task RenderChart()
    {
        var l = new LogAugmenter(Logger!, "RenderChart");

        l.LogInformation("Entering");

        if (chart is null)
        {
            l.LogInformation("Bailing early as no chart available");
            return;
        }

        await chart.Clear();

        var title = string.Empty;
        var subtitle = string.Empty;
        List<ChartTrendlineData>? trendlines = null;
        dynamic scales = new ExpandoObject();

        if (ChartLoadingErrored)
        {
            await chart.SetOptionsObject(new { });

            l.LogError("We have identified an error. Will not try to render the chart normally");
        }
        else if (ChartSeriesWithData == null || ChartSeriesWithData.Count == 0 || chartTrendline == null)
        {
            await chart.SetOptionsObject(new { });

            ChartLoadingIndicatorVisible = false;
            StateHasChanged();

            l.LogInformation("Bailing early as no chart data available");
            return;
        }
        else
        {
            LogChartSeriesList();

            // We now set ChartType to Bar if any series is of type Bar, and Line otherwise.
            var newInternalChartType =
                ChartSeriesWithData.Any(x => x.ChartSeries!.DisplayStyle == SeriesDisplayStyle.Bar)
                ? ChartType.Bar
                : ChartType.Line;

            if (newInternalChartType != InternalChartType)
            {
                InternalChartType = newInternalChartType;

                await chart.ChangeType(newInternalChartType);
            }

            Colours = new ColourServer();

            // Data sets sometimes have internal gaps in data (i.e. years which have no data even though earlier
            // and later years have data). Additionally, they may have external gaps in data if the overall period
            // to be charted goes beyond the range of the available data in one particular data set.
            //
            // To ensure these gaps are handled correctly in the plotted chart, we build a new dataset that includes
            // records for each missing year. Value is set to null for those records.
            l.LogInformation("Calling BuildProcessedDataSets");

            BuildProcessedDataSets(ChartSeriesWithData, ChartAllData);

            title = ChartLogic.BuildChartTitle(ChartSeriesWithData, LocationDictionary);
            subtitle = ChartLogic.BuildChartSubtitle(chartStartBin, chartEndBin, SelectedBinGranularity, IsMobileDevice!.Value, SelectedGroupingDays, GetGroupingThresholdText());

            l.LogInformation("Calling AddDataSetsToGraph");

            trendlines = await AddDataSetsToChart();

            l.LogInformation("Trendlines count: " + trendlines.Count);

            l.LogInformation("Calling AddLabels");

            var labels = ChartBins!.Select(x => x.Label).ToArray();
            await chart.AddLabels(labels);

            scales = BuildChartScales();

            var chartOptions = CreateChartOptions(title, subtitle, scales);

            await chart.SetOptionsObject(chartOptions);

            if (trendlines != null && trendlines.Count > 0)
            {
                await chartTrendline.AddTrendLineOptions(trendlines);
            }
        }

        await chart.Update();

        // The below line is required to get the chart.js component to honour the styling applied on the parent div
        // If you don't call resize, the chart will apply the styling only after you resize the window,
        // but it does not apply the style on the initial load of the page.
        // See https://www.chartjs.org/docs/latest/configuration/responsive.html for more information
        if (!haveCalledResizeAtLeastOnce && !ChartLoadingErrored)
        {
            await chart.Resize();
            haveCalledResizeAtLeastOnce = true;
        }

        ChartLoadingIndicatorVisible = false;
        StateHasChanged();

        l.LogInformation("Leaving");
    }

    protected void LoadingChart()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;
        LogChartSeriesList();
    }

    protected async Task ChangedLocation()
    {
        var additionalCsds = new List<ChartSeriesDefinition>();

        // Update data series to reflect new location
        foreach (var csd in ChartSeriesList!.ToArray())
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
                    if (csd.SourceSeriesSpecifications.Length == 1 && !Regions!.Any(x => x.Id == sss.LocationId))
                    {
                        sss.LocationId = LocationId!.Value;
                        sss.LocationName = LocationDictionary![LocationId!.Value].Name;

                        var dataMatches = new List<DataSubstitute>
                        {
                            new ()
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
                                LocationId!.Value,
                                dataMatches,
                                throwIfNoMatch: false);

                        if (dsd == null)
                        {
                            var dataType = ChartSeriesDefinition.MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType);

                            await SnackbarMessageEvent.InvokeAsync(new SnackbarMessage { Message = $"{dataType} data is not available at {LocationDictionary[LocationId.Value].FullTitle}", Type = SnackbarColor.Warning });
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
                                        LocationId = LocationId!.Value,
                                        LocationName = LocationDictionary![LocationId!.Value].Name,
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

        var draftList = ChartSeriesList.Concat(additionalCsds).ToList();

        ChartSeriesList = draftList.CreateNewListWithoutDuplicates();

        await BuildDataSets();
    }

    private static ContainerAggregationFunctions MapSeriesAggregationOptionToBinAggregationFunction(SeriesAggregationOptions a)
    {
        return a switch
        {
            SeriesAggregationOptions.Mean => ContainerAggregationFunctions.Mean,
            SeriesAggregationOptions.Minimum => ContainerAggregationFunctions.Min,
            SeriesAggregationOptions.Maximum => ContainerAggregationFunctions.Max,
            SeriesAggregationOptions.Median => ContainerAggregationFunctions.Median,
            SeriesAggregationOptions.Sum => ContainerAggregationFunctions.Sum,
            _ => throw new NotImplementedException($"SeriesAggregationOptions {a}"),
        };
    }

    private object CreateChartOptions(string title, string subtitle, dynamic scales)
    {
        return new
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
                    Display = true,
                    Color = "black",
                },
                Subtitle = new
                {
                    Text = subtitle,
                    Display = true,
                    Color = "black",
                },
                Tooltip = new
                {
                    Mode = IsMobileDevice!.Value ? "point" : "index",
                    Intersect = false,
                },
                Legend = new
                {
                    Position = "bottom",
                },
            },
            Scales = scales,
        };
    }

    private string GetGlobalQueryStringSettings()
    {
        string url = PageName!;

        var chartAllData = ChartAllData.ToString() !.ToLower();
        var startYear = SelectedStartYear;
        var endYear = SelectedEndYear;

        url += "?chartAllData=" + chartAllData;
        if (!string.IsNullOrWhiteSpace(startYear))
        {
            url += $"&startYear={startYear}";
        }

        if (!string.IsNullOrWhiteSpace(endYear))
        {
            url += $"&endYear={endYear}";
        }

        var groupingDays = SelectedGroupingDays;
        if (SelectedGroupingDays > 0)
        {
            url += $"&groupingDays={groupingDays}";
        }

        var groupingThresholdText = GroupingThresholdText;
        if (!string.IsNullOrWhiteSpace(groupingThresholdText))
        {
            url += $"&groupingThreshold={groupingThresholdText}";
        }

        return url;
    }

    private async Task UpdateUiStateBasedOnQueryString(Uri uri)
    {
        if (DataSetDefinitions is null || Regions is null)
        {
            throw new NullReferenceException("DataSetDefinitions or Regions is null in UpdateUiStateBasedOnQueryString");
        }

        if (updateUiStateInProcess)
        {
            return;
        }

        updateUiStateInProcess = true;

        var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);

        ChartAllData = queryDictionary["chartAllData"] == null ? false : bool.Parse(queryDictionary["chartAllData"] !);
        SelectedStartYear = queryDictionary["startYear"];
        SelectedEndYear = queryDictionary["endYear"];
        SelectedGroupingDays = queryDictionary["groupingDays"] == null ? (short)14 : short.Parse(queryDictionary["groupingDays"] !);
        GroupingThresholdText = string.IsNullOrWhiteSpace(queryDictionary["groupingThreshold"]) ? "70" : queryDictionary["groupingThreshold"];

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
        {
            try
            {
                var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger!, csdSpecifier!, DataSetDefinitions!, LocationDictionary!, Regions);

                if (csdList.Any())
                {
                    SelectedBinGranularity = csdList.First().BinGranularity;
                }

                Logger!.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

                ChartSeriesList = csdList.ToList();

                try
                {
                    await BuildDataSets();
                    ChartLoadingErrored = false;
                }
                catch (Exception)
                {
                    await SnackbarMessageEvent.InvokeAsync(new SnackbarMessage { Message = "Failed to create the chart with the current settings", Type = SnackbarColor.Danger });
                    ChartLoadingErrored = true;
                    await RenderChart();
                }
            }
            catch (Exception ex)
            {
                Logger!.LogError(ex.ToString());
            }
        }

        updateUiStateInProcess = false;
    }

    private async Task<List<SeriesWithData>> RetrieveDataSets(IEnumerable<ChartSeriesDefinition> chartSeriesList)
    {
        var datasetsToReturn = new List<SeriesWithData>();

        Logger!.LogInformation("RetrieveDataSets: starting enumeration");

        List<Task<DataSet>> tasksToRun = [];

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

            var dataSetTask =
                DataService!.PostDataSet(
                    SelectedBinGranularity,
                    binAggregationFunction,
                    bucketAggregationFunction,
                    cupAggregationFunction,
                    csd.Value,
                    csd.SourceSeriesSpecifications!.Select(BuildDataPrepSeriesSpecification).ToArray(),
                    csd.SeriesDerivationType,
                    GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold, csd.BinGranularity.IsLinear()),
                    GetGroupingThreshold(csd.GroupingThreshold),
                    SelectedGroupingDays,
                    csd.SeriesTransformation,
                    csd.Year,
                    csd.MinimumDataResolution);

            tasksToRun.Add(dataSetTask);
        }

        datasetsToReturn.AddRange(
                await Task.WhenAll(
                    chartSeriesList.Select((series, i) =>
                        tasksToRun[i].ContinueWith(t => new SeriesWithData
                        {
                            ChartSeries = series,
                            SourceDataSet = t.Result,
                        }))));

        Logger!.LogInformation("RetrieveDataSets: completed enumeration");

        return datasetsToReturn;
    }

    private async Task SetUpLocalDefaultCharts(Guid locationId)
    {
        // Use the provided location for building the default chart. We want temperature and precipitation on the default chart.
        var location = LocationDictionary![locationId];

        if (ChartSeriesList == null)
        {
            ChartSeriesList = [];
        }

        var tempMaxOrMean = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, location.Id, DataSubstitute.StandardTemperatureDataMatches(), throwIfNoMatch: true) !;

        ChartSeriesList.Add(
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMaxOrMean),
                Aggregation = SeriesAggregationOptions.Mean,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 20,
                Value = SeriesValueOptions.Value,
                Year = null,
            });

        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, location.Id, DataType.Precipitation, null, throwIfNoMatch: false);
        if (precipitation is not null)
        {
            ChartSeriesList.Add(
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

        await BuildDataSets();
    }

    private async Task AddDefaultChart()
    {
        if (ChartSeriesList == null || ChartSeriesList.Count != 0)
        {
            return;
        }

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, Region.RegionId(Region.Atmosphere), DataType.CO2, null, throwIfNoMatch: true);

        ChartSeriesList!.Add(
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), co2!),
                Aggregation = SeriesAggregationOptions.Maximum,
                BinGranularity = BinGranularities.ByYear,
                SecondaryCalculation = SecondaryCalculationOptions.AnnualChange,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 10,
                Value = SeriesValueOptions.Value,
                Year = null,
                DisplayStyle = SeriesDisplayStyle.Line,
                RequestedColour = UiLogic.Colours.Brown,
            });

        await BuildDataSets();
    }

    private void LogChartSeriesList()
    {
        Logger!.LogInformation("ChartSeriesList: (SelectedBinGranularity is " + SelectedBinGranularity + ")");

        foreach (var csd in ChartSeriesList!)
        {
            Logger!.LogInformation("    " + csd.ToString());
        }
    }

    private async Task ChartAllDataToggle()
    {
        ChartAllData = !ChartAllData;
        await BuildDataSets();
    }

    private Task ShowChartOptionsInfo()
    {
        return chartOptionsInfoPanel!.ShowAsync();
    }

    private Task ShowAggregationOptionsInfo()
    {
        return aggregationOptionsInfoPanel!.ShowAsync();
    }

    private SourceSeriesSpecification BuildSourceSeriesSpecification(DataSetLibraryEntry.SourceSeriesSpecification sss, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        var dsd = dataSetDefinitions.Single(x => x.Id == sss.SourceDataSetId);

        var md = dsd.MeasurementDefinitions!.Single(x => x.DataType == sss.DataType && x.DataAdjustment == sss.DataAdjustment);

        return
            new SourceSeriesSpecification
            {
                LocationId = sss.LocationId,
                LocationName = sss.LocationName!,
                DataSetDefinition = dsd,
                MeasurementDefinition = md,
            };
    }

    private SeriesSpecification BuildDataPrepSeriesSpecification(SourceSeriesSpecification sss)
    {
        return
            new SeriesSpecification
            {
                DataSetDefinitionId = sss.DataSetDefinition!.Id,
                DataType = sss.MeasurementDefinition!.DataType,
                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
                LocationId = sss.LocationId,
            };
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

    private async Task<List<ChartTrendlineData>> AddDataSetsToChart()
    {
        var dataSetIndex = 0;

        Colours = new ColourServer();

        var trendlines = new List<ChartTrendlineData>();

        var requestedColours = ChartSeriesWithData!
            .Where(x => x.ChartSeries!.RequestedColour != UiLogic.Colours.AutoAssigned)
            .Select(x => x.ChartSeries!.RequestedColour)
            .ToList();

        foreach (var chartSeries in ChartSeriesWithData!)
        {
            var dataSet = chartSeries.ProcessedDataSet!;
            var htmlColourCode = Colours.GetNextColour(chartSeries.ChartSeries!.RequestedColour, (List<Colours>)requestedColours);
            var renderSmallPoints = (bool)IsMobileDevice! || dataSet.DataRecords.Count() > 400;
            var defaultLabel = (bool)IsMobileDevice!
                ? chartSeries.ChartSeries.GetFriendlyTitleShort()
                : $"{chartSeries.ChartSeries.FriendlyTitle} | {UnitOfMeasureLabelShort(dataSet.MeasurementDefinition!.UnitOfMeasure)}";

            await ChartLogic.AddDataSetToChart(
                chart!,
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

    private void RebuildChartSeriesListToReflectSelectedYears()
    {
        var years = SelectedYears!.Any() ? SelectedYears!.Select(x => (short?)x).ToList() : new List<short?>() { null };

        List<ChartSeriesDefinition> newCsds = new List<ChartSeriesDefinition>();

        var uniqueChartSeriesList = ChartSeriesList!.Distinct(new ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked()).ToArray();

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
                        SecondaryCalculation = csd.SecondaryCalculation,
                        Smoothing = csd.Smoothing,
                        SmoothingWindow = csd.SmoothingWindow,
                        Value = csd.Value,
                        Year = year,
                        SeriesTransformation = csd.SeriesTransformation,
                        GroupingThreshold = csd.GroupingThreshold,
                        MinimumDataResolution = csd.MinimumDataResolution,
                    });
            }
        }

        Logger!.LogInformation("RebuildChartSeriesListToReflectSelectedYears() setting ChartSeriesList");
        ChartSeriesList = newCsds;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    private void BuildProcessedDataSets(List<SeriesWithData> chartSeriesWithData, bool chartAllData)
    {
        var l = new LogAugmenter(Logger!, "BuildProcessedDataSets");

        l.LogInformation("entering");

        foreach (var cs in chartSeriesWithData)
        {
            if (cs.ChartSeries!.SecondaryCalculation == SecondaryCalculationOptions.AnnualChange)
            {
                var yearlyDifferenceValues =
                    cs.SourceDataSet!.DataRecords
                    .Select(x => x.Value)
                    .CalculateYearlyDifference();

                // Now, join back to the original DataRecord set
                var newDataRecords =
                    yearlyDifferenceValues
                    .Zip(
                        cs.SourceDataSet.DataRecords,
                        (val, dr) => new BinnedRecord(dr.BinId, val))
                    .ToList();

                cs.SourceDataSet =
                    new DataSet
                    {
                        GeographicalEntity = cs.SourceDataSet.GeographicalEntity,
                        MeasurementDefinition = cs.SourceDataSet.MeasurementDefinition,
                        DataRecords = newDataRecords,
                    };
            }
        }

        var badChartSeries = new List<SeriesWithData>();

        // If we're doing smoothing via the moving average, precalculate these data and add them to PreProcessedDataSets.
        // We do this because the SimpleMovingAverage calculate function will remove some years from the start of the data set.
        // It removes these years because it doesn't have a good enough average to present it to the user.
        // Therefore, we need to calculate the smoothing before we calculate the start year - the basis for labelling the chart
        // If we're not calculating a moving average, PreProcessedDataSets = SourceDataSets
        foreach (var cs in chartSeriesWithData)
        {
            // We only support moving averages on linear bin granularities (e.g. Year, YearAndMonth) - not modular ones like MonthOnly
            if (SelectedBinGranularity.IsLinear() && cs.ChartSeries!.Smoothing == SeriesSmoothingOptions.MovingAverage)
            {
                var movingAverageValues =
                    cs.SourceDataSet!.DataRecords
                    .Select(x => x.Value)
                    .CalculateCentredMovingAverage(cs.ChartSeries.SmoothingWindow, 0.75f);

                if (movingAverageValues.All(y => y == null))
                {
                    l.LogError("Moving average calculation has resulted in no records. Will remove this series from the chart.");
                    badChartSeries.Add(cs);
                    continue;
                }

                // Now, join back to the original DataRecord set
                var newDataRecords =
                    movingAverageValues
                    .Zip(
                        cs.SourceDataSet.DataRecords,
                        (val, dr) => new BinnedRecord(dr.BinId, val))
                    .ToList();

                cs.PreProcessedDataSet =
                    new DataSet
                    {
                        GeographicalEntity = cs.SourceDataSet.GeographicalEntity,
                        MeasurementDefinition = cs.SourceDataSet.MeasurementDefinition,
                        DataRecords = newDataRecords,
                    };
            }
            else
            {
                cs.PreProcessedDataSet = cs.SourceDataSet;
            }
        }

        badChartSeries.ForEach(x => chartSeriesWithData.Remove(x));

        l.LogInformation("done with moving average calculation");

        // There must be exactly one bin granularity or else something odd's going on.
        var binGranularity = chartSeriesWithData.Select(x => x.ChartSeries!.BinGranularity).Distinct().Single();

        if (binGranularity != SelectedBinGranularity)
        {
            throw new Exception($"BinGranularity selected for series ({binGranularity}) doesn't match overall selected granularity {SelectedBinGranularity}");
        }

        BinIdentifier[]? chartBins = null;

        switch (binGranularity)
        {
            case BinGranularities.ByYear:
            case BinGranularities.ByYearAndMonth:
            case BinGranularities.ByYearAndWeek:
            case BinGranularities.ByYearAndDay:
                // Calculate first and last year which we have a data record for, across all data sets underpinning all chart series
                var preProcessedDataSets = chartSeriesWithData.Select(x => x.PreProcessedDataSet);
                var allDataRecords = preProcessedDataSets.SelectMany(x => x!.DataRecords);

                (chartStartBin, chartEndBin) =
                    ChartLogic.GetBinRangeToPlotForGaplessRange(
                        preProcessedDataSets!, // Pass in the data available for plotting
                        chartAllData, // and the user's preferences about what x axis range they'd like plotted
                        SelectedStartYear!,
                        SelectedEndYear!);

                chartBins = BinHelpers.EnumerateBinsInRange(chartStartBin, chartEndBin).ToArray();

                SetStartAndEndYears(chartSeriesWithData);

                break;

            case BinGranularities.ByMonthOnly:
            case BinGranularities.BySouthernHemisphereTemperateSeasonOnly:
            case BinGranularities.BySouthernHemisphereTropicalSeasonOnly:
                chartStartBin = null;
                chartEndBin = null;
                chartBins = BinHelpers.GetBinsForModularGranularity(binGranularity);
                break;

            default:
                throw new NotImplementedException($"binGranularity {binGranularity}");
        }

        foreach (var cs in chartSeriesWithData)
        {
            l.LogInformation("constructing ProcessedDataSet");

            var recordsByBinId = cs.PreProcessedDataSet!.DataRecords.ToLookup(x => x.BinId);

            l.LogInformation("First chart bin: " + chartBins.First() + ", last chart: " + chartBins.Last());

            // Create new datasets, same as the source, but with any gaps filled with null records
            cs.ProcessedDataSet =
                new DataSet
                {
                    GeographicalEntity = cs.PreProcessedDataSet.GeographicalEntity,
                    MeasurementDefinition = cs.PreProcessedDataSet.MeasurementDefinition,
                    DataRecords =
                        chartBins
                        .Select(
                            bin =>

                            // If there's a record in the source dataset, use it
                            recordsByBinId[bin.Id].SingleOrDefault()

                            // Otherwise, create a null record
                            ?? new BinnedRecord(bin.Id, null))
                        .ToList(),
                };
        }

        ChartBins = chartBins;

        // Now, we cut down the processed datasets to just the bins that we intend to display on the chart.
        // This should only affect linear (gapless) BinGranularities, but executes either way, in case we
        // later allow users to say "just give me month-ignoring-year, but only for months after 4 and before 7",
        // for example.
        var binIdsToPlot = new HashSet<string>(ChartBins.Select(x => x.Id));
        foreach (var cswd in ChartSeriesWithData!)
        {
            cswd.ProcessedDataSet!.DataRecords =
                cswd.ProcessedDataSet.DataRecords
                .Where(x => binIdsToPlot.Contains(x.BinId!))
                .ToList();
        }

        l.LogInformation("leaving");
    }

    private string GetChartLabel(SeriesTransformations seriesTransformation, string defaultLabel, SeriesAggregationOptions seriesAggregationOptions)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.IsZero => "Number of days without rain",
            SeriesTransformations.IsFrosty => "Number of days of frost",
            SeriesTransformations.DayOfYearIfFrost => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "Last day of frost" : "First day of frost",
            SeriesTransformations.EqualOrAbove25 => "Number of days 25°C or above",
            SeriesTransformations.EqualOrAbove35 => "Number of days 35°C or above",
            SeriesTransformations.EqualOrAbove1 => "Number of days with 1mm of rain or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "Number of days between 1mm and 10mm of rain",
            SeriesTransformations.EqualOrAbove10 => "Number of days with 10mm of rain or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "Number of days between 10mm and 25mm of rain",
            SeriesTransformations.EqualOrAbove25mm => "Number of days with 25mm of rain or more",
            _ => defaultLabel,
        };
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
            Grid = new { DrawOnChartArea = false },
            Title = new
            {
                Text = xLabel,
                Display = true,
                Color = "black",
            },
        };

        var axes = new List<string>();
        foreach (var s in ChartSeriesList!.Where(x => x.DataAvailable))
        {
            var uom = s.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
            var axisId = ChartLogic.GetYAxisId(s.SeriesTransformation, uom, s.Aggregation);
            if (!axes.Contains(axisId))
            {
                ((IDictionary<string, object>)scales).Add(
                    axisId,
                    new
                    {
                        Display = true,
                        Axis = "y",
                        Position = axes.Count % 2 == 0 ? "left" : "right",
                        Grid = new { DrawOnChartArea = axes.Count == 0 },
                        Title = new
                        {
                            Text = UnitOfMeasureLabel(s.SeriesTransformation, uom, s.Aggregation, s.Value),
                            Display = true,
                            Color = s.Colour,
                        },
                    });
                axes.Add(axisId);
            }
        }

        return scales;
    }

    private async Task OnSelectedBinGranularityChanged(BinGranularities value, bool rebuildDataSets = true)
    {
        SelectedBinGranularity = value;

        foreach (var csd in ChartSeriesList!)
        {
            csd.BinGranularity = value;
        }

        ChartSeriesList = ChartSeriesList.CreateNewListWithoutDuplicates();

        if (SelectedBinGranularity == BinGranularities.ByYearAndMonth
            || SelectedBinGranularity == BinGranularities.ByYearAndWeek
            || SelectedBinGranularity == BinGranularities.ByYearAndDay)
        {
            await ShowRangeSliderChanged(true);
        }

        if (rebuildDataSets)
        {
            await BuildDataSets();
        }
    }

    private void OnGroupingThresholdTextChanged(string value)
    {
        GroupingThresholdText = value;
    }

    private async Task ApplyYearlyAverageParameters()
    {
        UserOverridePresetAggregationSettings = true;
        InternalGroupingThreshold = float.Parse(GroupingThresholdText!) / 100;
        SelectedGroupingDays = SelectingGroupingDays == 0 ? SelectedGroupingDays : SelectingGroupingDays;
        await BuildDataSets();
    }

    private string GetGroupingThresholdText()
    {
        var groupingThreshold = ChartSeriesList!.FirstOrDefault() == null ? null : ChartSeriesList!.First().GroupingThreshold;

        return UserOverridePresetAggregationSettings
            ? $"{MathF.Round(InternalGroupingThreshold * 100, 0)}% (user override)"
            : groupingThreshold == null
                    ? $"{MathF.Round(InternalGroupingThreshold * 100, 0)}%"
                    : $"{MathF.Round((float)groupingThreshold * 100, 0)}% (preset defined)";
    }

    private async Task OnLineChartClicked(ChartMouseEventArgs e)
    {
        if (SelectedBinGranularity != BinGranularities.ByYear)
        {
            // TODO: Add support for SelectedBinGranularity != BinGranularities.ByYear
            return;
        }

        int startYear = ChartAllData ? StartYears!.First() : StartYears!.Last();

        var year = (short)(startYear + e.Index);

        var dataType = ChartSeriesWithData![e.DatasetIndex].SourceDataSet!.DataType;
        var dataAdjustment = ChartSeriesWithData[e.DatasetIndex].SourceDataSet!.DataAdjustment;

        await HandleOnYearFilterChange(new YearAndDataTypeFilter(year) { DataType = dataType, DataAdjustment = dataAdjustment });
    }

    private async Task ShowRangeSliderChanged(bool? value)
    {
        EnableRangeSlider = value;
        if (EnableRangeSlider.GetValueOrDefault() && SliderStart == null)
        {
            SetStartAndEndYears(ChartSeriesWithData!);

            var proportionToShow = SelectedBinGranularity == BinGranularities.ByYearAndDay ? 0.05f
                                 : SelectedBinGranularity == BinGranularities.ByYearAndWeek ? 0.1f
                                 : SelectedBinGranularity == BinGranularities.ByYearAndMonth ? 0.15f
                                 : .3f;
            var rangeStart = (int)MathF.Ceiling((EndYear - StartYears!.Max()) * proportionToShow);
            await OnStartYearTextChanged((EndYear - rangeStart).ToString());
        }
    }

    private async Task OnSelectedYearsChanged(ExtentValues extentValues)
    {
        await ChangeStartYear(extentValues.FromValue!, false);
        await ChangeEndYear(extentValues.ToValue!, false);
        await RenderChart();
    }

    private async Task OnStartYearTextChanged(string? text)
    {
        await ChangeStartYear(text, true);
    }

    private async Task ChangeStartYear(string? text, bool redraw)
    {
        SelectedStartYear = text;
        if (SelectedStartYear != null)
        {
            SliderStart = Convert.ToInt32(SelectedStartYear);
            if (redraw)
            {
                await RenderChart();
            }
        }
    }

    private async Task OnEndYearTextChanged(string text)
    {
        await ChangeEndYear(text, true);
    }

    private async Task ChangeEndYear(string text, bool redraw)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SelectedEndYear = null;
            SliderEnd = null;
        }
        else
        {
            SelectedEndYear = text;
            SliderEnd = Convert.ToInt32(SelectedEndYear);
        }

        if (redraw)
        {
            await RenderChart();
        }
    }

    private async Task OnDynamicStartYearChanged(bool? value)
    {
        ChartAllData = value.GetValueOrDefault();
        if (value != null)
        {
            SliderStart = null;
            SelectedStartYear = null;
            await RenderChart();
        }
    }

    private void OnSelectedGroupingDaysChanged(short value)
    {
        SelectingGroupingDays = value;
    }

    private async Task ClearUserAggregationOverride()
    {
        UserOverridePresetAggregationSettings = false;
        await BuildDataSets();
    }

    private void SetStartAndEndYears(List<SeriesWithData> chartSeriesWithData)
    {
        var binGranularity = chartSeriesWithData.Select(x => x.ChartSeries!.BinGranularity).Distinct().Single();
        var dataSet = binGranularity == BinGranularities.ByYear ? chartSeriesWithData.Select(x => x.PreProcessedDataSet) : chartSeriesWithData.Select(x => x.SourceDataSet);

        // build a list of all the years in which data sets start, used by the UI to allow the user to conveniently select from them
        StartYears = dataSet.Select(x => x!.GetStartYearForDataSet()).Distinct().OrderBy(x => x).ToList();
        SliderMin = StartYears.Min();
        if (SliderStart < SliderMin)
        {
            SliderStart = SliderMin;
        }

        var lastYears = dataSet.Select(x => x!.GetEndYearForDataSet()).Distinct().OrderBy(x => x).ToList();
        SliderMax = EndYear = lastYears.Max();
        if (SliderEnd > SliderMax)
        {
            SliderEnd = SliderMax;
        }
    }

    private async Task OnClearFilter()
    {
        SelectedStartYear = null;
        SelectedEndYear = null;
        SliderStart = null;
        SliderEnd = null;
        EnableRangeSlider = false;

        await BuildDataSets();
    }

    private async Task OnDownloadDataClicked()
    {
        await DownloadDataEvent.InvokeAsync(new DataDownloadPackage { ChartSeriesWithData = ChartSeriesWithData!, Bins = ChartBins!, BinGranularity = SelectedBinGranularity });
    }

    private async Task ShowAddDataSetModal()
    {
        await ShowAddDataSetModalEvent.InvokeAsync();
    }

    private Task ShowOptionsModal()
    {
        GroupingThresholdText = MathF.Round(InternalGroupingThreshold * 100, 0).ToString();
        return OptionsModal!.Show();
    }

    private string GroupingDaysText(int groupingDays)
    {
        return groupingDays switch
        {
            5 => "Groups of 5 days (73 groups)",
            7 => "Groups of 7 days (52 groups)",
            13 => "Groups of 13 days (28 groups)",
            14 => "Groups of 14 days (26 groups)",
            26 => "Groups of 26 days (14 groups)",
            28 => "Groups of 28 days (13 groups)",
            73 => "Groups of 73 days (5 groups)",
            91 => "Groups of 91 days (4 groups)",
            182 => "Groups of 182 days (2 groups)",
            _ => throw new NotImplementedException(groupingDays.ToString()),
        };
    }
}
