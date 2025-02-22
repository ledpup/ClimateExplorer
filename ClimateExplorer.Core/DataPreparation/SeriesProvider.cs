﻿namespace ClimateExplorer.Core.DataPreparation;

using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public static class SeriesProvider
{
    public static async Task<Series> GetSeriesDataRecordsForRequest(SeriesDerivationTypes seriesDerivationType, SeriesSpecification[] seriesSpecifications)
    {
        switch (seriesDerivationType)
        {
            case SeriesDerivationTypes.ReturnSingleSeries:
            case SeriesDerivationTypes.AverageOfAnomaliesInRegion:
                if (seriesSpecifications.Length != 1)
                {
                    throw new Exception($"When SeriesDerivationType is {nameof(seriesDerivationType)}, exactly one SeriesSpecification must be provided.");
                }

                return await GetSeriesDataRecords(seriesSpecifications.Single());

            case SeriesDerivationTypes.DifferenceBetweenTwoSeries:
                return await BuildDifferenceBetweenTwoSeries(seriesSpecifications);

            case SeriesDerivationTypes.AverageOfMultipleSeries:
                return await BuildAverageOfMultipleSeries(seriesSpecifications);

            default:
                throw new NotImplementedException($"SeriesDerivationType {seriesDerivationType}");
        }
    }

    private static async Task<Series> BuildAverageOfMultipleSeries(SeriesSpecification[] seriesSpecifications)
    {
        if (seriesSpecifications.Length < 2)
        {
            throw new Exception($"When SeriesDerivationType is {nameof(seriesSpecifications)}, more than one SeriesSpecifications must be provided.");
        }

        DateOnly minDate = DateOnly.FromDateTime(DateTime.Today.Date);
        DateOnly maxDate = DateOnly.FromDateTime(DateTime.MinValue);

        var dbGroups = new Dictionary<DateOnly, List<DataRecord>>();

        UnitOfMeasure? uom = null;
        DataResolution? dataResolution = null;

        foreach (var seriesSpec in seriesSpecifications)
        {
            var series = await GetSeriesDataRecords(seriesSpec);
            var dp = series.DataRecords;

            if (uom != null && uom != series.UnitOfMeasure)
            {
                throw new Exception($"Cannot mix the unit of measure when average between series. {uom} != {series.UnitOfMeasure}");
            }

            if (dataResolution != null && dataResolution != series.DataResolution)
            {
                throw new Exception($"Cannot mix the data resolution when average between series. {dataResolution} != {series.DataResolution}");
            }

            uom = series.UnitOfMeasure;
            dataResolution = series.DataResolution;

            foreach (var point in dp!)
            {
                var date = new DateOnly(point.Year, point.Month ?? 1, point.Day ?? 1);
                if (!dbGroups.ContainsKey(date))
                {
                    dbGroups.Add(date, []);
                }

                dbGroups[date].Add(point);
            }

            DateOnly minDateDp = dbGroups.Select(x => x.Key).Min();
            DateOnly maxDateDp = dbGroups.Select(x => x.Key).Max();

            minDate = minDateDp < minDate ? minDateDp : minDate;
            maxDate = maxDateDp > maxDate ? maxDateDp : maxDate;
        }

        DateOnly d = minDate;

        var results = new List<DataRecord>();

        while (d <= maxDate)
        {
            var dpsForDateExists = dbGroups.TryGetValue(d, out var dpsForDate);

            double? val = null;

            if (dpsForDateExists && dpsForDate!.All(x => x.Value != null))
            {
                val = dpsForDate!.Average(x => x.Value);
                results.Add(dpsForDate!.First().WithValue(val));
            }
            else
            {
                results.Add(new DataRecord((short)d.Year, (short)d.Month, (short)d.Day, val));
            }

            d = d.AddDays(1);
        }

        return
            new Series
            {
                DataRecords = results.ToArray(),
                UnitOfMeasure = uom!.Value,
                DataResolution = dataResolution!.Value,
            };
    }

    private static async Task<Series> BuildDifferenceBetweenTwoSeries(SeriesSpecification[] seriesSpecifications)
    {
        if (seriesSpecifications.Length != 2)
        {
            throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.DifferenceBetweenTwoSeries)}, exactly two SeriesSpecifications must be provided.");
        }

        var series1 = await GetSeriesDataRecords(seriesSpecifications[0]);
        var series2 = await GetSeriesDataRecords(seriesSpecifications[1]);

        var dp1 = series1.DataRecords;
        var dp2 = series2.DataRecords;

        if (dp1!.Length == 0 || dp2!.Length == 0)
        {
            return new Series { DataRecords = Array.Empty<DataRecord>(), UnitOfMeasure = series1.UnitOfMeasure };
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

        var results = new List<DataRecord>();

        while (d <= maxDate)
        {
            var dp1ForDateExists = dp1Grouped.TryGetValue(d, out var dp1ForDate);
            var dp2ForDateExists = dp2Grouped.TryGetValue(d, out var dp2ForDate);

            double? val = null;

            if (dp1ForDateExists && dp2ForDateExists)
            {
                val = dp1ForDate!.Value - dp2ForDate!.Value;
            }

            var dpToUse = dp1ForDateExists ? dp1ForDate : dp2ForDate;

            results.Add(dpToUse!.WithValue(val));

            d = d.AddDays(1);
        }

        return
            new Series
            {
                DataRecords = results.ToArray(),
                UnitOfMeasure = series1.UnitOfMeasure,
                DataResolution = series1.DataResolution,
            };
    }

    private static async Task<Series> GetSeriesDataRecords(SeriesSpecification seriesSpecification)
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();

        var dsd = definitions.Single(x => x.Id == seriesSpecification.DataSetDefinitionId);

        var measurementDefinition =
            dsd.MeasurementDefinitions!
            .Single(
                x =>
                x.DataType == seriesSpecification.DataType &&
                x.DataAdjustment == seriesSpecification.DataAdjustment);

        var geoEntity = await GeographicalEntity.GetGeographicalEntity(seriesSpecification.LocationId);

        if (!dsd.DataLocationMapping!.LocationIdToDataFileMappings.TryGetValue(geoEntity.Id, out List<DataFileFilterAndAdjustment>? dataFileFilterAndAdjustments))
        {
            throw new Exception($"DataSetDefinition {dsd.Id} does not have a LocationIdToDataFileMapping entry for location {geoEntity.Id}");
        }

        // We have two different "data readers".
        //
        // DataReader is the configurable reg-ex-y one that can handle one-sample-per-line file formats. It's
        // used for temperature, precipitation and CO2, for example.
        //
        // The other one is a twelve-month-per-line reader.
        List<DataRecord> dataRecords;

        switch (measurementDefinition.RowDataType)
        {
            case RowDataType.OneValuePerRow:
                dataRecords = await DataReaderFunctions.GetDataRecords(measurementDefinition, dataFileFilterAndAdjustments!);
                break;

            case RowDataType.TwelveMonthsPerRow:
                dataRecords = await TwelveMonthPerLineDataReader.GetTwelveMonthsPerRowData(measurementDefinition, dataFileFilterAndAdjustments);
                break;

            default:
                throw new NotImplementedException($"RowDataType {measurementDefinition.RowDataType}");
        }

        return
            new Series
            {
                DataRecords = dataRecords.Select(x => new DataRecord(x.Year, x.Month, x.Day, x.Value)).ToArray(),
                UnitOfMeasure = measurementDefinition.UnitOfMeasure,
                DataResolution = measurementDefinition.DataResolution,
            };
    }

    public class Series
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
        public DataRecord[]? DataRecords { get; set; }
        public UnitOfMeasure UnitOfMeasure { get; set; }
        public DataResolution DataResolution { get; set; }
    }
}
