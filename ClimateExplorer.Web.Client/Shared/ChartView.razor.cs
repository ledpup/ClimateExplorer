namespace ClimateExplorer.Web.Client.Shared;

using System.Dynamic;
using CurrentDevice;
using Blazorise;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using System;
using ClimateExplorer.WebApiClient.Services;
using ApexCharts;

public partial class ChartView
{
    private ApexChart<ChartDataItem>? chart;
    private List<ChartSeries> chartSeries = new ();

    private BinIdentifier? chartStartBin;
    private BinIdentifier? chartEndBin;

    private Modal? chartOptionsModal;
    private Modal? aggregationOptionsModal;

    private string? groupingThresholdText;

    public bool ChartLoadingIndicatorVisible { get; set; }
    public bool ChartLoadingErrored { get; set; }

    public BinGranularities SelectedBinGranularity { get; set; } = BinGranularities.ByYear;

    public List<SeriesWithData>? ChartSeriesWithData { get; set; }

    public bool ChartAllData { get; set; }
    public string? SelectedStartYear { get; set; }
    public string? SelectedEndYear { get; set; }
    public short SelectedGroupingDays { get; set; }
    public string? GroupingThresholdText
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

    public List<ChartSeriesDefinition>? ChartSeriesList { get; set; } = new List<ChartSeriesDefinition>();

    [Parameter]
    public EventCallback BuildDataSetsEvent { get; set; }

    [Parameter]
    public EventCallback<DataDownloadPackage> DownloadDataEvent { get; set; }

    [Parameter]
    public EventCallback ShowAddDataSetModalEvent { get; set; }

    public string ChartOptionsText { get; set; } = @"<div style=""padding-bottom: 24px;""><img style=""max-width: 100%;"" src=""images/ChartOptions.png"" alt=""Chart Options image"" /></div>
<p><strong>Chart all data</strong>: when checked, all data from all series will be displayed. When unchecked, data will be cropped to a starting point that all datasets share.</p>
<p><strong>Clear filter</strong>: the clear filter button is only displayed when there is a start or end year filter applied to the chart. Clicking this button will reset the chart back to the default filter and remove the range slider (if it has been turned on).</p>
<p><strong>Grouping</strong>: the grouping option allows you to look at the data from another point of view. The default view is ""Yearly""; i.e., each point on the graph represents a single year in the series. To represent daily data at the yearly level, ClimateExplorer applies rules and aggregations to average (or sum) the data together. If you select ""Year + Month"" the data will be re-processed, starting with the daily data for the particular year, to present twelve points on the chart per year. This view works best in combination with the range slider. If you select ""Month"", the data will be sliced, again starting with the lowest level of the data (usually daily), into only twelve points, one point for every month of the year. The value for each point will be an average (or sum) of the data across all years. This will give you a climatic view of the data for the location, it will not be as useful for viewing the change in the climate over time.</p>
<p><strong>Download data</strong>: the download data button allows you to download, as a csv file, the data for the chart you are currently looking at. The button is context sensitive; it'll download data that applies to the current view. For example, if you are looking at the data as a ""Year + Month"" grouping, you will get twelve records for each year.</p>
<p><strong>Aggregation options</strong>: the aggregation options allow you to change the underlying grouping parameters for the chart. The default values will group the daily data into 14 day (i.e., fortnightly) sub-bins. If each of those sub-bins has records for 70% of those days (i.e., 10 days of the 14 days will need to have records) then the whole year is considered valid. This means that you can still have substantial data loss, while the data remains valid. E.g., a meteorologist unwilling to come in on some weekends to record the min and max temperatures may not invalidate the data for the year. However, going on a 3-week holiday in the middle of winter would invalidate the year as it would distort the average to make the year seem warmer than it was.</p>";

