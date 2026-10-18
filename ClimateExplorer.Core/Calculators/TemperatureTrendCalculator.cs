namespace ClimateExplorer.Core.Calculators;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// Fits the full-record and last-30-years linear trends of a location's yearly temperature series. The
/// API uses it to put compact <see cref="TrendSummary"/>s on every <see cref="Location"/>, and the
/// dashboard's "about trends" modal recalculates the full statistics with the same <see cref="Fit"/>, so
/// the two always describe the same windows.
/// </summary>
public static class TemperatureTrendCalculator
{
    private const int SlopeDecimalPlaces = 5;

    /// <summary>
    /// Fits the trend windows. Null below <see cref="TrendDefaults.MinimumYearsForTrend"/> complete years;
    /// the full period only below <see cref="TrendDefaults.MinimumYearsForSubPeriodWindows"/>; otherwise
    /// the full period and the last <see cref="TrendDefaults.RecentWindowYears"/> complete years.
    /// </summary>
    public static TemperatureTrendFits? Fit(IReadOnlyList<DataPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count < TrendDefaults.MinimumYearsForTrend)
        {
            return null;
        }

        var ordered = points.OrderBy(x => x.X).ToList();

        var fullPeriod = PolynomialRegressionCalculator.Calculate(ordered, degree: 1);
        var last30Years = ordered.Count >= TrendDefaults.MinimumYearsForSubPeriodWindows
            ? PolynomialRegressionCalculator.Calculate(ordered.TakeLast(TrendDefaults.RecentWindowYears), degree: 1)
            : null;

        return new TemperatureTrendFits(fullPeriod, last30Years, ordered);
    }

    /// <summary>
    /// The compact form of <paramref name="fits"/>: the full-period summary, then the last-30-years one
    /// when there is one. A window whose slope isn't finite is dropped (the full period's takes the whole
    /// list with it), because the locations cache is serialised with options that reject NaN/Infinity.
    /// </summary>
    public static List<TrendSummary>? Summarise(TemperatureTrendFits? fits)
    {
        if (fits is null || !double.IsFinite(fits.FullPeriod.Curve.Slope))
        {
            return null;
        }

        var summaries = new List<TrendSummary> { ToSummary(fits.FullPeriod) };

        if (fits.Last30Years is not null && double.IsFinite(fits.Last30Years.Curve.Slope))
        {
            summaries.Add(ToSummary(fits.Last30Years));
        }

        return summaries;
    }

    /// <summary>
    /// Sets <see cref="Location.TemperatureTrends"/> and <see cref="Location.LastYear"/>
    /// together from a yearly series - both null when there isn't enough data for a trend.
    /// </summary>
    public static void SetTemperatureTrends(Location location, IEnumerable<ChartableDataPoint> dataPoints)
    {
        ArgumentNullException.ThrowIfNull(location);

        var fits = Fit(ToDataPoints(dataPoints));
        var summaries = Summarise(fits);

        location.TemperatureTrends = summaries;
        location.LastYear = summaries is null ? null : (int)Math.Round(fits!.FullPeriod.Input.MaximumX);
    }

    public static List<DataPoint> ToDataPoints(IEnumerable<ChartableDataPoint> dataPoints)
    {
        ArgumentNullException.ThrowIfNull(dataPoints);

        return ToDataPoints(dataPoints.Select(x => (x.BinId, x.Value)));
    }

    public static List<DataPoint> ToDataPoints(IEnumerable<BinnedRecord> dataRecords)
    {
        ArgumentNullException.ThrowIfNull(dataRecords);

        return ToDataPoints(dataRecords.Select(x => ((string?)x.BinId, x.Value)));
    }

    private static List<DataPoint> ToDataPoints(IEnumerable<(string? BinId, double? Value)> yearlyValues)
    {
        return
            [.. yearlyValues
                .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value))
                .Select(x => new DataPoint(((YearBinIdentifier)BinIdentifier.Parse(x.BinId!)).Year, x.Value!.Value))];
    }

    private static TrendSummary ToSummary(PolynomialRegressionResult fit)
    {
        return new TrendSummary(
            (int)Math.Round(fit.Input.MinimumX),
            Math.Round(fit.Curve.Slope, SlopeDecimalPlaces),
            fit.Significance.IsSlopeSignificant);
    }
}
