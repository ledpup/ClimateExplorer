namespace ClimateExplorer.Web.Client.Shared;

using Blazorise;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AggregationOptions
{
    private InfoPanel? aggregationOptionsInfoPanel;

    private string? thresholdText;
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

    private bool HaveOptionsChanged => overridePresetThreshold || thresholdText != "70" || currentGroupingDays != 14;

    protected override void OnParametersSet()
    {
        var newThresholdText = MathF.Round(CurrentThreshold * 100, 0).ToString();
        if (newThresholdText != thresholdText)
        {
            thresholdText = newThresholdText;
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

    private static string GroupingDaysText(int groupingDays) =>
        groupingDays switch
        {
            7 => "52 groups - 7 days each",
            14 => "26 groups - 14 days each",
            28 => "13 groups - 28 days each",
            91 => "4 groups - 91 days each",
            182 => "2 groups - 182 days each",
            _ => throw new NotImplementedException(groupingDays.ToString()),
        };

    private async Task OnSelectedGroupingDaysChanged(short value)
    {
        currentGroupingDays = value;
        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdText ?? "70", overridePresetThreshold));
    }

    private async Task OnThresholdTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            thresholdText = value;
            await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdText, overridePresetThreshold));
        }
    }

    private async Task ResetThreshold()
    {
        currentGroupingDays = 14;
        thresholdText = "70";
        overridePresetThreshold = false;

        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdText, false));
    }

    private async Task OnOverrideChanged(bool value)
    {
        overridePresetThreshold = value;

        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdText ?? "70", overridePresetThreshold));
    }

    private Task ShowAggregationOptionsInfo()
    {
        return aggregationOptionsInfoPanel!.ShowAsync();
    }
}
