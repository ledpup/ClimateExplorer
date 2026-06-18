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

    [TestMethod]
    public void ExpandAllExpandsOnlyExpandableTiles()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        var nonExpandableState = states.GetOrAdd("summary");

        states.ExpandAll(
        [
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("summary", false),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        ]);

        Assert.IsTrue(states.GetOrAdd("latest-day").IsExpanded);
        Assert.IsTrue(states.GetOrAdd("month-to-date").IsExpanded);
        Assert.IsFalse(nonExpandableState.IsExpanded);
    }

    [TestMethod]
    public void CollapseAllCollapsesOnlyExpandableTiles()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        states.GetOrAdd("latest-day").Expand();
        states.GetOrAdd("summary").Expand();
        states.GetOrAdd("month-to-date").Expand();

        states.CollapseAll(
        [
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("summary", false),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        ]);

        Assert.IsFalse(states.GetOrAdd("latest-day").IsExpanded);
        Assert.IsFalse(states.GetOrAdd("month-to-date").IsExpanded);
        Assert.IsTrue(states.GetOrAdd("summary").IsExpanded);
    }

    [TestMethod]
    public void ToggleAllLabelIsExpandAllWhenAnyExpandableTileIsCollapsed()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        var tiles = new[]
        {
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        };

        states.GetOrAdd("latest-day").Expand();

        Assert.AreEqual("Expand all", states.CreateToggleAllLabel(tiles));
    }

    [TestMethod]
    public void ToggleAllLabelIsCollapseAllWhenAllExpandableTilesAreExpanded()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        var tiles = new[]
        {
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("summary", false),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        };

        states.ExpandAll(tiles);

        Assert.AreEqual("Collapse all", states.CreateToggleAllLabel(tiles));
    }

    [TestMethod]
    public void IndividualToggleChangesToggleAllLabel()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        var tiles = new[]
        {
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        };
        states.ExpandAll(tiles);

        states.GetOrAdd("month-to-date").Toggle();

        Assert.AreEqual("Expand all", states.CreateToggleAllLabel(tiles));
    }

    [TestMethod]
    public void ToggleAllExpandsWhenAnyExpandableTileIsCollapsed()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        var tiles = new[]
        {
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        };
        states.GetOrAdd("latest-day").Expand();

        states.ToggleAll(tiles);

        Assert.IsTrue(states.GetOrAdd("latest-day").IsExpanded);
        Assert.IsTrue(states.GetOrAdd("month-to-date").IsExpanded);
    }

    [TestMethod]
    public void ToggleAllCollapsesWhenAllExpandableTilesAreExpanded()
    {
        var states = new RecentObservationTileExpansionStateCollection();
        var tiles = new[]
        {
            new RecentObservationTileExpansionTarget("latest-day", true),
            new RecentObservationTileExpansionTarget("month-to-date", true),
        };
        states.ExpandAll(tiles);

        states.ToggleAll(tiles);

        Assert.IsFalse(states.GetOrAdd("latest-day").IsExpanded);
        Assert.IsFalse(states.GetOrAdd("month-to-date").IsExpanded);
    }
}
