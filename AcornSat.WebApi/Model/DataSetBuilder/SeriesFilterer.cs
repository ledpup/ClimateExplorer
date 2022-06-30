using System.Collections.Generic;
using System.Linq;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public static class SeriesFilterer
    {
        public static TemporalDataPoint[] ApplySeriesFilters(
            TemporalDataPoint[] transformedDataPoints,
            TemperateSeasons? filterToTemperateSeason,
            TropicalSeasons? filterToTropicalSeason,
            int? filterToYearsAfterAndIncluding,
            int? filterToYearsBefore)
        {
            IEnumerable<TemporalDataPoint> query = transformedDataPoints;

            if (filterToTemperateSeason != null)
            {
                query = query.Where(x => DateHelpers.GetTemperateSeasonForMonth(x.Month.Value) == filterToTemperateSeason.Value);
            }

            if (filterToTropicalSeason != null)
            {
                query = query.Where(x => DateHelpers.GetTropicalSeasonForMonth(x.Month.Value) == filterToTropicalSeason.Value);
            }

            if (filterToYearsAfterAndIncluding != null)
            {
                query = query.Where(x => x.Year >= filterToYearsAfterAndIncluding.Value);
            }

            if (filterToYearsBefore != null)
            {
                query = query.Where(x => x.Year >= filterToYearsBefore.Value);
            }

            return query.ToArray();
        }
    }
}
