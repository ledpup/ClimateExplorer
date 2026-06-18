namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecentObservationRecordDetectionTests
{
    [TestMethod]
    public void DetectsNewRecordAtEitherExtreme()
    {
        var newHigh = new RecentObservationComparisonResult { IsNewHighRecord = true, HighRank = 1 };
        var newLow = new RecentObservationComparisonResult { IsNewLowRecord = true, LowRank = 1 };

        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, RecentObservationComparison.DetermineRecordStatus(newHigh));
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, RecentObservationComparison.DetermineRecordStatus(newLow));
    }

    [TestMethod]
    public void DetectsEqualRecordOnlyWhenTiedAtAnExtreme()
    {
        var tiedTop = new RecentObservationComparisonResult { IsTiedHighRecord = true, HighRank = 1 };
        var tiedBottom = new RecentObservationComparisonResult { IsTiedLowRecord = true, LowRank = 1 };
        var tiedMidway = new RecentObservationComparisonResult { IsTiedHighRecord = true, HighRank = 3 };

        Assert.AreEqual(RecentObservationRecordStatus.EqualRecord, RecentObservationComparison.DetermineRecordStatus(tiedTop));
        Assert.AreEqual(RecentObservationRecordStatus.EqualRecord, RecentObservationComparison.DetermineRecordStatus(tiedBottom));
        Assert.AreEqual(RecentObservationRecordStatus.None, RecentObservationComparison.DetermineRecordStatus(tiedMidway));
    }

    [TestMethod]
    public void ValuesBetweenExtremesAreNotRecords()
    {
        var inBetween = new RecentObservationComparisonResult { HighRank = 4, LowRank = 7 };

        Assert.AreEqual(RecentObservationRecordStatus.None, RecentObservationComparison.DetermineRecordStatus(inBetween));
    }

    [TestMethod]
    public void IntegratesWithRankForANewHighRecord()
    {
        var ranking = RecentObservationComparison.Rank(100d, [1d, 2d, 3d, 4d, 5d]);

        Assert.IsNotNull(ranking);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, RecentObservationComparison.DetermineRecordStatus(ranking));
    }

    [TestMethod]
    public void IntegratesWithRankForANewLowRecord()
    {
        var ranking = RecentObservationComparison.Rank(-5d, [1d, 2d, 3d, 4d, 5d]);

        Assert.IsNotNull(ranking);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, RecentObservationComparison.DetermineRecordStatus(ranking));
    }

    [TestMethod]
    public void IntegratesWithRankForAnInBetweenValue()
    {
        var ranking = RecentObservationComparison.Rank(3d, [1d, 2d, 3d, 4d, 5d]);

        Assert.IsNotNull(ranking);
        Assert.AreEqual(RecentObservationRecordStatus.None, RecentObservationComparison.DetermineRecordStatus(ranking));
    }
}
