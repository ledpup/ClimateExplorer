using ClimateExplorer.Core.DataPreparation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class DateHelpersTests
{
    [TestMethod]
    public void MonthInto14Days()
    {
        var segments = DateHelpers.DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(
            new DateOnly(1990, 01, 01),
            new DateOnly(1990, 01, 31),
            14
        );

        Assert.HasCount(2, segments);
        Assert.AreEqual(new DateOnly(1990, 01, 01), segments[0].Start);
        Assert.AreEqual(new DateOnly(1990, 01, 14), segments[0].End);
        Assert.AreEqual(new DateOnly(1990, 01, 15), segments[1].Start);
        Assert.AreEqual(new DateOnly(1990, 01, 31), segments[1].End);
    }

    [TestMethod]
    public void WhenSegmentSizeIsLargerThanTimeSpan_ReturnASingleSegmentCoveringWholeSpan()
    {
        var segments = DateHelpers.DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(
            new DateOnly(1990, 01, 01),
            new DateOnly(1990, 01, 31),
            60
        );

        Assert.HasCount(1, segments);
        Assert.AreEqual(new DateOnly(1990, 01, 01), segments[0].Start);
        Assert.AreEqual(new DateOnly(1990, 01, 31), segments[0].End);
    }

    [TestMethod]
    public void NonLeapYearInto14Days()
    {
        var segments = DateHelpers.DivideDateSpanIntoSegmentsWithIncompleteFinalSegmentAddedToFinalSegment(
            new DateOnly(1990, 01, 01),
            new DateOnly(1990, 12, 31),
            14
        );

        Assert.HasCount(26, segments);


        // The first segment should start on the start date
        Assert.AreEqual(new DateOnly(1990, 01, 01), segments[0].Start);
        Assert.AreEqual(new DateOnly(1990, 01, 14), segments[0].End);

        for (int i = 0; i < segments.Length - 1; i++)
        {
            // Every segment (except the last one) should end 13 days after it started
            Assert.AreEqual(segments[i].Start.AddDays(13), segments[i].End);
        }

        for (int i = 1; i < segments.Length; i++)
        {
            // Every segment (except the first one) should start the day after its predecessor ended
            Assert.AreEqual(segments[i - 1].End.AddDays(1), segments[i].Start);

            // Every segment (except the first one) should start 14 days after its predecessor started
            Assert.AreEqual(segments[i - 1].Start.AddDays(14), segments[i].Start);
        }

        // The last segment should end on the last day of the year
        Assert.AreEqual(new DateOnly(1990, 12, 31), segments[25].End);
    }

    [TestMethod]
    public void DecimalDateTest()
    {
        // These data are from co2_daily_mlo.csv so we can assume that the decimal column is correct.
        // Cross-check the decimal column with the year, month and day fields of the first 3 columns.
        var data = @"1974,12,29,1974.9932
1974,12,30,1974.9959
1974,12,31,1974.9986
1975,1,1,1975.0014
1975,1,2,1975.0041
1975,1,3,1975.0068
1994,8,19,1994.6315
1994,8,20,1994.6342
2004,12,24,2004.9795
2023,4,11,2023.2753";

        var rows = data.Split("\r\n");
        foreach (var row in rows)
        {
            var fields = row.Split(',');

            var result = DateHelpers.ConvertDecimalDate(double.Parse(fields[3]));
            Assert.AreEqual(new DateOnly(int.Parse(fields[0]), int.Parse(fields[1]), int.Parse(fields[2])), result);
        }
    }
}
