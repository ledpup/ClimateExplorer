namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TrendSeriesColourTests
{
    [TestMethod]
    public void Derive_SameParentColour_ReturnsTheSameResultEveryTime()
    {
        var first = TrendSeriesColour.Derive("#36A2EB");
        var second = TrendSeriesColour.Derive("#36A2EB");

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void Derive_SaturatedParentColour_ReturnsADifferentColour()
    {
        var derived = TrendSeriesColour.Derive("#FF2D2D");

        Assert.AreNotEqual("#FF2D2D", derived);
    }

    [TestMethod]
    public void Derive_DistinctParentColours_RemainDistinctAfterTheTransform()
    {
        var red = TrendSeriesColour.Derive("#FF2D2D");
        var blue = TrendSeriesColour.Derive("#36A2EB");

        Assert.AreNotEqual(red, blue);
    }

    [TestMethod]
    public void Derive_BlackParent_StaysAchromatic()
    {
        // Scaling saturation rather than flooring it is what keeps an undefined hue from turning
        // black into an arbitrary colour.
        var derived = TrendSeriesColour.Derive("#000000");

        Assert.AreEqual('#', derived[0]);
        Assert.AreEqual(derived.Substring(1, 2), derived.Substring(3, 2));
        Assert.AreEqual(derived.Substring(3, 2), derived.Substring(5, 2));
    }

    [TestMethod]
    public void Derive_GreyParent_StaysAchromatic()
    {
        var derived = TrendSeriesColour.Derive("#666666");

        Assert.AreEqual(derived.Substring(1, 2), derived.Substring(3, 2));
        Assert.AreEqual(derived.Substring(3, 2), derived.Substring(5, 2));
    }

    [TestMethod]
    public void Derive_DarkParent_ProducesALighterColour()
    {
        var derived = TrendSeriesColour.Derive("#000000");

        Assert.AreNotEqual("#000000", derived);
    }

    [TestMethod]
    public void Derive_EveryPaletteColour_ProducesAValidSixDigitHexCode()
    {
        string[] palette = ["#FF2D2D", "#36A2EB", "#4DAF4A", "#9966FF", "#FF9532", "#000000", "#FFCD56", "#a65628", "#f781bf", "#666666"];

        foreach (var colour in palette)
        {
            var derived = TrendSeriesColour.Derive(colour);

            Assert.AreEqual(7, derived.Length, $"Unexpected length for {colour}");
            Assert.AreEqual('#', derived[0]);
            Assert.IsTrue(int.TryParse(derived.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out _), $"Not hex for {colour}");
        }
    }

    [TestMethod]
    public void Derive_UnparseableColour_ReturnsTheInputUnchanged()
    {
        Assert.AreEqual("rgba(1,2,3,1)", TrendSeriesColour.Derive("rgba(1,2,3,1)"));
        Assert.AreEqual(string.Empty, TrendSeriesColour.Derive(string.Empty));
    }
}
