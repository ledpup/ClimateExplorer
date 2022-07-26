using AcornSat.Core.InputOutput;
using AcornSat.Core.ViewModel;
using AcornSat.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;
using ClimateExplorer.Core.InputOutput;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class SeriesProvider
    {
        public class Series
        {
            public TemporalDataPoint[] DataPoints { get; set; }
            public UnitOfMeasure UnitOfMeasure { get; set; }
            public DataCategory? DataCategory { get; set; }
            public DataResolution DataResolution { get; set; }
        }

        static public async Task<Series> GetSeriesDataPointsForRequest(SeriesDerivationTypes seriesDerivationType, SeriesSpecification[] seriesSpecifications)
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

        static async Task<Series> BuildDifferenceBetweenTwoSeries(SeriesSpecification[] seriesSpecifications)
        {
            if (seriesSpecifications.Length != 2)
            {
                throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.DifferenceBetweenTwoSeries)}, exactly two SeriesSpecifications must be provided.");
            }

            var series1 = await GetSeriesDataPoints(seriesSpecifications[0]);
            var series2 = await GetSeriesDataPoints(seriesSpecifications[1]);

            var dp1 = series1.DataPoints;
            var dp2 = series2.DataPoints;

            if (dp1.Length == 0 || dp2.Length == 0)
            {
                return new Series { DataPoints = new TemporalDataPoint[0], UnitOfMeasure = series1.UnitOfMeasure };
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

            return
                new Series
                {
                    DataPoints = results.ToArray(),
                    UnitOfMeasure = series1.UnitOfMeasure,
                    DataCategory = series1.DataCategory,
                    DataResolution = series1.DataResolution
                };
        }

        static async Task<Series> GetSeriesDataPoints(SeriesSpecification seriesSpecification)
        {
            var definitions = await DataSetDefinition.GetDataSetDefinitions();

            var dsd = definitions.Single(x => x.Id == seriesSpecification.DataSetDefinitionId);

            var measurementDefinition =
                dsd.MeasurementDefinitions
                .Single(
                    x =>
                    x.DataType == seriesSpecification.DataType &&
                    x.DataAdjustment == seriesSpecification.DataAdjustment);


            var location = seriesSpecification.LocationId == null ? null : (await Location.GetLocations(false)).Single(x => x.Id == seriesSpecification.LocationId);

            List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments = null;

            if (location != null)
            {
                if (!dsd.DataLocationMapping.LocationIdToDataFileMappings.TryGetValue(location.Id, out dataFileFilterAndAdjustments))
                {
                    throw new Exception($"DataSetDefinition {dsd.Id} does not have a LocationIdToDataFileMapping entry for location {location.Id}");
                }
            }

            // We have two different "data readers".
            //
            // DataReader is the configurable reg-ex-y one that can handle one-sample-per-line file formats. It's
            // used for temperature, rainfall and CO2, for example.
            //
            // The other one is a twelve-month-per-line reader.
            List<DataRecord> dataRecords;

            switch (measurementDefinition.RowDataType)
            {
                case RowDataType.OneValuePerRow:
                    dataRecords = await DataReader.GetDataRecords(measurementDefinition, dataFileFilterAndAdjustments);
                    break;

                case RowDataType.TwelveMonthsPerRow:
                    var dataSet = await TwelveMonthPerLineDataReader.GetTwelveMonthsPerRowData(measurementDefinition);
                    dataRecords = dataSet.DataRecords;
                    break;

                default:
                    throw new NotImplementedException($"RowDataType {measurementDefinition.RowDataType}");
            }

            return
                new Series
                {
                    DataPoints = dataRecords.Select(x => new TemporalDataPoint { Year = x.Year, Month = x.Month, Day = x.Day, Value = x.Value }).ToArray(),
                    UnitOfMeasure = measurementDefinition.UnitOfMeasure,
                    DataCategory = measurementDefinition.DataCategory,
                    DataResolution = measurementDefinition.DataResolution
                };
        }
    }
}
