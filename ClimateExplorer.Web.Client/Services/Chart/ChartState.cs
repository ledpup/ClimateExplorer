namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Web.UiModel;

public sealed record ChartState
{
    public bool ChartAllData { get; init; }
    public string? StartYear { get; init; }
    public string? EndYear { get; init; }
    public short GroupingDays { get; init; } = 14;
    public string GroupingThresholdText { get; init; } = "70";
    public bool UserOverrideAggregationSettings { get; init; }
    public Dictionary<string, bool> AxesScaleToZero { get; init; } = [];
    public List<ChartSeriesDefinition> Series { get; init; } = [];
}
