namespace ClimateExplorer.Web.Client.UiModel.Trends;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// One of the three trend windows fitted for a chart series. All three are fitted together, and
/// each is reported here whether or not it turned out significant - the About-trends panel shows
/// the statistics for every window, while only significant ones can be drawn on the chart.
/// </summary>
public sealed record ChartSeriesTrendWindowResult(
    TrendWindow Window,
    LinearRegressionResult Regression,
    IReadOnlyList<DataPoint> Points)
{
    public bool IsSignificant => Regression.Significance.IsSlopeSignificant;

    public int FirstYear => (int)Math.Round(Regression.Input.MinimumX);

    public int LastYear => (int)Math.Round(Regression.Input.MaximumX);
}
