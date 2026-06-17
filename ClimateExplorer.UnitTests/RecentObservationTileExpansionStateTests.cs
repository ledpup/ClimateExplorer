namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecentObservationTileExpansionStateTests
{
    private static readonly IReadOnlyList<RecentObservationMetricGroupViewModel> Groups =
    [
        new RecentObservationMetricGroupViewModel { Key = "period", Title = "Period" },
        new RecentObservationMetricGroupViewModel { Key = "daily-extremes", Title = "Daily Extremes" },
    ];

    [TestMethod]
    public void IsCollapsedByDefault()
    {
        var state = new RecentObservationTileExpansionState();

        Assert.IsFalse(state.IsExpanded);
        Assert.IsNull(state.SelectedGroupKey);
    }

    [TestMethod]
    public void ToggleExpandsAndCollapsesIndependently()
    {
        var state = new RecentObservationTileExpansionState();

        state.Toggle();
        Assert.IsTrue(state.IsExpanded);

        state.Toggle();
        Assert.IsFalse(state.IsExpanded);
    }

    [TestMethod]
    public void EnsureSelectionDefaultsToFirstGroup()
    {
        var state = new RecentObservationTileExpansionState();

        state.EnsureSelection(Groups);

        Assert.AreEqual("period", state.SelectedGroupKey);
        Assert.IsTrue(state.IsGroupSelected("period"));
        Assert.IsFalse(state.IsGroupSelected("daily-extremes"));
    }

    [TestMethod]
    public void EnsureSelectionPreservesAValidExistingSelection()
    {
        var state = new RecentObservationTileExpansionState();
        state.EnsureSelection(Groups);
        state.SelectGroup("daily-extremes");

        state.EnsureSelection(Groups);

        Assert.AreEqual("daily-extremes", state.SelectedGroupKey);
    }

    [TestMethod]
    public void EnsureSelectionResetsAStaleSelection()
    {
        var state = new RecentObservationTileExpansionState();
        state.SelectGroup("no-longer-present");

        state.EnsureSelection(Groups);

        Assert.AreEqual("period", state.SelectedGroupKey);
    }

    [TestMethod]
    public void EnsureSelectionClearsWhenNoGroups()
    {
        var state = new RecentObservationTileExpansionState();
        state.SelectGroup("period");

        state.EnsureSelection([]);

        Assert.IsNull(state.SelectedGroupKey);
    }

    [TestMethod]
    public void ToggleStateSurvivesGroupSelectionChanges()
    {
        var state = new RecentObservationTileExpansionState();
        state.EnsureSelection(Groups);
        state.Toggle();

        state.SelectGroup("daily-extremes");

        Assert.IsTrue(state.IsExpanded);
        Assert.IsTrue(state.IsGroupSelected("daily-extremes"));
    }
}
