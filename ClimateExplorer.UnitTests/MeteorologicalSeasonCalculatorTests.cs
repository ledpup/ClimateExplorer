namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Core.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MeteorologicalSeasonCalculatorTests
{
    [TestMethod]
    public void GetHemisphereUsesLatitude()
    {
        Assert.AreEqual(MeteorologicalHemisphere.Northern, MeteorologicalSeasonCalculator.GetHemisphere(51.5d));
        Assert.AreEqual(MeteorologicalHemisphere.Southern, MeteorologicalSeasonCalculator.GetHemisphere(-35.3d));
        Assert.AreEqual(MeteorologicalHemisphere.Northern, MeteorologicalSeasonCalculator.GetHemisphere(0d));
    }

    [TestMethod]
    public void GetCurrentSeasonUsesHemisphereSpecificSeasonNames()
    {
        var date = new DateOnly(2026, 1, 15);

        var northernSeason = MeteorologicalSeasonCalculator.GetCurrentSeason(date, 40.7d);
        var southernSeason = MeteorologicalSeasonCalculator.GetCurrentSeason(date, -33.9d);

        Assert.AreEqual(MeteorologicalSeason.Winter, northernSeason.Season);
        Assert.AreEqual(MeteorologicalSeason.Summer, southernSeason.Season);
        Assert.AreEqual(new DateOnly(2025, 12, 1), northernSeason.StartDate);
        Assert.AreEqual(new DateOnly(2026, 2, 28), northernSeason.EndDate);
        Assert.AreEqual(northernSeason.StartDate, southernSeason.StartDate);
        Assert.AreEqual(northernSeason.EndDate, southernSeason.EndDate);
    }

    [TestMethod]
    public void GetCurrentSeasonHandlesDecFebCrossYearSeason()
    {
        var season = MeteorologicalSeasonCalculator.GetCurrentSeason(new DateOnly(2024, 1, 15), -33.9d);

        Assert.AreEqual(MeteorologicalSeason.Summer, season.Season);
        Assert.AreEqual(new DateOnly(2023, 12, 1), season.StartDate);
        Assert.AreEqual(new DateOnly(2024, 2, 29), season.EndDate);
        Assert.IsTrue(season.SpansCalendarYears);
        Assert.AreEqual("Summer 2023-24", MeteorologicalSeasonCalculator.FormatTitle(season, toDate: false));
    }

    [TestMethod]
    public void GetCurrentSeasonToDateUsesDateAsEndDate()
    {
        var season = MeteorologicalSeasonCalculator.GetCurrentSeasonToDate(new DateOnly(2026, 6, 14), -35.3d);

        Assert.AreEqual(MeteorologicalSeason.Winter, season.Season);
        Assert.AreEqual(new DateOnly(2026, 6, 1), season.StartDate);
        Assert.AreEqual(new DateOnly(2026, 6, 14), season.EndDate);
        Assert.AreEqual("Winter to Date", MeteorologicalSeasonCalculator.FormatTitle(season, toDate: true));
        Assert.AreEqual("Winter-to-date periods", MeteorologicalSeasonCalculator.FormatComparisonLabelPlural(season, toDate: true));
    }

    [TestMethod]
    public void GetPreviousSeasonsReturnsNewestToOldest()
    {
        var seasons = MeteorologicalSeasonCalculator.GetPreviousSeasons(new DateOnly(2026, 6, 14), -35.3d, 3);

        Assert.HasCount(3, seasons);
        Assert.AreEqual(MeteorologicalSeason.Autumn, seasons[0].Season);
        Assert.AreEqual(new DateOnly(2026, 3, 1), seasons[0].StartDate);
        Assert.AreEqual(new DateOnly(2026, 5, 31), seasons[0].EndDate);
        Assert.AreEqual("Autumn 2026", MeteorologicalSeasonCalculator.FormatTitle(seasons[0], toDate: false));

        Assert.AreEqual(MeteorologicalSeason.Summer, seasons[1].Season);
        Assert.AreEqual(new DateOnly(2025, 12, 1), seasons[1].StartDate);
        Assert.AreEqual(new DateOnly(2026, 2, 28), seasons[1].EndDate);
        Assert.AreEqual("Summer 2025-26", MeteorologicalSeasonCalculator.FormatTitle(seasons[1], toDate: false));

        Assert.AreEqual(MeteorologicalSeason.Spring, seasons[2].Season);
        Assert.AreEqual(new DateOnly(2025, 9, 1), seasons[2].StartDate);
        Assert.AreEqual(new DateOnly(2025, 11, 30), seasons[2].EndDate);
        Assert.AreEqual("Spring 2025", MeteorologicalSeasonCalculator.FormatTitle(seasons[2], toDate: false));
    }

    [TestMethod]
    public void GetPreviousSeasonsReturnsEmptyListWhenCountIsZero()
    {
        var seasons = MeteorologicalSeasonCalculator.GetPreviousSeasons(new DateOnly(2026, 6, 14), -35.3d, 0);

        Assert.HasCount(0, seasons);
    }
}
