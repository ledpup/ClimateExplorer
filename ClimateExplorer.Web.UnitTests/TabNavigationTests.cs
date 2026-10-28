namespace ClimateExplorer.Web.UnitTests;

using System;
using ClimateExplorer.Web.Client.Components.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TabNavigationTests
{
    private static readonly Func<int, bool> NoneDisabled = _ => false;

    [TestMethod]
    public void NextIndex_ArrowRightOnLast_WrapsToFirst()
    {
        var next = TabNavigation.NextIndex(current: 3, count: 4, NoneDisabled, "ArrowRight");

        Assert.AreEqual(0, next);
    }

    [TestMethod]
    public void NextIndex_ArrowLeftOnFirst_WrapsToLast()
    {
        var next = TabNavigation.NextIndex(current: 0, count: 4, NoneDisabled, "ArrowLeft");

        Assert.AreEqual(3, next);
    }

    [TestMethod]
    public void NextIndex_ArrowRightNextDisabled_SkipsDisabled()
    {
        var next = TabNavigation.NextIndex(current: 0, count: 4, i => i == 1, "ArrowRight");

        Assert.AreEqual(2, next);
    }

    [TestMethod]
    public void NextIndex_ArrowLeftPreviousDisabled_SkipsDisabled()
    {
        var next = TabNavigation.NextIndex(current: 2, count: 4, i => i == 1, "ArrowLeft");

        Assert.AreEqual(0, next);
    }

    [TestMethod]
    public void NextIndex_Home_ReturnsFirstEnabled()
    {
        var next = TabNavigation.NextIndex(current: 3, count: 4, i => i == 0, "Home");

        Assert.AreEqual(1, next);
    }

    [TestMethod]
    public void NextIndex_End_ReturnsLastEnabled()
    {
        var next = TabNavigation.NextIndex(current: 0, count: 4, i => i == 3, "End");

        Assert.AreEqual(2, next);
    }

    [TestMethod]
    public void NextIndex_AllOthersDisabled_ReturnsCurrent()
    {
        var next = TabNavigation.NextIndex(current: 1, count: 3, i => i != 1, "ArrowRight");

        Assert.AreEqual(1, next);
    }

    [TestMethod]
    public void NextIndex_UnhandledKey_ReturnsCurrent()
    {
        var next = TabNavigation.NextIndex(current: 1, count: 4, NoneDisabled, "Tab");

        Assert.AreEqual(1, next);
    }

    [TestMethod]
    public void IdFor_NameWithSpacesAndPunctuation_ReplacesThemWithHyphens()
    {
        var id = Tabs.IdFor("records", "Max temp (°C)");

        Assert.AreEqual("records-Max-temp---C-", id);
    }
}
