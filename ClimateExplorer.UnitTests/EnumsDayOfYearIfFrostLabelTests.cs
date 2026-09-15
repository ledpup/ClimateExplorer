namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Covers <see cref="Enums.GetDayOfYearIfFrostLabel"/> and the <see cref="Enums.UnitOfMeasureLabel(SeriesTransformations, string?, UnitOfMeasure, SeriesAggregationOptions, SeriesValueOptions, MeteorologicalHemisphere?)"/>
/// overload that uses it - this is the Y-axis title text (ChartOptionsFactory.CreateYAxes), a separate
/// display path from the chart legend/tooltip label covered by ChartSeriesDefinition.GetTransformationOverrideLabel
/// (see ChartTooltipMetadataBuilderTests), but which must agree with it on which aggregation is
/// "first"/"last day of frost" for a given hemisphere.
/// </summary>
[TestClass]
public class EnumsDayOfYearIfFrostLabelTests
{
    [TestMethod]
    [DataRow(null, SeriesAggregationOptions.Minimum, "First day of frost")]
    [DataRow(null, SeriesAggregationOptions.Maximum, "Last day of frost")]
    [DataRow(MeteorologicalHemisphere.Southern, SeriesAggregationOptions.Minimum, "First day of frost")]
    [DataRow(MeteorologicalHemisphere.Southern, SeriesAggregationOptions.Maximum, "Last day of frost")]
    [DataRow(MeteorologicalHemisphere.Northern, SeriesAggregationOptions.Minimum, "Last day of frost")]
    [DataRow(MeteorologicalHemisphere.Northern, SeriesAggregationOptions.Maximum, "First day of frost")]
    public void GetDayOfYearIfFrostLabel_ReturnsExpectedLabel(MeteorologicalHemisphere? hemisphere, SeriesAggregationOptions aggregation, string expected)
    {
        Assert.AreEqual(expected, GetDayOfYearIfFrostLabel(aggregation, hemisphere));
    }

    [TestMethod]
    public void UnitOfMeasureLabel_NorthernHemisphereMinimum_IsLastDayOfFrost()
    {
        var label = UnitOfMeasureLabel(
            SeriesTransformations.DayOfYearIfFrost,
            customTransformation: null,
            UnitOfMeasure.DegreesCelsius,
            SeriesAggregationOptions.Minimum,
            SeriesValueOptions.Value,
            MeteorologicalHemisphere.Northern);

        Assert.AreEqual("Last day of frost", label);
    }

    [TestMethod]
    public void UnitOfMeasureLabel_SouthernHemisphereMinimum_IsFirstDayOfFrost()
    {
        var label = UnitOfMeasureLabel(
            SeriesTransformations.DayOfYearIfFrost,
            customTransformation: null,
            UnitOfMeasure.DegreesCelsius,
            SeriesAggregationOptions.Minimum,
            SeriesValueOptions.Value,
            MeteorologicalHemisphere.Southern);

        Assert.AreEqual("First day of frost", label);
    }

    [TestMethod]
    public void UnitOfMeasureLabel_HemisphereOmitted_DefaultsToSouthernHemisphereWording()
    {
        var label = UnitOfMeasureLabel(
            SeriesTransformations.DayOfYearIfFrost,
            customTransformation: null,
            UnitOfMeasure.DegreesCelsius,
            SeriesAggregationOptions.Minimum,
            SeriesValueOptions.Value);

        Assert.AreEqual("First day of frost", label);
    }
}
