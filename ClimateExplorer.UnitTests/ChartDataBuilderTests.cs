namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartDataBuilderTests
{
    private static readonly Guid DataSetId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid LocationId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    [TestMethod]
    public async Task EmptyStateReturnsNonRenderableResultWithoutCallingDataService()
    {
        var dataService = new Mock<IDataService>(MockBehavior.Strict);

        var result = await CreateBuilder(dataService).BuildAsync(new ChartState());

        Assert.IsFalse(result.HasRenderableData);
        Assert.IsEmpty(result.SeriesWithData);
        Assert.IsNull(result.ChartBins);
        Assert.IsEmpty(result.Messages);
        dataService.Verify(
            x => x.PostDataSet(
                It.IsAny<BinGranularities>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<SeriesValueOptions>(),
                It.IsAny<SeriesSpecification[]>(),
                It.IsAny<SeriesDerivationTypes>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<SeriesTransformations>(),
                It.IsAny<string?>(),
                It.IsAny<short?>(),
                It.IsAny<DataResolution?>()),
            Times.Never);
    }

    [TestMethod]
    public async Task BuildAppliesAnnualChangeSecondaryCalculation()
    {
        var dataSet = CreateYearDataSet([(2000, 1), (2001, 2), (2002, 4), (2003, 7)]);
        var dataService = CreateDataService(dataSet);

        var series = CreateSeries(secondaryCalculation: SecondaryCalculationOptions.AnnualChange);
        var state = new ChartState { ChartAllData = true, Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        Assert.IsTrue(result.HasRenderableData);

        // The first year becomes null (no prior year to difference against) and is therefore excluded
        // from the plotted range, which starts at the first year that has a value.
        var processedValues = result.SeriesWithData.Single().ProcessedDataSet!.DataRecords.Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new double?[] { 1, 2, 3 }, processedValues);

        var binYears = result.ChartBins!.Cast<YearBinIdentifier>().Select(x => x.Year).ToArray();
        CollectionAssert.AreEqual(new short[] { 2001, 2002, 2003 }, binYears);
    }

    [TestMethod]
    public async Task BuildAsync_AnnualChangeSeries_PreservesSourceMetadata()
    {
        var sourceMetadata = CreateSourceMetadata();
        var dataSet = CreateYearDataSet([(2000, 1), (2001, 2), (2002, 4), (2003, 7)], sourceMetadata: sourceMetadata);
        var dataService = CreateDataService(dataSet);
        var series = CreateSeries(secondaryCalculation: SecondaryCalculationOptions.AnnualChange);

        var result = await CreateBuilder(dataService).BuildAsync(new ChartState { ChartAllData = true, Series = [series] });

        var seriesWithData = result.SeriesWithData.Single();
        Assert.AreSame(sourceMetadata, seriesWithData.SourceDataSet.SourceMetadata);
        Assert.AreSame(sourceMetadata, seriesWithData.PreProcessedDataSet!.SourceMetadata);
        Assert.AreSame(sourceMetadata, seriesWithData.ProcessedDataSet!.SourceMetadata);
    }

    [TestMethod]
    public async Task BuildAppliesCumulativeSecondaryCalculation()
    {
        var dataSet = CreateYearDataSet([(2000, 1), (2001, 2), (2002, 4), (2003, 7)]);
        var dataService = CreateDataService(dataSet);

        var series = CreateSeries(secondaryCalculation: SecondaryCalculationOptions.Cumulative);
        var state = new ChartState { ChartAllData = true, Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        Assert.IsTrue(result.HasRenderableData);

        var processedValues = result.SeriesWithData.Single().ProcessedDataSet!.DataRecords.Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new double?[] { 1, 3, 7, 14 }, processedValues);

        var binYears = result.ChartBins!.Cast<YearBinIdentifier>().Select(x => x.Year).ToArray();
        CollectionAssert.AreEqual(new short[] { 2000, 2001, 2002, 2003 }, binYears);
    }

    [TestMethod]
    public async Task BuildAsync_CumulativeSeries_PreservesSourceMetadata()
    {
        var sourceMetadata = CreateSourceMetadata();
        var dataSet = CreateYearDataSet([(2000, 1), (2001, 2), (2002, 4), (2003, 7)], sourceMetadata: sourceMetadata);
        var dataService = CreateDataService(dataSet);
        var series = CreateSeries(secondaryCalculation: SecondaryCalculationOptions.Cumulative);

        var result = await CreateBuilder(dataService).BuildAsync(new ChartState { ChartAllData = true, Series = [series] });

        var seriesWithData = result.SeriesWithData.Single();
        Assert.AreSame(sourceMetadata, seriesWithData.SourceDataSet.SourceMetadata);
        Assert.AreSame(sourceMetadata, seriesWithData.PreProcessedDataSet!.SourceMetadata);
        Assert.AreSame(sourceMetadata, seriesWithData.ProcessedDataSet!.SourceMetadata);
    }

    [TestMethod]
    public async Task BuildAppliesCumulativeSecondaryCalculation_GapInSourceLeavesGapButResumesRunningTotal()
    {
        var dataSet = CreateYearDataSet([(2000, 1), (2001, null), (2002, 4)]);
        var dataService = CreateDataService(dataSet);

        var series = CreateSeries(secondaryCalculation: SecondaryCalculationOptions.Cumulative);
        var state = new ChartState { ChartAllData = true, Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        var processedValues = result.SeriesWithData.Single().ProcessedDataSet!.DataRecords.Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new double?[] { 1, null, 5 }, processedValues);
    }

    [TestMethod]
    public async Task ChartAllDataSelectsFullRangeWhileStartYearClampsIt()
    {
        var records = Enumerable.Range(2000, 11).Select(y => (year: y, value: (double?)y)).ToArray();
        var dataService = CreateDataService(CreateYearDataSet(records));
        var series = CreateSeries();

        var allDataResult = await CreateBuilder(dataService).BuildAsync(
            new ChartState { ChartAllData = true, Series = [series] });

        var clampedResult = await CreateBuilder(dataService).BuildAsync(
            new ChartState { ChartAllData = false, StartYear = "2005", Series = [series] });

        CollectionAssert.AreEqual(
            Enumerable.Range(2000, 11).Select(x => (short)x).ToArray(),
            allDataResult.ChartBins!.Cast<YearBinIdentifier>().Select(x => x.Year).ToArray());

        CollectionAssert.AreEqual(
            Enumerable.Range(2005, 6).Select(x => (short)x).ToArray(),
            clampedResult.ChartBins!.Cast<YearBinIdentifier>().Select(x => x.Year).ToArray());

        // Start years metadata reflects the underlying data, independent of the display range.
        CollectionAssert.AreEqual(new short[] { 2000 }, clampedResult.StartYears.ToArray());
    }

    [TestMethod]
    public async Task StartYearAtOrBeforeDataStartRendersFromActualDataStart()
    {
        // Regression test: a preset (or URL) can specify a StartYear that matches, or predates,
        // the earliest year actually present in the data — e.g. when a chart series with an
        // earlier start gets removed, leaving only a series whose data starts exactly on the
        // preset's StartYear. This must render from the data's actual start rather than leaving
        // the bin range unresolved.
        var records = Enumerable.Range(2000, 6).Select(y => (year: y, value: (double?)y)).ToArray();
        var dataService = CreateDataService(CreateYearDataSet(records));
        var series = CreateSeries();

        var result = await CreateBuilder(dataService).BuildAsync(
            new ChartState { ChartAllData = false, StartYear = "2000", Series = [series] });

        Assert.IsTrue(result.HasRenderableData);
        CollectionAssert.AreEqual(
            Enumerable.Range(2000, 6).Select(x => (short)x).ToArray(),
            result.ChartBins!.Cast<YearBinIdentifier>().Select(x => x.Year).ToArray());
    }

    [TestMethod]
    public async Task ModularGranularityProducesTwelveMonthBins()
    {
        var records = Enumerable.Range(1, 12)
            .Select(m => new BinnedRecord(new MonthOnlyBinIdentifier((short)m).Id, m))
            .ToList();
        var dataSet = new DataSet
        {
            GeographicalEntity = CreateLocation(),
            MeasurementDefinition = CreateMeasurementDefinition(),
            DataRecords = records,
        };
        var dataService = CreateDataService(dataSet);
        var series = CreateSeries(binGranularity: BinGranularities.ByMonthOnly);

        var result = await CreateBuilder(dataService).BuildAsync(new ChartState { Series = [series] });

        Assert.IsTrue(result.HasRenderableData);
        Assert.HasCount(12, result.ChartBins!);
        Assert.IsNull(result.ChartStartBin);
        Assert.IsNull(result.ChartEndBin);
    }

    [TestMethod]
    public async Task MovingAverageFallsBackToUnsmoothedDataAndWarnsWhenTooFewPointsRemain()
    {
        var dataSet = CreateYearDataSet([(2000, 1), (2001, 2), (2002, 3), (2003, 4), (2004, 5)]);
        var dataService = CreateDataService(dataSet);

        // A 20-year smoothing window over only five years removes too many points, so the builder
        // reverts to the unsmoothed data and surfaces a warning.
        var series = CreateSeries(smoothing: SeriesSmoothingOptions.MovingAverage, smoothingWindow: 20);
        var state = new ChartState { ChartAllData = true, Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        Assert.IsTrue(result.HasRenderableData);
        Assert.HasCount(1, result.Messages);
        StringAssert.Contains(result.Messages.Single().Message, "moving");
        Assert.AreEqual(ChartSeriesDataStatus.FallbackToUnsmoothedData, result.SeriesWithData.Single().DataStatus);

        var processedValues = result.SeriesWithData.Single().ProcessedDataSet!.DataRecords.Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new double?[] { 1, 2, 3, 4, 5 }, processedValues);
    }

    [TestMethod]
    public async Task BuildAsync_MovingAverageSeries_PreservesSourceMetadata()
    {
        var sourceMetadata = CreateSourceMetadata();
        var dataSet = CreateYearDataSet(
            Enumerable.Range(2000, 30).Select(year => (year, value: (double?)year)),
            sourceMetadata: sourceMetadata);
        var dataService = CreateDataService(dataSet);
        var series = CreateSeries(smoothing: SeriesSmoothingOptions.MovingAverage, smoothingWindow: 3);

        var result = await CreateBuilder(dataService).BuildAsync(new ChartState { ChartAllData = true, Series = [series] });

        var seriesWithData = result.SeriesWithData.Single();
        Assert.AreSame(sourceMetadata, seriesWithData.SourceDataSet.SourceMetadata);
        Assert.AreSame(sourceMetadata, seriesWithData.PreProcessedDataSet!.SourceMetadata);
        Assert.AreSame(sourceMetadata, seriesWithData.ProcessedDataSet!.SourceMetadata);
    }

    [TestMethod]
    public async Task BuildAsync_ProcessedDataSet_PreservesSourceMetadata()
    {
        var sourceMetadata = CreateSourceMetadata();
        var dataSet = CreateYearDataSet([(2000, 1), (2002, 3)], sourceMetadata: sourceMetadata);
        var dataService = CreateDataService(dataSet);
        var series = CreateSeries();

        var result = await CreateBuilder(dataService).BuildAsync(new ChartState { ChartAllData = true, Series = [series] });

        Assert.AreSame(sourceMetadata, result.SeriesWithData.Single().ProcessedDataSet!.SourceMetadata);
    }

    [TestMethod]
    public async Task BuildRendersAvailableSeriesWhenAnotherSeriesHasNoChartableDataAfterCompletenessFiltering()
    {
        var temperatureDataSet = CreateYearDataSet([(2000, 10), (2001, 11), (2002, 12)]);
        var precipitationDataSet = CreateYearDataSet(
            [],
            DataType.Precipitation,
            UnitOfMeasure.Millimetres,
            rawDataRecords: [new DataRecord(2000, 1, 1, 1)]);
        var dataService = CreateSequentialDataService(temperatureDataSet, precipitationDataSet);

        var temperatureSeries = CreateSeries();
        var precipitationSeries = CreateSeries(
            dataType: DataType.Precipitation,
            unitOfMeasure: UnitOfMeasure.Millimetres,
            aggregation: SeriesAggregationOptions.Sum);
        var state = new ChartState { ChartAllData = true, Series = [temperatureSeries, precipitationSeries] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        Assert.IsTrue(result.HasRenderableData);
        Assert.HasCount(1, result.SeriesWithData);
        Assert.AreSame(temperatureSeries, result.SeriesWithData.Single().ChartSeries);
        Assert.HasCount(1, result.NonRenderedSeriesWithData);
        Assert.AreSame(precipitationSeries, result.NonRenderedSeriesWithData.Single().ChartSeries);
        Assert.AreEqual(ChartSeriesDataStatus.NoChartableDataAfterCompletenessFiltering, result.NonRenderedSeriesWithData.Single().DataStatus);
        Assert.HasCount(1, result.Messages);
        StringAssert.Contains(result.Messages.Single().Message, "the completeness threshold removed all <b>precipitation</b> observations");
        Assert.HasCount(2, state.Series);

        var processedValues = result.SeriesWithData.Single().ProcessedDataSet!.DataRecords.Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new double?[] { 10, 11, 12 }, processedValues);
    }

    [TestMethod]
    public async Task BuildReturnsFullNoDataResultOnlyWhenNoRequestedSeriesHasChartableData()
    {
        var solarDataSet = CreateYearDataSet(
            [],
            DataType.SolarRadiation,
            UnitOfMeasure.MegajoulesPerSquareMetre,
            rawDataRecords: [new DataRecord(2000, 1, 1, 20)]);
        var dataService = CreateDataService(solarDataSet);

        var series = CreateSeries(
            dataType: DataType.SolarRadiation,
            unitOfMeasure: UnitOfMeasure.MegajoulesPerSquareMetre);
        var state = new ChartState { ChartAllData = true, Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        Assert.IsFalse(result.HasRenderableData);
        Assert.IsEmpty(result.SeriesWithData);
        Assert.HasCount(1, result.NonRenderedSeriesWithData);
        Assert.AreEqual(ChartSeriesDataStatus.NoChartableDataAfterCompletenessFiltering, result.NonRenderedSeriesWithData.Single().DataStatus);
        Assert.HasCount(1, result.Messages);
        StringAssert.Contains(result.Messages.Single().Message, "the completeness threshold removed all <b>solar radiation</b> observations");
    }

    [TestMethod]
    public async Task BuildRendersPreviouslySkippedSeriesWhenNextResultHasChartableData()
    {
        var emptyPrecipitationDataSet = CreateYearDataSet(
            [],
            DataType.Precipitation,
            UnitOfMeasure.Millimetres,
            rawDataRecords: [new DataRecord(2000, 1, 1, 1)]);
        var validPrecipitationDataSet = CreateYearDataSet(
            [(2000, 100), (2001, 110)],
            DataType.Precipitation,
            UnitOfMeasure.Millimetres);
        var dataService = CreateSequentialDataService(emptyPrecipitationDataSet, validPrecipitationDataSet);

        var series = CreateSeries(
            dataType: DataType.Precipitation,
            unitOfMeasure: UnitOfMeasure.Millimetres,
            aggregation: SeriesAggregationOptions.Sum);
        var state = new ChartState { ChartAllData = true, Series = [series] };
        var builder = CreateBuilder(dataService);

        var firstResult = await builder.BuildAsync(state);
        var secondResult = await builder.BuildAsync(state);

        Assert.IsFalse(firstResult.HasRenderableData);
        Assert.HasCount(1, firstResult.NonRenderedSeriesWithData);
        Assert.IsTrue(secondResult.HasRenderableData);
        Assert.HasCount(1, secondResult.SeriesWithData);
        Assert.IsEmpty(secondResult.NonRenderedSeriesWithData);
        Assert.AreEqual(ChartSeriesDataStatus.Rendered, secondResult.SeriesWithData.Single().DataStatus);
        Assert.HasCount(1, state.Series);
    }

    [TestMethod]
    public async Task BuildAsync_SwitchingToInsufficientDataAndBack_PreservesEachTrendsRequestedWindow()
    {
        // Regression test: a series with two full-period trends (linear and quadratic) that moves to
        // a location with too few years for a trend, then back to one with plenty, must keep both
        // trends on "Full" - not lose the second one to the exclude-already-shown-window fallback
        // that's meant for freshly-added trends with no preference of their own.
        var sufficientDataSet = CreateYearDataSet(
            Enumerable.Range(1950, 70).Select(year => (year, (double?)(10 + ((year - 1950) * 0.1)))));
        var insufficientDataSet = CreateYearDataSet(
            Enumerable.Range(1990, 30).Select(year => (year, (double?)(10 + ((year - 1990) * 0.1)))));
        var dataService = CreateSequentialDataService(sufficientDataSet, insufficientDataSet, sufficientDataSet);

        var series = CreateSeries();
        series.Trends =
        [
            new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Linear, TrendPeriod = TrendWindow.Full },
            new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Quadratic, TrendPeriod = TrendWindow.Full },
        ];
        var state = new ChartState { ChartAllData = true, Series = [series] };
        var builder = CreateBuilder(dataService);

        var atSufficientLocation = await builder.BuildAsync(state);
        var atInsufficientLocation = await builder.BuildAsync(state);
        var backAtSufficientLocation = await builder.BuildAsync(state);

        Assert.AreEqual(TrendWindow.Full, atSufficientLocation.SeriesWithData.Single().Trends[0].Projection?.Window);
        Assert.AreEqual(TrendWindow.Full, atSufficientLocation.SeriesWithData.Single().Trends[1].Projection?.Window);

        // Neither trend could be fitted at all here, so the requests must be left as they were.
        Assert.IsNotNull(atInsufficientLocation.SeriesWithData.Single().Trends[0].UnavailableReason);
        Assert.AreEqual(TrendWindow.Full, series.Trends[0].TrendPeriod);
        Assert.AreEqual(TrendWindow.Full, series.Trends[1].TrendPeriod);

        Assert.AreEqual(TrendWindow.Full, backAtSufficientLocation.SeriesWithData.Single().Trends[0].Projection?.Window);
        Assert.AreEqual(TrendWindow.Full, backAtSufficientLocation.SeriesWithData.Single().Trends[1].Projection?.Window);
    }

    [TestMethod]
    public async Task BuildAsync_TrendOnMovingAverageSmoothedSeries_ProjectsFromAfterTheTrueLastRawYear()
    {
        // A centred 10-year moving average can't fill a full window for the last 5 years of a
        // record, so the smoothed series plotted on the chart stops 5 years short of the raw data.
        // The trend's projection must resume after the true last raw year (2025), not after the
        // last smoothed point (2020) - otherwise it draws "predicted" points for years that are
        // already measured, just not smoothed.
        var records = Enumerable.Range(1900, 126).Select(y => (year: y, value: (double?)(10 + ((y - 1900) * 0.03)))).ToArray();
        var dataService = CreateDataService(CreateYearDataSet(records));

        var series = CreateSeries(smoothing: SeriesSmoothingOptions.MovingAverage, smoothingWindow: 10);
        series.Trends = [new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Linear, TrendPeriod = TrendWindow.Full, TrendPredictionYears = 5 }];
        var state = new ChartState { ChartAllData = true, Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        var seriesWithData = result.SeriesWithData.Single();
        var lastSmoothedYear = seriesWithData.PreProcessedDataSet!.DataRecords.Last(x => x.Value.HasValue).Year;

        Assert.AreEqual((short)2020, lastSmoothedYear); // sanity check: the smoothing did trim the tail
        Assert.AreEqual(2025, seriesWithData.Trends.Single().LastDataYear);
        Assert.AreEqual(2026, seriesWithData.Trends.Single().Projection!.FirstYear);
    }

    [TestMethod]
    public async Task BuildAsync_TrendOnMovingAverageSmoothedSeriesWithExplicitEndYear_DoesNotProjectPastTheRequestedEndYear()
    {
        // The true-last-raw-year anchor still respects an explicit end-year filter, the same way
        // the chart's own bin range does - it must not claim data exists past what the user asked
        // to see, even though the full raw record actually runs further.
        var records = Enumerable.Range(1900, 126).Select(y => (year: y, value: (double?)(10 + ((y - 1900) * 0.03)))).ToArray();
        var dataService = CreateDataService(CreateYearDataSet(records));

        var series = CreateSeries(smoothing: SeriesSmoothingOptions.MovingAverage, smoothingWindow: 10);
        series.Trends = [new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Linear, TrendPeriod = TrendWindow.Full, TrendPredictionYears = 5 }];
        var state = new ChartState { ChartAllData = false, EndYear = "2022", Series = [series] };

        var result = await CreateBuilder(dataService).BuildAsync(state);

        var trend = result.SeriesWithData.Single().Trends.Single();
        Assert.AreEqual(2022, trend.LastDataYear);
        Assert.AreEqual(2023, trend.Projection!.FirstYear);
    }

    private static ChartDataBuilder CreateBuilder(Mock<IDataService> dataService)
    {
        return new ChartDataBuilder(dataService.Object, NullLogger<ChartDataBuilder>.Instance);
    }

    private static Mock<IDataService> CreateDataService(DataSet dataSet)
    {
        var dataService = new Mock<IDataService>();
        dataService
            .Setup(x => x.PostDataSet(
                It.IsAny<BinGranularities>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<SeriesValueOptions>(),
                It.IsAny<SeriesSpecification[]>(),
                It.IsAny<SeriesDerivationTypes>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<SeriesTransformations>(),
                It.IsAny<string?>(),
                It.IsAny<short?>(),
                It.IsAny<DataResolution?>()))
            .ReturnsAsync(dataSet);
        return dataService;
    }

    private static Mock<IDataService> CreateSequentialDataService(params DataSet[] dataSets)
    {
        var queue = new Queue<DataSet>(dataSets);
        var dataService = new Mock<IDataService>();
        dataService
            .Setup(x => x.PostDataSet(
                It.IsAny<BinGranularities>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<SeriesValueOptions>(),
                It.IsAny<SeriesSpecification[]>(),
                It.IsAny<SeriesDerivationTypes>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<SeriesTransformations>(),
                It.IsAny<string?>(),
                It.IsAny<short?>(),
                It.IsAny<DataResolution?>()))
            .ReturnsAsync(() => queue.Dequeue());
        return dataService;
    }

    private static DataSet CreateYearDataSet(
        IEnumerable<(int Year, double? Value)> records,
        DataType dataType = DataType.TempMean,
        UnitOfMeasure unitOfMeasure = UnitOfMeasure.DegreesCelsius,
        DataRecord[]? rawDataRecords = null,
        List<DataSetMetadata>? sourceMetadata = null)
    {
        return new DataSet
        {
            GeographicalEntity = CreateLocation(),
            MeasurementDefinition = CreateMeasurementDefinition(dataType, unitOfMeasure),
            DataRecords = [.. records.Select(r => new BinnedRecord(new YearBinIdentifier((short)r.Year).Id, r.Value))],
            RawDataRecords = rawDataRecords,
            SourceMetadata = sourceMetadata,
        };
    }

    private static List<DataSetMetadata> CreateSourceMetadata()
    {
        return
        [
            new DataSetMetadata
            {
                DataSetDefinitionId = DataSetId,
                LocationId = LocationId,
                LocationName = "Testville",
                SourceCode = "Test",
                SourceName = "Test data set",
                Stations =
                [
                    new DataSetStationMetadata
                    {
                        StationId = "TEST001",
                        StationName = "Test Station",
                        StationStartDate = new DateOnly(2000, 1, 1),
                    },
                ],
            },
        ];
    }

    private static Location CreateLocation()
    {
        return new Location
        {
            Id = LocationId,
            Name = "Testville",
            Country = "Test Country",
            CountryCode = "TC",
            Coordinates = new Coordinates(1, 2),
        };
    }

    private static MeasurementDefinitionViewModel CreateMeasurementDefinition(
        DataType dataType = DataType.TempMean,
        UnitOfMeasure unitOfMeasure = UnitOfMeasure.DegreesCelsius)
    {
        return new MeasurementDefinitionViewModel
        {
            DataType = dataType,
            DataAdjustment = DataAdjustment.Adjusted,
            DataResolution = DataResolution.Monthly,
            UnitOfMeasure = unitOfMeasure,
        };
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition(
        DataType dataType = DataType.TempMean,
        UnitOfMeasure unitOfMeasure = UnitOfMeasure.DegreesCelsius)
    {
        return new DataSetDefinitionViewModel
        {
            Id = DataSetId,
            Name = "Test data set",
            ShortName = "Test",
            LocationIds = [LocationId],
            MeasurementDefinitions = [CreateMeasurementDefinition(dataType, unitOfMeasure)],
        };
    }

    private static ChartSeriesDefinition CreateSeries(
        BinGranularities binGranularity = BinGranularities.ByYear,
        SecondaryCalculationOptions secondaryCalculation = SecondaryCalculationOptions.None,
        SeriesSmoothingOptions smoothing = SeriesSmoothingOptions.None,
        int smoothingWindow = 20,
        DataType dataType = DataType.TempMean,
        UnitOfMeasure unitOfMeasure = UnitOfMeasure.DegreesCelsius,
        SeriesAggregationOptions aggregation = SeriesAggregationOptions.Mean)
    {
        var dataSetDefinition = CreateDataSetDefinition(dataType, unitOfMeasure);

        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications =
            [
                new SourceSeriesSpecification
                {
                    LocationId = LocationId,
                    LocationName = "Testville",
                    DataSetDefinition = dataSetDefinition,
                    MeasurementDefinition = dataSetDefinition.MeasurementDefinitions!.Single(),
                },
            ],
            Aggregation = aggregation,
            BinGranularity = binGranularity,
            DisplayStyle = SeriesDisplayStyle.Line,
            SecondaryCalculation = secondaryCalculation,
            Smoothing = smoothing,
            SmoothingWindow = smoothingWindow,
            Value = SeriesValueOptions.Value,
            Year = null,
            SeriesTransformation = SeriesTransformations.Identity,
            MinimumDataResolution = DataResolution.Monthly,
        };
    }
}
