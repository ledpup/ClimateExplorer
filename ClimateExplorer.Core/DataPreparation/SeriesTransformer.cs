namespace ClimateExplorer.Core.DataPreparation;

using ClimateExplorer.Core.Model;

public static class SeriesTransformer
{
    public static DataRecord[] ApplySeriesTransformation(DataRecord[] dataRecords, SeriesTransformations seriesTransformation)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.Identity => [.. dataRecords], // Clone the input array because we want no side-effects
            SeriesTransformations.IsPositive => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value > 0 ? 1 : 0)))],
            SeriesTransformations.IsNegative => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value < 0 ? 1 : 0)))],
            SeriesTransformations.EnsoCategory => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value > 0.5 ? 1 : (x.Value < -0.5 ? -1 : 0))))],
            SeriesTransformations.Negate => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : x.Value * -1))],
            SeriesTransformations.IsZero => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value == 0 ? 1 : 0)))],

            // Temperature is measured by standard instruments which are located in a shelter (Stevenson screen) at a height of approximately 1.2 m above the ground.
            // These observations are then used to approximate the conditions at surface level.
            // An observed temperature of 2.2°C at screen level indicates that the temperature at the surface is approaching 0°C.
            // http://www.bom.gov.au/climate/map/frost/what-is-frost.shtml#indicator
            SeriesTransformations.IsFrosty => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value <= 2.2 ? 1 : 0)))],
            SeriesTransformations.EqualOrAbove25 or SeriesTransformations.EqualOrAbove25mm => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value >= 25 ? 1 : 0)))],
            SeriesTransformations.EqualOrAbove35 => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value >= 35 ? 1 : 0)))],

            // A rain day is recorded when there has been a daily rainfall total of at least 0.2 mm (or 0.1 mm for some more recent sites).
            // A rain day does not occur when there is only a trace of moisture in the rain gauge, or when the precipitation was observed to be solely from frost, dew or fog.
            // A rainfall total of 0.2 mm is quite a small amount of rain, and unlikely to have much impact on many activities.
            // Therefore, days of rain greater than or equal to 1 mm, 10 mm, and 25 mm are often used as indicators of the number of "wet" days.
            // http://www.bom.gov.au/climate/data-services/content/faqs-elements.html
            SeriesTransformations.EqualOrAbove1 => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value >= 1 ? 1 : 0)))],
            SeriesTransformations.EqualOrAbove1AndLessThan10 => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value >= 1 && x.Value < 10 ? 1 : 0)))],
            SeriesTransformations.EqualOrAbove10 => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value >= 10 ? 1 : 0)))],
            SeriesTransformations.EqualOrAbove10AndLessThan25 => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value >= 10 && x.Value < 25 ? 1 : 0)))],
            SeriesTransformations.DayOfYearIfFrost => [.. dataRecords.Select(x => x.WithValue(x.Value == null ? null : (x.Value <= 2.2 ? new DateTime(x.Year, x.Month!.Value, x.Day!.Value).DayOfYear : 0)))],
            _ => throw new NotImplementedException($"SeriesTransformation {seriesTransformation}"),
        };
    }
}
