using ClimateExplorer.Core.Stats.Smoothing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

/// <summary>
/// Where the window sits for each point. Every case uses data at indexes 0 to 9 unless stated.
/// </summary>
[TestClass]
public class BoundaryAdjustedWindowTests
{
    [TestMethod]
    public void GetStart_PointInMiddle_WindowIsCentred()
    {
        // Point 5, window 3: [4 5 6]
        Assert.AreEqual(4, BoundaryAdjustedWindow.GetStart(index: 5, windowSize: 3, firstIndex: 0, lastIndex: 9));
    }

    [TestMethod]
    public void GetStart_FirstPoint_WindowStartsAtFirstValue()
    {
        // Point 0, window 7: [0 1 2 3 4 5 6]
        Assert.AreEqual(0, BoundaryAdjustedWindow.GetStart(index: 0, windowSize: 7, firstIndex: 0, lastIndex: 9));
    }

    [TestMethod]
    public void GetStart_PointWithinHalfWindowOfStart_WindowStartsAtFirstValue()
    {
        // Point 2, window 7: centring would start at -1, so the window moves forward to [0 .. 6]
        Assert.AreEqual(0, BoundaryAdjustedWindow.GetStart(index: 2, windowSize: 7, firstIndex: 0, lastIndex: 9));
    }

    [TestMethod]
    public void GetStart_FirstPointWithFullCentredWindow_WindowIsCentred()
    {
        // Point 3, window 7: [0 .. 6], 3 either side
        Assert.AreEqual(0, BoundaryAdjustedWindow.GetStart(index: 3, windowSize: 7, firstIndex: 0, lastIndex: 9));
    }

    [TestMethod]
    public void GetStart_LastPoint_WindowEndsAtLastValue()
    {
        // Point 9, window 7: [3 .. 9]
        Assert.AreEqual(3, BoundaryAdjustedWindow.GetStart(index: 9, windowSize: 7, firstIndex: 0, lastIndex: 9));
    }

    [TestMethod]
    public void GetStart_EvenWindowInMiddle_HasExtraSlotBeforePoint()
    {
        // Point 5, window 4: [3 4 5 6], two before and one after, the same as the centred moving average
        Assert.AreEqual(3, BoundaryAdjustedWindow.GetStart(index: 5, windowSize: 4, firstIndex: 0, lastIndex: 9));
    }

    [TestMethod]
    public void GetStart_DataStartsAfterPadding_WindowStartsAtFirstValue()
    {
        // Data at indexes 2 to 6. Point 2, window 3: [2 3 4], not [1 2 3]
        Assert.AreEqual(2, BoundaryAdjustedWindow.GetStart(index: 2, windowSize: 3, firstIndex: 2, lastIndex: 6));
    }

    [TestMethod]
    public void GetStart_WindowSameSizeAsData_WindowCoversAllData()
    {
        // Every point in data 0 to 9 with window 10 uses [0 .. 9]
        Assert.AreEqual(0, BoundaryAdjustedWindow.GetStart(index: 0, windowSize: 10, firstIndex: 0, lastIndex: 9));
        Assert.AreEqual(0, BoundaryAdjustedWindow.GetStart(index: 5, windowSize: 10, firstIndex: 0, lastIndex: 9));
        Assert.AreEqual(0, BoundaryAdjustedWindow.GetStart(index: 9, windowSize: 10, firstIndex: 0, lastIndex: 9));
    }
}
