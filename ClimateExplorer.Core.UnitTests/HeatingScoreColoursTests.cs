namespace ClimateExplorer.Core.UnitTests;

using System;
using System.Globalization;
using System.Linq;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HeatingScoreColoursTests
{
    private const double MinimumTextContrast = 4.5;

    [TestMethod]
    public void Get_EveryScore_TextMeetsWcagAaContrast()
    {
        foreach (var score in HeatingScoreColours.Scores)
        {
            var (background, text) = HeatingScoreColours.Get(score);

            var contrast = ContrastRatio(background, text);

            Assert.IsGreaterThanOrEqualTo(MinimumTextContrast, contrast, $"Score {score}: {text} on {background} is {contrast:0.00}:1");
        }
    }

    [TestMethod]
    public void NoScore_Text_MeetsWcagAaContrast()
    {
        var (background, text) = HeatingScoreColours.NoScore;

        Assert.IsGreaterThanOrEqualTo(MinimumTextContrast, ContrastRatio(background, text));
    }

    [TestMethod]
    public void Get_EveryScore_HasItsOwnBackground()
    {
        var backgrounds = HeatingScoreColours.Scores.Select(x => HeatingScoreColours.Get(x).Background).ToList();

        Assert.HasCount(backgrounds.Count, backgrounds.Distinct());
        Assert.DoesNotContain(HeatingScoreColours.NoScore.Background, backgrounds);
    }

    [TestMethod]
    public void Get_ZeroToNine_BackgroundDarkensWithEachScore()
    {
        for (short score = 1; score <= 9; score++)
        {
            var previous = RelativeLuminance(HeatingScoreColours.Get((short)(score - 1)).Background);
            var current = RelativeLuminance(HeatingScoreColours.Get(score).Background);

            Assert.IsLessThan(previous, current, $"Score {score}");
        }
    }

    [TestMethod]
    public void Get_ScoreOutsideTheRange_IsNoScore()
    {
        Assert.AreEqual(HeatingScoreColours.NoScore, HeatingScoreColours.Get(10));
    }

    private static double ContrastRatio(string first, string second)
    {
        var a = RelativeLuminance(first);
        var b = RelativeLuminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    // WCAG 2.x relative luminance of a #rrggbb colour.
    private static double RelativeLuminance(string hex)
    {
        double Channel(int offset)
        {
            var value = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(1)) + (0.7152 * Channel(3)) + (0.0722 * Channel(5));
    }
}
