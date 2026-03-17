namespace ClimateExplorer.Core.DataPreparation;

using ClimateExplorer.Core.Model;

public static class SeriesTransformer
{
    public static DataRecord[] ApplySeriesTransformation(DataRecord[] dataRecords, SeriesTransformations seriesTransformation, string? customTransformation = null)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.Identity => [.. dataRecords], // Clone the input array because we want no side-effects
            SeriesTransformations.IsPositive => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value > 0 ? 1 : 0)))],
            SeriesTransformations.IsNegative => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value < 0 ? 1 : 0)))],
            SeriesTransformations.Negate => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : x.Value * -1))],
            SeriesTransformations.DayOfYearIfFrost => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value <= 2.2 ? new DateTime(x.Year, x.Month!.Value, x.Day!.Value).DayOfYear : 0)))],
            SeriesTransformations.Custom => string.IsNullOrWhiteSpace(customTransformation)
                ? [.. dataRecords]
                : ApplyCustomTransformation(dataRecords, customTransformation),
            _ => throw new NotImplementedException($"SeriesTransformation {seriesTransformation}"),
        };
    }

    private static DataRecord[] ApplyCustomTransformation(DataRecord[] dataRecords, string expression)
    {
        var transform = CustomTransformationParser.Parse(expression);
        return [.. dataRecords.Select(x => x.WithValue(transform(x.Value)))];
    }
}
