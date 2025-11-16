using ClimateExplorer.Core.DataPreparation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class BinIdentifierTests
{
    [TestMethod]
    public void YearAndMonthRange()
    {
        var start = new YearAndMonthBinIdentifier(1920, 07);
        var end = new YearAndMonthBinIdentifier(1921, 06);

        var range = start.EnumerateYearAndMonthBinRangeUpTo(end).ToArray();

        Assert.HasCount(12, range);
        Assert.AreEqual("y1920m07", range.First().Id);
        Assert.AreEqual("y1921m06", range.Last().Id);
    }

    [TestMethod]
    public void YearAndDayRange()
    {
        var start = new YearAndDayBinIdentifier(1920, 07, 1);
        var end = new YearAndDayBinIdentifier(1921, 06, 30);

        var range = start.EnumerateYearAndDayBinRangeUpTo(end).ToArray();

        Assert.HasCount(365, range);
        Assert.AreEqual("y1920m07d01", range.First().Id);
        Assert.AreEqual("y1921m06d30", range.Last().Id);
    }

    [TestMethod]
    public void YearAndWeekRange()
    {
        var start = new YearAndWeekBinIdentifier(1920, 6);
        var end = new YearAndWeekBinIdentifier(1921, 12);

        var range = start.EnumerateYearAndWeekBinRangeUpTo(end).ToArray();

        Assert.HasCount(59, range);
        Assert.AreEqual("y1920w06", range.First().Id);
        Assert.AreEqual("y1921w12", range.Last().Id);
    }
}
