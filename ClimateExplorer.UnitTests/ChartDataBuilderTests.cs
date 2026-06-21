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

        var processedValues = result.SeriesWithData.Single().ProcessedDataSet!.DataRecords.Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new double?[] { 1, 2, 3, 4, 5 }, processedValues);
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

    private static DataSet CreateYearDataSet(IEnumerable<(int Year, double? Value)> records)
    {
        return new DataSet
        {
            GeographicalEntity = CreateLocation(),
            MeasurementDefinition = CreateMeasurementDefinition(),
            DataRecords = [.. records.Select(r => new BinnedRecord(new YearBinIdentifier((short)r.Year).Id, r.Value))],
        };
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

    private static MeasurementDefinitionViewModel CreateMeasurementDefinition()
    {
        return new MeasurementDefinitionViewModel
        {
            DataType = DataType.TempMean,
            DataAdjustment = DataAdjustment.Adjusted,
            DataResolution = DataResolution.Monthly,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
        };
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition()
    {
        return new DataSetDefinitionViewModel
        {
            Id = DataSetId,
            Name = "Test data set",
            ShortName = "Test",
            LocationIds = [LocationId],
            MeasurementDefinitions = [CreateMeasurementDefinition()],
        };
    }

    private static ChartSeriesDefinition CreateSeries(
        BinGranularities binGranularity = BinGranularities.ByYear,
        SecondaryCalculationOptions secondaryCalculation = SecondaryCalculationOptions.None,
        SeriesSmoothingOptions smoothing = SeriesSmoothingOptions.None,
        int smoothingWindow = 20)
    {
        var dataSetDefinition = CreateDataSetDefinition();

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
            Aggregation = SeriesAggregationOptions.Mean,
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
