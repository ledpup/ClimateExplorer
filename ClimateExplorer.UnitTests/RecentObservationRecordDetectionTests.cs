namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Calculators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecentObservationRecordDetectionTests
{
    [TestMethod]
    public void HighDirectionDetectsNewRecord()
    {
        var ranking = new RecentObservationComparisonResult { IsNewHighRecord = true, HighRank = 1 };

        Assert.AreEqual(
            RecentObservationRecordStatus.NewRecord,
            RecentObservationComparison.DetermineRecordStatus(ranking, RecentObservationRecordDirection.High));
    }

    [TestMethod]
    public void HighDirectionDetectsEqualRecordOnlyAtRankOne()
    {
        var tiedTop = new RecentObservationComparisonResult { IsTiedHighRecord = true, HighRank = 1 };
        var tiedLower = new RecentObservationComparisonResult { IsTiedHighRecord = true, HighRank = 3 };

        Assert.AreEqual(
            RecentObservationRecordStatus.EqualRecord,
            RecentObservationComparison.DetermineRecordStatus(tiedTop, RecentObservationRecordDirection.High));
        Assert.AreEqual(
            RecentObservationRecordStatus.BelowRecord,
            RecentObservationComparison.DetermineRecordStatus(tiedLower, RecentObservationRecordDirection.High));
    }

    [TestMethod]
    public void HighDirectionDetectsBelowRecord()
    {
        var ranking = new RecentObservationComparisonResult { HighRank = 4 };

        Assert.AreEqual(
            RecentObservationRecordStatus.BelowRecord,
            RecentObservationComparison.DetermineRecordStatus(ranking, RecentObservationRecordDirection.High));
    }

    [TestMethod]
    public void LowDirectionDetectsNewAndEqualRecords()
    {
        var newLow = new RecentObservationComparisonResult { IsNewLowRecord = true, LowRank = 1 };
        var equalLow = new RecentObservationComparisonResult { IsTiedLowRecord = true, LowRank = 1 };
        var belowLow = new RecentObservationComparisonResult { LowRank = 6 };

        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, RecentObservationComparison.DetermineRecordStatus(newLow, RecentObservationRecordDirection.Low));
        Assert.AreEqual(RecentObservationRecordStatus.EqualRecord, RecentObservationComparison.DetermineRecordStatus(equalLow, RecentObservationRecordDirection.Low));
        Assert.AreEqual(RecentObservationRecordStatus.BelowRecord, RecentObservationComparison.DetermineRecordStatus(belowLow, RecentObservationRecordDirection.Low));
    }

    [TestMethod]
    public void IntegratesWithRankForANewHighRecord()
    {
        var ranking = RecentObservationComparison.Rank(100d, [1d, 2d, 3d, 4d, 5d]);

        Assert.IsNotNull(ranking);
        Assert.AreEqual(
            RecentObservationRecordStatus.NewRecord,
            RecentObservationComparison.DetermineRecordStatus(ranking, RecentObservationRecordDirection.High));
    }

    [TestMethod]
    public void IntegratesWithRankForANewLowRecord()
    {
        var ranking = RecentObservationComparison.Rank(-5d, [1d, 2d, 3d, 4d, 5d]);

        Assert.IsNotNull(ranking);
        Assert.AreEqual(
            RecentObservationRecordStatus.NewRecord,
            RecentObservationComparison.DetermineRecordStatus(ranking, RecentObservationRecordDirection.Low));
    }
}
