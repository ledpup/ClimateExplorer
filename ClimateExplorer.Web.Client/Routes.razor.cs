namespace ClimateExplorer.Web.Client;

using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using Microsoft.AspNetCore.Components;

public partial class Routes : IDisposable
{
    /// <summary>
    /// The day/month order the server chose for this reader while prerendering, carried over to
    /// the interactive render (server circuit or WebAssembly) so dates don't change order.
    /// </summary>
    [PersistentState]
    public bool? MonthFirstDates { get; set; }

    [Inject]
    private DateLabels DateLabels { get; set; } = default!;

    /// <summary>
    /// Cascaded to every component as an <see cref="IUnitFormatter"/>. Choosing another unit system
    /// swaps the formatter instance, which re-renders every component that receives it.
    /// </summary>
    [Inject]
    private UnitPreference UnitPreference { get; set; } = default!;

    public void Dispose()
    {
        UnitPreference.Changed -= OnUnitsChanged;
    }

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

        UnitPreference.Changed += OnUnitsChanged;
    }

    // Not during prerender: localStorage is only readable once the page is interactive.
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        return firstRender ? UnitPreference.LoadAsync() : Task.CompletedTask;
    }

    private void OnUnitsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
