namespace ClimateExplorer.Web.Client.Components.Chart;

using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class ChartAxisListView
{
    [Parameter]
    public List<AxisInfo>? Axes { get; set; }

    [Parameter]
    public Dictionary<string, bool>? ScaleToZero { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    private bool GetScaleToZero(string axisId) =>
        ScaleToZero != null && ScaleToZero.TryGetValue(axisId, out var val) && val;

    private async Task OnScaleToZeroChanged(string axisId, bool value)
    {
        if (ScaleToZero != null)
        {
            ScaleToZero[axisId] = value;
            await OnChanged.InvokeAsync();
        }
    }
}
