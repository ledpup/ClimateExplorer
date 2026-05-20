namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class ClimateRecords
{
    private enum RecordView
    {
        Daily,
        Monthly,
        Yearly,
        Top100,
    }

    [Parameter]
    [EditorRequired]
    public Location? Location { get; set; }

    [Parameter]
    [EditorRequired]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [PersistentState]
    public ClimateRecordsResponse? ClimateRecordsResult { get; set; }

    [Inject]
    private IDataService? DataService { get; set; }

    [Inject]
    private IExporter? Exporter { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    [Inject]
    private NavigationManager? NavManager { get; set; }

    [Inject]
    private ILogger<ClimateRecords>? Logger { get; set; }

    private bool LoadingIndicatorVisible { get; set; }

    private List<DataType> AvailableDataTypes { get; set; } = [];
    private List<DataAdjustment?> AvailableDataAdjustments { get; set; } = [];
    private List<string> ComputedRowStyles { get; set; } = [];
    private List<string> ComputedTopPercentages { get; set; } = [];
    private List<MeasurementDefinitionViewModel> LocationMeasurements { get; set; } = [];

    private DataType? SelectedDataType { get; set; }
    private DataAdjustment? SelectedDataAdjustment { get; set; } = DataAdjustment.Adjusted;
    private UnitOfMeasure SelectedUnitOfMeasure { get; set; } = UnitOfMeasure.DegreesCelsius;
    private int SelectedMonth { get; set; } = 0;
    private int SelectedDay { get; set; } = 0;
    private bool Ascending { get; set; } = false;
    private int Count { get; set; } = 10;
    private int CurrentPage { get; set; } = 1;
    private RecordView ActiveView { get; set; } = RecordView.Top100;

    private int TotalPages => ClimateRecordsResult?.TotalCount > 0 && Count > 0 ? (int)Math.Ceiling((double)ClimateRecordsResult.TotalCount / Count) : 1;
    private int StartRecord => ClimateRecordsResult?.Records?.Count > 0 ? ((CurrentPage - 1) * Count) + 1 : 0;
    private int EndRecord => ClimateRecordsResult?.Records?.Count > 0 ? Math.Min(StartRecord + ClimateRecordsResult.Records.Count - 1, ClimateRecordsResult.TotalCount) : 0;

    private bool IsTodaySelected
    {
        get
        {
            var today = DateTime.Today;
            return SelectedMonth == today.Month && SelectedDay == today.Day;
        }
    }

    private int DaysInSelectedMonth => SelectedMonth > 0
        ? DateTime.DaysInMonth(DateTime.Today.Year, SelectedMonth)
        : 31;
    private string SortIcon => Ascending ? "fa-up-long" : "fa-down-long";

    private string SortLabel
    {
        get
        {
            return SelectedDataType switch
            {
                DataType.TempMax or DataType.TempMin or DataType.TempMean => Ascending ? "Coldest" : "Hottest",
                DataType.Precipitation => Ascending ? "Driest" : "Wettest",
                DataType.SolarRadiation => Ascending ? "Darkest" : "Brightest",
                _ => Ascending ? "Ascending" : "Descending",
            };
        }
    }

    private string RecordsTitle
    {
        get
        {
            if (SelectedDataType == null)
            {
                return string.Empty;
            }

            var raw = $"{ActiveView}{(AvailableDataAdjustments.Count <= 1 || SelectedDataAdjustment == DataAdjustment.Adjusted ? string.Empty : " unadjusted")} {ChartSeriesDefinition.MapDataTypeToFriendlyName(SelectedDataType.Value)} records";
            var casing = char.ToUpper(raw[0]) + raw[1..].ToLower();
            return casing;
        }
    }

    private Guid? InternalLocationId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (DataSetDefinitions is null || Location is null || Location.Id == InternalLocationId)
        {
            return;
        }

        InternalLocationId = Location.Id;
        await UpdateAvailableOptions();
        await LoadRecords();
    }

    private static List<string> ComputeRowStyles(ClimateRecordsResponse? response)
    {
        if (response is null)
        {
            return [];
        }

        if (response.StartYear is not int start || response.EndYear is not int end || end == start)
        {
            return response.Records.Select(_ => string.Empty).ToList();
        }

        return [.. response.Records
            .Select(record =>
            {
                double recentness = Math.Clamp((double)(record.Year - start) / (end - start), 0, 1);
                int red, green, blue;
                if (recentness >= 0.5)
                {
                    // Midpoint #F7F7F7 → newest #5AB732
                    double t = (recentness - 0.5) * 2;
                    red = (int)(247 - (157 * t));
                    green = (int)(247 - (64 * t));
                    blue = (int)(247 - (197 * t));
                }
                else
                {
                    // Oldest #6E6E6E → midpoint #F7F7F7
                    double t = recentness * 2;
                    red = green = blue = (int)(110 + (137 * t));
                }

                return string.Format(CultureInfo.InvariantCulture, "background-color: rgb({0}, {1}, {2}, 0.85)", red, green, blue);
            })];
    }

    private static List<string> ComputeTopPercentLabels(ClimateRecordsResponse? response, int startRank, int overallTotal)
    {
        if (response is null || response.Records is null || response.Records.Count == 0 || overallTotal <= 0)
        {
            return [];
        }

        var n = response.Records.Count;
        var thresholds = new[] { 1, 5, 10, 15, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        return [.. Enumerable.Range(0, n).Select(i =>
        {
            var rank = startRank + i;
            foreach (var t in thresholds)
            {
                var limit = (int)Math.Ceiling(overallTotal * (t / 100.0));
                if (limit < 1)
                {
                    limit = 1;
                }

                if (rank <= limit)
                {
                    return $"{t}%";
                }
            }

            return string.Empty;
        })];
    }

    private static string FormatAnomaly(double anomaly, UnitOfMeasure unitOfMeasure)
    {
        var sign = anomaly >= 0 ? "+" : string.Empty;
        return $"{sign}{anomaly.ToString("0.0", CultureInfo.InvariantCulture)}{Enums.UnitOfMeasureLabelShort(unitOfMeasure)}";
    }

    private static string FormatValue(double value, UnitOfMeasure unitOfMeasure)
    {
        return $"{value.ToString("0.0", CultureInfo.InvariantCulture)}{Enums.UnitOfMeasureLabelShort(unitOfMeasure)}";
    }

    private async Task UpdateAvailableOptions()
    {
        LocationMeasurements = [.. DataSetDefinitions!
            .Where(x => x.LocationIds != null && x.LocationIds.Contains(Location!.Id))
            .SelectMany(x => x.MeasurementDefinitions!)];

        if (ActiveView == RecordView.Daily || ActiveView == RecordView.Top100)
        {
            LocationMeasurements = [.. LocationMeasurements.Where(x => x.DataResolution == DataResolution.Daily)];
        }

        AvailableDataTypes = [.. LocationMeasurements
            .Select(x => x.DataType)
            .Distinct()];

        if (SelectedDataType is null)
        {
            if (AvailableDataTypes.Contains(DataType.TempMax))
            {
                SelectedDataType = DataType.TempMax;
            }
            else
            {
                SelectedDataType = AvailableDataTypes.First();
            }
        }
        else if (!AvailableDataTypes.Contains(SelectedDataType.Value))
        {
            SelectedDataType = AvailableDataTypes.First();
        }

        UpdateAvailableAdjustments();
    }

    private void UpdateAvailableAdjustments()
    {
        AvailableDataAdjustments = [.. LocationMeasurements
            .Where(x => x.DataType == SelectedDataType)
            .Select(x => x.DataAdjustment)
            .Distinct()];

        if (AvailableDataAdjustments.Contains(DataAdjustment.Adjusted))
        {
            SelectedDataAdjustment = DataAdjustment.Adjusted;
        }
        else if (AvailableDataAdjustments.Any())
        {
            SelectedDataAdjustment = AvailableDataAdjustments.First();
        }
        else
        {
            SelectedDataAdjustment = null;
        }

        var selectedMeasurement = LocationMeasurements.FirstOrDefault(x => x.DataType == SelectedDataType && x.DataAdjustment == SelectedDataAdjustment);
        SelectedUnitOfMeasure = selectedMeasurement?.UnitOfMeasure ?? UnitOfMeasure.DegreesCelsius;
    }

    private async Task LoadRecords()
    {
        if (Location is null || SelectedDataType is null)
        {
            return;
        }

        LoadingIndicatorVisible = true;

        try
        {
                if (ActiveView == RecordView.Top100)
            {
                ClimateRecordsResult = null;
                ComputedRowStyles = [];
                ComputedTopPercentages = [];
            }
            else if (ActiveView == RecordView.Yearly)
            {
                await LoadYearlyRecords();
            }
            else
            {
                var hasDailyData = LocationMeasurements.Any(x => x.DataType == SelectedDataType && x.DataResolution == DataResolution.Daily);
                if (ActiveView == RecordView.Daily && !hasDailyData)
                {
                    ClimateRecordsResult = new ClimateRecordsResponse();
                    ComputedRowStyles = [];
                    ComputedTopPercentages = [];
                }
                else
                {
                    var month = SelectedMonth != 0 ? (int?)SelectedMonth : null;
                    var day = ActiveView == RecordView.Daily && SelectedMonth != 0 && SelectedDay != 0 ? (int?)SelectedDay : null;
                    ClimateRecordsResult = await DataService!.GetClimateRecords(Location.Id, SelectedDataType.Value, SelectedDataAdjustment, Ascending, take: Count, skip: CurrentPage, month, ActiveView == RecordView.Monthly, day);
                    ComputedRowStyles = ComputeRowStyles(ClimateRecordsResult);
                    var startRank = ((CurrentPage - 1) * Count) + 1;
                    ComputedTopPercentages = ComputeTopPercentLabels(ClimateRecordsResult, startRank, ClimateRecordsResult.TotalCount);
                }
            }
        }
        finally
        {
            LoadingIndicatorVisible = false;
        }
    }

    private async Task LoadYearlyRecords()
    {
        if (DataSetDefinitions is null || Location is null || SelectedDataType is null)
        {
            return;
        }

        var dataSubstitutes = new List<DataSubstitute>
        {
            new() { DataType = SelectedDataType.Value, DataAdjustment = SelectedDataAdjustment },
        };

        var fn = SelectedDataType == DataType.Precipitation ? ContainerAggregationFunctions.Sum : ContainerAggregationFunctions.Mean;
        var summary = await ClimateDataHelper.CalculateAnomaly(DataService!, DataSetDefinitions, Location, dataSubstitutes, fn);

        if (summary?.AnomalyRecords is not { Count: > 0 })
        {
            ClimateRecordsResult = summary is not null ? new ClimateRecordsResponse() : null;
            ComputedRowStyles = [];
            ComputedTopPercentages = [];
            return;
        }

        var startYear = (int?)summary.AnomalyRecords.Min(x => (int)x.Year);
        var endYear = (int?)summary.AnomalyRecords.Max(x => (int)x.Year);
        var ordered = Ascending
            ? summary.AnomalyRecords.OrderBy(x => x.Absolute)
            : summary.AnomalyRecords.OrderByDescending(x => x.Absolute);
        var totalCount = ordered.Count();
        var paginated = ordered.Skip((CurrentPage - 1) * Count).Take(Count);

        var records = paginated.Select(x => new ClimateRecord
        {
            Year = x.Year,
            Month = 1,
            Value = x.Absolute,
            Anomaly = x.Relative,
            Average = x.Absolute,
            DataType = SelectedDataType.Value,
            DataAdjustment = SelectedDataAdjustment,
            DataResolution = DataResolution.Yearly,
            UnitOfMeasure = SelectedUnitOfMeasure,
        }).ToList();

        ClimateRecordsResult = new ClimateRecordsResponse
        {
            Records = records,
            StartYear = startYear,
            EndYear = endYear,
            TotalCount = totalCount,
        };
        ComputedRowStyles = ComputeRowStyles(ClimateRecordsResult);
        var startRank = ((CurrentPage - 1) * Count) + 1;
        ComputedTopPercentages = ComputeTopPercentLabels(ClimateRecordsResult, startRank, ClimateRecordsResult.TotalCount);
    }

    private async Task OnViewChanged(string name)
    {
        ActiveView = Enum.Parse<RecordView>(name);
        SelectedDay = 0;
        CurrentPage = 1;
        await UpdateAvailableOptions();
        await LoadRecords();
    }

    private async Task OnDataTypeChanged(DataType value)
    {
        SelectedDataType = value;
        UpdateAvailableAdjustments();
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnAdjustedChanged(bool value)
    {
        SelectedDataAdjustment = value
            ? DataAdjustment.Adjusted
            : AvailableDataAdjustments.FirstOrDefault(x => x != DataAdjustment.Adjusted);
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnMonthChanged(int value)
    {
        SelectedMonth = value;
        SelectedDay = 0;
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnDayChanged(int value)
    {
        SelectedDay = value;
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnTodayClicked()
    {
        var today = DateTime.Today;
        SelectedMonth = today.Month;
        SelectedDay = today.Day;
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnSortToggled()
    {
        Ascending = !Ascending;
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnCountChanged(int value)
    {
        Count = value;
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnPageChanged(int page)
    {
        if (page < 1 || page > TotalPages)
        {
            return;
        }

        CurrentPage = page;
        await LoadRecords();
    }

    private async Task DownloadAllRecords()
    {
        if (Location is null || ActiveView == RecordView.Top100 || SelectedDataType is null)
        {
            return;
        }

        ClimateRecord[] records;

        if (ActiveView == RecordView.Yearly)
        {
            if (DataSetDefinitions is null)
            {
                return;
            }

            var dataSubstitutes = new List<DataSubstitute>
            {
                new() { DataType = SelectedDataType.Value, DataAdjustment = SelectedDataAdjustment },
            };

            var fn = SelectedDataType.Value == DataType.Precipitation ? ContainerAggregationFunctions.Sum : ContainerAggregationFunctions.Mean;
            var summary = await ClimateDataHelper.CalculateAnomaly(DataService!, DataSetDefinitions, Location, dataSubstitutes, fn);

            if (summary?.AnomalyRecords is not { Count: > 0 })
            {
                records = [];
            }
            else
            {
                var ordered = Ascending
                    ? summary.AnomalyRecords.OrderBy(x => x.Absolute)
                    : summary.AnomalyRecords.OrderByDescending(x => x.Absolute);

                records = [.. ordered.Select(x => new ClimateRecord
                {
                    Year = x.Year,
                    Month = 1,
                    Value = x.Absolute,
                    Anomaly = x.Relative,
                    Average = x.Absolute,
                    DataType = SelectedDataType.Value,
                    DataAdjustment = SelectedDataAdjustment,
                    DataResolution = DataResolution.Yearly,
                    UnitOfMeasure = SelectedUnitOfMeasure,
                })];
            }
        }
        else
        {
            var hasDailyData = LocationMeasurements.Any(x => x.DataType == SelectedDataType && x.DataResolution == DataResolution.Daily);
            if (ActiveView == RecordView.Daily && !hasDailyData)
            {
                records = [];
            }
            else
            {
                var monthFilter = SelectedMonth != 0 ? (int?)SelectedMonth : null;
                var dayFilter = ActiveView == RecordView.Daily && SelectedMonth != 0 && SelectedDay != 0 ? (int?)SelectedDay : null;
                var allData = await DataService!.GetClimateRecords(Location.Id, SelectedDataType.Value, SelectedDataAdjustment, Ascending, null, null, monthFilter, ActiveView == RecordView.Monthly, dayFilter);
                records = [.. allData.Records];
            }
        }

        if (records.Length == 0)
        {
            return;
        }

        var fileStream = Exporter!.ExportClimateRecords(Logger!, Location, records, NavManager!.Uri);
        var adj = SelectedDataAdjustment.HasValue ? $"-{SelectedDataAdjustment.Value}" : string.Empty;
        var monthLabel = SelectedMonth != 0 ? $"-{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(SelectedMonth)}" : string.Empty;
        var fileName = $"{Location.Name}-{SelectedDataType}{adj}-{ActiveView.ToString().ToLowerInvariant()}{monthLabel}-records.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);
        await JsRuntime!.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }
}
