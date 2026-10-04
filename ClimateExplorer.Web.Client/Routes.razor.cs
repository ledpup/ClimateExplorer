namespace ClimateExplorer.Web.Client;

using ClimateExplorer.Web.UiLogic;
using Microsoft.AspNetCore.Components;

public partial class Routes
{
    /// <summary>
    /// The day/month order the server chose for this reader while prerendering, carried over to
    /// the interactive render (server circuit or WebAssembly) so dates don't change order.
    /// </summary>
    [PersistentState]
    public bool? MonthFirstDates { get; set; }

    [Inject]
    private DateLabels DateLabels { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (MonthFirstDates.HasValue)
        {
            DateLabels.MonthFirst = MonthFirstDates.Value;
        }
        else
        {
            MonthFirstDates = DateLabels.MonthFirst;
        }
    }
}
