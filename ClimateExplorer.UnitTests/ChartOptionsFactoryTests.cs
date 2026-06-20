namespace ClimateExplorer.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartOptionsFactoryTests
{
    [TestMethod]
    public void CalculateAxisMinMaxMergesSeriesSharingAnAxis()
    {
        var seriesWithData = new List<SeriesWithData>
        {
            CreateSeriesWithData(UnitOfMeasure.DegreesCelsius, [1, 5]),
            CreateSeriesWithData(UnitOfMeasure.DegreesCelsius, [3, 9]),
        };

        var (axisMinMax, axisHasBarSeries) = ChartOptionsFactory.CalculateAxisMinMax(seriesWithData);

        Assert.HasCount(1, axisMinMax);
        var (min, max) = axisMinMax.Single().Value;
        Assert.AreEqual(1, min);
        Assert.AreEqual(9, max);
        Assert.IsEmpty(axisHasBarSeries);
    }

    [TestMethod]
    public void CalculateAxisMinMaxKeepsDifferentUnitsOnSeparateAxes()
    {
        var seriesWithData = new List<SeriesWithData>
        {
            CreateSeriesWithData(UnitOfMeasure.DegreesCelsius, [1, 5]),
            CreateSeriesWithData(UnitOfMeasure.Millimetres, [100, 400]),
        };

        var (axisMinMax, _) = ChartOptionsFactory.CalculateAxisMinMax(seriesWithData);

        Assert.HasCount(2, axisMinMax);
        Assert.IsTrue(axisMinMax.Values.Any(v => v is { Min: 1, Max: 5 }));
        Assert.IsTrue(axisMinMax.Values.Any(v => v is { Min: 100, Max: 400 }));
    }

    [TestMethod]
    public void CalculateAxisMinMaxFlagsAxesThatHaveBarSeries()
    {
        var seriesWithData = new List<SeriesWithData>
        {
            CreateSeriesWithData(UnitOfMeasure.Millimetres, [100, 400], SeriesDisplayStyle.Bar),
        };

        var (axisMinMax, axisHasBarSeries) = ChartOptionsFactory.CalculateAxisMinMax(seriesWithData);

        Assert.HasCount(1, axisHasBarSeries);
        Assert.Contains(axisMinMax.Single().Key, axisHasBarSeries);
    }

    [TestMethod]
    public void CalculateAxisMinMaxIgnoresSeriesWithNoValues()
    {
        var seriesWithData = new List<SeriesWithData>
        {
            CreateSeriesWithData(UnitOfMeasure.DegreesCelsius, [null, null]),
        };

        var (axisMinMax, _) = ChartOptionsFactory.CalculateAxisMinMax(seriesWithData);

        Assert.IsEmpty(axisMinMax);
    }

    private static SeriesWithData CreateSeriesWithData(
        UnitOfMeasure unitOfMeasure,
        IEnumerable<double?> values,
        SeriesDisplayStyle displayStyle = SeriesDisplayStyle.Line)
    {
        var measurementDefinition = new MeasurementDefinitionViewModel
        {
            DataType = DataType.TempMean,
            DataAdjustment = DataAdjustment.Adjusted,
            DataResolution = DataResolution.Monthly,
            UnitOfMeasure = unitOfMeasure,
        };

        var chartSeries = new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications =
            [
                new SourceSeriesSpecification
                {
                    LocationId = System.Guid.NewGuid(),
                    LocationName = "Testville",
                    MeasurementDefinition = measurementDefinition,
                },
            ],
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = displayStyle,
            SeriesTransformation = SeriesTransformations.Identity,
            Value = SeriesValueOptions.Value,
        };

        var dataSet = new DataSet
        {
            MeasurementDefinition = measurementDefinition,
            DataRecords = [.. values.Select((v, i) => new BinnedRecord($"y{2000 + i}", v))],
        };

        return new SeriesWithData
        {
            ChartSeries = chartSeries,
            SourceDataSet = dataSet,
            PreProcessedDataSet = dataSet,
        };
    }
}
