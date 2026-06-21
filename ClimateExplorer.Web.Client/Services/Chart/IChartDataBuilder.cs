namespace ClimateExplorer.Web.Client.Services.Chart;

/// <summary>
/// Fetches and prepares the data required to render a chart for a given <see cref="ChartState"/>.
/// This isolates data retrieval, secondary calculations, smoothing, bin selection, gap filling,
/// and start-year metadata from <see cref="ChartView"/> so it can be tested without a rendered chart.
/// </summary>
public interface IChartDataBuilder
{
    Task<ChartDataBuildResult> BuildAsync(ChartState state, CancellationToken cancellationToken = default);
}
