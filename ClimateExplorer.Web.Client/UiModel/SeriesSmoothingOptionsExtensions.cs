namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.Stats.Smoothing;

public static class SeriesSmoothingOptionsExtensions
{
    /// <summary>
    /// The smoother for this option, or <c>null</c> for options that don't smooth over a window
    /// (<see cref="SeriesSmoothingOptions.None"/>, <see cref="SeriesSmoothingOptions.Trendline"/>).
    /// </summary>
    public static ISeriesSmoother? CreateSmoother(this SeriesSmoothingOptions option)
    {
        return option switch
        {
            SeriesSmoothingOptions.CentredMovingAverage => new CentredMovingAverageCalculator(),
            SeriesSmoothingOptions.LocalLinearRegression => new LocalLinearRegressionCalculator(),
            _ => null,
        };
    }

    public static bool UsesWindow(this SeriesSmoothingOptions option)
    {
        return option.CreateSmoother() != null;
    }
}
