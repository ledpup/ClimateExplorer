namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class WarmestYears
{
    [Parameter]
    public List<YearAndValue>? DataRecords { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    private List<short> WarmestYearsList => DataRecords!.OrderByDescending(x => x.Value).Take(5).Select(x => x.Year).ToList();
    private List<short> CoolestYears => DataRecords!.OrderBy(x => x.Value).Take(5).Select(x => x.Year).ToList();

    private async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}
