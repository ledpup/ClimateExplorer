namespace ClimateExplorer.Web.Client.Shared;

using Blazorise;
using Microsoft.AspNetCore.Components;

public partial class AggregationOptionsModal
{
    private Modal? modal;
    private InfoPanel? aggregationOptionsInfoPanel;

    private string? thresholdText;
    private short currentGroupingDays;
    private bool overridePresetThreshold;

    [Parameter]
    public float? PresetGroupingThreshold { get; set; }

    [Parameter]
    public EventCallback<AggregationSettings> OnChanged { get; set; }

    private bool HasThresholdChanged =>
        overridePresetThreshold ||
        (PresetGroupingThreshold == null && thresholdText != "70");

    public Task Show(float currentThreshold, short groupingDays, bool userOverride)
    {
        thresholdText = MathF.Round(currentThreshold * 100, 0).ToString();
        currentGroupingDays = groupingDays;
        overridePresetThreshold = userOverride;
        return modal!.Show();
    }

    private static string GroupingDaysText(int groupingDays) =>
        groupingDays switch
        {
            5 => "73 groups (5 days each)",
            7 => "52 groups (7 days each)",
            13 => "28 groups (13 days each)",
            14 => "26 groups (14 days each)",
            26 => "14 groups (26 days each)",
            28 => "13 groups (28 days each)",
            73 => "5 groups (73 days each)",
            91 => "4 groups (91 days each)",
            182 => "2 groups (182 days each)",
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
        overridePresetThreshold = false;
        thresholdText = PresetGroupingThreshold != null
            ? MathF.Round(PresetGroupingThreshold.Value * 100, 0).ToString()
            : "70";

        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdText, false));
    }

    private async Task OnOverrideChanged(bool value)
    {
        overridePresetThreshold = value;
        if (!overridePresetThreshold && PresetGroupingThreshold != null)
        {
            thresholdText = MathF.Round(PresetGroupingThreshold.Value * 100, 0).ToString();
        }

        await OnChanged.InvokeAsync(new AggregationSettings(currentGroupingDays, thresholdText ?? "70", overridePresetThreshold));
    }

    private Task ShowAggregationOptionsInfo()
    {
        return aggregationOptionsInfoPanel!.ShowAsync();
    }

    private string GetCurrentThresholdDescription()
    {
        if (string.IsNullOrEmpty(thresholdText))
        {
            return string.Empty;
        }

        var threshold = float.Parse(thresholdText) / 100;

        return overridePresetThreshold
            ? $"{MathF.Round(threshold * 100, 0)}% (user override)"
            : PresetGroupingThreshold == null
                ? $"{MathF.Round(threshold * 100, 0)}%"
                : $"{MathF.Round(PresetGroupingThreshold.Value * 100, 0)}% (preset defined)";
    }
}
