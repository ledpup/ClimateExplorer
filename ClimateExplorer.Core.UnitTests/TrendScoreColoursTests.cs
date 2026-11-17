namespace ClimateExplorer.Core.UnitTests;

using System;
using System.Globalization;
using System.Linq;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TrendScoreColoursTests
{
    private const double MinimumTextContrast = 4.5;

    [TestMethod]
    public void Get_EveryScore_TextMeetsWcagAaContrast()
    {
        foreach (var score in TrendScoreColours.Scores)
        {
            var (background, text) = TrendScoreColours.Get(score);

            var contrast = ContrastRatio(background, text);

            Assert.IsGreaterThanOrEqualTo(MinimumTextContrast, contrast, $"Score {score}: {text} on {background} is {contrast:0.00}:1");
        }
    }

    [TestMethod]
    public void NoScore_Text_MeetsWcagAaContrast()
    {
        var (background, text) = TrendScoreColours.NoScore;

        Assert.IsGreaterThanOrEqualTo(MinimumTextContrast, ContrastRatio(background, text));
    }

    [TestMethod]
    public void Get_EveryScore_HasItsOwnBackground()
    {
        var backgrounds = TrendScoreColours.Scores.Select(x => TrendScoreColours.Get(x).Background).ToList();

        Assert.HasCount(backgrounds.Count, backgrounds.Distinct());
        Assert.DoesNotContain(TrendScoreColours.NoScore.Background, backgrounds);
    }

    // A difference of about 0.02 in OKLab is just noticeable side by side. Map markers are small and seen
    // apart, so every pair of scores must be several times that.
    [TestMethod]
    public void Get_AnyTwoScores_BackgroundsAreEasyToTellApart()
    {
        const double minimumDifference = 0.09;
        var scores = TrendScoreColours.Scores;

        for (var i = 0; i < scores.Count; i++)
        {
            for (var j = i + 1; j < scores.Count; j++)
            {
                var difference = OkLabDifference(TrendScoreColours.Get(scores[i]).Background, TrendScoreColours.Get(scores[j]).Background);

                Assert.IsGreaterThanOrEqualTo(minimumDifference, difference, $"Scores {scores[i]} and {scores[j]} are {difference:0.000} apart");
            }
        }
    }

    [TestMethod]
    public void Get_ScoreOutsideTheRange_IsNoScore()
    {
        Assert.AreEqual(TrendScoreColours.NoScore, TrendScoreColours.Get(10));
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
        var (r, g, b) = LinearRgb(hex);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    // Euclidean distance between two colours in OKLab, a perceptually uniform colour space.
    private static double OkLabDifference(string first, string second)
    {
        var (l1, a1, b1) = OkLab(first);
        var (l2, a2, b2) = OkLab(second);
        return Math.Sqrt(Math.Pow(l1 - l2, 2) + Math.Pow(a1 - a2, 2) + Math.Pow(b1 - b2, 2));
    }

    private static (double L, double A, double B) OkLab(string hex)
    {
        var (r, g, b) = LinearRgb(hex);
        var l = Math.Cbrt((0.4122214708 * r) + (0.5363325363 * g) + (0.0514459929 * b));
        var m = Math.Cbrt((0.2119034982 * r) + (0.6806995451 * g) + (0.1073969566 * b));
        var s = Math.Cbrt((0.0883024619 * r) + (0.2817188376 * g) + (0.6299787005 * b));
        return (
            (0.2104542553 * l) + (0.7936177850 * m) - (0.0040720468 * s),
            (1.9779984951 * l) - (2.4285922050 * m) + (0.4505937099 * s),
            (0.0259040371 * l) + (0.7827717662 * m) - (0.8086757660 * s));
    }

    private static (double R, double G, double B) LinearRgb(string hex)
    {
        double Channel(int offset)
        {
            var value = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (Channel(1), Channel(3), Channel(5));
    }
}
