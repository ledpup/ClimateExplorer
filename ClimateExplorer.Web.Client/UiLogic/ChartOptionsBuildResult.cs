namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Web.Client.UiModel;

/// <summary>
/// The output of <see cref="ChartOptionsFactory"/>: the Chart.js options object to apply to the chart,
/// and the list of y axes produced (used by the chart controls to offer scale-to-zero toggles).
/// </summary>
public sealed record ChartOptionsBuildResult(object Options, IReadOnlyList<AxisInfo> Axes);
