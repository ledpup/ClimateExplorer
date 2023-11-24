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

        Assert.AreEqual(2, segments.Length);
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

        Assert.AreEqual(1, segments.Length);
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

        Assert.AreEqual(26, segments.Length);


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
}
