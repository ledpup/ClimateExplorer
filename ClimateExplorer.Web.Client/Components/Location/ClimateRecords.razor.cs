namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class ClimateRecords
{
    private InfoPanel? colourExplainerInfoPanel;

    private enum RecordView
    {
        Daily,
        Monthly,
        Yearly,
    }

    [Inject]
    public IDataService? DataService { get; set; }

    [Inject]
    public IExporter? Exporter { get; set; }

    [Inject]
    public IJSRuntime? JsRuntime { get; set; }

    [Inject]
    public NavigationManager? NavManager { get; set; }

    [Inject]
    public ILogger<ClimateRecords>? Logger { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [PersistentState]
    public ClimateRecordsResponse? ClimateRecordsResult { get; set; }

    private bool LoadingIndicatorVisible { get; set; }

    private List<DataType> AvailableDataTypes { get; set; } = [];
    private List<DataAdjustment?> AvailableDataAdjustments { get; set; } = [];
    private List<string> ComputedRowStyles { get; set; } = [];
    private List<MeasurementDefinitionViewModel> LocationMeasurements { get; set; } = [];
    private IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    private DataType SelectedDataType { get; set; } = DataType.TempMax;
    private DataAdjustment? SelectedDataAdjustment { get; set; } = DataAdjustment.Adjusted;
    private UnitOfMeasure SelectedUnitOfMeasure { get; set; } = UnitOfMeasure.DegreesCelsius;
    private int SelectedMonth { get; set; } = 0;
    private bool Ascending { get; set; } = false;
    private int Count { get; set; } = 10;
    private int CurrentPage { get; set; } = 1;
    private RecordView ActiveView { get; set; } = RecordView.Daily;

    private int TotalPages => ClimateRecordsResult?.TotalCount > 0 && Count > 0 ? (int)Math.Ceiling((double)ClimateRecordsResult.TotalCount / Count) : 1;
    private int StartRecord => ClimateRecordsResult?.Records?.Count > 0 ? ((CurrentPage - 1) * Count) + 1 : 0;
    private int EndRecord => ClimateRecordsResult?.Records?.Count > 0 ? Math.Min(StartRecord + ClimateRecordsResult.Records.Count - 1, ClimateRecordsResult.TotalCount) : 0;
    private string SortIcon => Ascending ? "fa-up-long" : "fa-down-long";

    private string RecordsTitle
    {
        get
        {
            var raw = $"{ActiveView} {ChartSeriesDefinition.MapDataTypeToFriendlyName(SelectedDataType)} records";
            return char.ToUpper(raw[0]) + raw[1..].ToLower();
        }
    }

    private Guid? InternalLocationId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Location is null || Location.Id == InternalLocationId)
        {
            return;
        }

        InternalLocationId = Location.Id;
        await LoadAvailableOptions();
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

        return response.Records
            .Select(record =>
            {
                double recentness = Math.Clamp((double)(record.Year - start) / (end - start), 0, 1);
                int red, green, blue;
                if (recentness >= 0.5)
                {
                    double t = (recentness - 0.5) * 2;
                    red = 255;
                    green = blue = (int)(255 - (180 * t));
                }
                else
                {
                    double t = recentness * 2;
                    blue = 255;
                    red = green = (int)(75 + (180 * t));
                }

                return string.Format(CultureInfo.InvariantCulture, "background-color: rgb({0}, {1}, {2}, .1)", red, green, blue);
            })
            .ToList();
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

    private static string DataAdjustmentToString(DataAdjustment? da) => da.HasValue ? da.Value.ToString() : "none";

    private Task ShowColourExplainerInfo() => colourExplainerInfoPanel!.ShowAsync();

    private async Task LoadAvailableOptions()
    {
        DataSetDefinitions = await DataService!.GetDataSetDefinitions();
        LocationMeasurements = DataSetDefinitions
            .Where(x => x.LocationIds != null && x.LocationIds.Contains(Location!.Id))
            .SelectMany(x => x.MeasurementDefinitions!)
            .Where(x => x.DataResolution == DataResolution.Daily || x.DataResolution == DataResolution.Monthly)
            .ToList();

        AvailableDataTypes = LocationMeasurements
            .Select(x => x.DataType)
            .Distinct()
            .ToList();

        if (AvailableDataTypes.Contains(DataType.TempMax))
        {
            SelectedDataType = DataType.TempMax;
        }
        else if (AvailableDataTypes.Any())
        {
            SelectedDataType = AvailableDataTypes.First();
        }

        UpdateAvailableAdjustments();
    }

    private void UpdateAvailableAdjustments()
    {
        AvailableDataAdjustments = LocationMeasurements
            .Where(x => x.DataType == SelectedDataType)
            .Select(x => x.DataAdjustment)
            .Distinct()
            .ToList();

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
        if (Location is null)
        {
            return;
        }

        LoadingIndicatorVisible = true;

        try
        {
            if (ActiveView == RecordView.Yearly)
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
                }
                else
                {
                    var month = SelectedMonth != 0 ? (int?)SelectedMonth : null;
                    ClimateRecordsResult = await DataService!.GetClimateRecords(Location.Id, SelectedDataType, SelectedDataAdjustment, Ascending, Count, CurrentPage, month, ActiveView == RecordView.Monthly);
                    ComputedRowStyles = ComputeRowStyles(ClimateRecordsResult);
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
        if (DataSetDefinitions is null || Location is null)
        {
            return;
        }

        var dataSubstitutes = new List<DataSubstitute>
        {
            new() { DataType = SelectedDataType, DataAdjustment = SelectedDataAdjustment },
        };

        var fn = SelectedDataType == DataType.Precipitation ? ContainerAggregationFunctions.Sum : ContainerAggregationFunctions.Mean;
        var summary = await ClimateDataHelper.CalculateAnomaly(DataService!, DataSetDefinitions, Location, dataSubstitutes, fn);

        if (summary?.AnomalyRecords is not { Count: > 0 })
        {
            ClimateRecordsResult = summary is not null ? new ClimateRecordsResponse() : null;
            ComputedRowStyles = [];
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
            DataType = SelectedDataType,
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
    }

    private async Task OnViewChanged(string name)
    {
        ActiveView = Enum.Parse<RecordView>(name);
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnDataTypeChanged(DataType value)
    {
        SelectedDataType = value;
        UpdateAvailableAdjustments();
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnDataAdjustmentStringChanged(string value)
    {
        SelectedDataAdjustment = value == "none" ? null : Enum.Parse<DataAdjustment>(value);
        CurrentPage = 1;
        await LoadRecords();
    }

    private async Task OnMonthChanged(int value)
    {
        SelectedMonth = value;
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

    private List<int> GetVisiblePages()
    {
        const int maxPages = 9;

        if (TotalPages <= maxPages)
        {
            return [.. Enumerable.Range(1, TotalPages)];
        }

        // Budget: 1 + ... + [window] + ... + N
        // Both ellipses present → window gets maxPages - 4 slots.
        // One ellipsis (near an edge) → window expands to maxPages - 2 slots.
        const int innerWindow = maxPages - 4;

        var wStart = Math.Max(2, CurrentPage - (innerWindow / 2));
        var wEnd = Math.Min(TotalPages - 1, wStart + innerWindow - 1);
        wStart = Math.Max(2, wEnd - innerWindow + 1);

        var needLeading = wStart > 2;
        var needTrailing = wEnd < TotalPages - 1;

        if (!needLeading)
        {
            wStart = 1;
            wEnd = Math.Min(TotalPages - 1, maxPages - 2);
        }
        else if (!needTrailing)
        {
            wEnd = TotalPages;
            wStart = Math.Max(2, TotalPages - (maxPages - 3));
        }

        var pages = new List<int>();
        if (needLeading)
        {
            pages.Add(1);
            pages.Add(-1);
        }

        for (var i = wStart; i <= wEnd; i++)
        {
            pages.Add(i);
        }

        if (needTrailing)
        {
            pages.Add(-1);
            pages.Add(TotalPages);
        }

        return pages;
    }

    private async Task DownloadAllRecords()
    {
        if (Location is null)
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
                new() { DataType = SelectedDataType, DataAdjustment = SelectedDataAdjustment },
            };

            var fn = SelectedDataType == DataType.Precipitation ? ContainerAggregationFunctions.Sum : ContainerAggregationFunctions.Mean;
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

                records = ordered.Select(x => new ClimateRecord
                {
                    Year = x.Year,
                    Month = 1,
                    Value = x.Absolute,
                    Anomaly = x.Relative,
                    Average = x.Absolute,
                    DataType = SelectedDataType,
                    DataAdjustment = SelectedDataAdjustment,
                    DataResolution = DataResolution.Yearly,
                    UnitOfMeasure = SelectedUnitOfMeasure,
                }).ToArray();
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
                var allData = await DataService!.GetClimateRecords(Location.Id, SelectedDataType, SelectedDataAdjustment, Ascending, null, null, monthFilter, ActiveView == RecordView.Monthly);
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
