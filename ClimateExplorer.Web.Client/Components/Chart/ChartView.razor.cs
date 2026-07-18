namespace ClimateExplorer.Web.Client.Components.Chart;

using Blazorise.Charts;
using Blazorise.Charts.Trendline;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using CurrentDevice;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class ChartView : IAsyncDisposable
{
    private bool haveCalledResizeAtLeastOnce = false;
    private bool disposed;

    private Chart<double?>? chart;
    private ChartTrendline<double?>? chartTrendline;
    private ElementReference chartWrapper;

    private BinIdentifier? chartStartBin;
    private BinIdentifier? chartEndBin;

    private InfoPanel? chartOptionsInfoPanel;

    private string? groupingThresholdText;

    private bool hasRenderableChartData;
    private ChartState? appliedState;
    private ChartDataBuildResult? renderedData;
    private bool renderedLoadingErrored;
    private bool renderChartInProcess;

    [Parameter]
    public ChartState? State { get; set; }

    [Parameter]
    public ChartDataBuildResult? Data { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool LoadingErrored { get; set; }

    [Parameter]
    public EventCallback<ChartState> ChartStateChanged { get; set; }

    [Parameter]
    public EventCallback<DataDownloadPackage> DownloadRequested { get; set; }

    [Parameter]
    public EventCallback AddDataSetRequested { get; set; }

    [Parameter]
    public EventCallback<YearAndDataTypeFilter> YearFilterRequested { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    [Inject]
    private ILogger<ChartView>? Logger { get; set; }

    private bool ChartLoadingIndicatorVisible { get; set; }
    private bool ChartLoadingErrored { get; set; }
    private bool NoChartDataAvailable { get; set; }

    // Internal view state: whether the chart plots all available years or the filtered range. Surfaced
    // and updated via ChartState (CreateCurrentChartState/SetChartState) rather than as a parameter.
    private bool ChartAllData { get; set; }

    private BinGranularities SelectedBinGranularity { get; set; } = BinGranularities.ByYear;
    private List<SeriesWithData>? ChartSeriesWithData { get; set; }
    private List<SeriesWithData>? AllChartSeriesWithData { get; set; }
    private string? SelectedStartYear { get; set; }
    private string? SelectedEndYear { get; set; }
    private short SelectedGroupingDays { get; set; }
    private string? GroupingThresholdText
    {
        get => groupingThresholdText;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                groupingThresholdText = value;
                InternalGroupingThreshold = float.Parse(value) / 100;
            }
        }
    }

    private List<ChartSeriesDefinition>? ChartSeriesList { get; set; } = [];

    private float InternalGroupingThreshold { get; set; } = .7f;

    private bool UserOverridePresetAggregationSettings { get; set; }

    private BinIdentifier[]? ChartBins { get; set; }

    private bool? IsMobileDevice { get; set; }

    private List<short>? StartYears { get; set; }

    private ColourServer Colours { get; set; } = new ColourServer();

    /// <summary>
    /// The chart type applied to the chart control. If any series is in "Bar" mode, we switch
    /// the entire chart to Bar type to ensure it renders, at the cost of a small misalignment
    /// between grid lines and datapoints for any line series that are being displayed.
    /// Otherwise, we display in "Line" mode to avoid that cost. "Scatter" series are also rendered
    /// using "Line" mode, as a line dataset with no connecting line and larger points.
    /// </summary>
    private ChartType InternalChartType { get; set; }

    private List<AxisInfo> CurrentAxes { get; set; } = [];

    private Dictionary<string, bool> AxesScaleToZero { get; set; } = [];

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        if (chart is not null)
        {
            try
            {
                await chart.Destroy();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    protected override void OnInitialized()
    {
        ChartLoadingIndicatorVisible = true;
        ChartLoadingErrored = false;

        SelectedGroupingDays = 14;
        GroupingThresholdText = "70";
    }

    protected override void OnParametersSet()
    {
        ChartLoadingIndicatorVisible = IsLoading;
        ChartLoadingErrored = LoadingErrored;

        if (IsLoading)
        {
            NoChartDataAvailable = false;
        }

        if (State is not null && !ReferenceEquals(State, appliedState))
        {
            SetChartState(State);
            appliedState = State;
        }

        if (Data is null && !IsLoading)
        {
            NoChartDataAvailable = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        IsMobileDevice ??= await CurrentDeviceService!.Mobile();

        if (IsLoading)
        {
            return;
        }

        if (Data is not null && !renderChartInProcess && (!ReferenceEquals(Data, renderedData) || LoadingErrored != renderedLoadingErrored))
        {
            renderedData = Data;
            renderedLoadingErrored = LoadingErrored;
            renderChartInProcess = true;

            // In Blazor Server the parent can provide data before the child Chart<T> component
            // has completed its JS initialisation. Wait until chart.js has created the canvas instance.
            try
            {
                if (!OperatingSystem.IsBrowser() && JsRuntime is not null)
                {
                    await JsRuntime.InvokeVoidAsync("waitForChartReady", chartWrapper);
                }

                ApplyChartData(Data);
                await RenderChart();
            }
            catch
            {
                renderedData = null;
                throw;
            }
            finally
            {
                renderChartInProcess = false;
            }
        }
    }

    protected async Task RenderChart()
    {
        var l = new LogAugmenter(Logger!, "RenderChart");

        l.LogInformation("Entering");

        if (disposed)
        {
            l.LogInformation("Bailing early as component is disposed");
            ChartLoadingIndicatorVisible = false;
            return;
        }

        if (chart is null)
        {
            l.LogInformation("Bailing early as chart is unavailable");
            NoChartDataAvailable = true;
            ChartLoadingIndicatorVisible = false;
            StateHasChanged();
            return;
        }

        try
        {
            await chart.Clear();
        }
        catch (Exception ex)
        {
            Logger!.LogError(ex, "Failed to clear the chart before rendering.");
            NoChartDataAvailable = true;
            ChartLoadingIndicatorVisible = false;
            StateHasChanged();
            return;
        }

        var title = string.Empty;
        var subtitle = string.Empty;
        List<ChartTrendlineData>? trendlines = null;

        if (ChartLoadingErrored)
        {
            await chart.SetOptionsObject(new { });
            NoChartDataAvailable = true;

            l.LogError("We have identified an error. Will not try to render the chart normally");
        }
        else if (ChartSeriesWithData == null || ChartSeriesWithData.Count == 0 || chartTrendline == null)
        {
            await chart.SetOptionsObject(new { });

            NoChartDataAvailable = true;
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

            // The data has already been fetched and prepared by the parent (gap filling, smoothing,
            // secondary calculations, bin selection). RenderChart consumes that prepared state directly.
            if (!hasRenderableChartData)
            {
                await chart.SetOptionsObject(new { });
                NoChartDataAvailable = true;
                ChartLoadingIndicatorVisible = false;
                StateHasChanged();

                l.LogWarning("Bailing early because processed chart datasets contain no renderable values.");
                return;
            }

            NoChartDataAvailable = false;

            title = ChartLogic.BuildChartTitle(ChartSeriesWithData, GetChartTitleLocations());
            subtitle = ChartLogic.BuildChartSubtitle(chartStartBin, chartEndBin, SelectedBinGranularity, IsMobileDevice!.Value, SelectedGroupingDays, GetGroupingThresholdText());

            l.LogInformation("Calling AddDataSetsToGraph");

            trendlines = await AddDataSetsToChart();

            l.LogInformation("Trendlines count: " + trendlines.Count);

            l.LogInformation("Calling AddLabels");

            var labels = ChartBins!.Select(x => x.Label).ToArray();
            await chart.AddLabels(labels);

            var optionsResult = ChartOptionsFactory.Build(new ChartOptionsRequest
            {
                Title = title,
                Subtitle = subtitle,
                BinGranularity = SelectedBinGranularity,
                IsMobileDevice = IsMobileDevice!.Value,
                SeriesWithData = ChartSeriesWithData,
                Series = ChartSeriesList!,
                AxesScaleToZero = AxesScaleToZero,
            });

            CurrentAxes = [.. optionsResult.Axes];

            await chart.SetOptionsObject(optionsResult.Options);

            if (trendlines != null && trendlines.Count > 0)
            {
                await chartTrendline.AddTrendLineOptions(trendlines);
            }
        }

        await chart.Update();

        await JsRuntime!.InvokeVoidAsync("configureChartTooltip", chartWrapper);
        await JsRuntime!.InvokeVoidAsync("registerChartHoverCursor", chartWrapper);

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

    private static string GetChartLabel(SeriesTransformations seriesTransformation, string? customTransformation, string defaultLabel, SeriesAggregationOptions seriesAggregationOptions)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.DayOfYearIfFrost => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "Last day of frost" : "First day of frost",
            SeriesTransformations.Custom => ChartSeriesDefinition.GetFriendlyCustomTransformationLabel(customTransformation ?? "Custom transformation"),
            _ => defaultLabel,
        };
    }

    private ChartState CreateCurrentChartState()
    {
        return new ChartState
        {
            ChartAllData = ChartAllData,
            StartYear = SelectedStartYear,
            EndYear = SelectedEndYear,
            GroupingDays = SelectedGroupingDays,
            GroupingThresholdText = GroupingThresholdText ?? string.Empty,
            UserOverrideAggregationSettings = UserOverridePresetAggregationSettings,
            AxesScaleToZero = AxesScaleToZero,
            Series = ChartSeriesList ?? [],
        };
    }

    private void SetChartState(ChartState state)
    {
        ChartAllData = state.ChartAllData;
        SelectedStartYear = state.StartYear;
        SelectedEndYear = state.EndYear;
        SelectedGroupingDays = state.GroupingDays;
        GroupingThresholdText = state.GroupingThresholdText;
        UserOverridePresetAggregationSettings = state.UserOverrideAggregationSettings;
        AxesScaleToZero = state.AxesScaleToZero;

        if (state.Series.Any())
        {
            SelectedBinGranularity = state.Series.First().BinGranularity;
        }

        Logger!.LogInformation("Setting ChartSeriesList to list with " + state.Series.Count + " items");

        ChartSeriesList = state.Series.ToList();
    }

    private void ApplyChartData(ChartDataBuildResult buildResult)
    {
        ChartSeriesWithData = [.. buildResult.SeriesWithData];
        AllChartSeriesWithData = [.. buildResult.SeriesWithData, .. buildResult.NonRenderedSeriesWithData];
        ChartBins = buildResult.ChartBins;
        chartStartBin = buildResult.ChartStartBin;
        chartEndBin = buildResult.ChartEndBin;
        StartYears = [.. buildResult.StartYears];
        hasRenderableChartData = buildResult.HasRenderableData;

        Logger!.LogInformation("Applied chart data. ChartSeriesWithData now has " + ChartSeriesWithData.Count + " entries.");
    }

    private Dictionary<Guid, Location>? GetChartTitleLocations()
    {
        var locations = ChartSeriesWithData?
            .Select(x => x.SourceDataSet?.GeographicalEntity)
            .OfType<Location>()
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First());

        return locations is { Count: > 0 }
            ? locations
            : null;
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
        await RaiseChartStateChanged();
    }

    private Task ShowChartOptionsInfo()
    {
        return chartOptionsInfoPanel!.ShowAsync();
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
                GetChartLabel(chartSeries.ChartSeries.SeriesTransformation, chartSeries.ChartSeries.CustomTransformation, defaultLabel, chartSeries.ChartSeries.Aggregation),
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

    private async Task OnSelectedBinGranularityChanged(BinGranularities value, bool rebuildDataSets = true)
    {
        SelectedBinGranularity = value;

        foreach (var csd in ChartSeriesList!)
        {
            csd.BinGranularity = value;
        }

        ChartSeriesList = ChartSeriesList.CreateNewListWithoutDuplicates();

        if (rebuildDataSets)
        {
            await RaiseChartStateChanged();
        }
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

        var sourceDataSet = ChartSeriesWithData![e.DatasetIndex].SourceDataSet!;

        await YearFilterRequested.InvokeAsync(new YearAndDataTypeFilter(year) { DataType = sourceDataSet.DataType, DataAdjustment = sourceDataSet.DataAdjustment, UnitOfMeasure = sourceDataSet.MeasurementDefinition!.UnitOfMeasure });
    }

    private async Task OnClearFilter()
    {
        SelectedStartYear = null;
        SelectedEndYear = null;

        await RaiseChartStateChanged();
    }

    private async Task OnDownloadDataClicked()
    {
        await DownloadRequested.InvokeAsync(new DataDownloadPackage { ChartSeriesWithData = ChartSeriesWithData!, Bins = ChartBins!, BinGranularity = SelectedBinGranularity });
    }

    private async Task ShowAddDataSetModal()
    {
        await AddDataSetRequested.InvokeAsync();
    }

    private async Task OnAggregationSettingsChanged(AggregationSettings settings)
    {
        GroupingThresholdText = settings.ThresholdText;
        SelectedGroupingDays = settings.GroupingDays;
        UserOverridePresetAggregationSettings = settings.UserOverride;
        await RaiseChartStateChanged();
    }

    private async Task RaiseChartStateChanged()
    {
        await ChartStateChanged.InvokeAsync(CreateCurrentChartState());
    }
}
