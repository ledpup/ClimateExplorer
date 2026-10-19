namespace ClimateExplorer.Web.Client.UiModel.Trends;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// One of a location's temperature trend windows, recalculated from the climate stripe's yearly data
/// for the dashboard's temperature trend modal. Every window is reported whether or not it turned out
/// significant, so the modal can explain why the dashboard shows the trend it does.
/// </summary>
public sealed record LocationTrendWindow(
    TrendWindow Window,
    PolynomialRegressionResult Regression,
    IReadOnlyList<DataPoint> Points)
{
    public bool IsSignificant => Regression.Significance.IsSlopeSignificant;

    public int FirstYear => (int)Math.Round(Regression.Input.MinimumX);

    public int LastYear => (int)Math.Round(Regression.Input.MaximumX);
}
