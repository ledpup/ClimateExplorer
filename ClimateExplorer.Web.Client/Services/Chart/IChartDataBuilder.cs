namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Web.Client.Units;

/// <summary>
/// Fetches and prepares the data required to render a chart for a given <see cref="ChartState"/>.
/// This isolates data retrieval, secondary calculations, smoothing, bin selection, gap filling,
/// and start-year metadata from <see cref="ChartView"/> so it can be tested without a rendered chart.
/// </summary>
public interface IChartDataBuilder
{
    /// <param name="state">The chart to build.</param>
    /// <param name="units">
    /// The reader's unit system, used only for the text of the notifications in the result. The data is
    /// always calculated and returned in metric; <c>ChartView</c> converts it for display. Defaults to metric.
    /// </param>
    /// <param name="cancellationToken">Cancels the build.</param>
    Task<ChartDataBuildResult> BuildAsync(ChartState state, IUnitFormatter? units = null, CancellationToken cancellationToken = default);
}
