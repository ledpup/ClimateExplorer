namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartStateUrlServiceTests
{
    private static readonly Guid LocationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DataSetDefinitionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [TestMethod]
    public void ParseReturnsMissingWhenChartStateIsAbsent()
    {
        var service = CreateService();
        var result = service.Parse(new Uri("https://example.test/location"), CreateContext());

        Assert.AreEqual(ChartUrlStateKind.Missing, result.Kind);
        Assert.IsNull(result.State);
    }

    [TestMethod]
    public void ParseReturnsExplicitEmptyWhenChartAllDataExistsWithoutSeries()
    {
        var service = CreateService();
        var result = service.Parse(new Uri("https://example.test/location?chartAllData=true&groupingDays=21&groupingThreshold=65"), CreateContext());

        Assert.AreEqual(ChartUrlStateKind.ExplicitEmpty, result.Kind);
        Assert.IsNotNull(result.State);
        Assert.IsTrue(result.State!.ChartAllData);
        Assert.AreEqual(21, result.State.GroupingDays);
        Assert.AreEqual("65", result.State.GroupingThresholdText);
        Assert.IsEmpty(result.State.Series);
    }

    [TestMethod]
    public void ParseReturnsValidStateForSerializedChartSeries()
    {
        var service = CreateService();
        var context = CreateContext();
        var url = service.BuildRelativeUrl(
            "location",
            new ChartState
            {
                ChartAllData = true,
                StartYear = "1910",
                EndYear = "2020",
                GroupingDays = 28,
                GroupingThresholdText = "85",
                UserOverrideAggregationSettings = true,
                AxesScaleToZero = new Dictionary<string, bool> { ["temperature"] = true },
                Series = [CreateChartSeries(context.DataSetDefinitions.Single(), context.Locations![LocationId])],
            });

        var result = service.Parse(new Uri("https://example.test/" + url), context);

        Assert.AreEqual(ChartUrlStateKind.Valid, result.Kind);
        Assert.IsNotNull(result.State);
        Assert.IsTrue(result.State!.ChartAllData);
        Assert.AreEqual("1910", result.State.StartYear);
        Assert.AreEqual("2020", result.State.EndYear);
        Assert.AreEqual(28, result.State.GroupingDays);
        Assert.AreEqual("85", result.State.GroupingThresholdText);
        Assert.IsTrue(result.State.UserOverrideAggregationSettings);
        Assert.IsTrue(result.State.AxesScaleToZero["temperature"]);
        Assert.HasCount(1, result.State.Series);
        Assert.AreEqual(BinGranularities.ByYear, result.State.Series[0].BinGranularity);
        Assert.AreEqual(LocationId, result.State.Series[0].SourceSeriesSpecifications!.Single().LocationId);
        Assert.AreEqual(DataType.TempMean, result.State.Series[0].SourceSeriesSpecifications!.Single().MeasurementDefinition!.DataType);
    }

    [TestMethod]
    public void BuildRelativeUrlOmitsCsdForEmptySeries()
    {
        var service = CreateService();

        var url = service.BuildRelativeUrl(
            "regionalandglobal",
            new ChartState
            {
                ChartAllData = false,
                GroupingDays = 14,
                GroupingThresholdText = "70",
                Series = [],
            });

        Assert.AreEqual("regionalandglobal?chartAllData=false&groupingDays=14&groupingThreshold=70", url);
    }

    private static ChartStateUrlService CreateService()
    {
        return new ChartStateUrlService(NullLogger<ChartStateUrlService>.Instance);
    }

    private static ChartUrlStateContext CreateContext()
    {
        var location = new Location
        {
            Id = LocationId,
            Name = "Testville",
            CountryCode = "AU",
            Coordinates = new Coordinates(1, 2),
        };

        return new ChartUrlStateContext
        {
            Locations = new Dictionary<Guid, Location> { [location.Id] = location },
            Regions = Region.GetRegions(),
            DataSetDefinitions = [CreateDataSetDefinition()],
        };
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition()
    {
        return new DataSetDefinitionViewModel
        {
            Id = DataSetDefinitionId,
            Name = "Test temperature",
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

    private static ChartSeriesDefinition CreateChartSeries(DataSetDefinitionViewModel dataSetDefinition, Location location)
    {
        var measurementDefinition = dataSetDefinition.MeasurementDefinitions!.Single();

        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications =
            [
                new SourceSeriesSpecification
                {
                    LocationId = location.Id,
                    LocationName = location.Name,
                    DataSetDefinition = dataSetDefinition,
                    MeasurementDefinition = measurementDefinition,
                },
            ],
            Aggregation = SeriesAggregationOptions.Mean,
            RequestedColour = Colours.Blue,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
            IsLocked = false,
            SecondaryCalculation = SecondaryCalculationOptions.None,
            ShowTrendline = true,
            Smoothing = SeriesSmoothingOptions.MovingAverage,
            SmoothingWindow = 20,
            Value = SeriesValueOptions.Value,
            Year = null,
            IsExpanded = true,
            SeriesTransformation = SeriesTransformations.Identity,
            CustomTransformation = null,
            GroupingThreshold = .8f,
            MinimumDataResolution = DataResolution.Monthly,
        };
    }
}
