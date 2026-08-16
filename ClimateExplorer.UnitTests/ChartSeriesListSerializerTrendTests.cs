namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartSeriesListSerializerTrendTests
{
    private static readonly Guid LocationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DataSetDefinitionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [TestMethod]
    public void ParseChartSeriesDefinitionList_RoundTrippedTrendSettings_ArePreserved()
    {
        var series = CreateChartSeries();
        series.ShowTrend = true;
        series.TrendPeriod = TrendWindow.Recent;
        series.TrendPredictionYears = 42;

        var parsed = RoundTrip(series);

        Assert.IsTrue(parsed.ShowTrend);
        Assert.AreEqual(TrendWindow.Recent, parsed.TrendPeriod);
        Assert.AreEqual(42, parsed.TrendPredictionYears);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_TrendModuleOff_RoundTripsAsOff()
    {
        var parsed = RoundTrip(CreateChartSeries());

        Assert.IsFalse(parsed.ShowTrend);
        Assert.IsNull(parsed.TrendPeriod);
        Assert.AreEqual(TrendPredictionRange.Default, parsed.TrendPredictionYears);
        Assert.IsNull(parsed.TrendPredictionTargetYear);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_RoundTrippedTrendPredictionTargetYear_IsPreserved()
    {
        var series = CreateChartSeries();
        series.ShowTrend = true;
        series.TrendPredictionTargetYear = 2100;

        var parsed = RoundTrip(series);

        Assert.AreEqual(2100, parsed.TrendPredictionTargetYear);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_UrlWithoutTrendPredictionTargetYearSegment_DefaultsToNull()
    {
        // Links shared before this field existed carry only the original 23 segments (up to and
        // including RegressionType); they must keep working as "no fixed target year".
        var withoutTargetYear = TrimToSegmentCount(BuildUrlComponent(CreateChartSeries()), 23);

        var parsed = Parse(withoutTargetYear);

        Assert.IsNull(parsed.TrendPredictionTargetYear);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_UrlWithoutTrendSegments_StillParses()
    {
        // Links shared before the trend module existed carry only the original 19 segments; they
        // must keep working, as a series with the module switched off.
        var legacy = TrimToLegacySegmentCount(BuildUrlComponent(CreateChartSeries()));

        var parsed = Parse(legacy);

        Assert.IsFalse(parsed.ShowTrend);
        Assert.IsNull(parsed.TrendPeriod);
        Assert.AreEqual(TrendPredictionRange.Default, parsed.TrendPredictionYears);
        Assert.AreEqual(LocationId, parsed.SourceSeriesSpecifications!.Single().LocationId);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_PredictionYearsOutOfRange_IsClampedIntoRange()
    {
        var series = CreateChartSeries();
        series.ShowTrend = true;
        series.TrendPredictionYears = 9999;

        var parsed = Parse(BuildUrlComponent(series));

        Assert.AreEqual(TrendPredictionRange.Maximum, parsed.TrendPredictionYears);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_UnrecognisedTrendPeriod_FallsBackToNoSelection()
    {
        var component = BuildUrlComponent(CreateChartSeries());
        var segments = component.Split(',');
        segments[19] = "True";
        segments[20] = "SomethingElse";

        var parsed = Parse(string.Join(',', segments));

        Assert.IsTrue(parsed.ShowTrend);
        Assert.IsNull(parsed.TrendPeriod);
    }

    private static ChartSeriesDefinition RoundTrip(ChartSeriesDefinition series)
    {
        return Parse(BuildUrlComponent(series));
    }

    private static string BuildUrlComponent(ChartSeriesDefinition series)
    {
        return ChartSeriesListSerializer.BuildChartSeriesListUrlComponent([series]);
    }

    private static string TrimToLegacySegmentCount(string component)
    {
        return TrimToSegmentCount(component, 19);
    }

    private static string TrimToSegmentCount(string component, int count)
    {
        return string.Join(',', component.Split(',').Take(count));
    }

    private static ChartSeriesDefinition Parse(string component)
    {
        var dataSetDefinition = CreateDataSetDefinition();

        return ChartSeriesListSerializer
            .ParseChartSeriesDefinitionList(
                NullLogger.Instance,
                component,
                [dataSetDefinition],
                new Dictionary<Guid, Location> { [LocationId] = CreateLocation() },
                [])
            .Single();
    }

    private static Location CreateLocation()
    {
        return new Location
        {
            Id = LocationId,
            Name = "Testville",
            CountryCode = "AU",
            Coordinates = new Coordinates(1, 2),
        };
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition()
    {
        return new DataSetDefinitionViewModel
        {
            Id = DataSetDefinitionId,
            Name = "Test data set",
            ShortName = "Temp",
            LocationIds = [LocationId],
            MeasurementDefinitions =
            [
                new MeasurementDefinitionViewModel
                {
                    DataType = DataType.TempMean,
                    DataAdjustment = DataAdjustment.Adjusted,
                    DataResolution = DataResolution.Monthly,
                    UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                },
            ],
        };
    }

    private static ChartSeriesDefinition CreateChartSeries()
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
            RequestedColour = Colours.Blue,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
            IsLocked = false,
            SecondaryCalculation = SecondaryCalculationOptions.None,
            ShowTrendline = false,
            Smoothing = SeriesSmoothingOptions.None,
            SmoothingWindow = 20,
            Value = SeriesValueOptions.Value,
            Year = null,
            IsExpanded = false,
            SeriesTransformation = SeriesTransformations.Identity,
            CustomTransformation = null,
            GroupingThreshold = .8f,
            MinimumDataResolution = DataResolution.Monthly,
        };
    }
}
