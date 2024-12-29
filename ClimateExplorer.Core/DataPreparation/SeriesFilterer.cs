namespace ClimateExplorer.Core.DataPreparation;

using ClimateExplorer.Core.Model;

public static class SeriesFilterer
{
    public static DataRecord[] ApplySeriesFilters(
        DataRecord[] transformedDataPoints,
        SouthernHemisphereTemperateSeasons? filterToSouthernHemisphereTemperateSeason,
        SouthernHemisphereTropicalSeasons? filterToTropicalSeason,
        short? filterToYear,
        int? filterToYearsAfterAndIncluding,
        int? filterToYearsBefore)
    {
        IEnumerable<DataRecord> query = transformedDataPoints;

        if (filterToSouthernHemisphereTemperateSeason != null)
        {
            query = query.Where(x => DateHelpers.GetSouthernHemisphereTemperateSeasonForMonth(x.Month!.Value) == filterToSouthernHemisphereTemperateSeason.Value);
        }

        if (filterToTropicalSeason != null)
        {
            query = query.Where(x => DateHelpers.GetSouthernHemisphereTropicalSeasonForMonth(x.Month!.Value) == filterToTropicalSeason.Value);
        }

        if (filterToYearsAfterAndIncluding != null)
        {
            query = query.Where(x => x.Year >= filterToYearsAfterAndIncluding.Value);
        }

        if (filterToYearsBefore != null)
        {
            query = query.Where(x => x.Year < filterToYearsBefore.Value);
        }

        if (filterToYear != null)
        {
            query = query.Where(x => x.Year == filterToYear.Value);
        }

        return query.ToArray();
    }
}
