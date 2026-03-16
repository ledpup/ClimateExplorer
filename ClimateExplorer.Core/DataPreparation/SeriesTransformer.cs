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

            // Temperature is measured by standard instruments which are located in a shelter (Stevenson screen) at a height of approximately 1.2 m above the ground.
            // These observations are then used to approximate the conditions at surface level.
            // An observed temperature of 2.2°C at screen level indicates that the temperature at the surface is approaching 0°C.
            // http://www.bom.gov.au/climate/map/frost/what-is-frost.shtml#indicator
            SeriesTransformations.IsFrosty => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value <= 2.2 ? 1 : 0)))],
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
