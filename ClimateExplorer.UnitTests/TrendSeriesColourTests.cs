namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TrendSeriesColourTests
{
    [TestMethod]
    public void Derive_TierZero_ReturnsTheParentColourUnchanged()
    {
        var result = TrendSeriesColour.Derive("#36A2EB", 0);

        Assert.AreEqual("#36A2EB", result);
    }

    [TestMethod]
    public void Derive_MidLightnessColour_TierOneIsDarkerThanTierZero()
    {
        var tier0 = TrendSeriesColour.Derive("#36A2EB", 0);
        var tier1 = TrendSeriesColour.Derive("#36A2EB", 1);

        Assert.IsLessThan(Lightness(tier0), Lightness(tier1));
    }

    [TestMethod]
    public void Derive_MidLightnessColour_TierTwoIsDarkerThanTierOne()
    {
        var tier1 = TrendSeriesColour.Derive("#36A2EB", 1);
        var tier2 = TrendSeriesColour.Derive("#36A2EB", 2);

        Assert.IsLessThan(Lightness(tier1), Lightness(tier2));
    }

    [TestMethod]
    public void Derive_NearBlackColour_TiersAreLighterInstead()
    {
        var tier0 = TrendSeriesColour.Derive("#000000", 0);
        var tier1 = TrendSeriesColour.Derive("#000000", 1);
        var tier2 = TrendSeriesColour.Derive("#000000", 2);

        Assert.IsGreaterThan(Lightness(tier0), Lightness(tier1));
        Assert.IsGreaterThan(Lightness(tier1), Lightness(tier2));
    }

    [TestMethod]
    public void Derive_NearBlackColour_StaysGreyRatherThanTurningAnOffHueColour()
    {
        // Black has an undefined hue - the shifted tiers must stay achromatic (R == G == B)
        // rather than drifting towards some arbitrary colour.
        var tier1 = TrendSeriesColour.Derive("#000000", 1);

        var (r, g, b) = ToRgb(tier1);
        Assert.AreEqual(r, g);
        Assert.AreEqual(g, b);
    }

    [TestMethod]
    public void Derive_HueIsPreservedAcrossTiers()
    {
        var tier0 = TrendSeriesColour.Derive("#36A2EB", 0);
        var tier1 = TrendSeriesColour.Derive("#36A2EB", 1);

        Assert.AreEqual(Hue(tier0), Hue(tier1), 0.01);
    }

    [TestMethod]
    public void Derive_IsDeterministic()
    {
        var first = TrendSeriesColour.Derive("#4DAF4A", 1);
        var second = TrendSeriesColour.Derive("#4DAF4A", 1);

        Assert.AreEqual(first, second);
    }

    private static (int R, int G, int B) ToRgb(string hex)
    {
        var value = hex.TrimStart('#');
        return (
            Convert.ToInt32(value.Substring(0, 2), 16),
            Convert.ToInt32(value.Substring(2, 2), 16),
            Convert.ToInt32(value.Substring(4, 2), 16));
    }

    private static double Lightness(string hex)
    {
        var (r, g, b) = ToRgb(hex);
        var max = Math.Max(r, Math.Max(g, b)) / 255.0;
        var min = Math.Min(r, Math.Min(g, b)) / 255.0;
        return (max + min) / 2;
    }

    private static double Hue(string hex)
    {
        var (rInt, gInt, bInt) = ToRgb(hex);
        double r = rInt / 255.0, g = gInt / 255.0, b = bInt / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));

        if (max == min)
        {
            return 0;
        }

        var delta = max - min;
        double h;
        if (max == r)
        {
            h = ((g - b) / delta) + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            h = ((b - r) / delta) + 2;
        }
        else
        {
            h = ((r - g) / delta) + 4;
        }

        return h / 6;
    }
}
