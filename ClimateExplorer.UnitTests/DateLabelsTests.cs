namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DateLabelsTests
{
    [TestCleanup]
    public void ResetOrder()
    {
        DateLabels.MonthFirst = false;
    }

    [TestMethod]
    public void DayMonth_DayFirstOrder_ReturnsDayThenFullMonthName()
    {
        Assert.AreEqual("16 June", DateLabels.DayMonth(new DateOnly(2026, 6, 16)));
    }

    [TestMethod]
    public void DayMonth_MonthFirstOrder_ReturnsFullMonthNameThenDay()
    {
        DateLabels.MonthFirst = true;

        Assert.AreEqual("June 16", DateLabels.DayMonth(new DateOnly(2026, 6, 16)));
    }

    [TestMethod]
    public void DayMonthYear_DayFirstOrder_ReturnsDayMonthYear()
    {
        Assert.AreEqual("16 June 2026", DateLabels.DayMonthYear(new DateOnly(2026, 6, 16)));
    }

    [TestMethod]
    public void DayMonthYear_MonthFirstOrder_ReturnsMonthDayCommaYear()
    {
        DateLabels.MonthFirst = true;

        Assert.AreEqual("June 16, 2026", DateLabels.DayMonthYear(new DateOnly(2026, 6, 16)));
    }

    [TestMethod]
    public void ShortDayMonth_DayFirstOrder_ReturnsDayThenAbbreviatedMonthName()
    {
        Assert.AreEqual("16 Jun", DateLabels.ShortDayMonth(new DateOnly(2026, 6, 16)));
    }

    [TestMethod]
    public void ShortDayMonthYear_MonthFirstOrder_ReturnsAbbreviatedMonthDayCommaYear()
    {
        DateLabels.MonthFirst = true;

        Assert.AreEqual("Jun 16, 2026", DateLabels.ShortDayMonthYear(new DateOnly(2026, 6, 16)));
    }

    [TestMethod]
    public void IsMonthFirst_MonthPartBeforeDayPart_ReturnsTrue()
    {
        Assert.IsTrue(DateLabels.IsMonthFirst(["month", "literal", "day"]));
    }

    [TestMethod]
    public void IsMonthFirst_DayPartBeforeMonthPart_ReturnsFalse()
    {
        Assert.IsFalse(DateLabels.IsMonthFirst(["day", "literal", "month"]));
    }

    [TestMethod]
    public void IsMonthFirst_NoParts_ReturnsFalse()
    {
        Assert.IsFalse(DateLabels.IsMonthFirst([]));
    }
}
