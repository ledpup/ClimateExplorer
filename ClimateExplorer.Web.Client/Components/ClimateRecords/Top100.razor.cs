namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
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

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IDataService DataService { get; set; } = default!;

    private int TopCount { get; set; } = 100;
    private List<Top100Model.RecordCount> YearCounts { get; set; } = [];
    private int StartYear { get; set; }
    private int EndYear { get; set; }
    private bool IsLoading { get; set; }
    private Guid? InternalLocationId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Location?.Id != InternalLocationId)
        {
            InternalLocationId = Location?.Id;
        }

        await LoadData();
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
            }
            else
            {
                YearCounts = Top100Model.BuildYearCounts(top100.Records);
                StartYear = top100.StartYear!.Value;
                EndYear = top100.EndYear!.Value;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string BuildTitle()
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
        var monthSuffix = SelectedMonth == 0 ? string.Empty : " - " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(SelectedMonth);

        return $"{Location?.Name} - {prefix} 100 daily {recordType.ToLower()} records{monthSuffix}";
    }

    private async Task CopyAsImage() =>
        await JsRuntime.InvokeVoidAsync("svgToImageCopy", svgRef);

    private async Task DownloadImage()
    {
        var fileName = BuildTitle().Replace(" ", "-").Replace("/", "-").ToLowerInvariant() + ".png";
        await JsRuntime.InvokeVoidAsync("svgToImageDownload", svgRef, fileName);
    }

    private async Task DownloadSvg()
    {
        var fileName = BuildTitle().Replace(" ", "-").Replace("/", "-").ToLowerInvariant() + ".svg";
        await JsRuntime.InvokeVoidAsync("svgToSvgDownload", svgRef, fileName);
    }
}
