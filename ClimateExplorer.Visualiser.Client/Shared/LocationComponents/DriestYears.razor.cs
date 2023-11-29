using ClimateExplorer.Visualiser.UiModel;
using Microsoft.AspNetCore.Components;

namespace ClimateExplorer.Visualiser.Client.Shared.LocationComponents;

public partial class DriestYears
{
    [Parameter]
    public List<YearAndValue>? DataRecords { get; set; }

    [Parameter]
    public EventCallback<short> OnYearFilterChange { get; set; }

    List<short> DriestYearsList => DataRecords!.OrderBy(x => x.Value).Take(5).Select(x => x.Year).ToList();
    List<short> WettestYears => DataRecords!.OrderByDescending(x => x.Value).Take(5).Select(x => x.Year).ToList();

    async Task FilterToYear(short year)
    {
        await OnYearFilterChange.InvokeAsync(year);
    }
}
