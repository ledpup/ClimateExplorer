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
public class SeriesLabellerTests
{
    private static readonly Location Canberra = CreateLocation("Canberra");
    private static readonly Location Hobart = CreateLocation("Hobart");

    private static readonly string MeanTemp = ChartSeriesDefinition.MapDataTypeToFriendlyName(DataType.TempMean);
    private static readonly string MaxTemp = ChartSeriesDefinition.MapDataTypeToFriendlyName(DataType.TempMax);
    private static readonly string MinTemp = ChartSeriesDefinition.MapDataTypeToFriendlyName(DataType.TempMin);
    private static readonly string Rainfall = ChartSeriesDefinition.MapDataTypeToFriendlyName(DataType.Precipitation);

    [TestMethod]
    public void Build_OnePlace_RemovesLocationFromLabels()
    {
        var labels = Build([Series(Canberra, DataType.TempMax), Series(Canberra, DataType.TempMin)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { MaxTemp, MinTemp }, ToArray(labels.Labels));
    }

    [TestMethod]
    public void Build_OneSeries_KeepsAllPartsButLocationAndUnitAndHasNoNote()
    {
        var series = Series(Canberra, DataType.TempMean);
        series.Aggregation = SeriesAggregationOptions.Maximum;

        var labels = Build([series], ["°C"]);

        Assert.AreEqual($"{MeanTemp} | Adjusted | Statistic: Maximum", labels.Labels[0]);
        Assert.IsNull(labels.SeriesNote);
    }

    [TestMethod]
    public void Build_SharedUnit_RemovedFromLabelsAndNotInNote()
    {
        var labels = Build([Series(Canberra, DataType.TempMean), Series(Hobart, DataType.TempMean)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { "Canberra", "Hobart" }, ToArray(labels.Labels));
        Assert.AreEqual($"{MeanTemp} | Adjusted", labels.SeriesNote);
    }

    [TestMethod]
    public void Build_DifferentUnits_KeptInLabels()
    {
        var labels = Build([Series(Canberra, DataType.TempMean), Series(Canberra, DataType.Precipitation, adjustment: null)], ["°C", "mm"]);

        CollectionAssert.AreEqual(new[] { $"{MeanTemp} | Adjusted | °C", $"{Rainfall} | mm" }, ToArray(labels.Labels));
        Assert.IsNull(labels.SeriesNote);
    }

    [TestMethod]
    public void Build_TwoLocations_KeepsLocationsAndMovesSharedPartsToNote()
    {
        var labels = Build([Series(Canberra, DataType.TempMax), Series(Hobart, DataType.TempMin)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { $"Canberra | {MaxTemp}", $"Hobart | {MinTemp}" }, ToArray(labels.Labels));
        Assert.AreEqual("Adjusted", labels.SeriesNote);
    }

    [TestMethod]
    public void Build_SharedParts_NoteKeepsOrderOfFirstSeries()
    {
        var max = Smoothed(Series(Canberra, DataType.TempMax));
        var min = Smoothed(Series(Canberra, DataType.TempMin));

        var labels = Build([max, min], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { MaxTemp, MinTemp }, ToArray(labels.Labels));
        Assert.AreEqual("Adjusted | 20 year centred moving average", labels.SeriesNote);
    }

    [TestMethod]
    public void Build_PartMissingFromOneSeries_StaysInLabels()
    {
        var smoothed = Smoothed(Series(Canberra, DataType.TempMax));

        var labels = Build([smoothed, Series(Canberra, DataType.TempMin)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { $"{MaxTemp} | 20 year centred moving average", MinTemp }, ToArray(labels.Labels));
        Assert.AreEqual("Adjusted", labels.SeriesNote);
    }

    [TestMethod]
    public void Build_SameTextInDifferentParts_IsNotShared()
    {
        // The year segment of one series has the same text as the other's location.
        var yearly = Series(Canberra, DataType.TempMean);
        yearly.Year = 2000;
        var named = Series(CreateLocation("2000"), DataType.TempMean);

        var labels = Build([yearly, named], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { "Canberra | 2000", "2000" }, ToArray(labels.Labels));
        Assert.AreEqual($"{MeanTemp} | Adjusted", labels.SeriesNote);
    }

    [TestMethod]
    public void Build_LabelsEmptyAfterSharing_SkipsNote()
    {
        var line = Series(Canberra, DataType.TempMean);
        var bar = Series(Canberra, DataType.TempMean);
        bar.DisplayStyle = SeriesDisplayStyle.Bar;

        var labels = Build([line, bar], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { $"{MeanTemp} | Adjusted", $"{MeanTemp} | Adjusted" }, ToArray(labels.Labels));
        Assert.IsNull(labels.SeriesNote);
    }

    [TestMethod]
    public void Build_Mobile_RemovesLocationOnlyAndHasNoNote()
    {
        var labels = Build([Smoothed(Series(Canberra, DataType.TempMax)), Smoothed(Series(Canberra, DataType.TempMin))], ["°C", "°C"], isMobileDevice: true);

        CollectionAssert.AreEqual(new[] { MaxTemp, MinTemp }, ToArray(labels.Labels));
        Assert.IsNull(labels.SeriesNote);
    }

    [TestMethod]
    public void Build_MobileTwoLocations_LabelsUnchanged()
    {
        var labels = Build([Series(Canberra, DataType.TempMean), Series(Hobart, DataType.TempMean)], ["°C", "°C"], isMobileDevice: true);

        CollectionAssert.AreEqual(new[] { $"Canberra | {MeanTemp}", $"Hobart | {MeanTemp}" }, ToArray(labels.Labels));
        Assert.IsNull(labels.SeriesNote);
    }

    [TestMethod]
    public void Build_OverrideSeriesWithOthers_KeepsOtherUnitsAndHasNoNote()
    {
        var custom = Series(Canberra, DataType.TempMax);
        custom.SeriesTransformation = SeriesTransformations.Custom;
        custom.CustomTransformation = "x >= 25";

        var labels = Build([custom, Series(Canberra, DataType.TempMax)], ["", "°C"]);

        CollectionAssert.AreEqual(new[] { "Value is 25 or more", $"{MaxTemp} | Adjusted | °C" }, ToArray(labels.Labels));
        Assert.IsNull(labels.SeriesNote);
    }

    [TestMethod]
    public void Build_DifferenceSeriesOnePlace_RemovesLocationInsideBrackets()
    {
        var difference = Series(Canberra, DataType.TempMax);
        difference.SeriesDerivationType = SeriesDerivationTypes.DifferenceBetweenTwoSeries;
        difference.SourceSeriesSpecifications = [.. difference.SourceSeriesSpecifications!, Spec(Canberra, DataType.TempMin, DataAdjustment.Adjusted)];

        var labels = Build([difference], ["°C"]);

        Assert.AreEqual($"[{MaxTemp}] minus [{MinTemp}]", labels.Labels[0]);
    }

    [TestMethod]
    public void Build_OneRegion_RemovesRegionName()
    {
        var atmosphere = Region.GetRegion(Region.Atmosphere);

        var labels = Build([Series(atmosphere, DataType.TempMax), Series(atmosphere, DataType.TempMin)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { MaxTemp, MinTemp }, ToArray(labels.Labels));
    }

    [TestMethod]
    public void Build_LocationAndRegion_KeepsBoth()
    {
        var atmosphere = Region.GetRegion(Region.Atmosphere);

        var labels = Build([Series(Canberra, DataType.TempMax), Series(atmosphere, DataType.TempMin)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { $"Canberra | {MaxTemp}", $"{atmosphere.Name} | {MinTemp}" }, ToArray(labels.Labels));
    }

    [TestMethod]
    public void Build_Trends_PrefixDropsLocationAndSharedParts()
    {
        var labels = Build([Series(Canberra, DataType.TempMean), Series(Hobart, DataType.TempMean)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { "Canberra", "Hobart" }, ToArray(labels.TrendPrefixes));
    }

    [TestMethod]
    public void Build_TrendPrefixEmpty_UsesParentLabel()
    {
        var labels = Build([Series(Canberra, DataType.TempMean), Series(Canberra, DataType.TempMean, DataAdjustment.Unadjusted)], ["°C", "°C"]);

        CollectionAssert.AreEqual(new[] { "Adjusted", "Unadjusted" }, ToArray(labels.Labels));
        CollectionAssert.AreEqual(new[] { "Adjusted", "Unadjusted" }, ToArray(labels.TrendPrefixes));
        Assert.AreEqual(MeanTemp, labels.SeriesNote);
    }

    private static SeriesLabels Build(ChartSeriesDefinition[] series, string[] unitLabels, bool isMobileDevice = false)
    {
        return SeriesLabeller.Build(series, unitLabels, isMobileDevice);
    }

    private static string[] ToArray(IReadOnlyList<string> labels) => [.. labels];

    private static ChartSeriesDefinition Smoothed(ChartSeriesDefinition series)
    {
        series.Smoothing = SeriesSmoothingOptions.CentredMovingAverage;
        series.SmoothingWindow = 20;
        return series;
    }

    private static Location CreateLocation(string name)
    {
        return new Location { Id = Guid.NewGuid(), Name = name, Country = "Australia", CountryCode = "AU", Coordinates = new Coordinates(1, 2) };
    }

    private static SourceSeriesSpecification Spec(GeographicalEntity place, DataType dataType, DataAdjustment? adjustment)
    {
        return new SourceSeriesSpecification
        {
            LocationId = place.Id,
            LocationName = place.Name,
            MeasurementDefinition = new MeasurementDefinitionViewModel
            {
                DataType = dataType,
                DataAdjustment = adjustment,
                DataResolution = DataResolution.Monthly,
                UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
            },
        };
    }

    private static ChartSeriesDefinition Series(GeographicalEntity place, DataType dataType, DataAdjustment? adjustment = DataAdjustment.Adjusted)
    {
        return new ChartSeriesDefinition
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            SourceSeriesSpecifications = [Spec(place, dataType, adjustment)],
            Aggregation = SeriesAggregationOptions.Mean,
            BinGranularity = BinGranularities.ByYear,
            SeriesTransformation = SeriesTransformations.Identity,
            Value = SeriesValueOptions.Value,
        };
    }
}
