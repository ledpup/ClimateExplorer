namespace ClimateExplorer.Web.UnitTests;

using ClimateExplorer.Web.Client.CarbonSim;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CarbonSimFormatTests
{
    [TestMethod]
    [DataRow(2183.4, "2,183")]
    [DataRow(20.946, "20.9")]
    [DataRow(0.00019, "0.000190")]
    [DataRow(-3.21, "-3.21")]
    [DataRow(0.0, "0")]
    public void Number_Value_AtLeastThreeSignificantFigures(double value, string expected)
    {
        Assert.AreEqual(expected, CarbonSimFormat.Number(value));
    }

    [TestMethod]
    public void Percent_Fraction_ShownAsPercentage()
    {
        Assert.AreEqual("15.0%", CarbonSimFormat.Percent(0.15));
    }

    [TestMethod]
    [DataRow(45.04, "45.0 s")]
    [DataRow(725.0, "12 min 05 s")]
    [DataRow(11220.0, "3 h 07 min")]
    [DataRow(187200.0, "2 d 4 h")]
    public void Duration_Seconds_InLargestUsefulUnits(double seconds, string expected)
    {
        Assert.AreEqual(expected, CarbonSimFormat.Duration(seconds));
    }
}
