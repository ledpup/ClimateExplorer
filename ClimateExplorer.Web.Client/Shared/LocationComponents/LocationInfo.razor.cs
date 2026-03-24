namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Shared.Info;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class LocationInfo
{
    [Inject]
    public IDataService? DataService { get; set; }

    [Inject]
    public ILogger<LocationInfo>? Logger { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public EventCallback RequestLocationChange { get; set; }

    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public EventCallback<bool> OnOverviewShowOrHide { get; set; }

    [Parameter]
    public EventCallback<YearAndDataTypeFilter> OnYearFilterChange { get; set; }

    private Collapsible? LocationCollapsible { get; set;  }

    private string LocationMapUrl => Location == null ? "https://maps.google.com/" : $"https://maps.google.com/?q={Location.Coordinates.Latitude},{Location.Coordinates.Longitude}";
    private string? GeoLocationAsString { get; set; }

    private bool LocationLoadingIndicatorVisible { get; set; }
    private bool StripeLoadingIndicatorVisible { get; set; }
    private bool LoadStripeData { get; set; }
    private bool Precipitation { get; set; }

    private List<YearlyValues>? TemperatureAnomalyRecords { get; set; }
    private double? TemperatureLocationMean { get; set; }
    private RenderFragment? WarmingAnomalyContent { get; set; }
    private string? WarmingAnomalyAsString { get; set; }

    private List<YearlyValues>? PrecipitationAnomalyRecords { get; set; }
    private double? PrecipitationLocationMean { get; set; }
    private RenderFragment? PrecipitationAnomalyContent { get; set; }
    private string? PrecipitationAnomalyAsString { get; set; }

    private Guid? LocationIdLastTimeOnParametersSetAsyncWasCalled { get; set; }

    private DataType? TemperatureDataType { get; set; }

    private RenderFragment? HeatingScoreContent { get; set; }

    private string? RecordHighToolTip { get; set; }
    private RenderFragment? ClimateRecordsContent { get; set; }

    public void ChangeLocationClicked(EventArgs args)
    {
        RequestLocationChange.InvokeAsync();
    }

    public void OverviewShowOrHideHandler(bool showOrHide)
    {
        OnOverviewShowOrHide.InvokeAsync(showOrHide);
    }

    protected static string GetPrecipitationAnomalyAsString(CalculatedAnomaly anomaly)
    {
        if (anomaly == null)
        {
            return "NA";
        }

        var value = anomaly.AnomalyValue;

        return $"{(value >= 0 ? "+" : string.Empty)}{string.Format("{0:0}", value)}mm";
    }

    protected override void OnInitialized()
    {
        LocationLoadingIndicatorVisible = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Location == null || DataSetDefinitions == null)
        {
            return;
        }

        // We do manual change detection here because Blazor calls OnParametersSetAsync() repeatedly with the same input values.
        if (LocationIdLastTimeOnParametersSetAsyncWasCalled == Location?.Id)
        {
            return;
        }

        GeoLocationAsString = Location!.Coordinates.ToString();
        LocationIdLastTimeOnParametersSetAsyncWasCalled = Location?.Id;
        LocationLoadingIndicatorVisible = false;
        LoadStripeData = true;
        StripeLoadingIndicatorVisible = true;

        WarmingAnomalyAsString = Location!.WarmingAnomaly == null ? "NA" : new CalculatedAnomaly { AnomalyValue = Location.WarmingAnomaly.Value }.ValueAsString();

        if (Location?.RecordHigh is not null)
        {
            RecordHighToolTip = $"{Location.Name} record high of {Location.RecordHigh.Value}°C set {(Location.RecordHigh.Day == null ? string.Empty : Location.RecordHigh.Day)} {CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(Location.RecordHigh.Month)} {Location.RecordHigh.Year}.\r\nClick for more records.";
        }

        ClimateRecordsContent = Location == null
                ? null
                : builder =>
                {
                    builder.OpenComponent(0, typeof(ClimateRecords));
                    builder.AddAttribute(1, "Location", Location);
                    builder.CloseComponent();
                };

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Location == null || DataSetDefinitions == null)
        {
            return;
        }

        if (LoadStripeData)
        {
            LoadStripeData = false;

            Logger!.LogInformation("Loading data");

            var temperatureAnomaly = await CalculateAnomaly(DataSubstitute.StandardTemperatureDataMatches(), ContainerAggregationFunctions.Mean);
            if (temperatureAnomaly != null)
            {
                TemperatureDataType = temperatureAnomaly.DataSet?.DataType;
                WarmingAnomalyContent = Location == null
                        ? null
                        : builder =>
                        {
                            builder.OpenComponent(0, typeof(WarmingAnomalyInfo));
                            builder.AddAttribute(1, "Location", Location);
                            builder.AddAttribute(2, "CalculatedAnomaly", temperatureAnomaly.CalculatedAnomaly);
                            builder.CloseComponent();
                        };
                TemperatureLocationMean = temperatureAnomaly?.DataSet?.DataRecords.Average(x => x.Value);
                TemperatureAnomalyRecords = temperatureAnomaly?.AnomalyRecords;
            }
            else
            {
                TemperatureDataType = null;
                WarmingAnomalyContent = null;
                TemperatureLocationMean = null;
                TemperatureAnomalyRecords = null;
            }

            HeatingScoreContent = Location == null
                                ? null
                                : builder =>
                                {
                                    builder.OpenComponent(0, typeof(HeatingScoreInfo));
                                    builder.AddAttribute(1, "Location", Location);
                                    builder.CloseComponent();
                                };

            if (Precipitation)
            {
                await GeneratePrecipitationView();
            }

            StripeLoadingIndicatorVisible = false;
            StateHasChanged();

            Logger!.LogInformation("Finished loading data");
        }
    }

    protected async Task<LocationAnomalySummary?> CalculateAnomaly(List<DataSubstitute> dataSubstitutes, ContainerAggregationFunctions function)
    {
        return await ClimateDataHelper.CalculateAnomaly(DataService!, DataSetDefinitions!, Location!, dataSubstitutes, function);
    }

    protected async Task<DataSet?> GetData(List<DataSubstitute> dataSubstitutes, ContainerAggregationFunctions function)
    {
        return await ClimateDataHelper.GetData(DataService!, DataSetDefinitions!, Location!, dataSubstitutes, function);
    }

    protected async Task HandleOnYearFilterChange(short year)
    {
        var yearAndFilter = new YearAndDataTypeFilter(year) { DataType = TemperatureDataType, UnitOfMeasure = UnitOfMeasure.DegreesCelsius };
        await OnYearFilterChange.InvokeAsync(yearAndFilter);
    }

    protected async Task HandleOnPrecipitationYearFilterChange(short year)
    {
        var yearAndFilter = new YearAndDataTypeFilter(year) { DataType = DataType.Precipitation, UnitOfMeasure = UnitOfMeasure.Millimetres };
        await OnYearFilterChange.InvokeAsync(yearAndFilter);
    }

    protected async Task TogglePrecipitation()
    {
        Precipitation = !Precipitation;

        if (Precipitation && PrecipitationAnomalyAsString == null)
        {
            await GeneratePrecipitationView();
        }
    }

    protected async Task GeneratePrecipitationView()
    {
        var precipitationAnomaly = await CalculateAnomaly([new DataSubstitute { DataType = DataType.Precipitation }], ContainerAggregationFunctions.Sum);
        if (precipitationAnomaly != null)
        {
            PrecipitationAnomalyAsString = GetPrecipitationAnomalyAsString(precipitationAnomaly.CalculatedAnomaly!);
            PrecipitationLocationMean = precipitationAnomaly.DataSet?.DataRecords.Average(x => x.Value);
            PrecipitationAnomalyRecords = precipitationAnomaly.AnomalyRecords;
            PrecipitationAnomalyContent = Location == null
                    ? null
                    : builder =>
                    {
                        builder.OpenComponent(0, typeof(PrecipitationAnomalyInfo));
                        builder.AddAttribute(1, "Location", Location);
                        builder.AddAttribute(2, "CalculatedAnomaly", precipitationAnomaly.CalculatedAnomaly);
                        builder.CloseComponent();
                    };
        }
        else
        {
            PrecipitationAnomalyAsString = null;
            PrecipitationLocationMean = null;
            PrecipitationAnomalyRecords = null;
            PrecipitationAnomalyContent = null;
        }
    }
}
