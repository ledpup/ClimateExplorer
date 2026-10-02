using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Stats.Smoothing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

/// <summary>
/// Behaviour every <see cref="ISeriesSmoother"/> must have. Add each new smoother to <see cref="Smoothers"/>.
/// </summary>
[TestClass]
public class SeriesSmootherContractTests
{
    public static IEnumerable<object[]> Smoothers =>
    [
        [new CentredMovingAverageCalculator()],
        [new BoundaryAdjustedMovingAverageCalculator()],
    ];

    [TestMethod]
    [DynamicData(nameof(Smoothers))]
    public void Smooth_AnyInput_OutputLengthEqualsInputLength(ISeriesSmoother smoother)
    {
        double?[] values = [1, null, 3, 4, null, null, 7, 8, 9];

        var result = smoother.Smooth(values, 3, 0.75f);

        Assert.AreEqual(values.Length, result.Length);
    }

    [TestMethod]
    [DynamicData(nameof(Smoothers))]
    public void Smooth_Window1_ReturnsInputUnchanged(ISeriesSmoother smoother)
    {
        double?[] values = [1, null, 3, 4, null];

        var result = smoother.Smooth(values, 1, 0.75f);

        CollectionAssert.AreEqual(values, result);
    }

    [TestMethod]
    [DynamicData(nameof(Smoothers))]
    public void Smooth_AllNull_GivesAllNull(ISeriesSmoother smoother)
    {
        var result = smoother.Smooth([null, null, null, null, null], 3, 0.75f);

        Assert.IsTrue(result.All(x => x == null));
    }

    [TestMethod]
    [DynamicData(nameof(Smoothers))]
    public void Smooth_ConstantSeries_ReturnsSameConstantWhereDefined(ISeriesSmoother smoother)
    {
        double?[] values = [5, 5, 5, 5, 5, 5, 5];

        var result = smoother.Smooth(values, 3, 0.75f);

        Assert.IsTrue(result.Any(x => x != null));
        Assert.IsTrue(result.All(x => x == null || x == 5));
    }
}
