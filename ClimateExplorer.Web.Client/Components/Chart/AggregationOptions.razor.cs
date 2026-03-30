namespace ClimateExplorer.Web.Client.Components.Chart;

using Blazorise;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AggregationOptions
{
    private InfoPanel? aggregationOptionsInfoPanel;

    private int thresholdValue;
    private short currentGroupingDays;
    private bool overridePresetThreshold;

    [Parameter]
    public float CurrentThreshold { get; set; }

    [Parameter]
    public short CurrentGroupingDays { get; set; }

    [Parameter]
    public bool UserOverride { get; set; }

    [Parameter]
    public EventCallback<AggregationSettings> OnChanged { get; set; }

    private bool HaveOptionsChanged => overridePresetThreshold || thresholdValue != 70 || currentGroupingDays != 14;

    protected override void OnParametersSet()
    {
        var newThresholdValue = (int)MathF.Round(CurrentThreshold * 100, 0);
        if (newThresholdValue != thresholdValue)
        {
            thresholdValue = newThresholdValue;
        }

        if (CurrentGroupingDays != currentGroupingDays)
        {
            currentGroupingDays = CurrentGroupingDays;
        }

        if (UserOverride != overridePresetThreshold)
        {
            overridePresetThreshold = UserOverride;
        }
    }

    private async Task OnSelectedGroupingDaysChanged(short value)
    {
        currentGroupingDays = value;
        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdValue.ToString(), overridePresetThreshold));
    }

    private async Task OnThresholdChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value) && value >= 0 && value <= 100)
        {
            thresholdValue = value;
            await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdValue.ToString(), overridePresetThreshold));
        }
    }

    private async Task ResetThreshold()
    {
        currentGroupingDays = 14;
        thresholdValue = 70;
        overridePresetThreshold = false;

        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, "70", false));
    }

    private async Task OnOverrideChanged(bool value)
    {
        overridePresetThreshold = value;

        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdValue.ToString(), overridePresetThreshold));
    }

    private Task ShowAggregationOptionsInfo()
    {
        return aggregationOptionsInfoPanel!.ShowAsync();
    }
}
