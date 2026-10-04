namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DateLabelsTests
{
    private static readonly DateOnly Date = new(2026, 6, 16);

    [TestMethod]
    public void DayMonth_DayFirstOrder_ReturnsDayThenFullMonthName()
    {
        Assert.AreEqual("16 June", new DateLabels().DayMonth(Date));
    }

    [TestMethod]
    public void DayMonth_MonthFirstOrder_ReturnsFullMonthNameThenDay()
    {
        Assert.AreEqual("June 16", new DateLabels { MonthFirst = true }.DayMonth(Date));
    }

    [TestMethod]
    public void DayMonthYear_DayFirstOrder_ReturnsDayMonthYear()
    {
        Assert.AreEqual("16 June 2026", new DateLabels().DayMonthYear(Date));
    }

    [TestMethod]
    public void DayMonthYear_MonthFirstOrder_ReturnsMonthDayCommaYear()
    {
        Assert.AreEqual("June 16, 2026", new DateLabels { MonthFirst = true }.DayMonthYear(Date));
    }

    [TestMethod]
    public void ShortDayMonth_DayFirstOrder_ReturnsDayThenAbbreviatedMonthName()
    {
        Assert.AreEqual("16 Jun", new DateLabels().ShortDayMonth(Date));
    }

    [TestMethod]
    public void ShortDayMonthYear_MonthFirstOrder_ReturnsAbbreviatedMonthDayCommaYear()
    {
        Assert.AreEqual("Jun 16, 2026", new DateLabels { MonthFirst = true }.ShortDayMonthYear(Date));
    }

    [TestMethod]
    [DataRow("en-US", true)]
    [DataRow("en-us", true)]
    [DataRow("en-CA", true)]
    [DataRow("en", true)]
    [DataRow("en-Latn-US", true)]
    [DataRow("ja-JP", true)]
    [DataRow("zh", true)]
    [DataRow("en-AU", false)]
    [DataRow("en-GB", false)]
    [DataRow("fr-CA", false)]
    [DataRow("es-US", false)]
    [DataRow("de", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsMonthFirstLocale_LanguageTag_ReturnsWhetherLocaleWritesMonthFirst(string? languageTag, bool expected)
    {
        Assert.AreEqual(expected, DateLabels.IsMonthFirstLocale(languageTag));
    }

    [TestMethod]
    [DataRow("en-US,en;q=0.9", true)]
    [DataRow("en-AU,en-US;q=0.9,en;q=0.8", false)]
    [DataRow("en-GB;q=0.8, en-US", true)]
    [DataRow("*, en-US;q=0.5", true)]
    [DataRow("en-US;q=0, en-AU;q=0.1", false)]
    [DataRow("en-AU;q=abc, en-US;q=0.5", true)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsMonthFirstAcceptLanguage_Header_UsesMostPreferredLanguage(string? header, bool expected)
    {
        Assert.AreEqual(expected, DateLabels.IsMonthFirstAcceptLanguage(header));
    }
}
