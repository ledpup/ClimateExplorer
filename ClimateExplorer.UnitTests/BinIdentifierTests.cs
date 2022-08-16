using ClimateExplorer.Core.DataPreparation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        Assert.AreEqual(12, range.Length);
        Assert.AreEqual("y1920m07", range.First().Id);
        Assert.AreEqual("y1921m06", range.Last().Id);
    }
}
