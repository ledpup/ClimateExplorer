using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

public partial class WarmestYears
{
    [Parameter]
    public List<YearAndValue>? DataRecords { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    List<short> WarmestYearsList => DataRecords!.OrderByDescending(x => x.Value).Take(5).Select(x => x.Year).ToList();
    List<short> CoolestYears => DataRecords!.OrderBy(x => x.Value).Take(5).Select(x => x.Year).ToList();

    async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}
