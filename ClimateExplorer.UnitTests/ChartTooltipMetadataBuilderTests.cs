namespace ClimateExplorer.UnitTests;

using System;
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
public class ChartTooltipMetadataBuilderTests
{
    [TestMethod]
    public void Build_SingleLocationSeries_LabelIsLocationDataTypeUnit()
    {
        var series = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1990, 2019)); // 30 years

        var result = ChartTooltipMetadataBuilder.Build([series]);

        Assert.HasCount(1, result);
        Assert.AreEqual("Testville | Mean temperature | °C", result[0].Label);
    }

    [TestMethod]
    public void Build_UsesUnitOfMeasureRoundingForTheSeriesUnit()
    {
        var celsiusSeries = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1990, 2019), UnitOfMeasure.DegreesCelsius);
        var millimetreSeries = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1990, 2019), UnitOfMeasure.Millimetres);

        var result = ChartTooltipMetadataBuilder.Build([celsiusSeries, millimetreSeries]);

        Assert.HasCount(2, result);
        Assert.AreEqual(UnitOfMeasureRounding(UnitOfMeasure.DegreesCelsius), result[0].Rounding);
        Assert.AreEqual(UnitOfMeasureRounding(UnitOfMeasure.Millimetres), result[1].Rounding);
    }

    [TestMethod]
    public void Build_NonByYearGranularity_ReturnsNullAnomalyForThatSeries()
    {
        var series = CreateSeriesWithData(BinGranularities.ByYearAndMonth, YearRange(1950, 2019));

        var result = ChartTooltipMetadataBuilder.Build([series]);

        Assert.HasCount(1, result);
        Assert.IsNull(result[0].Anomaly);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result[0].Label));
    }

    [TestMethod]
    public void Build_ByYearWithFewerThanMinimumYears_ReturnsNullAnomalyForThatSeries()
    {
        var series = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1990, 2019)); // 30 years

        var result = ChartTooltipMetadataBuilder.Build([series]);

        Assert.HasCount(1, result);
        Assert.IsNull(result[0].Anomaly);
    }

    [TestMethod]
    public void Build_ByYearWithEnoughHistory_ReturnsPeriodsMatchingTheFullAvailableHistory()
    {
        // 70 consecutive years, well above the 60-year minimum.
        var series = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1950, 2019));

        var result = ChartTooltipMetadataBuilder.Build([series]);

        Assert.HasCount(1, result);
        var anomaly = result[0].Anomaly;
        Assert.IsNotNull(anomaly);
        Assert.AreEqual(1950, anomaly!.FullPeriod.FirstYear);
        Assert.AreEqual(2019, anomaly.FullPeriod.LastYear);
        Assert.AreEqual(0, anomaly.FullPeriod.MissingYears);
        Assert.AreEqual(10.0, anomaly.FullPeriod.Average);
        Assert.AreEqual(10.0, anomaly.Last30.Average);
        Assert.AreEqual(10.0, anomaly.Early.Average);
    }

    [TestMethod]
    public void Build_UsesFullSeriesHistoryNotTheZoomedProcessedDataSet()
    {
        // PreProcessedDataSet (full history) has enough years; ProcessedDataSet (the chart's currently
        // zoomed/filtered range) alone would not. The builder must use the former.
        var fullHistory = YearRange(1950, 2019);
        var zoomedRange = YearRange(2015, 2019);

        var chartSeries = new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications = [CreateSourceSeriesSpecification()],
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
            SeriesTransformation = SeriesTransformations.Identity,
            Value = SeriesValueOptions.Value,
        };

        var series = new SeriesWithData
        {
            ChartSeries = chartSeries,
            SourceDataSet = BuildDataSet(fullHistory),
            PreProcessedDataSet = BuildDataSet(fullHistory),
            ProcessedDataSet = BuildDataSet(zoomedRange),
        };

        var result = ChartTooltipMetadataBuilder.Build([series]);

        Assert.HasCount(1, result);
        Assert.IsNotNull(result[0].Anomaly);
        Assert.AreEqual(1950, result[0].Anomaly!.FullPeriod.FirstYear);
        Assert.AreEqual(2019, result[0].Anomaly!.FullPeriod.LastYear);
    }

    [TestMethod]
    public void Build_MultipleSeries_PreservesOrderAndPerSeriesEligibility()
    {
        var longSeries = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1950, 2019));
        var shortSeries = CreateSeriesWithData(BinGranularities.ByYear, YearRange(1990, 2019));
        var monthlySeries = CreateSeriesWithData(BinGranularities.ByYearAndMonth, YearRange(1950, 2019));

        var result = ChartTooltipMetadataBuilder.Build([longSeries, shortSeries, monthlySeries]);

        Assert.HasCount(3, result);
        Assert.IsNotNull(result[0].Anomaly);
        Assert.IsNull(result[1].Anomaly);
        Assert.IsNull(result[2].Anomaly);

        // Every series still gets a label, regardless of anomaly eligibility.
        Assert.IsTrue(result.All(x => !string.IsNullOrWhiteSpace(x.Label)));
    }

    private static List<BinnedRecord> YearRange(int firstYear, int lastYear)
    {
        return
            Enumerable.Range(firstYear, lastYear - firstYear + 1)
            .Select(year => new BinnedRecord($"y{year}", 10.0))
            .ToList();
    }

    private static SourceSeriesSpecification CreateSourceSeriesSpecification()
    {
        return new SourceSeriesSpecification
        {
            LocationId = Guid.NewGuid(),
            LocationName = "Testville",
            MeasurementDefinition = new MeasurementDefinitionViewModel
            {
                DataType = DataType.TempMean,
                DataAdjustment = DataAdjustment.Adjusted,
                DataResolution = DataResolution.Daily,
                UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
            },
        };
    }

    private static DataSet BuildDataSet(List<BinnedRecord> records, UnitOfMeasure unitOfMeasure = UnitOfMeasure.DegreesCelsius)
    {
        return new DataSet
        {
            MeasurementDefinition = new MeasurementDefinitionViewModel
            {
                DataType = DataType.TempMean,
                DataAdjustment = DataAdjustment.Adjusted,
                DataResolution = DataResolution.Daily,
                UnitOfMeasure = unitOfMeasure,
            },
            DataRecords = records,
        };
    }

    private static SeriesWithData CreateSeriesWithData(BinGranularities binGranularity, List<BinnedRecord> records, UnitOfMeasure unitOfMeasure = UnitOfMeasure.DegreesCelsius)
    {
        var chartSeries = new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications = [CreateSourceSeriesSpecification()],
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = binGranularity,
            DisplayStyle = SeriesDisplayStyle.Line,
            SeriesTransformation = SeriesTransformations.Identity,
            Value = SeriesValueOptions.Value,
        };

        var dataSet = BuildDataSet(records, unitOfMeasure);

        return new SeriesWithData
        {
            ChartSeries = chartSeries,
            SourceDataSet = dataSet,
            PreProcessedDataSet = dataSet,
        };
    }
}
