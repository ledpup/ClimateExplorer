namespace ClimateExplorer.Web.Client.Components.Common;

using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Switches the whole site between metric and US customary units. Off (metric) by default; the reader's
/// choice is remembered by <see cref="UnitPreference"/>.
/// </summary>
public partial class UnitSwitch
{
    [Parameter]
    public string? Class { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    [Inject]
    private UnitPreference UnitPreference { get; set; } = default!;

    private bool IsOn => Units.System == UnitSystem.UsCustomary;

    private Task ToggleAsync()
    {
        return UnitPreference.SetAsync(IsOn ? UnitSystem.Metric : UnitSystem.UsCustomary);
    }
}
