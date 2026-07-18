namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class DataSetFreshnessPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
    private readonly DataSetFreshnessPolicy policy = new(new FixedTimeProvider(Now));

    [TestMethod]
    public void IsFresh_NullCachedData_ReturnsFalse()
    {
        Assert.IsFalse(policy.IsFresh(null, DataResolution.Daily));
    }

    [TestMethod]
    public void IsFresh_NullRetrievedDate_ReturnsFalse()
    {
        Assert.IsFalse(policy.IsFresh(CreateState(null), DataResolution.Daily));
    }

    [TestMethod]
    public void IsFresh_DailyDataYoungerThan24Hours_ReturnsTrue()
    {
        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddHours(-24).AddTicks(1)), DataResolution.Daily));
    }

    [TestMethod]
    public void IsFresh_DailyDataExactly24HoursOld_ReturnsFalse()
    {
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddHours(-24)), DataResolution.Daily));
    }

    [TestMethod]
    public void IsFresh_MonthlyDataYoungerThan28Days_ReturnsTrue()
    {
        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddDays(-28).AddTicks(1)), DataResolution.Monthly));
    }

    [TestMethod]
    public void IsFresh_MonthlyDataExactly28DaysOld_ReturnsFalse()
    {
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddDays(-28)), DataResolution.Monthly));
    }

    [TestMethod]
    public void IsFresh_YearlyDataYoungerThan28Days_ReturnsTrue()
    {
        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddDays(-28).AddTicks(1)), DataResolution.Yearly));
    }

    [TestMethod]
    public void IsFresh_YearlyDataExactly28DaysOld_ReturnsFalse()
    {
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddDays(-28)), DataResolution.Yearly));
    }

    [TestMethod]
    public void IsFresh_RetrievedDateWithNonUtcOffset_ComparesAbsoluteTime()
    {
        var retrievedDate = Now.AddHours(-23).ToOffset(TimeSpan.FromHours(10));

        Assert.IsTrue(policy.IsFresh(CreateState(retrievedDate), DataResolution.Daily));
    }

    [TestMethod]
    public void IsFresh_FutureRetrievedDate_ReturnsFalse()
    {
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddMinutes(1)), DataResolution.Daily));
    }

    [TestMethod]
    public void IsFresh_UnsupportedResolution_ThrowsNotSupportedException()
    {
        Assert.ThrowsExactly<NotSupportedException>(
            () => policy.IsFresh(CreateState(Now), DataResolution.Weekly));
    }

    [TestMethod]
    public void IsFresh_BomOrGhcndStationWithYesterdaysRecord_IsFreshRegardlessOfRetrievedDateAge()
    {
        var state = CreateState(Now.AddDays(-2), latestRecordDate: DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1));

        Assert.IsTrue(policy.IsFresh(state, DataResolution.Daily, "bom-station"));
        Assert.IsTrue(policy.IsFresh(state, DataResolution.Daily, "ghcnd-station"));
    }

    [TestMethod]
    public void IsFresh_BomOrGhcndStationMissingYesterdaysRecord_IsFreshOnlyWithinSixHoursOfRetrieval()
    {
        var staleRecordDate = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-2);

        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddHours(-5).AddTicks(1), staleRecordDate), DataResolution.Daily, "bom-station"));
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddHours(-6), staleRecordDate), DataResolution.Daily, "bom-station"));
    }

    [TestMethod]
    public void IsFresh_BomOrGhcndStationWithFutureLatestRecordDate_TreatedAsMissingAndFallsBackToRetryWindow()
    {
        var futureRecordDate = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(1);

        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddHours(-1), futureRecordDate), DataResolution.Daily, "bom-station"));
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddHours(-7), futureRecordDate), DataResolution.Daily, "bom-station"));
    }

    [TestMethod]
    public void IsFresh_OtherDownloaderKeyWithDailyResolution_StillUsesTwentyFourHourWindow()
    {
        var recentRecordDate = DateOnly.FromDateTime(Now.UtcDateTime);

        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddHours(-23), recentRecordDate), DataResolution.Daily, "direct-http"));
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddHours(-25), recentRecordDate), DataResolution.Daily, "direct-http"));
    }

    [TestMethod]
    public void IsFresh_NonDailyResolutionForBomOrGhcnd_StillUsesTimeBasedWindow()
    {
        var recentRecordDate = DateOnly.FromDateTime(Now.UtcDateTime);

        Assert.IsTrue(policy.IsFresh(CreateState(Now.AddDays(-27), recentRecordDate), DataResolution.Monthly, "bom-station"));
        Assert.IsFalse(policy.IsFresh(CreateState(Now.AddDays(-29), recentRecordDate), DataResolution.Monthly, "bom-station"));
    }

    [TestMethod]
    public void OldestFor_MultipleContributingSources_ReturnsOldestRetrievalDate()
    {
        var oldest = Now.AddHours(-5);

        var result = DataSetRetrievalDate.OldestFor([CreateState(Now), CreateState(oldest), CreateState(Now.AddHours(-1))]);

        Assert.AreEqual(oldest, result);
    }

    [TestMethod]
    public void OldestFor_ContributingSourceWithoutSuccessfulRetrieval_ReturnsNull()
    {
        var result = DataSetRetrievalDate.OldestFor([CreateState(Now), CreateState(null)]);

        Assert.IsNull(result);
    }

    private static DataSetSourceState CreateState(DateTimeOffset? retrievedDate, DateOnly? latestRecordDate = null)
    {
        return new DataSetSourceState
        {
            AssetKey = "asset",
            RelativePath = "Dataset/source.txt",
            Length = 1,
            Sha256 = "hash",
            RetrievedDate = retrievedDate,
            LatestRecordDate = latestRecordDate,
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
