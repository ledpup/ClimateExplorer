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
    private static readonly Location Canberra = CreateLocation("Canberra", "Australia", "AU");
    private static readonly Location Hobart = CreateLocation("Hobart", "Australia", "AU");
    private static readonly Location Perth = CreateLocation("Perth", "Australia", "AU");
    private static readonly Location Auckland = CreateLocation("Auckland", "New Zealand", "NZ");
    private static readonly Location Suva = CreateLocation("Suva", "Fiji", "FJ");

    [TestMethod]
    public void BuildChartTitle_NoSeries_ReturnsClimateData()
    {
        var title = ChartLogic.BuildChartTitle([], Dictionary(Canberra));

        Assert.AreEqual("Climate data", title);
    }

    [TestMethod]
    public void BuildChartTitle_SingleSeriesWithKnownLocation_UsesLocationDisplayTitle()
    {
        var title = ChartLogic.BuildChartTitle([CreateSeriesWithData(Canberra, DataType.TempMean)], Dictionary(Canberra));

        Assert.AreEqual("Canberra, Australia", title);
    }

    [TestMethod]
    public void BuildChartTitle_SingleSeriesWithoutLocationDictionary_UsesLocationName()
    {
        var title = ChartLogic.BuildChartTitle([CreateSeriesWithData(Canberra, DataType.TempMean)], null);

        Assert.AreEqual("Canberra", title);
    }

    [TestMethod]
    public void BuildChartTitle_LocationMissingFromDictionary_UsesLocationNameAsOwnGroup()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Auckland, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra));

        Assert.AreEqual("Canberra, Australia — Auckland", title);
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
        var nuuk = CreateLocation("Nuuk", "Greenland [Denmark]", "GL");
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(nuuk, DataType.TempMean),
            CreateSeriesWithData(nuuk, DataType.Precipitation),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(nuuk));

        Assert.AreEqual("Nuuk, Greenland", title);
    }

    [TestMethod]
    public void BuildChartTitle_LocationsInSameCountry_GroupsNamesBeforeCountry()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Hobart, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Hobart));

        Assert.AreEqual("Canberra, Hobart | Australia", title);
    }

    [TestMethod]
    public void BuildChartTitle_TwoCountries_SeparatesCountryGroups()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Auckland, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Auckland));

        Assert.AreEqual("Canberra, Australia — Auckland, New Zealand", title);
    }

    [TestMethod]
    public void BuildChartTitle_TwoCountriesWithSeveralLocationsInOne_GroupsByCountry()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Hobart, DataType.TempMean),
            CreateSeriesWithData(Auckland, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Hobart, Auckland));

        Assert.AreEqual("Canberra, Hobart | Australia — Auckland, New Zealand", title);
    }

    [TestMethod]
    public void BuildChartTitle_MoreThanTwoCountries_UsesCountryCodes()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Hobart, DataType.TempMean),
            CreateSeriesWithData(Auckland, DataType.TempMean),
            CreateSeriesWithData(Suva, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Hobart, Auckland, Suva));

        Assert.AreEqual("Canberra, Hobart | AU — Auckland, NZ — Suva, FJ", title);
    }

    [TestMethod]
    public void BuildChartTitle_GroupsOutOfCountryOrder_KeepsOrderOfFirstLocation()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Auckland, DataType.TempMean),
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Hobart, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Hobart, Auckland));

        Assert.AreEqual("Auckland, New Zealand — Canberra, Hobart | Australia", title);
    }

    [TestMethod]
    public void BuildChartTitle_MoreThanFourLocations_ListsNamesOnly()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Canberra, DataType.TempMean),
            CreateSeriesWithData(Hobart, DataType.TempMean),
            CreateSeriesWithData(Perth, DataType.TempMean),
            CreateSeriesWithData(Auckland, DataType.TempMean),
            CreateSeriesWithData(Suva, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra, Hobart, Perth, Auckland, Suva));

        Assert.AreEqual("Canberra, Hobart, Perth, Auckland, Suva", title);
    }

    [TestMethod]
    public void BuildChartTitle_LocationAndRegion_PutsRegionAfterLocation()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Region.GetRegion(Region.Atmosphere), DataType.CO2),
            CreateSeriesWithData(Canberra, DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary(Canberra));

        Assert.AreEqual("Canberra, Australia — Atmosphere", title);
    }

    [TestMethod]
    public void BuildChartTitle_RegionsOnly_SeparatesRegionNames()
    {
        var series = new List<SeriesWithData>
        {
            CreateSeriesWithData(Region.GetRegion(Region.Atmosphere), DataType.CO2),
            CreateSeriesWithData(Region.GetRegion(Region.Ocean), DataType.TempMean),
        };

        var title = ChartLogic.BuildChartTitle(series, Dictionary());

        Assert.AreEqual("Atmosphere — Ocean", title);
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

    private static Location CreateLocation(string name, string country, string countryCode)
    {
        return new Location { Id = Guid.NewGuid(), Name = name, Country = country, CountryCode = countryCode, Coordinates = new Coordinates(1, 2) };
    }

    private static SeriesWithData CreateSeriesWithData(GeographicalEntity location, DataType dataType)
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
