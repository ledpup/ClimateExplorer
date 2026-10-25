namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using System.Text.RegularExpressions;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class Top100
{
    private ElementReference svgRef;

    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public DataType? DataType { get; set; }

    [Parameter]
    public DataAdjustment? DataAdjustment { get; set; }

    [Parameter]
    public bool Ascending { get; set; }

    [Parameter]
    public int SelectedMonth { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    [Inject]
    private UiLogic.DateLabels DateLabels { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IDataService DataService { get; set; } = default!;

    private int TopCount { get; set; } = 100;
    private List<Top100Model.RecordCount> YearCounts { get; set; } = [];
    private List<DataRecord> Records { get; set; } = [];
    private UnitOfMeasure? Unit { get; set; }
    private int StartYear { get; set; }
    private int EndYear { get; set; }
    private bool IsLoading { get; set; }
    private Guid? InternalLocationId { get; set; }
    private List<Top100Model.RecordCount>? TooltipConfiguredFor { get; set; }
    private IUnitFormatter? TooltipConfiguredUnits { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Location?.Id != InternalLocationId)
        {
            InternalLocationId = Location?.Id;
        }

        await LoadData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (YearCounts.Count > 0 && (!ReferenceEquals(YearCounts, TooltipConfiguredFor) || !ReferenceEquals(Units, TooltipConfiguredUnits)))
        {
            TooltipConfiguredFor = YearCounts;
            TooltipConfiguredUnits = Units;
            await JsRuntime.InvokeVoidAsync("configureTop100Tooltip", svgRef, BuildTooltipData());
        }
    }

    private async Task LoadData()
    {
        if (Location is null || DataType is null)
        {
            YearCounts = [];
            return;
        }

        IsLoading = true;

        try
        {
            var month = SelectedMonth != 0 ? (int?)SelectedMonth : null;
            var top100 = await DataService.GetClimateRecords(Location.Id, DataType!.Value, DataAdjustment, Ascending, take: 100, skip: 1, month, false);

            if (top100!.Records.Count < 100)
            {
                YearCounts = [];
                Records = [];
                Unit = null;
            }
            else
            {
                YearCounts = Top100Model.BuildYearCounts(top100.Records);
                Records = top100.Records;
                Unit = top100.UnitOfMeasure;
                StartYear = top100.StartYear!.Value;
                EndYear = top100.EndYear!.Value;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string BuildTitle() => Location?.DisplayTitle ?? string.Empty;

    private string BuildSubtitle()
    {
        if (DataType is null)
        {
            return string.Empty;
        }

        var prefix = DataType.Value switch
        {
            Core.Enums.DataType.TempMax or Core.Enums.DataType.TempMin or Core.Enums.DataType.TempMean => Ascending ? "Coldest" : "Hottest",
            Core.Enums.DataType.Precipitation => Ascending ? "Driest" : "Wettest",
            Core.Enums.DataType.SolarRadiation => Ascending ? "Darkest" : "Brightest",
            _ => throw new NotImplementedException(),
        };

        var recordType = ChartSeriesDefinition.MapDataTypeToFriendlyName(DataType.Value);
        var monthSuffix = SelectedMonth == 0 ? string.Empty : " | " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(SelectedMonth);

        return $"{prefix} 100 daily {recordType.ToLower()}{monthSuffix} | {StartYear}-{EndYear}";
    }

    private string BuildFileName(string extension)
    {
        var name = Regex.Replace($"{BuildTitle()} {BuildSubtitle()}".ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return $"{name}.{extension}";
    }

    private async Task CopyAsImage() =>
        await JsRuntime.InvokeVoidAsync("svgToImageCopy", svgRef);

    private async Task DownloadImage() =>
        await JsRuntime.InvokeVoidAsync("svgToImageDownload", svgRef, BuildFileName("png"));

    private async Task DownloadSvg() =>
        await JsRuntime.InvokeVoidAsync("svgToSvgDownload", svgRef, BuildFileName("svg"));

    private Dictionary<string, TooltipYearInfo> BuildTooltipData()
    {
        // Records is already ordered by rank (highest/lowest value first, per Ascending), so the
        // 1-based index into it is the record's position in the top 100.
        return Records
            .Select((record, index) => (record, position: index + 1))
            .GroupBy(x => x.record.Year)
            .ToDictionary(
                g => g.Key.ToString(CultureInfo.InvariantCulture),
                g => new TooltipYearInfo(g.Key, g.Count(), [.. g.Select(x => FormatRecordLine(x.record, x.position))]));
    }

    private TooltipLine FormatRecordLine(DataRecord record, int position)
    {
        var dateLabel = record.Day.HasValue && record.Month.HasValue
            ? DateLabels.ShortDayMonth(record.Day.Value, record.Month.Value)
            : "?";

        var valueLabel = record.Value.HasValue && Unit.HasValue
            ? Units.Format(ApiMeasures.ClimateRecordValue(record.Value.Value, Unit.Value))
            : "?";

        return new TooltipLine(position, dateLabel, valueLabel);
    }

    private sealed record TooltipYearInfo(int Year, int Count, List<TooltipLine> Lines);

    private sealed record TooltipLine(int Position, string Date, string Value);
}
