namespace ClimateExplorer.Web.Client.Services.Trends;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// Recalculates a location's temperature trends from the climate stripe's yearly data for the
/// dashboard's temperature trend modal. It uses the same <see cref="TemperatureTrendCalculator.Fit"/>
/// that produced the location's pre-calculated <see cref="TrendSummary"/>s, so the modal and the
/// dashboard field describe the same windows.
/// </summary>
public static class LocationTrendDetailsBuilder
{
    public static LocationTrendDetails Build(IEnumerable<BinnedRecord>? dataRecords)
    {
        var points = dataRecords is null ? [] : TemperatureTrendCalculator.ToDataPoints(dataRecords);
        var fits = TemperatureTrendCalculator.Fit(points);

        if (fits is null)
        {
            return new LocationTrendDetails(points.Count, [.. points.OrderBy(x => x.X)], []);
        }

        var windows = new List<LocationTrendWindow>();
        AddWindow(windows, fits, TrendWindow.Full, fits.FullPeriod);
        AddWindow(windows, fits, TrendWindow.Last30, fits.Last30Years);
        AddWindow(windows, fits, TrendWindow.FirstHalf, fits.FirstHalf);

        return new LocationTrendDetails(fits.OrderedPoints.Count, fits.OrderedPoints, windows);
    }

    private static void AddWindow(List<LocationTrendWindow> windows, TemperatureTrendFits fits, TrendWindow window, PolynomialRegressionResult? regression)
    {
        if (regression is not null)
        {
            windows.Add(new LocationTrendWindow(window, regression, TemperatureTrendCalculator.GetWindowPoints(fits.OrderedPoints, window)));
        }
    }
}
