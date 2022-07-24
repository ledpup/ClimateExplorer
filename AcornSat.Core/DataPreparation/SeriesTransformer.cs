using System;
using System.Linq;

namespace ClimateExplorer.Core.DataPreparation
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

                // Temperature is measured by standard instruments which are located in a shelter (Stevenson screen) at a height of approximately 1.2 m above the ground.
                // These observations are then used to approximate the conditions at surface level.
                // An observed temperature of 2.2°C at screen level indicates that the temperature at the surface is approaching 0°C.
                // http://www.bom.gov.au/climate/map/frost/what-is-frost.shtml#indicator
                // We'll use 2°C to keep it simple
                case SeriesTransformations.IsFrosty:
                    return
                        dataPoints
                        .Select(x => x.WithValue(x.Value == null ? null : (x.Value <= 2 ? 1 : 0)))
                        .ToArray();

                case SeriesTransformations.Above35:
                    return
                        dataPoints
                        .Select(x => x.WithValue(x.Value == null ? null : (x.Value > 35 ? 1 : 0)))
                        .ToArray();

                default:
                    throw new NotImplementedException($"SeriesTransformation {seriesTransformation}");
            }
        }
    }
}
