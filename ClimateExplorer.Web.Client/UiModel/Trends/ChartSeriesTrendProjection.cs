namespace ClimateExplorer.Web.Client.UiModel.Trends;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// The forward projection drawn on the chart for one trend window: one prediction per year,
/// starting the year after the last year of real data.
/// </summary>
/// <remarks>
/// Each entry is the full <see cref="RegressionPrediction"/> rather than just the fitted value, so
/// it also carries the mean confidence interval and the (wider) observation prediction interval for
/// that year. Only <see cref="RegressionPrediction.PredictedY"/> is rendered today; the interval
/// bounds are computed and carried here ready for the prediction-band overlay.
/// </remarks>
public sealed record ChartSeriesTrendProjection(
    TrendWindow Window,
    IReadOnlyList<RegressionPrediction> Predictions)
{
    public int FirstYear => (int)Math.Round(Predictions[0].X);

    public int LastYear => (int)Math.Round(Predictions[^1].X);
}
