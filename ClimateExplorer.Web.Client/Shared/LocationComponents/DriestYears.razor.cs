namespace ClimateExplorer.Web.Client.Shared.LocationComponents;

using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class DriestYears
{
    [Parameter]
    public List<YearAndValue>? DataRecords { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    private List<short> DriestYearsList => DataRecords!.OrderBy(x => x.Value).Take(5).Select(x => x.Year).ToList();
    private List<short> WettestYears => DataRecords!.OrderByDescending(x => x.Value).Take(5).Select(x => x.Year).ToList();

    private async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}