    public string AggregationOptionsInfoText { get; set; } = @"<p>The aggregation options are advanced features that allows you to change the underlying aggregation process. To calculate a single aggregated value for data for a year, from daily or monthly series, data is bundled together. If each bundle of data does not have enough records, the bundle is rejected as being unreliable.</p>
<p>By default, the bundles are groups of 14 days (fortnights) and each bundle requires 70% (10 days of the 14) of the records to be present for the year to be considered reliable enough for an average mean to be calculated. This means that a number of records can be missing for the year, so long as not too many consecutive days are missing. As temperature (and other climate data) follows cyclic patterns, missing data from a consecutive block is considered to be more untrustworthy than sporadic data missing throughout the year.</p>
<p>Some presets (specifically, the cryosphere reference data – sea ice extent and melt) have a lower threshold applied to them because the data has been curated and considered to be trustworthy enough that more of it can be missing while still not corrupting the results.</p>
<p>If you make changes to these settings and apply them, your settings will take precedence and override any preset specific settings. You can clear this by clicking “Clear override” which would have appeared after you applied your changes.</p>
<p><strong>Day grouping</strong>: select groups from weekly, fortnightly, monthly, and half-yearly, amongst other options.</p>
<p><strong>Threshold required to form a valid group (% percentage)</strong>: this is a percentage of how many records is considered sufficient to form a valid bundle of data.</p>
<p><strong>Apply</strong>: save your changes and apply them to the chart. These settings will persist as you change locations and datasets within the application.</p>
<p><strong>Clear override</strong>: this will reset the settings back to their default (14 days at 70% threshold). Only appears after applying your settings.</p>";

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

    private ApexChartOptions<ChartDataItem> ChartOptions { get; set; } = new ();

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
    private List<short>? DatasetYears { get; set; }
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

    public async Task<List<SeriesWithData>> RetrieveDataSets(IEnumerable<ChartSeriesDefinition> chartSeriesList)
    {
        var datasetsToReturn = new List<SeriesWithData>();

        Logger!.LogInformation("RetrieveDataSets: starting enumeration");

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
                await DataService!.PostDataSet(
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

            datasetsToReturn.Add(
                new SeriesWithData() { ChartSeries = csd, SourceDataSet = dataSet });
        }

        Logger!.LogInformation("RetrieveDataSets: completed enumeration");

        return datasetsToReturn;
    }

    public async Task HandleRedraw()
    {
        var l = new LogAugmenter(Logger!, "HandleRedraw");

        l.LogInformation("Entering");

        // This can happen at startup, or if the user switches off all data series
        if (ChartSeriesWithData == null || ChartSeriesWithData.Count == 0 || chart == null)
        {
            l.LogInformation("Bailing early as no chart data available");
            return;
        }

        if (ChartLoadingErrored)
        {
            l.LogInformation("We have identified an error. Will not try to render the chart normally");
            ChartLoadingIndicatorVisible = false;
            l.LogInformation("Leaving");
            return;
        }

        LogChartSeriesList();

        // We now set ChartType to Bar if any series is of type Bar, and Line otherwise.
        var newInternalChartType =
            ChartSeriesWithData.Any(x => x.ChartSeries!.DisplayStyle == SeriesDisplayStyle.Bar)
            ? ChartType.Bar
            : ChartType.Line;

        if (newInternalChartType != InternalChartType)
        {
            InternalChartType = newInternalChartType;

            // await chart.ChangeType(newInternalChartType);
        }

        Colours = new ColourServer();

        var title = ChartLogic.BuildChartTitle(ChartSeriesWithData);

        // Data sets sometimes have internal gaps in data (i.e. years which have no data even though earlier
        // and later years have data). Additionally, they may have external gaps in data if the overall period
        // to be charted goes beyond the range of the available data in one particular data set.
        //
        // To ensure these gaps are handled correctly in the plotted chart, we build a new dataset that includes
        // records for each missing year. Value is set to null for those records.
        l.LogInformation("Calling BuildProcessedDataSets");

        BuildProcessedDataSets(ChartSeriesWithData, ChartAllData);

        var subtitle =
            (chartStartBin != null && chartEndBin != null)
            ? chartStartBin is YearBinIdentifier
                ? $"{chartStartBin.Label}-{chartEndBin.Label}, {Convert.ToInt16(chartEndBin.Label) - Convert.ToInt16(chartStartBin.Label)} years"
                : $"{chartStartBin.Label}-{chartEndBin.Label}"
            : SelectedBinGranularity.ToFriendlyString();

        subtitle += $" | Aggregation: {SelectedGroupingDays} day groups, {GetGroupingThresholdText()} threshold";

        l.LogInformation("Calling AddDataSetsToGraph");

        AddDataSetsToChart();

        l.LogInformation("Setting up chart options");

        var labels = ChartBins!.Select(x => x.Label).ToArray();

        ChartOptions.Title = new Title { Text = title, Align = Align.Center, Style = new TitleStyle { FontSize = "14px" } };
        ChartOptions.Subtitle = new Subtitle { Text = subtitle, Align = Align.Center, Style = new SubtitleStyle { FontSize = "12px" } };
        ChartOptions.Chart.Animations = new Animations { Enabled = false };
        ChartOptions.Chart.Zoom = new Zoom { AllowMouseWheelZoom = false };
        ChartOptions.Chart.Height = "100%";
        BuildChartAxes();
        ChartOptions.Legend = new Legend { Position = LegendPosition.Bottom, FontSize = "12px", HorizontalAlign = Align.Right };
        ChartOptions.Grid = new ApexCharts.Grid
        {
            Xaxis = new GridXAxis
            {
                Lines = new Lines { Show = false },
            },
            Yaxis = new GridYAxis
            {
                Lines = new Lines { Show = true },
            },
        };

        // StateHasChanged needs to be called before UpdateOptionsAsync otherwise the chart will not update in sync with the options.
        StateHasChanged();

        await chart.UpdateOptionsAsync(true, false, false);

        l.LogInformation("Finished setting up chart options");

        ChartLoadingIndicatorVisible = false;

        l.LogInformation("Leaving");
    }

