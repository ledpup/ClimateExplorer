using AcornSat.Core.InputOutput;
using AcornSat.WebApi.Model.DataSetBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcornSat.WebApi
{
    public class DataSetBuilder
    {
        public async Task<DataSet> BuildDataSet(PostDataSetsRequestBody request)
        {
            ValidateRequest(request);

            // This method reads raw data & derives a series from it as per the request
            var dataPoints = await GetSeriesDataPointsForRequest(request.SeriesDerivationType, request.SeriesSpecifications);

            // This method applies the specified transformation to each data point in the series
            var transformedDataPoints = ApplySeriesTransformation(dataPoints, request.SeriesTransformation);

            // This method applies the specified series filters
            var filteredDataPoints = ApplySeriesFilters(transformedDataPoints, request.FilterToTemperateSeason, request.FilterToTropicalSeason, request.FilterToYearsAfterAndIncluding, request.FilterToYearsBefore);

            return
                new DataSet()
                {
                    MeasurementDefinition = new Core.ViewModel.MeasurementDefinitionViewModel(),
                    Location = new Location(),
                    DataRecords =
                        filteredDataPoints
                        .Select(
                            x => 
                            new DataRecord()
                            {
                                Year = x.Year,
                                Month = x.Month,
                                Day = x.Day,
                                Value = x.Value
                            }
                        )
                        .ToList()
                };
        }

        DataPoint[] ApplySeriesFilters(
            DataPoint[] transformedDataPoints, 
            TemperateSeasons? filterToTemperateSeason, 
            TropicalSeasons? filterToTropicalSeason,
            int? filterToYearsAfterAndIncluding, 
            int? filterToYearsBefore)
        {
            IEnumerable<DataPoint> query = transformedDataPoints;

            if (filterToTemperateSeason != null)
            {
                var months = GetMonthsForSeason(filterToTemperateSeason.Value);

                query = query.Where(x => months.Contains(x.Month.Value));
            }

            if (filterToTropicalSeason != null)
            {
                var months = GetMonthsForSeason(filterToTropicalSeason.Value);

                query = query.Where(x => months.Contains(x.Month.Value));
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

        int[] GetMonthsForSeason(TemperateSeasons value)
        {
            switch (value)
            {
                case TemperateSeasons.Summer: return new int[] { 12,  1,  2 };
                case TemperateSeasons.Autumn: return new int[] {  3,  4,  5 };
                case TemperateSeasons.Winter: return new int[] {  6,  7,  8 };
                case TemperateSeasons.Spring: return new int[] {  9, 10, 11 };
                default: throw new NotImplementedException($"TemperateSeason {value}");
            }
        }

        int[] GetMonthsForSeason(TropicalSeasons value)
        {
            switch (value)
            {
                case TropicalSeasons.Wet: return new int[] { 10, 11, 12, 1, 2, 3, 4 };
                case TropicalSeasons.Dry: return new int[] { 5, 6, 7, 8, 9 };
                default: throw new NotImplementedException($"TropicalSeason {value}");
            }
        }

        private DataPoint[] ApplySeriesTransformation(DataPoint[] dataPoints, SeriesTransformations seriesTransformation)
        {
            switch (seriesTransformation)
            {
                case SeriesTransformations.Identity:
                    // No side-effects - clone the input array
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

        public void ValidateRequest(PostDataSetsRequestBody request)
        {
            if (request.SeriesSpecifications == null) throw new ArgumentNullException(nameof(request.SeriesSpecifications));
        }

        async Task<DataPoint[]> GetSeriesDataPointsForRequest(SeriesDerivationTypes seriesDerivationType, SeriesSpecification[] seriesSpecifications)
        {
            switch (seriesDerivationType)
            {
                case SeriesDerivationTypes.ReturnSingleSeries:
                    if (seriesSpecifications.Length != 1)
                    {
                        throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.ReturnSingleSeries)}, exactly one SeriesSpecification must be provided.");
                    }

                    return await GetSeriesDataPoints(seriesSpecifications.Single());

                case SeriesDerivationTypes.DifferenceBetweenTwoSeries:
                    return await BuildDifferenceBetweenTwoSeries(seriesSpecifications);

                default:
                    throw new NotImplementedException($"SeriesDerivationType {seriesDerivationType}");

            }
        }

        private async Task<DataPoint[]> BuildDifferenceBetweenTwoSeries(SeriesSpecification[] seriesSpecifications)
        {
            if (seriesSpecifications.Length != 2)
            {
                throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.DifferenceBetweenTwoSeries)}, exactly two SeriesSpecifications must be provided.");
            }

            var dp1 = await GetSeriesDataPoints(seriesSpecifications[0]);
            var dp2 = await GetSeriesDataPoints(seriesSpecifications[1]);

            if (dp1.Length == 0 || dp2.Length == 0)
            {
                return new DataPoint[0];
            }

            var dp1Grouped = dp1.ToDictionary(x => new DateOnly(x.Year, x.Month ?? 1, x.Day ?? 1));
            var dp2Grouped = dp2.ToDictionary(x => new DateOnly(x.Year, x.Month ?? 1, x.Day ?? 1));

            DateOnly minDate1 = dp1Grouped.Select(x => x.Key).Min();
            DateOnly minDate2 = dp2Grouped.Select(x => x.Key).Min();

            DateOnly maxDate1 = dp1Grouped.Select(x => x.Key).Max();
            DateOnly maxDate2 = dp2Grouped.Select(x => x.Key).Max();

            DateOnly minDate = minDate1 < minDate2 ? minDate1 : minDate2;
            DateOnly maxDate = maxDate1 > maxDate2 ? maxDate1 : maxDate2;

            DateOnly d = minDate;

            List<DataPoint> results = new List<DataPoint>();

            while (d <= maxDate)
            {
                var dp1ForDateExists = dp1Grouped.TryGetValue(d, out var dp1ForDate);
                var dp2ForDateExists = dp2Grouped.TryGetValue(d, out var dp2ForDate);

                float? val = null;

                if (dp1ForDateExists && dp2ForDateExists)
                {
                    val = dp1ForDate.Value - dp2ForDate.Value;
                }

                var dpToUse = dp1ForDateExists ? dp1ForDate : dp2ForDate;

                results.Add(dpToUse.WithValue(val));

                d = d.AddDays(1);
            }

            return results.ToArray();
        }

        async Task<DataPoint[]> GetSeriesDataPoints(SeriesSpecification seriesSpecification)
        {
            var definitions = await DataSetDefinition.GetDataSetDefinitions();

            var dsd = definitions.Single(x => x.Id == seriesSpecification.DataSetDefinitionId);

            var measurementDefinition =
                dsd.MeasurementDefinitions
                .Single(
                    x =>
                    x.DataType == seriesSpecification.DataType &&
                    x.DataAdjustment == seriesSpecification.DataAdjustment);

            var location = seriesSpecification.LocationId == null ? null : (await Location.GetLocations(dsd.FolderName)).Single(x => x.Id == seriesSpecification.LocationId);

            var dataSets = await DataReader.ReadDataFile(dsd.FolderName, measurementDefinition, dsd.DataResolution, location);

            if (seriesSpecification.DataAdjustment == Core.Enums.DataAdjustment.Unadjusted)
            {
                // TODO: There are probably multiple datasets here, and we need to "merge" them by referring to primarysites, and taking the ranges it specifies
                throw new NotImplementedException("Not implemented: building unadjusted data for a main site out of out of subsets of the various contributing sites");
            }

            dataSets = dataSets.Where(x => x.DataRecords != null).ToList();

            if (dataSets.Count > 1)
            {
                // TODO: Include SeriesSpecification in thrown exception
                throw new Exception("Multiple data sets returned.");
            }

            if (dataSets.Count == 0)
            {
                // TODO: Include SeriesSpecification in thrown exception
                throw new Exception("No data sets available.");
            }

            return dataSets.Single().DataRecords.Select(x => new DataPoint { Year = x.Year, Month = x.Month, Day = x.Day, Value = x.Value }).ToArray();
        }
    }
}
