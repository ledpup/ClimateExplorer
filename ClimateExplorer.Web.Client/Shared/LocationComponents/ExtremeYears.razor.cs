namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

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

    private List<short> HighList => [.. DataRecords!.OrderByDescending(x => x.Absolute).Take(5).Select(x => x.Year)];
    private List<short> LowList => [.. DataRecords!.OrderBy(x => x.Absolute).Take(5).Select(x => x.Year)];

    private string FirstLabel => HighFirst ? HighLabel : LowLabel;
    private string SecondLabel => HighFirst ? LowLabel : HighLabel;
    private List<short> FirstList => HighFirst ? HighList : LowList;
    private List<short> SecondList => HighFirst ? LowList : HighList;

    private async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}
