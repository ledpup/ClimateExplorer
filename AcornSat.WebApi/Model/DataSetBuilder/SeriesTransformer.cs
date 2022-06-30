using System;
using System.Linq;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public static class SeriesTransformer
    {
        public static TemporalDataPoint[] ApplySeriesTransformation(TemporalDataPoint[] dataPoints, SeriesTransformations seriesTransformation)
        {
            switch (seriesTransformation)
            {
                case SeriesTransformations.Identity:
                    // Clone the input array because we want no side-effects
                    return dataPoints.ToArray();

                case SeriesTransformations.IsPositive:
                    return
                        dataPoints
                        .Select(x => x.WithValue(x.Value == null ? null : (x.Value > 0 ? 1 : 0)))
                        .ToArray();

                case SeriesTransformations.IsNegative:
                    return
                        dataPoints
                        .Select(x => x.WithValue(x.Value == null ? null : (x.Value < 0 ? 1 : 0)))
                        .ToArray();

                case SeriesTransformations.EnsoCategory:
                    return
                        dataPoints
                        .Select(x => x.WithValue(x.Value == null ? null : (x.Value > 0.5 ? 1 : (x.Value < -0.5 ? -1 : 0))))
                        .ToArray();

                case SeriesTransformations.Negate:
                    return
                        dataPoints
                        .Select(x => x.WithValue(x.Value == null ? null : x.Value * -1))
                        .ToArray();

                default:
                    throw new NotImplementedException($"SeriesTransformation {seriesTransformation}");
            }
        }
    }
}
