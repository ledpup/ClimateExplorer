namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Covers the collection-equality loop <c>ChartSeriesDefinition</c>'s two comparers grew for
/// <see cref="ChartSeriesDefinition.Trends"/> - genuinely new logic (the previous single-trend
/// scalar fields had no comparer test at all), not a gap reopened from an earlier plan.
/// </summary>
[TestClass]
public class ChartSeriesDefinitionEqualityTests
{
    [TestMethod]
    public void BaseComparer_DifferByTrendsCount_AreNotEqual()
    {
        var sss = CreateSourceSeriesSpecifications();
        var a = CreateSeries(sss);
        var b = CreateSeries(sss, new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Full });

        Assert.IsFalse(ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked.BaseComparer(a, b));
    }

    [TestMethod]
    public void BaseComparer_SameCountButOneEntryDiffersByPeriod_AreNotEqual()
    {
        var sss = CreateSourceSeriesSpecifications();
        var a = CreateSeries(sss, new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Full });
        var b = CreateSeries(sss, new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Last30 });

        Assert.IsFalse(ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked.BaseComparer(a, b));
    }

    [TestMethod]
    public void BaseComparer_SameCountButOneEntryDiffersByRegressionType_AreNotEqual()
    {
        var sss = CreateSourceSeriesSpecifications();
        var a = CreateSeries(sss, new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Linear });
        var b = CreateSeries(sss, new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Quadratic });

        Assert.IsFalse(ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked.BaseComparer(a, b));
    }

    [TestMethod]
    public void BaseComparer_IdenticalTrends_AreEqual()
    {
        var sss = CreateSourceSeriesSpecifications();
        var a = CreateSeries(sss, CreateTrend());
        var b = CreateSeries(sss, CreateTrend());

        Assert.IsTrue(ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked.BaseComparer(a, b));
    }

    [TestMethod]
    public void ChartSeriesDefinitionComparer_DifferOnlyByTrends_AreNotEqual()
    {
        var sss = CreateSourceSeriesSpecifications();
        var a = CreateSeries(sss);
        var b = CreateSeries(sss, new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Full });
        var comparer = new ChartSeriesDefinition.ChartSeriesDefinitionComparer();

        Assert.IsFalse(comparer.Equals(a, b));
    }

    [TestMethod]
    public void GetHashCode_DifferOnlyByTrends_ProducesDifferentHashes()
    {
        var sss = CreateSourceSeriesSpecifications();
        var a = CreateSeries(sss);
        var b = CreateSeries(sss, new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Full });
        var comparer = new ChartSeriesDefinition.ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked();

        Assert.AreNotEqual(comparer.GetHashCode(a), comparer.GetHashCode(b));
    }

    private static ChartSeriesTrendRequest CreateTrend()
    {
        return new ChartSeriesTrendRequest
        {
            RegressionType = TrendRegressionType.Quadratic,
            TrendPeriod = TrendWindow.Full,
            TrendPredictionYears = 30,
            TrendPredictionTargetYear = 2100,
        };
    }

    private static ChartSeriesDefinition CreateSeries(SourceSeriesSpecification[] sss, params ChartSeriesTrendRequest[] trends)
    {
        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications = sss,
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
            Smoothing = SeriesSmoothingOptions.None,
            SmoothingWindow = 20,
            Value = SeriesValueOptions.Value,
            SeriesTransformation = SeriesTransformations.Identity,
            Trends = [.. trends],
        };
    }

    private static SourceSeriesSpecification[] CreateSourceSeriesSpecifications()
    {
        var dsd = new DataSetDefinitionViewModel { Id = Guid.NewGuid(), Name = "Test data set", ShortName = "Temp" };
        var md = new MeasurementDefinitionViewModel
        {
            DataType = DataType.TempMean,
            DataAdjustment = DataAdjustment.Adjusted,
            DataResolution = DataResolution.Monthly,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
        };

        return
        [
            new SourceSeriesSpecification
            {
                DataSetDefinition = dsd,
                MeasurementDefinition = md,
                LocationId = Guid.NewGuid(),
                LocationName = "Testville",
            },
        ];
    }
}
