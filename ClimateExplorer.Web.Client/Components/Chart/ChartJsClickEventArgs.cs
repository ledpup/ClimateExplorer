namespace ClimateExplorer.Web.Client.Components.Chart;

/// <summary>
/// The point a click on a <see cref="ChartJsChart"/> landed on.
/// </summary>
/// <param name="DatasetIndex">Index of the dataset, in the order passed to SetDataAsync.</param>
/// <param name="Index">Index of the point within the dataset, i.e. the x-axis label index.</param>
public sealed record ChartJsClickEventArgs(int DatasetIndex, int Index);
