namespace ClimateExplorer.Web.Client.Components.Location;

using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class ExtremeYears
{
    [Parameter]
    public List<YearlyValues>? DataRecords { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    [Parameter]
    public string HighLabel { get; set; } = string.Empty;

    [Parameter]
    public string LowLabel { get; set; } = string.Empty;

    [Parameter]
    public bool HighFirst { get; set; } = true;

    [Parameter]
    public UnitOfMeasure UnitOfMeasure { get; set; }

    private List<short> HighList => [.. DataRecords!.OrderByDescending(x => x.Absolute).Take(5).Select(x => x.Year)];
    private List<short> LowList => [.. DataRecords!.OrderBy(x => x.Absolute).Take(5).Select(x => x.Year)];

    private string FirstLabel => HighFirst ? HighLabel : LowLabel;
    private string SecondLabel => HighFirst ? LowLabel : HighLabel;
    private List<short> FirstList => HighFirst ? HighList : LowList;
    private List<short> SecondList => HighFirst ? LowList : HighList;

    private string GetTooltip(short year)
    {
        var record = DataRecords?.FirstOrDefault(x => x.Year == year);
        return record is null ? string.Empty : YearlyValuesHelper.GetTooltip(record, UnitOfMeasure, false);
    }

    private async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}