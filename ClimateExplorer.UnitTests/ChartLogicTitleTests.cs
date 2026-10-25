namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartLogicTitleTests
{
    private static readonly Location Canberra = CreateLocation("Canberra", "Australia");
    private static readonly Location Hobart = CreateLocation("Hobart", "Australia");

    [TestMethod]
    public void BuildChartTitle_SingleSeriesWithKnownLocation_UsesLocationDisplayTitle()
    {
        var title = ChartLogic.BuildChartTitle([CreateSeriesWithData(Canberra, DataType.TempMean)], Dictionary(Canberra));

        Assert.AreEqual("Canberra, Australia | Mean temperature | Adjusted", title);
    }

    [TestMethod]
    public void BuildChartTitle_SingleSeriesWithoutLocationDictionary_UsesLocationName()
    {
        var title = ChartLogic.BuildChartTitle([CreateSeriesWithData(Canberra, DataType.TempMean)], null);

        Assert.AreEqual("Canberra | Mean temperature | Adjusted", title);
    }

    [TestMethod]
    public void BuildChartTitle_MultipleSeriesForOneLocation_UsesLocationDisplayTitle()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Canberra, DataType.Precipitation),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra));

        Assert.AreEqual("Canberra, Australia", title);
    }

    [TestMethod]
    public void BuildChartTitle_LocationOwnedByAnotherCountry_OmitsOwnerFromTitle()
    {
        var nuuk = CreateLocation("Nuuk", "Greenland [Denmark]");
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(nuuk, DataType.TempMean),
            CreateSeriesWithData(nuuk, DataType.Precipitation),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(nuuk));

        Assert.AreEqual("Nuuk, Greenland", title);
    }

    [TestMethod]
    public void BuildChartTitle_MultipleLocations_JoinsLocationNames()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Hobart, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Hobart));

        Assert.AreEqual("Canberra, Hobart", title);
    }

    private static Dictionary<Guid, Location> Dictionary(params Location[] locations)
    {
        var dictionary = new Dictionary<Guid, Location>();
        foreach (var location in locations)
        {
            dictionary[location.Id] = location;
        }

        return dictionary;
    }

    private static Location CreateLocation(string name, string country)
    {
        return new Location { Id = Guid.NewGuid(), Name = name, Country = country, CountryCode = "XX", Coordinates = new Coordinates(1, 2) };
    }

    private static SeriesWithData CreateSeriesWithData(Location location, DataType dataType)
    {
        var measurementDefinition = new MeasurementDefinitionViewModel
        {
            DataType = dataType,
            DataAdjustment = DataAdjustment.Adjusted,
            DataResolution = DataResolution.Monthly,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
        };

        var chartSeries = new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications =
            [
                new SourceSeriesSpecification
                {
                    LocationId = location.Id,
                    LocationName = location.Name,
                    MeasurementDefinition = measurementDefinition,
                },
            ],
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = BinGranularities.ByYear,
            SeriesTransformation = SeriesTransformations.Identity,
            Value = SeriesValueOptions.Value,
        };

        var dataSet = new DataSet
        {
            MeasurementDefinition = measurementDefinition,
            GeographicalEntity = location,
            DataRecords = [],
        };

        return new SeriesWithData
        {
            ChartSeries = chartSeries,
            SourceDataSet = dataSet,
            PreProcessedDataSet = dataSet,
        };
    }
}
