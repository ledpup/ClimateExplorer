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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class DefaultChartProviderTests
{
    private static readonly Guid LocationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [TestMethod]
    public void LocationDefaultAddsTemperatureAndPrecipitationForDesktop()
    {
        var provider = new LocationDefaultChartProvider();
        var state = provider.CreateDefault(CreateLocationContext(isMobileDevice: false));

        Assert.HasCount(2, state.Series);
        Assert.AreEqual(DataType.TempMean, state.Series[0].SourceSeriesSpecifications!.Single().MeasurementDefinition!.DataType);
        Assert.AreEqual(SeriesAggregationOptions.Mean, state.Series[0].Aggregation);
        Assert.AreEqual(SeriesSmoothingOptions.MovingAverage, state.Series[0].Smoothing);
        Assert.AreEqual(20, state.Series[0].SmoothingWindow);
        Assert.AreEqual(DataType.Precipitation, state.Series[1].SourceSeriesSpecifications!.Single().MeasurementDefinition!.DataType);
        Assert.AreEqual(SeriesAggregationOptions.Sum, state.Series[1].Aggregation);
    }

    [TestMethod]
    public void LocationDefaultOmitsPrecipitationForMobile()
    {
        var provider = new LocationDefaultChartProvider();
        var state = provider.CreateDefault(CreateLocationContext(isMobileDevice: true));

        Assert.HasCount(1, state.Series);
        Assert.AreEqual(DataType.TempMean, state.Series[0].SourceSeriesSpecifications!.Single().MeasurementDefinition!.DataType);
    }

    [TestMethod]
    public void LocationDefaultOmitsPrecipitationWhenUnavailable()
    {
        var provider = new LocationDefaultChartProvider();
        var state = provider.CreateDefault(CreateLocationContext(isMobileDevice: false, includePrecipitation: false));

        Assert.HasCount(1, state.Series);
        Assert.AreEqual(DataType.TempMean, state.Series[0].SourceSeriesSpecifications!.Single().MeasurementDefinition!.DataType);
    }

    [TestMethod]
    public void RegionalAndGlobalDefaultAddsCo2AnnualChange()
    {
        var provider = new RegionalAndGlobalDefaultChartProvider();
        var state = provider.CreateDefault(CreateRegionalContext());

        Assert.HasCount(1, state.Series);
        var series = state.Series.Single();
        Assert.AreEqual(DataType.CO2, series.SourceSeriesSpecifications!.Single().MeasurementDefinition!.DataType);
        Assert.AreEqual(Region.RegionId(Region.Atmosphere), series.SourceSeriesSpecifications!.Single().LocationId);
        Assert.AreEqual(SecondaryCalculationOptions.AnnualChange, series.SecondaryCalculation);
        Assert.AreEqual(SeriesSmoothingOptions.MovingAverage, series.Smoothing);
        Assert.AreEqual(10, series.SmoothingWindow);
        Assert.AreEqual(Colours.Brown, series.RequestedColour);
    }

    private static LocationDefaultChartContext CreateLocationContext(bool isMobileDevice, bool includePrecipitation = true)
    {
        var location = CreateLocation();

        return new LocationDefaultChartContext
        {
            Location = location,
            DataSetDefinitions = CreateDataSetDefinitions(includePrecipitation),
            IsMobileDevice = isMobileDevice,
        };
    }

    private static RegionalAndGlobalDefaultChartContext CreateRegionalContext()
    {
        return new RegionalAndGlobalDefaultChartContext
        {
            DataSetDefinitions = CreateDataSetDefinitions(includePrecipitation: true),
        };
    }

    private static Location CreateLocation()
    {
        return new Location
        {
            Id = LocationId,
            Name = "Defaultville",
            CountryCode = "AU",
            Coordinates = new Coordinates(1, 2),
        };
    }

    private static List<DataSetDefinitionViewModel> CreateDataSetDefinitions(bool includePrecipitation)
    {
        var dataSetDefinitions = new List<DataSetDefinitionViewModel>
        {
            CreateDataSetDefinition(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                "Temperature",
                LocationId,
                DataType.TempMean,
                DataAdjustment.Adjusted,
                DataResolution.Monthly,
                UnitOfMeasure.DegreesCelsius),
            CreateDataSetDefinition(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "Carbon dioxide",
                Region.RegionId(Region.Atmosphere),
                DataType.CO2,
                null,
                DataResolution.Monthly,
                UnitOfMeasure.PartsPerMillion),
        };

        if (includePrecipitation)
        {
            dataSetDefinitions.Add(
                CreateDataSetDefinition(
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    "Precipitation",
                    LocationId,
                    DataType.Precipitation,
                    null,
                    DataResolution.Monthly,
                    UnitOfMeasure.Millimetres));
        }

        return dataSetDefinitions;
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition(
        Guid id,
        string name,
        Guid locationId,
        DataType dataType,
        DataAdjustment? dataAdjustment,
        DataResolution dataResolution,
        UnitOfMeasure unitOfMeasure)
    {
        return new DataSetDefinitionViewModel
        {
            Id = id,
            Name = name,
            ShortName = name,
            LocationIds = [locationId],
            MeasurementDefinitions =
            [
                new MeasurementDefinitionViewModel
                {
                    DataType = dataType,
                    DataAdjustment = dataAdjustment,
                    DataResolution = dataResolution,
                    UnitOfMeasure = unitOfMeasure,
                },
            ],
        };
    }
}
