namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Extenders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class AcornSatRecordExtenderTests
{
    private static readonly DateOnly Today20260710 = new(2026, 7, 10);

    [TestMethod]
    public void Extend_EligibleComparisonYearWithAcornCompleteThroughPriorYear_AppendsOnlyCurrentYearRange()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), day => 20.0 + (day.DayOfYear % 3));
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, day => 20.0 + (day.DayOfYear % 3));

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
        Assert.AreEqual(2025, result.ComparisonYear);
        Assert.IsTrue(result.CdoContributed);
        Assert.AreEqual(new DateOnly(2026, 1, 1), result.OverlayRecords.First().Date);
        Assert.AreEqual(Today20260710, result.OverlayRecords.Last().Date);
        Assert.IsTrue(result.OverlayRecords.All(x => x.Date!.Value.Year is 2026));
    }

    [TestMethod]
    public void Extend_AcornStaleByTwoYears_AppendsTwoCalendarYearOverlayFromSingleComparison()
    {
        // Mirrors station 046012's mean/minimum, which currently stops at 2024-12-31.
        var acorn = BuildDailySeries(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), day => 15.0 + (day.DayOfYear % 4));
        var cdo = BuildDailySeries(new DateOnly(2024, 1, 1), Today20260710, day => 15.0 + (day.DayOfYear % 4));

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "046012", "046012", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
        Assert.AreEqual(2024, result.ComparisonYear);
        Assert.AreEqual(new DateOnly(2025, 1, 1), result.OverlayRecords.First().Date);
        Assert.AreEqual(Today20260710, result.OverlayRecords.Last().Date);
        Assert.IsTrue(result.OverlayRecords.Select(x => x.Date!.Value.Year).Distinct().SequenceEqual([2025, 2026]));
    }

    [TestMethod]
    public void Extend_AcornAlreadyContainsDateInRequestYear_ProducesNoOverlay()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 5), day => 20.0);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, day => 20.0);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.AcornNotThroughPreviousYear, result.Decision);
        Assert.AreEqual(0, result.OverlayRecords.Count);
    }

    [TestMethod]
    public void Extend_AcornHasNoCompletePriorYear_ProducesNoOverlay()
    {
        // Only a partial 2026 year (no year with a 31 December date before "today"'s year).
        var acorn = BuildDailySeries(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1), day => 20.0);
        var cdo = BuildDailySeries(new DateOnly(2026, 1, 1), Today20260710, day => 20.0);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.AcornNotThroughPreviousYear, result.Decision);
        Assert.AreEqual(0, result.OverlayRecords.Count);
    }

    [TestMethod]
    public void Extend_ZeroComparisonOverlap_FailsClosedAsInsufficientData()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), day => 20.0);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => null);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.InsufficientComparisonData, result.Decision);
    }

    [TestMethod]
    public void Extend_OneSidedNonNullDates_FailsClosedAsInsufficientData()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), day => day.Month <= 6 ? 20.0 : null);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), day => day.Month > 6 ? 20.0 : null);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.InsufficientComparisonData, result.Decision);
    }

    [TestMethod]
    public void Extend_AllNullComparisonYear_FailsClosedAsInsufficientData()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => null);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => null);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.InsufficientComparisonData, result.Decision);
    }

    [TestMethod]
    public void Extend_ExactEquality_IsEligible()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, _ => 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
    }

    [TestMethod]
    public void Extend_DifferenceExactlyAtTolerance_IsAcceptedAsEligible()
    {
        // Rounds to exactly 0.1, which is not greater than the 0.1 tolerance.
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, day => day.Year == 2025 ? 18.5 : 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
    }

    [TestMethod]
    public void Extend_DifferenceAboveTolerance_IsRejectedAsAdjustmentsDetected()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, day => day.Year == 2025 ? 18.6 : 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.AdjustmentsDetected, result.Decision);
        Assert.AreEqual(0, result.OverlayRecords.Count);
    }

    [TestMethod]
    public void Extend_CurrentYearCdoNullsOmitted_OverlaySkipsThoseDates()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, day => day.Year == 2026 && day.Day == 4 ? null : 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
        Assert.IsFalse(result.OverlayRecords.Any(x => x.Date == new DateOnly(2026, 1, 4)));
    }

    [TestMethod]
    public void Extend_EmptyCurrentYearCdoContribution_LeavesOverlayEmptyButStillEligible()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
        Assert.IsFalse(result.CdoContributed);
        Assert.AreEqual(0, result.OverlayRecords.Count);
    }

    [TestMethod]
    public void Extend_NoOpenCdoStation_ReturnsNoOpenCdoStationDecision()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, null, "023000", null, Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.NoOpenCdoStation, result.Decision);
    }

    [TestMethod]
    public void Extend_NullCdoRecordsWithKnownStation_ReturnsSourceUnavailable()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);

        var result = AcornSatRecordExtender.Extend(acorn, null, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.SourceUnavailable, result.Decision);
    }

    [TestMethod]
    public void Extend_EmptyAcornSatSeries_ReturnsSourceUnavailable()
    {
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, _ => 18.4);

        var result = AcornSatRecordExtender.Extend([], cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.SourceUnavailable, result.Decision);
    }

    [TestMethod]
    public void Extend_MatchingComparisonSignature_IsStableAcrossEquivalentInputs()
    {
        var acorn1 = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var acorn2 = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, _ => 18.4);

        var result1 = AcornSatRecordExtender.Extend(acorn1, cdo, "023000", "023000", Today20260710);
        var result2 = AcornSatRecordExtender.Extend(acorn2, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(result1.ComparisonSignature, result2.ComparisonSignature);
    }

    [TestMethod]
    public void Extend_ChangedComparisonYearValue_InvalidatesSignature()
    {
        var acorn1 = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 18.4);
        var acorn2 = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), day => day.DayOfYear == 1 ? 19.0 : 18.4);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, _ => 18.4);

        var result1 = AcornSatRecordExtender.Extend(acorn1, cdo, "023000", "023000", Today20260710);
        var result2 = AcornSatRecordExtender.Extend(acorn2, cdo, "023000", "023000", Today20260710);

        Assert.AreNotEqual(result1.ComparisonSignature, result2.ComparisonSignature);
    }

    /// <summary>
    /// Mirrors location 70a07bb0-2220-402b-be83-f2c35edfdd12 (Adelaide): the CDO mapping closes 023000 in
    /// 1977, opens 023090 until 2018-06-30, then reopens 023000. Only the currently open station matters to
    /// the extender; the historical middle segment is irrelevant to eligibility.
    /// </summary>
    [TestMethod]
    public void Extend_AdelaideCloseReopenStationHistory_UsesOpenStationForComparisonAndOverlay()
    {
        var acorn = BuildDailySeries(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), _ => 21.3);
        var cdo = BuildDailySeries(new DateOnly(2025, 1, 1), Today20260710, _ => 21.3);

        var result = AcornSatRecordExtender.Extend(acorn, cdo, "023000", "023000", Today20260710);

        Assert.AreEqual(AcornSatExtensionDecision.Eligible, result.Decision);
        Assert.AreEqual("023000", result.OpenCdoStationId);
    }

    private static List<DataRecord> BuildDailySeries(DateOnly start, DateOnly end, Func<DateOnly, double?> valueSelector)
    {
        var records = new List<DataRecord>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            records.Add(new DataRecord(date, valueSelector(date)));
        }

        return records;
    }
}
