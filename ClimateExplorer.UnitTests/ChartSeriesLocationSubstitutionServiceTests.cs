namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ChartSeriesLocationSubstitutionServiceTests
{
    private static readonly Guid OldLocationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid NewLocationId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid OldTemperatureDataSetId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid NewTemperatureDataSetId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid OldPrecipitationDataSetId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid RegionTemperatureDataSetId = Guid.Parse("20000000-0000-0000-0000-000000000004");

    [TestMethod]
    public void SubstituteMovesUnlockedSingleLocationSeriesToNewLocation()
    {
        var definitions = CreateDataSetDefinitions(includeNewPrecipitation: true);
        var oldDefinition = definitions.Single(x => x.Id == OldTemperatureDataSetId);
        var state = new ChartState
        {
            ChartAllData = true,
            Series = [CreateSeries(oldDefinition, OldLocation(), isLocked: false)],
        };

        var result = CreateService().Substitute(CreateContext(state, definitions));

        Assert.IsEmpty(result.Messages);
        Assert.HasCount(1, result.State.Series);
        Assert.IsTrue(result.State.ChartAllData);

        var specification = result.State.Series.Single().SourceSeriesSpecifications!.Single();
        Assert.AreEqual(NewLocationId, specification.LocationId);
        Assert.AreEqual(NewLocation().Name, specification.LocationName);
        Assert.AreEqual(NewTemperatureDataSetId, specification.DataSetDefinition!.Id);
        Assert.AreEqual(DataType.TempMean, specification.MeasurementDefinition!.DataType);
        Assert.IsTrue(result.State.Series.Single().DataAvailable);
    }

    [TestMethod]
    public void SubstituteDoesNotRewriteRegionSourceSpecifications()
    {
        var definitions = CreateDataSetDefinitions(includeNewPrecipitation: true);
        var region = Region.GetRegion(Region.Atmosphere);
        var regionDefinition = definitions.Single(x => x.Id == RegionTemperatureDataSetId);
        var state = new ChartState
        {
            Series = [CreateSeries(regionDefinition, region, isLocked: false)],
        };

        var result = CreateService().Substitute(CreateContext(state, definitions));

        var specification = result.State.Series.Single().SourceSeriesSpecifications!.Single();
        Assert.AreEqual(region.Id, specification.LocationId);
        Assert.AreEqual(region.Name, specification.LocationName);
        Assert.AreEqual(RegionTemperatureDataSetId, specification.DataSetDefinition!.Id);
    }

    [TestMethod]
    public void SubstituteDuplicatesLockedSeriesForNewLocation()
    {
        var definitions = CreateDataSetDefinitions(includeNewPrecipitation: true);
        var oldDefinition = definitions.Single(x => x.Id == OldTemperatureDataSetId);
        var state = new ChartState
        {
            Series = [CreateSeries(oldDefinition, OldLocation(), isLocked: true)],
        };

        var result = CreateService().Substitute(CreateContext(state, definitions));

        Assert.IsEmpty(result.Messages);
        Assert.HasCount(2, result.State.Series);

        var lockedSeries = result.State.Series.Single(x => x.IsLocked);
        Assert.AreEqual(OldLocationId, lockedSeries.SourceSeriesSpecifications!.Single().LocationId);

        var duplicate = result.State.Series.Single(x => !x.IsLocked);
        Assert.AreEqual(NewLocationId, duplicate.SourceSeriesSpecifications!.Single().LocationId);
        Assert.AreEqual(NewLocation().Name, duplicate.SourceSeriesSpecifications!.Single().LocationName);
        Assert.AreEqual(oldDefinition.Id, duplicate.SourceSeriesSpecifications!.Single().DataSetDefinition!.Id);
        Assert.AreEqual(SeriesSmoothingOptions.MovingAverage, duplicate.Smoothing);
        Assert.AreEqual(DataResolution.Monthly, duplicate.MinimumDataResolution);
    }

    [TestMethod]
    public void SubstituteMarksUnavailableSeriesAndReturnsWarningWhenMeasurementIsUnavailable()
    {
        var definitions = CreateDataSetDefinitions(includeNewPrecipitation: false);
        var precipitationDefinition = definitions.Single(x => x.Id == OldPrecipitationDataSetId);
        var state = new ChartState
        {
            Series = [CreateSeries(precipitationDefinition, OldLocation(), isLocked: false)],
        };

        var result = CreateService().Substitute(CreateContext(state, definitions));

        Assert.HasCount(1, result.State.Series);
        Assert.IsFalse(result.State.Series.Single().DataAvailable);
        Assert.HasCount(1, result.Messages);
        StringAssert.Contains(result.Messages.Single().Message, "Precipitation data is not available at Newtown, New Country.");
    }

    private static ChartSeriesLocationSubstitutionService CreateService()
    {
        return new ChartSeriesLocationSubstitutionService();
    }

    private static ChartLocationSubstitutionContext CreateContext(
        ChartState state,
        IReadOnlyList<DataSetDefinitionViewModel> definitions)
    {
        return new ChartLocationSubstitutionContext
        {
            State = state,
            Location = NewLocation(),
            Regions = Region.GetRegions(),
            DataSetDefinitions = definitions,
        };
    }

    private static Location OldLocation()
    {
        return new Location
        {
            Id = OldLocationId,
            Name = "Oldtown",
            Country = "Old Country",
            CountryCode = "OC",
            Coordinates = new Coordinates(1, 2),
        };
    }

    private static Location NewLocation()
    {
        return new Location
        {
            Id = NewLocationId,
            Name = "Newtown",
            Country = "New Country",
            CountryCode = "NC",
            Coordinates = new Coordinates(3, 4),
        };
    }

    private static List<DataSetDefinitionViewModel> CreateDataSetDefinitions(bool includeNewPrecipitation)
    {
        var definitions = new List<DataSetDefinitionViewModel>
        {
            CreateDataSetDefinition(OldTemperatureDataSetId, "Old temperature", OldLocationId, DataType.TempMean, DataAdjustment.Adjusted, UnitOfMeasure.DegreesCelsius),
            CreateDataSetDefinition(NewTemperatureDataSetId, "New temperature", NewLocationId, DataType.TempMean, DataAdjustment.Adjusted, UnitOfMeasure.DegreesCelsius),
            CreateDataSetDefinition(OldPrecipitationDataSetId, "Old precipitation", OldLocationId, DataType.Precipitation, null, UnitOfMeasure.Millimetres),
            CreateDataSetDefinition(RegionTemperatureDataSetId, "Regional temperature", Region.RegionId(Region.Atmosphere), DataType.TempMean, DataAdjustment.Adjusted, UnitOfMeasure.DegreesCelsius),
        };

        if (includeNewPrecipitation)
        {
            definitions.Add(CreateDataSetDefinition(Guid.Parse("20000000-0000-0000-0000-000000000005"), "New precipitation", NewLocationId, DataType.Precipitation, null, UnitOfMeasure.Millimetres));
        }

        return definitions;
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition(
        Guid id,
        string name,
        Guid locationId,
        DataType dataType,
        DataAdjustment? dataAdjustment,
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
                    DataResolution = DataResolution.Monthly,
                    UnitOfMeasure = unitOfMeasure,
                },
            ],
        };
    }

    private static ChartSeriesDefinition CreateSeries(DataSetDefinitionViewModel dataSetDefinition, GeographicalEntity entity, bool isLocked)
    {
        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications =
            [
                new SourceSeriesSpecification
                {
                    LocationId = entity.Id,
                    LocationName = entity.Name,
                    DataSetDefinition = dataSetDefinition,
                    MeasurementDefinition = dataSetDefinition.MeasurementDefinitions!.Single(),
                },
            ],
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
            IsLocked = isLocked,
            ShowTrendline = true,
            Smoothing = SeriesSmoothingOptions.MovingAverage,
            SmoothingWindow = 20,
            Value = SeriesValueOptions.Value,
            Year = null,
            SeriesTransformation = SeriesTransformations.Identity,
            MinimumDataResolution = DataResolution.Monthly,
        };
    }
}
