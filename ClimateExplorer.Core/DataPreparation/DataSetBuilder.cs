namespace ClimateExplorer.Core.DataPreparation;

using System.Diagnostics;
using ClimateExplorer.Core.DataPreparation.Model;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public class DataSetBuilder
{
    public async Task<BuildDataSetResult> BuildDataSet(PostDataSetsRequestBody request)
    {
        ValidateRequest(request);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        // Reads raw data (from one or multiple sources) & derive a series from it as per the request
        var series = await SeriesProvider.GetSeriesDataRecordsForRequest(request.SeriesDerivationType, request.SeriesSpecifications!);

        Console.WriteLine("GetSeriesDataRecordsForRequest completed in " + sw.Elapsed);

        return BuildDataSetFromSeries(request, series);
    }

    /// <summary>
    /// Builds a result from a series the caller has already prepared (e.g. an ACORN-SAT series extended
    /// with a CDO overlay), sharing the same validation, filtering, and binning pipeline as
    /// <see cref="BuildDataSet"/> instead of reading through <see cref="SeriesProvider"/>.
    /// </summary>
    public BuildDataSetResult BuildDataSetFromSeries(PostDataSetsRequestBody request, SeriesProvider.Series series)
    {
        ValidateRequest(request);

        if (series.DataRecords != null && series.DataRecords.All(x => x.Value == null))
        {
            throw new Exception("All data records in the series are null. Check the raw input file");
        }

        if (request.MinimumDataResolution != null && series.DataResolution < request.MinimumDataResolution)
        {
            throw new Exception($"The data resolution of this series is {series.DataResolution}. A minimum data resolution thresold of {request.MinimumDataResolution} is required for this type of aggregation.");
        }

        // Run the rest of the pipeline (this is a separate method for testability)
        var built = BuildDataSetFromDataRecordsCore(series.DataRecords!, series.DataResolution, request);
        var dataPoints = built.DataPoints;

        if (dataPoints.All(x => x.Value == null))
        {
            // There is insufficient data for aggregation. Will return an empty set.
            dataPoints = [];
        }

        return
            new BuildDataSetResult
            {
                DataPoints = dataPoints,
                RawDataRecords = request.IncludeRawDataRecords == true ? series.DataRecords : null,
                UnitOfMeasure = series.UnitOfMeasure,
                AggregationApplied = built.AggregationApplied,
            };
    }

    /// <summary>
    /// Same pipeline as <see cref="BuildDataSetFromDataRecordsCore"/>, exposed for callers/tests that only
    /// need the resulting points, not whether any bin actually combined more than one raw data record.
    /// </summary>
    public ChartableDataPoint[] BuildDataSetFromDataRecords(DataRecord[] dataRecords, DataResolution dataResolution, PostDataSetsRequestBody request)
    {
        return BuildDataSetFromDataRecordsCore(dataRecords, dataResolution, request).DataPoints;
    }

    public void ValidateRequest(PostDataSetsRequestBody request)
    {
        if (request.SeriesSpecifications == null)
        {
            throw new ArgumentNullException(nameof(request.SeriesSpecifications));
        }
    }

    private static ChartableDataPoint[] ConvertDataRecordsToChartableDataPoints(DataRecord[] filteredDataRecords)
    {
        return filteredDataRecords
            .Select(x => (new YearAndDayBinIdentifier(x.Year, x.Month!.Value, x.Day!.Value), x.Value))
            .Select(
            x =>
            new ChartableDataPoint
            {
                BinId = x.Item1.Id,
                Label = x.Item1.Label,
                Value = x.Value == null ? null : x.Value.Value,
            })
        .ToArray();
    }

    private static ChartableDataPoint[] ConvertDataRecordsToDayOnlyChartableDataPoints(DataRecord[] filteredDataRecords)
    {
        return filteredDataRecords
            .Select(x => (new DayOnlyBinIdentifier(x.Month!.Value, x.Day!.Value), x.Value))
            .Select(
            x =>
            new ChartableDataPoint
            {
                BinId = x.Item1.Id,
                Label = x.Item1.Label,
                Value = x.Value == null ? null : x.Value.Value,
            })
        .ToArray();
    }

    /// <summary>
    /// Builds the chartable points for a series, alongside whether any output bin actually combined more
    /// than one raw data record. When it's false, every <see cref="SeriesAggregationOptions"/> function
    /// (Mean, Min, Max, Median, Sum) would produce an identical result - each bin already has at most one
    /// raw value flowing into it - so the choice of aggregation function makes no difference to the output.
    /// </summary>
    private DataSetPointsResult BuildDataSetFromDataRecordsCore(DataRecord[] dataRecords, DataResolution dataResolution, PostDataSetsRequestBody request)
    {
        Stopwatch sw = new();
        sw.Start();

        // Apply specified transformation (if any) to each data point in the series
        var transformedDataRecords = SeriesTransformer.ApplySeriesTransformation(dataRecords, request.SeriesTransformation, request.CustomTransformation);

        Console.WriteLine("ApplySeriesTransformation completed in " + sw.Elapsed);
        sw.Restart();

        // Filter data at series level
        var filteredDataRecords = SeriesFilterer.ApplySeriesFilters(transformedDataRecords, request.FilterToSouthernHemisphereTemperateSeason, request.FilterToTropicalSeason, request.FilterToYear, request.FilterToYearsAfterAndIncluding, request.FilterToYearsBefore);

        Console.WriteLine("ApplySeriesFilters completed in " + sw.Elapsed);
        sw.Restart();

        // When BinningRule is ByYearAndDay, we can drop-out of the data pipeline process here.
        // No aggregation is required because we're just returning the data at the original resolution (i.e., daily)
        if (request.BinningRule == BinGranularities.ByYearAndDay)
        {
            return new DataSetPointsResult(ConvertDataRecordsToChartableDataPoints(filteredDataRecords), AggregationApplied: false);
        }

        // When BinningRule is ByDayOnly with a year filter, no aggregation across years is required.
        // Return the data for the requested year using DayOnlyBinIdentifier.
        if (request.BinningRule == BinGranularities.ByDayOnly && request.FilterToYear.HasValue)
        {
            // This path assumes the underlying series is daily (non-null Month/Day).
            // Guard against non-daily resolutions to avoid downstream null Month/Day exceptions.
            if (dataResolution != DataResolution.Daily)
            {
                throw new System.InvalidOperationException("ByDayOnly binning with a year filter is only supported for daily data resolution.");
            }

            return new DataSetPointsResult(ConvertDataRecordsToDayOnlyChartableDataPoints(filteredDataRecords), AggregationApplied: false);
        }

        // Assign to Bins, Buckets and Cups
        var rawBins = Binner.ApplyBinningRules(filteredDataRecords, request.BinningRule, request.CupSize, dataResolution);

        Console.WriteLine("ApplyBinningRules completed in " + sw.Elapsed);
        sw.Restart();

        // True if at least one bin actually combines more than one raw data record - i.e. the chosen
        // SeriesAggregationOptions function was exercised somewhere. When every bin has at most one raw
        // record (e.g. yearly-resolution source data binned ByYear), every aggregation function produces
        // the same output, so the choice is inert.
        bool aggregationApplied =
            rawBins.Any(
                bin =>
                bin.Buckets!
                .SelectMany(bucket => bucket.Cups!)
                .SelectMany(cup => cup.DataRecords!)
                .Count(dr => dr.Value.HasValue) > 1);

        // Flag bins that have a bucket containing a cup with insufficient data
        var filteredRawBins =
            BinRejector.ApplyBinRejectionRules(
                rawBins,
                request.RequiredCupDataProportion,
                request.RequiredBucketDataProportion,
                request.RequiredBinDataProportion);

        Console.WriteLine("ApplyBinRejectionRules completed in " + sw.Elapsed);
        sw.Restart();

        // Calculate aggregates for each bin
        var aggregatedBins = BinAggregator.AggregateBins(filteredRawBins, request.BinAggregationFunction, request.BucketAggregationFunction, request.CupAggregationFunction, request.SeriesTransformation);

        // Calculate final value based on bin aggregates
        var finalBins = FinalBinValueCalculator.CalculateFinalBinValues(aggregatedBins, request.Anomaly == true);

        Console.WriteLine("AggregateBins completed in " + sw.Elapsed);
        sw.Restart();

        var dataPoints =
            finalBins
            .Select(
                x =>
                new ChartableDataPoint
                {
                    BinId = x.Identifier!.Id,
                    Label = x.Identifier.Label,
                    Value = x.Value,
                })
            .ToArray();

        return new DataSetPointsResult(dataPoints, aggregationApplied);
    }

    private readonly record struct DataSetPointsResult(ChartableDataPoint[] DataPoints, bool AggregationApplied);

    public class BuildDataSetResult
    {
        public UnitOfMeasure UnitOfMeasure { get; set; }

        public ChartableDataPoint[]? DataPoints { get; set; }
        public DataRecord[]? RawDataRecords { get; set; }

        /// <summary>
        /// Whether at least one output bin combined more than one raw data record. False means every
        /// <see cref="SeriesAggregationOptions"/> function would have produced the same <see cref="DataPoints"/>,
        /// e.g. because the source data is already at the requested bin granularity (yearly data binned by year).
        /// </summary>
        public bool AggregationApplied { get; set; }
    }
}
