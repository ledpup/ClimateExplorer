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
    [DataRow(0.78084, "78.1%")]
    [DataRow(0.15, "15.0%")]
    [DataRow(0.0117, "1.2%")]
    [DataRow(0.001, "0.1%")]
    [DataRow(0.000999, "999 ppm")]
    [DataRow(0.00042, "420 ppm")]
    [DataRow(0.00004235, "42 ppm")]
    [DataRow(0.00000999, "10 ppm")]
    [DataRow(0.0000019, "1.9 ppm")]
    [DataRow(0.00000001, "< 0.1 ppm")]
    [DataRow(0.0, "0 ppm")]
    public void Share_Fraction_PercentFromOneTenthOfAPercentElsePpm(double fraction, string expected)
    {
        Assert.AreEqual(expected, CarbonSimFormat.Share(fraction));
    }

    [TestMethod]
    [DataRow(-0.0043, "-0.4 pp")]
    [DataRow(0.0012, "+0.1 pp")]
    [DataRow(0.000035, "+35 ppm")]
    [DataRow(-0.0000004, "-0.4 ppm")]
    [DataRow(0.00000001, "0")]
    [DataRow(0.0, "0")]
    public void ShareChange_Change_SignedInPointsOrPpm(double change, string expected)
    {
        Assert.AreEqual(expected, CarbonSimFormat.ShareChange(change));
    }

    [TestMethod]
    [DataRow(45.04, "45 s")]
    [DataRow(725.0, "12 min 05 s")]
    [DataRow(11220.0, "3 h 07 min")]
    [DataRow(187200.0, "2 d 4 h")]
    public void Duration_Seconds_InLargestUsefulUnits(double seconds, string expected)
    {
        Assert.AreEqual(expected, CarbonSimFormat.Duration(seconds));
    }
}