    public void LogChartSeriesList()
    {
        Logger!.LogInformation("ChartSeriesList: (SelectedBinGranularity is " + SelectedBinGranularity + ")");

        foreach (var csd in ChartSeriesList!)
        {
            Logger!.LogInformation("    " + csd.ToString());
        }
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
                new List<ChartSeriesDefinition>()
                {
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
                })
            .ToList();

        await BuildDataSets();
    }

    protected override void OnInitialized()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;

        SelectedYears = [];

        var datasetYears = new List<short>();
        for (short i = 1800; i <= (short)DateTime.Now.Year; i++)
        {
            datasetYears.Add(i);
        }

        DatasetYears = datasetYears;

        SliderMax = DateTime.Now.Year;
    }

    protected override void OnParametersSet()
    {
        ChartLoadingErrored = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsMobileDevice == null)
        {
            IsMobileDevice = await CurrentDeviceService!.Mobile();
        }
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

    private async Task ChartAllDataToggle()
    {
        ChartAllData = !ChartAllData;
        await BuildDataSets();
    }

    private Task ShowChartOptionsInfo()
    {
        if (!string.IsNullOrWhiteSpace(ChartOptionsText))
        {
            return chartOptionsModal!.Show();
        }

        return Task.CompletedTask;
    }

    private Task ShowAggregationOptionsInfo()
    {
        if (!string.IsNullOrWhiteSpace(AggregationOptionsInfoText))
        {
            return aggregationOptionsModal!.Show();
        }

        return Task.CompletedTask;
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

    private void AddDataSetsToChart()
    {
        var dataSetIndex = 0;

        Colours = new ColourServer();

        var requestedColours = ChartSeriesWithData!
            .Where(x => x.ChartSeries!.RequestedColour != UiLogic.Colours.AutoAssigned)
            .Select(x => x.ChartSeries!.RequestedColour)
            .ToList();

        chartSeries = [];

        foreach (var cs in ChartSeriesWithData!)
        {
            var dataSet = cs.ProcessedDataSet!;
            var htmlColourCode = Colours.GetNextColour(cs.ChartSeries!.RequestedColour, requestedColours);
            var renderSmallPoints = (bool)IsMobileDevice! || dataSet.DataRecords.Count > 400;
            var defaultLabel = (bool)IsMobileDevice!
                ? cs.ChartSeries.GetFriendlyTitleShort()
                : $"{cs.ChartSeries.FriendlyTitle} | {UnitOfMeasureLabelShort(dataSet.MeasurementDefinition!.UnitOfMeasure)}";

            var result = ChartLogic.AddDataSetToChart(
                cs,
                dataSet,
                GetChartLabel(cs.ChartSeries.SeriesTransformation, defaultLabel, cs.ChartSeries.Aggregation),
                htmlColourCode,
                renderSmallPoints: renderSmallPoints);

            chartSeries.Add(result);

            dataSetIndex++;
        }
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
                        (val, dr) => new DataRecord
                        {
                            Label = dr.Label,
                            BinId = dr.BinId,
                            Value = val,
                        })
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
                        (val, dr) => new DataRecord
                        {
                            Label = dr.Label,
                            BinId = dr.BinId,
                            Value = val,
                        })
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
                            ?? new DataRecord { BinId = bin.Id, Label = bin.Label, Value = null })
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

    private void BuildChartAxes()
    {
        var xLabel = ChartLogic.GetXAxisLabel(SelectedBinGranularity);

        ChartOptions.Xaxis = new XAxis
        {
            Title = new AxisTitle { Text = xLabel, Style = new AxisTitleStyle { FontSize = "13px", FontFamily = "Helvetica, Arial, sans-serif", FontWeight = "400" } },
            Labels = new XAxisLabels
            {
                Style = new AxisLabelStyle { FontSize = "12px" },
            },
            TickAmount = 20,
        };

        ChartOptions.Yaxis = [];

        var axes = new List<string>();
        foreach (var s in ChartSeriesList!.Where(x => x.DataAvailable))
        {
            var uom = s.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
            var axisId = ChartLogic.GetYAxisId(s.SeriesTransformation, uom, s.Aggregation);
            if (!axes.Contains(axisId))
            {
                var label = UnitOfMeasureLabel(s.SeriesTransformation, uom, s.Aggregation, s.Value);
                ChartOptions.Yaxis.Add(
                    new YAxis
                    {
                        Title = new AxisTitle
                        {
                            Text = label,
                            Style = new AxisTitleStyle { FontSize = "13px", FontFamily = "Helvetica, Arial, sans-serif", FontWeight = "400", Color = s.Colour },
                        },
                        SeriesName = label,
                        DecimalsInFloat = 1,
                        Labels = new YAxisLabels
                        {
                            Style = new AxisLabelStyle { FontSize = "12px" },
                        },
                        Opposite = axes.Count % 2 != 0,
                    });

                axes.Add(axisId);
            }
        }
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

    //private async Task OnLineChartClicked(ChartMouseEventArgs e)
    //{
    //    if (SelectedBinGranularity != BinGranularities.ByYear)
    //    {
    //        // TODO: Add support for SelectedBinGranularity != BinGranularities.ByYear
    //        return;
    //    }

    //    int startYear = ChartAllData ? StartYears!.First() : StartYears!.Last();

    //    var year = (short)(startYear + e.Index);

    //    var dataType = ChartSeriesWithData![e.DatasetIndex].SourceDataSet!.DataType;
    //    var dataAdjustment = ChartSeriesWithData[e.DatasetIndex].SourceDataSet!.DataAdjustment;

    //    await HandleOnYearFilterChange(new YearAndDataTypeFilter(year) { DataType = dataType, DataAdjustment = dataAdjustment });
    //}

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
        await HandleRedraw();
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
                await HandleRedraw();
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
            await HandleRedraw();
        }
    }

    private async Task OnDynamicStartYearChanged(bool? value)
    {
        ChartAllData = value.GetValueOrDefault();
        if (value != null)
        {
            SliderStart = null;
            SelectedStartYear = null;
            await HandleRedraw();
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

    private async Task BuildDataSets()
    {
        await BuildDataSetsEvent.InvokeAsync();
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

public class ChartSeries
{
    public string? Name { get; set; }
    public List<ChartDataItem>? ChartDataItems { get; set; }
    public string? Colour { get; set; }
}

public class ChartDataItem(string year, decimal? value)
{
    public string Year { get; set; } = year;
    public decimal? Value { get; set; } = value;
}