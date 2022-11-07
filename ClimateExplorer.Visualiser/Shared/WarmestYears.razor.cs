using ClimateExplorer.Visualiser.UiModel;
using Microsoft.AspNetCore.Components;

namespace ClimateExplorer.Visualiser.Shared
{
    public partial class WarmestYears
    {
        [Parameter]
        public List<YearAndValue> DataRecords { get; set; }

        [Parameter]
        public EventCallback<short> OnYearFilterChange { get; set; }

        string WarmestYearsAsString => string.Join(", ", DataRecords.OrderByDescending(x => x.Value).Take(5).Select(x => x.Year));
        string CoolestYearsAsString => string.Join(", ", DataRecords.OrderBy(x => x.Value).Take(5).Select(x => x.Year));

        async Task FilterToYear(short year)
        {
            await OnYearFilterChange.InvokeAsync(year);
        }
    }
}
