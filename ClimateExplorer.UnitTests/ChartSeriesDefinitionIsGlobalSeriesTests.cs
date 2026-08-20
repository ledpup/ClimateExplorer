namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Covers <see cref="ChartSeriesDefinition.IsGlobalSeries"/> - the local/global data set browsers'
/// "add a trend to an existing series" pickers use this to route a chart series to the right one
/// (see the multi-trend chart series plan doc's "Later stage" addendum).
/// </summary>
[TestClass]
public class ChartSeriesDefinitionIsGlobalSeriesTests
{
    [TestMethod]
    public void IsGlobalSeries_SingleSourceAtARegion_ReturnsTrue()
    {
        var series = CreateSeries(Region.RegionId(Region.Atmosphere));

        Assert.IsTrue(series.IsGlobalSeries);
    }

    [TestMethod]
    public void IsGlobalSeries_SingleSourceAtARealLocation_ReturnsFalse()
    {
        var series = CreateSeries(Guid.NewGuid());

        Assert.IsFalse(series.IsGlobalSeries);
    }

    [TestMethod]
    public void IsGlobalSeries_BothSourcesAtRealLocations_ReturnsFalse()
    {
        var series = CreateSeries(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsFalse(series.IsGlobalSeries);
    }

    [TestMethod]
    public void IsGlobalSeries_MixOfRegionAndRealLocation_ReturnsFalse()
    {
        var series = CreateSeries(Region.RegionId(Region.Ocean), Guid.NewGuid());

        Assert.IsFalse(series.IsGlobalSeries);
    }

    [TestMethod]
    public void IsGlobalSeries_NoSourceSeriesSpecifications_ReturnsFalse()
    {
        var series = new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications = null,
            BinGranularity = BinGranularities.ByYear,
        };

        Assert.IsFalse(series.IsGlobalSeries);
    }

    private static ChartSeriesDefinition CreateSeries(params Guid[] locationIds)
    {
        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            BinGranularity = BinGranularities.ByYear,
            SourceSeriesSpecifications =
                [.. locationIds.Select(id => new SourceSeriesSpecification { LocationId = id, LocationName = "Test" })],
        };
    }
}
