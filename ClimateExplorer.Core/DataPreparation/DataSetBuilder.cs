namespace ClimateExplorer.Core.DataPreparation;

using ClimateExplorer.Core.DataPreparation.Model;
using ClimateExplorer.Core.Model;
using System.Diagnostics;
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

        if (series.DataRecords != null && series.DataRecords.All(x => x.Value == null))
        {
            throw new Exception("All data records in the series are null. Check the raw input file");
        }

        Console.WriteLine("GetSeriesDataRecordsForRequest completed in " + sw.Elapsed);

        if (request.MinimumDataResolution != null && series.DataResolution < request.MinimumDataResolution)
        {
            throw new Exception($"The data resolution of this series is {series.DataResolution}. A minimum data resolution thresold of {request.MinimumDataResolution} is required for this type of aggregation.");
        }

        // Run the rest of the pipeline (this is a separate method for testability)
        var dataPoints = BuildDataSetFromDataRecords(series.DataRecords!, series.DataResolution, request);

        if (dataPoints.All(x => x.Value == null))
        {
            // There is insufficient data for aggregation. Will return an empty set.
            dataPoints = [];
        }

        return
            new BuildDataSetResult
            {
                DataPoints = dataPoints,
                RawDataRecords = request.IncludeRawDataRecords ? series.DataRecords : null,
                UnitOfMeasure = series.UnitOfMeasure,
            };
    }

    public ChartableDataPoint[] BuildDataSetFromDataRecords(DataRecord[] dataRecords, DataResolution dataResolution, PostDataSetsRequestBody request)
    {
        Stopwatch sw = new ();
        sw.Start();

        // Apply specified transformation (if any) to each data point in the series
        var transformedDataRecords = SeriesTransformer.ApplySeriesTransformation(dataRecords, request.SeriesTransformation);

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
            return ConvertDataRecordsToChartableDataPoints(filteredDataRecords);
        }

        // Assign to Bins, Buckets and Cups
        var rawBins = Binner.ApplyBinningRules(filteredDataRecords, request.BinningRule, request.CupSize, dataResolution);

        Console.WriteLine("ApplyBinningRules completed in " + sw.Elapsed);
        sw.Restart();

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
        var finalBins = FinalBinValueCalculator.CalculateFinalBinValues(aggregatedBins, request.Anomaly);

        Console.WriteLine("AggregateBins completed in " + sw.Elapsed);
        sw.Restart();

        return
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
                Value = x.Item2 == null ? null : x.Item2.Value,
            })
        .ToArray();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public class BuildDataSetResult
    {
        public UnitOfMeasure UnitOfMeasure { get; set; }

        public ChartableDataPoint[]? DataPoints { get; set; }
        public DataRecord[]? RawDataRecords { get; set; }
    }
}
