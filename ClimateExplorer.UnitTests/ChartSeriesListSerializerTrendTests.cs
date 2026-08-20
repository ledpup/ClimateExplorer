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
    public void ParseChartSeriesDefinitionList_RoundTrippedSingleTrend_IsPreserved()
    {
        var series = CreateChartSeries();
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Last30, TrendPredictionYears = 42 });

        var parsed = RoundTrip(series);

        Assert.HasCount(1, parsed.Trends);
        Assert.AreEqual(TrendWindow.Last30, parsed.Trends[0].TrendPeriod);
        Assert.AreEqual(42, parsed.Trends[0].TrendPredictionYears);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_TrendModuleOff_RoundTripsAsOff()
    {
        var parsed = RoundTrip(CreateChartSeries());

        Assert.IsEmpty(parsed.Trends);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_RoundTrippedThreeTrends_AllArePreservedInOrder()
    {
        var series = CreateChartSeries();
        series.Trends.Add(new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Linear, TrendPeriod = TrendWindow.Last30 });
        series.Trends.Add(new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Quadratic, TrendPeriod = TrendWindow.Full, TrendPredictionTargetYear = 2100 });
        series.Trends.Add(new ChartSeriesTrendRequest { RegressionType = TrendRegressionType.Cubic, TrendPeriod = TrendWindow.FirstHalf });

        var parsed = RoundTrip(series);

        Assert.HasCount(3, parsed.Trends);
        Assert.AreEqual(TrendRegressionType.Linear, parsed.Trends[0].RegressionType);
        Assert.AreEqual(TrendWindow.Last30, parsed.Trends[0].TrendPeriod);
        Assert.AreEqual(TrendRegressionType.Quadratic, parsed.Trends[1].RegressionType);
        Assert.AreEqual(TrendWindow.Full, parsed.Trends[1].TrendPeriod);
        Assert.AreEqual(2100, parsed.Trends[1].TrendPredictionTargetYear);
        Assert.AreEqual(TrendRegressionType.Cubic, parsed.Trends[2].RegressionType);
        Assert.AreEqual(TrendWindow.FirstHalf, parsed.Trends[2].TrendPeriod);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_RoundTrippedTrendPredictionTargetYear_IsPreserved()
    {
        var series = CreateChartSeries();
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPredictionTargetYear = 2100 });

        var parsed = RoundTrip(series);

        Assert.AreEqual(2100, parsed.Trends[0].TrendPredictionTargetYear);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_UrlWithoutTrendsSegment_StillParses()
    {
        // Links shared before this segment existed carry only the original 19 segments; they must
        // keep working, as a series with no trends.
        var legacy = TrimToSegmentCount(BuildUrlComponent(CreateChartSeries()), 19);

        var parsed = Parse(legacy);

        Assert.IsEmpty(parsed.Trends);
        Assert.AreEqual(LocationId, parsed.SourceSeriesSpecifications!.Single().LocationId);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_PredictionYearsOutOfRange_IsClampedIntoRange()
    {
        var series = CreateChartSeries();
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPredictionYears = 9999 });

        var parsed = Parse(BuildUrlComponent(series));

        Assert.AreEqual(TrendPredictionRange.Maximum, parsed.Trends[0].TrendPredictionYears);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_UnrecognisedTrendPeriod_FallsBackToNoSelection()
    {
        var series = CreateChartSeries();
        series.Trends.Add(new ChartSeriesTrendRequest());

        var component = BuildUrlComponent(series);
        var segments = component.Split(',');
        segments[19] = "Linear*SomethingElse*50*";

        var parsed = Parse(string.Join(',', segments));

        Assert.HasCount(1, parsed.Trends);
        Assert.IsNull(parsed.Trends[0].TrendPeriod);
    }

    [TestMethod]
    public void ParseChartSeriesDefinitionList_MoreThanThreeTrends_TruncatesToThree()
    {
        var series = CreateChartSeries();
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Full });
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.Last30 });
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.RecentDecade });
        series.Trends.Add(new ChartSeriesTrendRequest { TrendPeriod = TrendWindow.FirstHalf });

        var component = BuildUrlComponent(series);
        var segments = component.Split(',');
        Assert.AreEqual(4, segments[19].Split('|').Length, "test setup: the hand-built URL must actually carry four entries");

        var parsed = Parse(component);

        Assert.HasCount(ChartSeriesDefinition.MaxTrends, parsed.Trends);
        Assert.AreEqual(TrendWindow.Full, parsed.Trends[0].TrendPeriod);
        Assert.AreEqual(TrendWindow.Last30, parsed.Trends[1].TrendPeriod);
        Assert.AreEqual(TrendWindow.RecentDecade, parsed.Trends[2].TrendPeriod);
    }

    private static ChartSeriesDefinition RoundTrip(ChartSeriesDefinition series)
    {
        return Parse(BuildUrlComponent(series));
    }

    private static string BuildUrlComponent(ChartSeriesDefinition series)
    {
        return ChartSeriesListSerializer.BuildChartSeriesListUrlComponent([series]);
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
