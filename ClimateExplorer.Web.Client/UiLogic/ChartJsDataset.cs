namespace ClimateExplorer.Web.UiLogic;

using System.Text.Json.Serialization;

/// <summary>
/// One Chart.js dataset, serialised as-is into <c>chart.data.datasets</c> by <c>ChartJsChart</c>.
/// Only the properties the chart uses are modelled; any left null are omitted so that Chart.js
/// applies its own default.
/// </summary>
public sealed record ChartJsDataset
{
    /// <summary>"line" or "bar". Set per dataset so line series still draw as lines on a bar chart.</summary>
    public required string Type { get; init; }

    public required string Label { get; init; }

    public required IReadOnlyList<double?> Data { get; init; }

    public required IReadOnlyList<string?> BackgroundColor { get; init; }

    public required IReadOnlyList<string?> BorderColor { get; init; }

    public int BorderWidth { get; init; }

    [JsonPropertyName("yAxisID")]
    public required string YAxisId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fill { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShowLine { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? PointRadius { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? PointHoverRadius { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PointBackgroundColor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PointBorderWidth { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string?>? PointBorderColor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string?>? PointHoverBackgroundColor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? BorderDash { get; init; }

    /// <summary>
    /// Read by chartjs-plugin-trendline, which draws a linear fit over the dataset when set.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChartJsTrendline? TrendlineLinear { get; init; }
}
