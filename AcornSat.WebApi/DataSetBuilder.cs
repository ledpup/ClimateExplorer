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
        public async Task<ChartableDataPoint[]> BuildDataSet(PostDataSetsRequestBody request)
        {
            ValidateRequest(request);

            // This method reads raw data & derives a series from it as per the request
            var dataPoints = await GetSeriesDataPointsForRequest(request.SeriesDerivationType, request.SeriesSpecifications);

            // This method applies the specified transformation to each data point in the series
            var transformedDataPoints = ApplySeriesTransformation(dataPoints, request.SeriesTransformation);

            // Filter data at series level
            var filteredDataPoints = ApplySeriesFilters(transformedDataPoints, request.FilterToTemperateSeason, request.FilterToTropicalSeason, request.FilterToYearsAfterAndIncluding, request.FilterToYearsBefore);

            // Assign to Bins & Sub-bins, and reject Bins with insufficient data.
            var rawBins = ApplyBinningRules(filteredDataPoints, request.BinningRule, request.SubBinSize, request.SubBinRequiredDataProportion);

            // Calculate aggregates for each bin
            var aggregatedBins = AggregateBins(rawBins, request.BinAggregationFunction);

            return
                aggregatedBins
                .Select(x => new ChartableDataPoint { Label = x.Identifier.Label, Value = x.Value })
                .ToArray();
        }

        Bin[] AggregateBins(RawBin[] rawBins, BinAggregationFunctions binAggregationFunction)
        {
            switch (binAggregationFunction)
            {
                case BinAggregationFunctions.Mean:
                    var w =
                        rawBins
                        // First, aggregate the data points in each bucket
                        .Select(x => new { RawBin = x, BucketAggregates = x.SubBinnedDataPoints.Select(y => y.Where(z => z.Value.HasValue).Average(z => z.Value)) })
                        .ToArray();

                    var z = 
                        // Second, aggregate the bucket-level values we calculated in step one
                        w
                        .Select(x => new Bin { Identifier = x.RawBin.Identifier, Value = x.BucketAggregates.Average() })
                        .ToArray();

                    return z;

                default:
                    throw new NotImplementedException($"BinAgregationFunction {binAggregationFunction}");
            }
        }

        RawBin[] ApplyBinningRules(TemporalDataPoint[] dataPoints, BinningRules binningRule, int subBinSize, float subBinRequiredDataProportion)
        {
            var dataPointsByBinId =
                dataPoints
                .ToLookup(x => GetBinIdentifier(x, binningRule));

            switch (binningRule)
            {
                case BinningRules.ByYear:
                case BinningRules.ByYearAndMonth:
                    return
                        dataPointsByBinId
                        .Select(
                            x =>
                            new RawBin()
                            {
                                Identifier = x.Key,
                                SubBinnedDataPoints = BuildBucketsForGaplessBin(x, subBinSize)
                            }
                        )
                        .ToArray();

                default:
                    // TODO: Sub-binning. Remember we will need to handle sub-binning across disjoint bins (e.g. the bin containing all Januaries across all years)
                    return
                        dataPointsByBinId
                        .Select(
                            x =>
                            new RawBin()
                            {
                                Identifier = x.Key,
                                SubBinnedDataPoints =
                                    // For now, just put everything in a single sub-bin, and don't reject
                                    new TemporalDataPoint[][]
                                    {
                                        x.ToArray()
                                    }
                            }
                        )
                        .ToArray();
            }
        }

        TemporalDataPoint[][] BuildBucketsForGaplessBin(IGrouping<BinIdentifier, TemporalDataPoint> bin, int subBinSize)
        {
            var binIdentifier = bin.Key as BinIdentifierForGaplessBin;

            if (binIdentifier == null)
            {
                throw new Exception($"Cannot treat BinIdentifier of type {bin.Key.GetType().Name} as a {nameof(BinIdentifierForGaplessBin)}");
            }

            List<TemporalDataPoint[]> arrays = new List<TemporalDataPoint[]>();

            var d = binIdentifier.FirstDayInBin;

            List<TemporalDataPoint> bucket = new List<TemporalDataPoint>();

            var points = bin.ToArray();
            var i = 0;
            int bucketCounter = 0;

            while (d <= binIdentifier.LastDayInBin && i < points.Length)
            {
                if (points[i].Year == d.Year && points[i].Month == d.Month && points[i].Day == d.Day)
                {
                    bucket.Add(points[i]);
                    i++;
                }

                bucketCounter++;
                if (bucketCounter == subBinSize)
                {
                    arrays.Add(bucket.ToArray());
                    bucket = new List<TemporalDataPoint>();
                    bucketCounter = 0;
                }

                d = d.AddDays(1);
            }

            // If we have leftovers, add them to the last bucket
            if (bucketCounter > 0)
            {
                var last = arrays.Last();
                arrays.Remove(last);
                arrays.Add(last.Concat(bucket).ToArray());
            }

            return arrays.ToArray();
        }

        BinIdentifier GetBinIdentifier(TemporalDataPoint dp, BinningRules binningRule)
        {
            switch (binningRule)
            {
                case BinningRules.ByYear:                return new YearBinIdentifier(dp.Year);
                case BinningRules.ByYearAndMonth:        return new YearAndMonthBinIdentifier(dp.Year, dp.Month.Value);
                case BinningRules.ByMonthOnly:           return new MonthOnlyBinIdentifier(dp.Month.Value);
                case BinningRules.ByTemperateSeasonOnly: return new TemperateSeasonOnlyBinIdentifier(GetTemperateSeasonForMonth(dp.Month.Value));
                case BinningRules.ByTropicalSeasonOnly:  return new TropicalSeasonOnlyBinIdentifier(GetTropicalSeasonForMonth(dp.Month.Value));
                default: throw new NotImplementedException($"BinningRule {binningRule}");
            }
        }

        TemporalDataPoint[] ApplySeriesFilters(
            TemporalDataPoint[] transformedDataPoints, 
            TemperateSeasons? filterToTemperateSeason, 
            TropicalSeasons? filterToTropicalSeason,
            int? filterToYearsAfterAndIncluding, 
            int? filterToYearsBefore)
        {
            IEnumerable<TemporalDataPoint> query = transformedDataPoints;

            if (filterToTemperateSeason != null)
            {
                query = query.Where(x => GetTemperateSeasonForMonth(x.Month.Value) == filterToTemperateSeason.Value);
            }

            if (filterToTropicalSeason != null)
            {
                query = query.Where(x => GetTropicalSeasonForMonth(x.Month.Value) == filterToTropicalSeason.Value);
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
        
        TemperateSeasons GetTemperateSeasonForMonth(int month)
        {
            if (month <= 2 || month == 12) return TemperateSeasons.Summer;
            if (month <= 5) return TemperateSeasons.Autumn;
            if (month <= 8) return TemperateSeasons.Winter;
            return TemperateSeasons.Spring;
        }

        TropicalSeasons GetTropicalSeasonForMonth(int month)
        {
            if (month <= 4 || month >= 10) return TropicalSeasons.Wet;
            return TropicalSeasons.Dry;
        }

        private TemporalDataPoint[] ApplySeriesTransformation(TemporalDataPoint[] dataPoints, SeriesTransformations seriesTransformation)
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

        async Task<TemporalDataPoint[]> GetSeriesDataPointsForRequest(SeriesDerivationTypes seriesDerivationType, SeriesSpecification[] seriesSpecifications)
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

        private async Task<TemporalDataPoint[]> BuildDifferenceBetweenTwoSeries(SeriesSpecification[] seriesSpecifications)
        {
            if (seriesSpecifications.Length != 2)
            {
                throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.DifferenceBetweenTwoSeries)}, exactly two SeriesSpecifications must be provided.");
            }

            var dp1 = await GetSeriesDataPoints(seriesSpecifications[0]);
            var dp2 = await GetSeriesDataPoints(seriesSpecifications[1]);

            if (dp1.Length == 0 || dp2.Length == 0)
            {
                return new TemporalDataPoint[0];
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

            List<TemporalDataPoint> results = new List<TemporalDataPoint>();

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

        async Task<TemporalDataPoint[]> GetSeriesDataPoints(SeriesSpecification seriesSpecification)
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

            return dataSets.Single().DataRecords.Select(x => new TemporalDataPoint { Year = x.Year, Month = x.Month, Day = x.Day, Value = x.Value }).ToArray();
        }
    }
}
