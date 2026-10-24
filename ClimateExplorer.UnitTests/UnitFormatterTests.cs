namespace ClimateExplorer.UnitTests;

using System;
using System.Globalization;
using System.Linq;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class UnitFormatterTests
{
    private const double Tolerance = 1e-9;

    private static readonly UnitOfMeasure[] ConvertedUnits =
    [
        UnitOfMeasure.DegreesCelsius,
        UnitOfMeasure.DegreesCelsiusAnomaly,
        UnitOfMeasure.Millimetres,
        UnitOfMeasure.SqKm,
        UnitOfMeasure.MillionSqKm,
    ];

    private static IUnitFormatter Us => UnitFormatter.UsCustomary;

    [TestMethod]
    [DataRow(0d, 32d)]
    [DataRow(100d, 212d)]
    [DataRow(-40d, -40d)]
    [DataRow(37d, 98.6d)]
    public void ToDisplay_LevelCelsiusUsCustomary_AppliesOffset(double celsius, double fahrenheit)
    {
        Assert.AreEqual(fahrenheit, Us.ToDisplay(Measure.Level(celsius, UnitOfMeasure.DegreesCelsius)), Tolerance);
    }

    [TestMethod]
    [DataRow(1d, 1.8d)]
    [DataRow(0d, 0d)]
    [DataRow(-0.5d, -0.9d)]
    public void ToDisplay_ChangeCelsiusUsCustomary_ScalesWithoutOffset(double celsius, double fahrenheit)
    {
        Assert.AreEqual(fahrenheit, Us.ToDisplay(Measure.Change(celsius, UnitOfMeasure.DegreesCelsius)), Tolerance);
    }

    [TestMethod]
    public void ToDisplay_LevelCelsiusAnomalyUsCustomary_NeverAppliesOffset()
    {
        // The anomaly unit is always a difference, whatever kind a caller states.
        Assert.AreEqual(1.8, Us.ToDisplay(Measure.Level(1, UnitOfMeasure.DegreesCelsiusAnomaly)), Tolerance);
        Assert.AreEqual(1.8, Us.ToDisplay(Measure.Change(1, UnitOfMeasure.DegreesCelsiusAnomaly)), Tolerance);
    }

    [TestMethod]
    public void ToDisplay_LevelAndChangeOfTwoTemperatures_DifferenceOfLevelsEqualsChange()
    {
        var warmer = Us.ToDisplay(Measure.Level(21.3, UnitOfMeasure.DegreesCelsius));
        var cooler = Us.ToDisplay(Measure.Level(18.1, UnitOfMeasure.DegreesCelsius));

        Assert.AreEqual(Us.ToDisplay(Measure.Change(21.3 - 18.1, UnitOfMeasure.DegreesCelsius)), warmer - cooler, Tolerance);
    }

    [TestMethod]
    public void ToDisplay_MillimetresUsCustomary_ReturnsInches()
    {
        Assert.AreEqual(1, Us.ToDisplay(Measure.Level(25.4, UnitOfMeasure.Millimetres)), Tolerance);
        Assert.AreEqual(1, Us.ToDisplay(Measure.Change(25.4, UnitOfMeasure.Millimetres)), Tolerance);
    }

    [TestMethod]
    public void ToDisplay_SqKmUsCustomary_ReturnsSqMiles()
    {
        Assert.AreEqual(1, Us.ToDisplay(Measure.Level(2.589988110336, UnitOfMeasure.SqKm)), Tolerance);
        Assert.AreEqual(1, Us.ToDisplay(Measure.Level(2.589988110336, UnitOfMeasure.MillionSqKm)), Tolerance);
    }

    [TestMethod]
    public void ToDisplay_UnlistedUnitUsCustomary_ReturnsValueUnchanged()
    {
        foreach (var unit in Enum.GetValues<UnitOfMeasure>().Except(ConvertedUnits))
        {
            Assert.IsFalse(Us.Converts(unit), unit.ToString());
            Assert.AreEqual(412.3, Us.ToDisplay(Measure.Level(412.3, unit)), unit.ToString());
            Assert.AreEqual(UnitFormatter.Metric.Format(Measure.Level(412.3, unit)), Us.Format(Measure.Level(412.3, unit)), unit.ToString());
        }
    }

    [TestMethod]
    public void ToDisplay_Metric_ReturnsValueUnchangedForEveryUnit()
    {
        foreach (var unit in Enum.GetValues<UnitOfMeasure>())
        {
            Assert.IsFalse(UnitFormatter.Metric.Converts(unit));
            Assert.AreEqual(21.37, UnitFormatter.Metric.ToDisplay(Measure.Level(21.37, unit)));
            Assert.AreEqual(21.37, UnitFormatter.Metric.ToDisplay(Measure.Change(21.37, unit)));
        }
    }

    [TestMethod]
    public void Format_Metric_MatchesExistingLabelsAndRoundingForEveryUnit()
    {
        foreach (var unit in Enum.GetValues<UnitOfMeasure>())
        {
            var value = 12.3456;
            var expectedNumber = value.ToString("F" + UnitOfMeasureRounding(unit), CultureInfo.InvariantCulture);
            var label = UnitOfMeasureLabelShort(unit);
            var tight = unit is UnitOfMeasure.DegreesCelsius or UnitOfMeasure.Millimetres;

            Assert.AreEqual(label, UnitFormatter.Metric.ShortLabel(unit));
            Assert.AreEqual(UnitOfMeasureRounding(unit), UnitFormatter.Metric.Decimals(unit));
            Assert.AreEqual(UnitOfMeasureLabel(SeriesTransformations.Identity, null, unit, SeriesAggregationOptions.Mean, SeriesValueOptions.Value), UnitFormatter.Metric.Label(unit));
            Assert.AreEqual(tight ? $"{expectedNumber}{label}" : $"{expectedNumber} {label}", UnitFormatter.Metric.Format(Measure.Level(value, unit)), unit.ToString());
        }
    }

    [TestMethod]
    public void Format_MetricCelsiusAndMillimetres_MatchesSiteFormatting()
    {
        Assert.AreEqual("14.1°C", UnitFormatter.Metric.Format(Measure.Level(14.06, UnitOfMeasure.DegreesCelsius)));
        Assert.AreEqual("+0.8°C", UnitFormatter.Metric.Format(Measure.Change(0.84, UnitOfMeasure.DegreesCelsius), signed: true));
        Assert.AreEqual("-0.8°C", UnitFormatter.Metric.Format(Measure.Change(-0.84, UnitOfMeasure.DegreesCelsius), signed: true));
        Assert.AreEqual("612mm", UnitFormatter.Metric.Format(Measure.Level(611.7, UnitOfMeasure.Millimetres)));
        Assert.AreEqual("+12mm", UnitFormatter.Metric.Format(Measure.Change(12.2, UnitOfMeasure.Millimetres), signed: true));
    }

    [TestMethod]
    public void Format_UsCustomary_UsesDisplayLabelsAndPrecision()
    {
        Assert.AreEqual("57.2°F", Us.Format(Measure.Level(14, UnitOfMeasure.DegreesCelsius)));
        Assert.AreEqual("+1.4°F", Us.Format(Measure.Change(0.8, UnitOfMeasure.DegreesCelsius), signed: true));
        Assert.AreEqual("24.08 in", Us.Format(Measure.Level(611.7, UnitOfMeasure.Millimetres)));
        Assert.AreEqual("1000 mi²", Us.Format(Measure.Level(2589.988110336, UnitOfMeasure.SqKm)));
        Assert.AreEqual("1.74 million mi²", Us.Format(Measure.Level(4.5, UnitOfMeasure.MillionSqKm)));
        Assert.AreEqual("°F anomaly", Us.ShortLabel(UnitOfMeasure.DegreesCelsiusAnomaly));
    }

    [TestMethod]
    public void Format_UsCustomary_RoundsAfterConverting()
    {
        // 21.06°C is 69.908°F. Rounding to 21.1°C first would give 69.98°F, shown as 70.0°F.
        Assert.AreEqual("69.9°F", Us.Format(Measure.Level(21.06, UnitOfMeasure.DegreesCelsius)));
    }

    [TestMethod]
    public void Format_ExtraAndMinimumDecimals_AppliedToDisplayUnit()
    {
        Assert.AreEqual("14.06°C", UnitFormatter.Metric.Format(Measure.Level(14.06, UnitOfMeasure.DegreesCelsius), extraDecimals: 1));
        Assert.AreEqual("125.4mm", UnitFormatter.Metric.Format(Measure.Level(125.4, UnitOfMeasure.Millimetres), minimumDecimals: 1));
        Assert.AreEqual("4.94 in", Us.Format(Measure.Level(125.4, UnitOfMeasure.Millimetres), minimumDecimals: 1));
    }

    [TestMethod]
    public void ToDisplay_DefaultMeasure_Throws()
    {
        // default(Measure) has no kind, so it can't be converted as either.
        Assert.ThrowsExactly<ArgumentException>(() => Us.ToDisplay(default));
    }

    [TestMethod]
    public void For_EachSystem_ReturnsTheSameInstance()
    {
        Assert.AreSame(UnitFormatter.Metric, UnitFormatter.For(UnitSystem.Metric));
        Assert.AreSame(UnitFormatter.UsCustomary, UnitFormatter.For(UnitSystem.UsCustomary));
    }
}
