namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartDisplayBuilderTests
{
    private static readonly Guid DataSetId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid LocationId = Guid.Parse("40000000-0000-0000-0000-000000000002");

    [TestMethod]
    public async Task BuildDatasets_ShiftedTemperatureInput_LevelsMoveAndChangesDoNot()
    {
        // A mean temperature series with a fitted trend (levels: values, projections, axis range,
        // tooltip averages) and an annual-change series calculated in the client (changes).
        var original = await RecordDisplayMeasures(shift: 0);
        var shifted = await RecordDisplayMeasures(shift: 10);

        OffsetShift.AssertLevelsMoveAndChangesDoNot(original, shifted, 10, minimumLevels: 100, minimumChanges: 50);
    }

    [TestMethod]
    public async Task BuildDatasets_UsCustomary_AxisLabelLegendTooltipAndValuesShareUnit()
    {
        var result = await BuildChart([CreateSeries()], [CreateDataSet(Constant(10))]);

        var (datasets, tooltip) = ChartDisplayBuilder.BuildDatasets(result.SeriesWithData, result.ChartBins!, false, UnitFormatter.UsCustomary);
        var options = ChartOptionsFactory.Build(CreateOptionsRequest(result, UnitFormatter.UsCustomary));

        Assert.AreEqual(50, datasets[0].Data[0]!.Value, 1e-9);
        StringAssert.EndsWith(datasets[0].Label, "| °F");
        StringAssert.EndsWith(tooltip[0].Label, "| °F");
        Assert.AreEqual(1, tooltip[0].Rounding);
        Assert.AreEqual(50, tooltip[0].Anomaly!.FullPeriod.Average, 1e-9);
        Assert.AreEqual("Degrees Fahrenheit (°F)", options.Axes.Single().Label);
    }

    [TestMethod]
    public async Task BuildDatasets_Metric_UnchangedFromBuildResult()
    {
        var result = await BuildChart([CreateSeries()], [CreateDataSet(Constant(10))]);

        var (datasets, tooltip) = ChartDisplayBuilder.BuildDatasets(result.SeriesWithData, result.ChartBins!, false, UnitFormatter.Metric);
        var options = ChartOptionsFactory.Build(CreateOptionsRequest(result, UnitFormatter.Metric));

        Assert.AreEqual(10, datasets[0].Data[0]!.Value);
        StringAssert.EndsWith(datasets[0].Label, "| °C");
        StringAssert.EndsWith(tooltip[0].Label, "| °C");
        Assert.AreEqual("Degrees celsius (°C)", options.Axes.Single().Label);
        Assert.AreEqual("degreesCelsius", datasets[0].YAxisId);
    }

    [TestMethod]
    public async Task BuildDatasets_UsCustomary_DoesNotModifyBuildResult()
    {
        var result = await BuildChart([CreateSeries()], [CreateDataSet(Constant(10))]);

        _ = ChartDisplayBuilder.BuildDatasets(result.SeriesWithData, result.ChartBins!, false, UnitFormatter.UsCustomary);
        _ = ChartOptionsFactory.Build(CreateOptionsRequest(result, UnitFormatter.UsCustomary));

        var series = result.SeriesWithData.Single();
        Assert.IsTrue(series.ProcessedDataSet!.DataRecords.All(x => x.Value == 10));
        Assert.IsTrue(series.PreProcessedDataSet!.DataRecords.All(x => x.Value == 10));
        Assert.AreEqual(UnitOfMeasure.DegreesCelsius, series.ProcessedDataSet.MeasurementDefinition!.UnitOfMeasure);
        Assert.AreEqual(UnitOfMeasure.DegreesCelsius, series.ChartSeries!.SourceSeriesSpecifications!.Single().MeasurementDefinition!.UnitOfMeasure);
    }

    [TestMethod]
    public async Task BuildDatasets_UsCustomaryAnomalySeries_ScaledWithoutOffset()
    {
        var series = CreateSeries();
        series.Value = SeriesValueOptions.Anomaly;

        var result = await BuildChart([series], [CreateDataSet(Constant(1))]);
        var (datasets, _) = ChartDisplayBuilder.BuildDatasets(result.SeriesWithData, result.ChartBins!, false, UnitFormatter.UsCustomary);
        var options = ChartOptionsFactory.Build(CreateOptionsRequest(result, UnitFormatter.UsCustomary));

        Assert.AreEqual(1.8, datasets[0].Data[0]!.Value, 1e-9);
        Assert.AreEqual("Degrees Fahrenheit (°F) - Anomaly", options.Axes.Single().Label);
    }

    [TestMethod]
    public async Task BuildDatasets_UsCustomarySumOfCelsius_LeftInMetricOnItsOwnAxis()
    {
        var sum = CreateSeries(aggregation: SeriesAggregationOptions.Sum);

        var result = await BuildChart([CreateSeries(), sum], [CreateDataSet(Constant(10)), CreateDataSet(Constant(10))]);
        var (datasets, _) = ChartDisplayBuilder.BuildDatasets(result.SeriesWithData, result.ChartBins!, false, UnitFormatter.UsCustomary);
        var options = ChartOptionsFactory.Build(CreateOptionsRequest(result, UnitFormatter.UsCustomary));

        Assert.AreEqual(50, datasets[0].Data[0]!.Value, 1e-9);
        Assert.AreEqual(10, datasets[1].Data[0]!.Value);
        StringAssert.EndsWith(datasets[1].Label, "| °C");
        Assert.AreNotEqual(datasets[0].YAxisId, datasets[1].YAxisId);
        Assert.HasCount(2, options.Axes);
    }

    private static async Task<List<Measure>> RecordDisplayMeasures(double shift)
    {
        var mean = CreateSeries();
        mean.Trends = [new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Linear, TrendPeriod = TrendWindow.Full }];
        var annualChange = CreateSeries(temporalCalculation: TemporalCalculationOptions.AnnualChange);

        // A warming trend with year-to-year variation, so averages, slopes and differences are all non-trivial.
        double Temperature(int year) => 12 + ((year - 1950) * 0.03) + Math.Sin(year * 1.7) + shift;

        var result = await BuildChart([mean, annualChange], [CreateDataSet(Temperature), CreateDataSet(Temperature)]);

        var units = new RecordingUnitFormatter(UnitFormatter.UsCustomary);
        _ = ChartDisplayBuilder.BuildDatasets(result.SeriesWithData, result.ChartBins!, false, units);
        _ = ChartOptionsFactory.Build(CreateOptionsRequest(result, units));

        return units.Measures;
    }

    private static ChartOptionsRequest CreateOptionsRequest(ChartDataBuildResult result, IUnitFormatter units)
    {
        return new ChartOptionsRequest
        {
            Title = "Test",
            Subtitle = "Test",
            BinGranularity = BinGranularities.ByYear,
            IsMobileDevice = false,
            SeriesWithData = result.SeriesWithData,
            Series = [.. result.SeriesWithData.Select(x => x.ChartSeries!)],
            AxesScaleToZero = new Dictionary<string, bool>(),
            Units = units,
        };
    }

    private static Func<int, double> Constant(double value) => _ => value;

    private static async Task<ChartDataBuildResult> BuildChart(IReadOnlyList<ChartSeriesDefinition> series, IReadOnlyList<DataSet> dataSets)
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
            .ReturnsAsync(queue.Dequeue);

        var builder = new ChartDataBuilder(dataService.Object, NullLogger<ChartDataBuilder>.Instance);

        return await builder.BuildAsync(new ChartState { ChartAllData = true, Series = [.. series] });
    }

    private static DataSet CreateDataSet(Func<int, double> valueForYear)
    {
        return new DataSet
        {
            GeographicalEntity = new Location { Id = LocationId, Name = "Testville", CountryCode = "TC", Coordinates = new Coordinates(1, 2) },
            MeasurementDefinition = CreateMeasurementDefinition(),
            DataRecords = [.. Enumerable.Range(1950, 70).Select(year => new BinnedRecord(new YearBinIdentifier((short)year).Id, valueForYear(year)))],
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

    private static ChartSeriesDefinition CreateSeries(
        TemporalCalculationOptions temporalCalculation = TemporalCalculationOptions.None,
        SeriesAggregationOptions aggregation = SeriesAggregationOptions.Mean)
    {
        var measurementDefinition = CreateMeasurementDefinition();

        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications =
            [
                new SourceSeriesSpecification
                {
                    LocationId = LocationId,
                    LocationName = "Testville",
                    DataSetDefinition = new DataSetDefinitionViewModel
                    {
                        Id = DataSetId,
                        Name = "Test data set",
                        ShortName = "Test",
                        LocationIds = [LocationId],
                        MeasurementDefinitions = [measurementDefinition],
                    },
                    MeasurementDefinition = measurementDefinition,
                },
            ],
            Aggregation = aggregation,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
            TemporalCalculation = temporalCalculation,
            Value = SeriesValueOptions.Value,
            SeriesTransformation = SeriesTransformations.Identity,
            MinimumDataResolution = DataResolution.Monthly,
        };
    }
}
