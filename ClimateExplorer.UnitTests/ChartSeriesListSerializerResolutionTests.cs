namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartSeriesListSerializerResolutionTests
{
    private static readonly Guid LocationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DataSetDefinitionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [TestMethod]
    public void ParseChartSeriesDefinitionList_DataTypeHasMonthlyAndDailyDefinitions_UrlWithNoMinimumResolutionResolvesToMonthly()
    {
        // Regression test: a dataset with two measurement definitions sharing the same DataType and
        // DataAdjustment but different DataResolution (as CO2 now does - Monthly and Daily) used to
        // throw InvalidOperationException ("MoreThanOneMatch") when parsing a URL whose
        // MinimumDataResolution segment is empty, because the SingleOrDefault lookup no longer
        // filtered by resolution. It should instead deterministically fall back to the coarsest
        // (Monthly) definition, preserving the resolution that URLs saved before Daily existed.
        var dataSetDefinition = CreateDataSetDefinitionWithMonthlyAndDailyCo2();

        // No MinimumDataResolution segment (empty segment 18) and no trailing trend segments -
        // mirrors a URL saved before the Daily CO2 definition was introduced.
        var component = "ReturnSingleSeries," +
            $"{DataSetDefinitionId}*CO2**{LocationId}," +
            "Mean,Brown,ByYear,Line,False,None,False,None,5,Value,,False,Identity,,,True,";

        var parsed = ChartSeriesListSerializer
            .ParseChartSeriesDefinitionList(
                NullLogger.Instance,
                component,
                [dataSetDefinition],
                new Dictionary<Guid, Location> { [LocationId] = CreateLocation() },
                []);

        var sourceSeriesSpecification = parsed.Single().SourceSeriesSpecifications!.Single();

        Assert.AreEqual(DataResolution.Monthly, sourceSeriesSpecification.MeasurementDefinition!.DataResolution);
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

    private static DataSetDefinitionViewModel CreateDataSetDefinitionWithMonthlyAndDailyCo2()
    {
        return new DataSetDefinitionViewModel
        {
            Id = DataSetDefinitionId,
            Name = "Test CO2 data set",
            ShortName = "CO2",
            LocationIds = [LocationId],
            MeasurementDefinitions =
            [
                new MeasurementDefinitionViewModel
                {
                    DataType = DataType.CO2,
                    DataAdjustment = null,
                    DataResolution = DataResolution.Monthly,
                    UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                },
                new MeasurementDefinitionViewModel
                {
                    DataType = DataType.CO2,
                    DataAdjustment = null,
                    DataResolution = DataResolution.Daily,
                    UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                },
            ],
        };
    }
}
