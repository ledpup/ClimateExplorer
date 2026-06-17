namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class RecentObservationsServiceTests
{
    private static readonly Guid LocationId = Guid.Parse("9f0ac026-4840-44df-8155-80c7f8d6a1d6");

    [TestMethod]
    public async Task GetPrecipitationRecordsIncludesPreviousMonthsAcrossCalendarYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);

        var previousMonths = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.PreviousMonth)
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Last month - May 2026",
                "April 2026",
                "March 2026",
                "February 2026",
                "January 2026",
                "December 2025",
                "November 2025",
                "October 2025",
                "September 2025",
                "August 2025",
                "July 2025",
            },
            previousMonths.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 11).ToArray(), previousMonths.Select(x => x.PeriodOffset!.Value).ToArray());
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsIncludesPreviousSeasonsAcrossCalendarYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);

        var previousSeasons = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.PreviousSeason)
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Autumn 2026",
                "Summer 2025-26",
                "Spring 2025",
            },
            previousSeasons.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 3).ToArray(), previousSeasons.Select(x => x.PeriodOffset!.Value).ToArray());
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsPreservesTileOrderAndKeysAcrossMultipleYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);
        var keys = result.Tiles.Select(x => $"{x.PeriodKind}:{x.PeriodStartDate:yyyy-MM-dd}:{x.PeriodEndDate:yyyy-MM-dd}:{x.PeriodTitle}").ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Yesterday",
                "12 June",
                "11 June",
                "10 June",
                "9 June",
                "8 June",
                "Latest 7 days",
                "June 2026 to date",
                "Last month - May 2026",
                "April 2026",
                "March 2026",
                "February 2026",
                "January 2026",
                "December 2025",
                "November 2025",
                "October 2025",
                "September 2025",
                "August 2025",
                "July 2025",
                "Autumn 2026",
                "Summer 2025-26",
                "Spring 2025",
                "2026 to date",
            },
            result.Tiles.Select(x => x.PeriodTitle).ToArray());
        Assert.AreEqual(keys.Count, keys.Distinct().Count());
    }

    [TestMethod]
    [DataRow(2026, 3, 14, -35.3d)]
    [DataRow(2026, 6, 14, -35.3d)]
    [DataRow(2026, 9, 14, -35.3d)]
    [DataRow(2026, 12, 14, -35.3d)]
    [DataRow(2026, 3, 14, 40.7d)]
    [DataRow(2026, 6, 14, 40.7d)]
    [DataRow(2026, 9, 14, 40.7d)]
    [DataRow(2026, 12, 14, 40.7d)]
    public async Task GetPrecipitationRecordsHidesSeasonToDateDuringFirstSeasonMonth(int year, int month, int day, double latitude)
    {
        var today = new DateOnly(year, month, day);
        var service = CreateService(recentStartDate: today.AddMonths(-4), recentEndDate: today, today: today);

        var result = await service.GetPrecipitationRecords(
            CreateLocation(latitude),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 3);

        Assert.IsFalse(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
    }

    [TestMethod]
    [DataRow(2026, 7, 14, -35.3d, "Winter to Date")]
    [DataRow(2026, 8, 14, -35.3d, "Winter to Date")]
    [DataRow(2026, 10, 14, -35.3d, "Spring to Date")]
    [DataRow(2026, 11, 14, -35.3d, "Spring to Date")]
    [DataRow(2027, 1, 14, -35.3d, "Summer to Date")]
    [DataRow(2027, 2, 14, -35.3d, "Summer to Date")]
    [DataRow(2026, 7, 14, 40.7d, "Summer to Date")]
    [DataRow(2026, 8, 14, 40.7d, "Summer to Date")]
    [DataRow(2026, 10, 14, 40.7d, "Autumn to Date")]
    [DataRow(2026, 11, 14, 40.7d, "Autumn to Date")]
    [DataRow(2027, 1, 14, 40.7d, "Winter to Date")]
    [DataRow(2027, 2, 14, 40.7d, "Winter to Date")]
    public async Task GetPrecipitationRecordsShowsSeasonToDateDuringSecondAndThirdSeasonMonths(
        int year,
        int month,
        int day,
        double latitude,
        string expectedTitle)
    {
        var today = new DateOnly(year, month, day);
        var service = CreateService(recentStartDate: today.AddMonths(-4), recentEndDate: today, today: today);

        var result = await service.GetPrecipitationRecords(
            CreateLocation(latitude),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 3);
        var currentSeason = result.Tiles.SingleOrDefault(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason);

        Assert.IsNotNull(currentSeason);
        Assert.AreEqual(expectedTitle, currentSeason.PeriodTitle);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsShowsOneDailyTileByDefault()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.DefaultPreviousDayCount,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();

        Assert.HasCount(1, dailyTiles);
        Assert.AreEqual("Today", dailyTiles[0].PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 6, 14), dailyTiles[0].PeriodStartDate);
        Assert.AreEqual(1, dailyTiles[0].PeriodOffset);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsGeneratesDayTilesNewestToOldest()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 4,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();

        CollectionAssert.AreEqual(
            new[] { "Today", "Yesterday", "12 June", "11 June" },
            dailyTiles.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 6, 14), new DateOnly(2026, 6, 13), new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 11) },
            dailyTiles.Select(x => x.PeriodStartDate).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 4).ToArray(), dailyTiles.Select(x => x.PeriodOffset!.Value).ToArray());
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsUsesDateLabelWhenLatestAvailableDayIsNotTodayOrYesterday()
    {
        var service = CreateService(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.DefaultPreviousDayCount,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);

        Assert.AreEqual("12 June", dailyTile.PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 6, 12), dailyTile.PeriodStartDate);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsLabelsLatestSevenDaysWithRecentDateRange()
    {
        var endLabel = new DateOnly(2026, 6, 14).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual("Latest 7 days", latestSevenDays.PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 6, 8), latestSevenDays.PeriodStartDate);
        Assert.AreEqual(new DateOnly(2026, 6, 14), latestSevenDays.PeriodEndDate);
        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual($"Wettest 7 days ending {endLabel}", latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsLabelsDelayedLatestSevenDaysWithActualDateRangeAndStaleNote()
    {
        var latestDate = new DateOnly(2026, 6, 2);
        var latestSevenDaysStart = new DateOnly(2026, 5, 27);
        var endLabel = latestDate.ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalRangeRecords(latestSevenDaysStart, latestDate);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 5, 1),
            recentEndDate: latestDate,
            historicalRecords: historicalRecords,
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual("Latest 7 days", latestSevenDays.PeriodTitle);
        Assert.AreEqual(latestSevenDaysStart, latestSevenDays.PeriodStartDate);
        Assert.AreEqual(latestDate, latestSevenDays.PeriodEndDate);
        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.AreEqual("Latest available data ends 2 Jun 2026", latestSevenDays.Note);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual($"Wettest 7 days ending {endLabel}", latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsComparesGeneratedDaysAgainstHistoricalSameCalendarDate()
    {
        var dayLabel = new DateOnly(2026, 6, 13).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalSameDateRecords(new DateOnly(2026, 6, 13), startYear: 2000, endYear: 2025, valueOffset: 0d);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 2,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var generatedDay = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .Single(x => x.PeriodStartDate == new DateOnly(2026, 6, 13));

        Assert.IsTrue(generatedDay.HasComparison);
        Assert.AreEqual($"Wettest {dayLabel}", generatedDay.HistoricalMaxLabel);
        Assert.AreEqual("26mm", generatedDay.HistoricalMaxValue);
        Assert.IsNull(generatedDay.HistoricalMinLabel);
    }

    [TestMethod]
    public async Task GetTemperatureRecordsGeneratedDaysKeepTemperatureStatsAndHistoricalRange()
    {
        var dayLabel = new DateOnly(2026, 6, 13).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalSameDateRecords(new DateOnly(2026, 6, 13), startYear: 2000, endYear: 2025, valueOffset: 10d);
        var service = CreateTemperatureService(historicalRecords);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 2,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var generatedDay = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .Single(x => x.PeriodStartDate == new DateOnly(2026, 6, 13));

        Assert.IsTrue(generatedDay.HasComparison);
        Assert.AreEqual("Mean temperature", generatedDay.PrimaryLabel);
        CollectionAssert.AreEqual(new[] { "Max temp", "Min temp", "Historical average", "Anomaly" }, generatedDay.Stats.Select(x => x.Label).ToArray());
        Assert.AreEqual($"Warmest {dayLabel}", generatedDay.HistoricalMaxLabel);
        Assert.AreEqual("36.0°C", generatedDay.HistoricalMaxValue);
        Assert.AreEqual($"Coolest {dayLabel}", generatedDay.HistoricalMinLabel);
        Assert.AreEqual("11.0°C", generatedDay.HistoricalMinValue);
    }

    [TestMethod]
    public async Task CompletenessThresholdAllowsComparisonsAtFullCompleteness()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(1f, latestSevenDays.Completeness);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsNotNull(latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task CompletenessThresholdAllowsComparisonsExactlyAtThreshold()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseLatestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var latestSevenDays = baseLatestSevenDays.ApplyCompletenessThreshold(baseLatestSevenDays.Completeness);

        Assert.AreEqual(5, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
        Assert.AreEqual(5f / 7f, latestSevenDays.Completeness);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsNotNull(latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task CompletenessThresholdSuppressesComparisonsBelowThreshold()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual("Comparison unavailable", latestSevenDays.Headline);
        Assert.AreEqual("Only 5 of 7 days are available (only 71% completeness).", latestSevenDays.Note);
        Assert.IsNull(latestSevenDays.HistoricalMaxLabel);
        CollectionAssert.AreEqual(new[] { "Historical average", "Anomaly" }, latestSevenDays.Stats.Select(x => x.Label).ToArray());
    }

    [TestMethod]
    public async Task CompletenessThresholdUsesMissingMinAndMaxTemperatureObservations()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), valueOffset: 10d);
        var service = CreateTemperatureService(
            historicalRecords,
            includeMaxRecord: date => date != new DateOnly(2026, 6, 9),
            includeMinRecord: date => date != new DateOnly(2026, 6, 12));

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(5, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual("Only 5 of 7 days are available (only 71% completeness).", latestSevenDays.Note);
        CollectionAssert.AreEqual(
            new[] { "Average max temp", "Average min temp", "Historical average", "Anomaly" },
            latestSevenDays.Stats.Select(x => x.Label).ToArray());
    }

    [TestMethod]
    public async Task CompletenessThresholdUsesMissingPrecipitationObservations()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(5, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual("Only 5 of 7 days are available (only 71% completeness).", latestSevenDays.Note);
    }

    [TestMethod]
    public void CompletenessThresholdConvertsUiPercentagesToInternalThresholds()
    {
        Assert.AreEqual(0.8f, RecentObservationCompletenessThreshold.FromPercentage(80));
        Assert.AreEqual(80, RecentObservationCompletenessThreshold.ToPercentage(RecentObservationCompletenessThreshold.Default));
        Assert.AreEqual(0f, RecentObservationCompletenessThreshold.FromPercentage(-10));
        Assert.AreEqual(1f, RecentObservationCompletenessThreshold.FromPercentage(120));
    }

    [TestMethod]
    public async Task CompletenessThresholdVisibilityTogglesWithoutReloadingTiles()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseLatestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var suppressed = baseLatestSevenDays.ApplyCompletenessThreshold(0.80f);
        var allowed = baseLatestSevenDays.ApplyCompletenessThreshold(0.70f);

        Assert.IsFalse(suppressed.HasComparison);
        Assert.IsTrue(allowed.HasComparison);
        Assert.IsNull(allowed.Note);
        Assert.IsNotNull(allowed.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task CompletenessThresholdAppliesToAllRangeTiles()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseRangeTiles = result.Tiles
            .Where(x => x.PeriodKind is RecentObservationPeriodKind.LatestSevenDays or
                RecentObservationPeriodKind.CurrentMonth or
                RecentObservationPeriodKind.CurrentSeason or
                RecentObservationPeriodKind.YearToDate)
            .ToList();
        var thresholdedRangeTiles = result
            .ApplyCompletenessThreshold(1f)
            .Tiles
            .Where(x => x.PeriodKind is RecentObservationPeriodKind.LatestSevenDays or
                RecentObservationPeriodKind.CurrentMonth or
                RecentObservationPeriodKind.CurrentSeason or
                RecentObservationPeriodKind.YearToDate)
            .ToList();

        Assert.HasCount(3, baseRangeTiles);
        Assert.IsTrue(baseRangeTiles.All(x => x.HasComparison));
        Assert.IsTrue(thresholdedRangeTiles.All(x => !x.HasComparison));
        Assert.IsTrue(thresholdedRangeTiles.All(x => !string.IsNullOrWhiteSpace(x.Note)));
    }

    [TestMethod]
    public async Task PeriodSelectionCreatesAddButtonLabelsFromGeneratedTiles()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();

        Assert.IsFalse(tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
        Assert.AreEqual("Add Yesterday", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
        Assert.AreEqual("Add May 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousMonth, tiles, "month"));
        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddSeasonLabelStartsWithMostRecentlyCompletedSeasonWhenSeasonToDateIsHidden()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var seasonOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousSeason);

        Assert.IsFalse(tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Summer 2025-26", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Spring 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelsCrossCalendarYears()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var monthOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousMonth);

        for (var i = 0; i < 5; i++)
        {
            selection.AddEarlierMonth(monthOffsets);
        }

        Assert.AreEqual("Add December 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousMonth, tiles, "month"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelsCrossSeasonYearBoundaries()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var seasonOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousSeason);

        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Summer 2025-26", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Spring 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelUpdatesAfterRemovalAndAddition()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var dayOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.Daily);

        selection.AddEarlierDay(dayOffsets);
        selection.AddEarlierDay(dayOffsets);

        Assert.AreEqual("Add 11 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));

        selection.Remove(GetTile(tiles, RecentObservationPeriodKind.Daily, 3));

        Assert.AreEqual("Add 12 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));

        selection.AddEarlierDay(dayOffsets);

        Assert.AreEqual("Add 11 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelRetainsLastValidLabelAtMaximum()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var dayOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.Daily);

        for (var i = 0; i < 20; i++)
        {
            selection.AddEarlierDay(dayOffsets);
        }

        Assert.IsFalse(selection.CanAddEarlierDay(dayOffsets));
        Assert.AreEqual("Add 8 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
    }

    [TestMethod]
    public void PeriodSelectionMarksOnlyDynamicTilesRemovable()
    {
        var selection = new RecentObservationPeriodSelection();
        var currentDay = CreateTile(RecentObservationPeriodKind.Daily, 1, "Today");
        var previousDay = CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday");
        var latestSevenDays = CreateTile(RecentObservationPeriodKind.LatestSevenDays, null, "Latest 7 days");
        var currentMonth = CreateTile(RecentObservationPeriodKind.CurrentMonth, null, "June 2026 to date");
        var previousMonth = CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026");
        var currentSeason = CreateTile(RecentObservationPeriodKind.CurrentSeason, null, "Winter to Date");
        var previousSeason = CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");
        var yearToDate = CreateTile(RecentObservationPeriodKind.YearToDate, null, "2026 to date");

        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierSeason();

        Assert.IsFalse(selection.IsRemovable(currentDay));
        Assert.IsTrue(selection.IsRemovable(previousDay));
        Assert.IsFalse(selection.IsRemovable(latestSevenDays));
        Assert.IsFalse(selection.IsRemovable(currentMonth));
        Assert.IsTrue(selection.IsRemovable(previousMonth));
        Assert.IsFalse(selection.IsRemovable(currentSeason));
        Assert.IsTrue(selection.IsRemovable(previousSeason));
        Assert.IsFalse(selection.IsRemovable(yearToDate));
    }

    [TestMethod]
    public void PeriodSelectionRemovesSpecificDynamicTiles()
    {
        var selection = new RecentObservationPeriodSelection();
        var previousDay = CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday");
        var previousMonth = CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026");
        var previousSeason = CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");

        selection.AddEarlierDay();
        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.AddEarlierSeason();

        selection.Remove(previousDay);
        selection.Remove(previousMonth);
        selection.Remove(previousSeason);

        Assert.IsFalse(selection.IsVisible(previousDay));
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 3, "12 June")));
        Assert.IsFalse(selection.IsVisible(previousMonth));
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousMonth, 2, "April 2026")));
        Assert.IsFalse(selection.IsVisible(previousSeason));
    }

    [TestMethod]
    public void PeriodSelectionAddAfterRemoveContinuesWithNextEarlierAvailablePeriod()
    {
        var selection = new RecentObservationPeriodSelection();

        selection.AddEarlierDay();
        selection.AddEarlierDay();
        selection.Remove(CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday"));
        selection.AddEarlierDay();

        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"));
        selection.AddEarlierMonth();

        selection.AddEarlierSeason();
        selection.AddEarlierSeason();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026"));
        selection.AddEarlierSeason();

        CollectionAssert.AreEqual(
            new[] { "Today", "12 June", "11 June" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.Daily, 4)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "PreviousMonth 2", "PreviousMonth 3" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.PreviousMonth, 3)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "PreviousSeason 2", "PreviousSeason 3" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.PreviousSeason, 3)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
    }

    [TestMethod]
    public void PeriodSelectionRespectsMaxAddLimits()
    {
        var selection = new RecentObservationPeriodSelection();

        for (var i = 0; i < 20; i++)
        {
            selection.AddEarlierDay();
            selection.AddEarlierMonth();
            selection.AddEarlierSeason();
        }

        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousDayCount, selection.PreviousDayCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousMonthCount, selection.PreviousMonthCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousSeasonCount, selection.PreviousSeasonCount);
        Assert.IsTrue(selection.IsAddEarlierDayDisabled);
        Assert.IsTrue(selection.IsAddEarlierMonthDisabled);
        Assert.IsTrue(selection.IsAddEarlierSeasonDisabled);
        Assert.IsFalse(selection.CanAddEarlierDay(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierMonth(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierSeason(Enumerable.Range(1, 20)));
    }

    [TestMethod]
    public void PeriodSelectionKeepsFilteredTileOrder()
    {
        var selection = new RecentObservationPeriodSelection();
        var tiles = new[]
        {
            CreateTile(RecentObservationPeriodKind.Daily, 1, "Today"),
            CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday"),
            CreateTile(RecentObservationPeriodKind.Daily, 3, "12 June"),
            CreateTile(RecentObservationPeriodKind.LatestSevenDays, null, "Latest 7 days"),
            CreateTile(RecentObservationPeriodKind.CurrentMonth, null, "June 2026 to date"),
            CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"),
            CreateTile(RecentObservationPeriodKind.PreviousMonth, 2, "April 2026"),
            CreateTile(RecentObservationPeriodKind.CurrentSeason, null, "Winter to Date"),
            CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026"),
            CreateTile(RecentObservationPeriodKind.YearToDate, null, "2026 to date"),
        };

        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"));
        selection.AddEarlierSeason();

        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Yesterday",
                "Latest 7 days",
                "June 2026 to date",
                "April 2026",
                "Winter to Date",
                "Autumn 2026",
                "2026 to date",
            },
            tiles.Where(selection.IsVisible).Select(x => x.PeriodTitle).ToArray());
    }

    [TestMethod]
    public void PeriodSelectionDefaultsToOneCurrentDayAndNoDynamicMonthsOrSeasons()
    {
        var selection = new RecentObservationPeriodSelection();

        Assert.AreEqual(1, selection.PreviousDayCount);
        Assert.AreEqual(0, selection.PreviousMonthCount);
        Assert.AreEqual(0, selection.PreviousSeasonCount);
        Assert.IsFalse(selection.IsAddEarlierDayDisabled);
        Assert.IsFalse(selection.IsAddEarlierMonthDisabled);
        Assert.IsFalse(selection.IsAddEarlierSeasonDisabled);
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 1, "Today")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026")));
    }

    [TestMethod]
    public async Task ExpandedTilesExposePeriodAndDailyExtremesMetricGroups()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        CollectionAssert.AreEqual(new[] { "period", "daily-extremes" }, latestSevenDays.MetricGroups.Select(x => x.Key).ToArray());
        CollectionAssert.AreEqual(new[] { "7 Days", "Daily extremes" }, latestSevenDays.MetricGroups.Select(x => x.Title).ToArray());

        var period = latestSevenDays.MetricGroups.Single(x => x.Key == "period");
        CollectionAssert.AreEqual(new[] { "Total precipitation" }, period.Metrics.Select(x => x.Label).ToArray());
        Assert.AreEqual("7mm", period.Metrics[0].CurrentValue);

        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == "daily-extremes");
        CollectionAssert.AreEqual(new[] { "Highest daily precipitation" }, dailyExtremes.Metrics.Select(x => x.Label).ToArray());
        Assert.AreEqual("1mm", dailyExtremes.Metrics[0].CurrentValue);
    }

    [TestMethod]
    public async Task ExpandedTilesUseConcisePeriodMetricGroupLabels()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 6,
            previousSeasonCount: 2);

        AssertMetricGroupLabel(result, RecentObservationPeriodKind.LatestSevenDays, null, "7 Days");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.CurrentMonth, null, "June");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousMonth, 1, "May");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousMonth, 6, "December");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousSeason, 2, "Summer 2025-26");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.YearToDate, null, "2026");
    }

    [TestMethod]
    public async Task DailyTilesKeepSingleMetricGroupSoExpandedToggleIsHidden()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 3,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();

        Assert.HasCount(3, dailyTiles);
        Assert.IsTrue(dailyTiles.All(x => x.MetricGroups.Count == 1));
        Assert.IsTrue(dailyTiles.All(x => x.MetricGroups[0].Key == "day"));
    }

    [TestMethod]
    public async Task ExpandedTilesOmitRecordsWhenNoHistoryIsAvailable()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var dayGroup = dailyTile.MetricGroups.Single();
        Assert.AreEqual("day", dayGroup.Key);
        var total = dayGroup.Metrics.Single();

        Assert.AreEqual("Precipitation", total.Label);
        Assert.AreEqual("1mm", total.CurrentValue);
        Assert.IsFalse(total.HasRecords);
        Assert.IsNull(total.RecordHigh);
        Assert.IsNull(total.RecordLow);
        Assert.IsNull(total.RankText);
        Assert.AreEqual(RecentObservationRecordStatus.None, total.RecordStatus);
    }

    [TestMethod]
    public async Task PrecipitationDailyExtremesDetectHistoricalRecords()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        // Recent 7-day total is 7mm, below every historical equivalent period
        // (min 77mm in 2000), so it is a new low record (driest).
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == "period").Metrics.Single();
        Assert.AreEqual("7mm", total.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, total.RecordStatus);
        Assert.IsNull(total.RankText);
        Assert.AreEqual("252mm", total.RecordHigh!.Value);
        Assert.AreEqual("2025", total.RecordHigh.Year);
        Assert.AreEqual("77mm", total.RecordLow!.Value);
        Assert.AreEqual("2000", total.RecordLow.Year);

        var highestDaily = latestSevenDays.MetricGroups.Single(x => x.Key == "daily-extremes").Metrics.Single();
        Assert.AreEqual("1mm", highestDaily.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, highestDaily.RecordStatus);
        Assert.AreEqual("39mm", highestDaily.RecordHigh!.Value);
        Assert.AreEqual("2025", highestDaily.RecordHigh.Year);
        Assert.AreEqual("14mm", highestDaily.RecordLow!.Value);
        Assert.AreEqual("2000", highestDaily.RecordLow.Year);
    }

    [TestMethod]
    public async Task TemperatureMetricGroupsComputeCurrentValuesAndRecords()
    {
        var historicalMax = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (_, date) => date.Day);
        var historicalMin = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (_, date) => date.Day - 3);
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: date => date.Day + 20,
            recentMin: date => date.Day,
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        var period = latestSevenDays.MetricGroups.Single(x => x.Key == "period");
        CollectionAssert.AreEqual(
            new[] { "Average maximum temperature", "Average minimum temperature", "Mean temperature" },
            period.Metrics.Select(x => x.Label).ToArray());

        var avgMax = period.Metrics.Single(x => x.Label == "Average maximum temperature");
        Assert.AreEqual("31.0°C", avgMax.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, avgMax.RecordStatus);
        Assert.AreEqual("New record", avgMax.RecordStatusText);
        Assert.IsNull(avgMax.RankText);
        Assert.AreEqual("11.0°C", avgMax.RecordHigh!.Value);
        Assert.AreEqual("2000", avgMax.RecordHigh.Year);

        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == "daily-extremes");
        CollectionAssert.AreEqual(
            new[] { "Highest daily maximum", "Lowest daily maximum", "Highest daily minimum", "Lowest daily minimum" },
            dailyExtremes.Metrics.Select(x => x.Label).ToArray());

        var highestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Highest daily maximum");
        Assert.AreEqual("34.0°C", highestDailyMax.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, highestDailyMax.RecordStatus);
        Assert.AreEqual("14.0°C", highestDailyMax.RecordHigh!.Value);

        // The period's coolest daily maximum (28°C) is warmer than any historical
        // coolest daily maximum (8°C) — a record at the high end, the very case the
        // old single-direction "102nd lowest of 102" wording got wrong.
        var lowestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Lowest daily maximum");
        Assert.AreEqual("28.0°C", lowestDailyMax.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, lowestDailyMax.RecordStatus);
        Assert.IsNull(lowestDailyMax.RankText);
        Assert.AreEqual("8.0°C", lowestDailyMax.RecordHigh!.Value);

        var lowestDailyMin = dailyExtremes.Metrics.Single(x => x.Label == "Lowest daily minimum");
        Assert.AreEqual("8.0°C", lowestDailyMin.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, lowestDailyMin.RecordStatus);
        Assert.AreEqual("5.0°C", lowestDailyMin.RecordHigh!.Value);
    }

    [TestMethod]
    public async Task DailyTemperatureTileShowsSingleDayMaximumMinimumAndMean()
    {
        // A single day has a maximum, a minimum and a mean — not aggregates across
        // days — so the daily tile has one group (no Period / Daily extremes toggle),
        // each metric compared against the all-time record for that calendar date.
        var historicalMax = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (year, _) => year - 2000);
        var historicalMin = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (year, _) => year - 2040);
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: date => date.Day == 14 ? 30 : date.Day,
            recentMin: date => date.Day == 14 ? -50 : date.Day - 10,
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var daily = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);

        // Single group → the UI hides the Period / Daily extremes toggle.
        var day = daily.MetricGroups.Single();
        Assert.AreEqual("day", day.Key);
        CollectionAssert.AreEqual(
            new[] { "Maximum", "Minimum", "Mean" },
            day.Metrics.Select(x => x.Label).ToArray());

        // Each stat shows ONE rank for the observed value (or a New/Equal record badge
        // at an extreme) plus the record high and record low as plain reference values.
        // Today's max (30°C) beats the record high (25°C, 2025) → a new record.
        var maximum = day.Metrics.Single(x => x.Label == "Maximum");
        Assert.AreEqual("30.0°C", maximum.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, maximum.RecordStatus);
        Assert.AreEqual("New record", maximum.RecordStatusText);
        Assert.IsNull(maximum.RankText);
        Assert.AreEqual("Record high", maximum.RecordHigh!.Label);
        Assert.AreEqual("25.0°C", maximum.RecordHigh.Value);
        Assert.AreEqual("2025", maximum.RecordHigh.Year);
        Assert.AreEqual("Record low", maximum.RecordLow!.Label);
        Assert.AreEqual("0.0°C", maximum.RecordLow.Value);
        Assert.AreEqual("2000", maximum.RecordLow.Year);

        // Today's min (-50°C) is below the record low (-40°C, 2000) → a new record.
        var minimum = day.Metrics.Single(x => x.Label == "Minimum");
        Assert.AreEqual("-50.0°C", minimum.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, minimum.RecordStatus);
        Assert.IsNull(minimum.RankText);
        Assert.AreEqual("-15.0°C", minimum.RecordHigh!.Value);
        Assert.AreEqual("-40.0°C", minimum.RecordLow!.Value);
        Assert.AreEqual("2000", minimum.RecordLow.Year);

        // Today's mean (-10°C) sits between the extremes, nearer the low end, so it
        // ranks lowest-first as a single rank; the records are plain references.
        var mean = day.Metrics.Single(x => x.Label == "Mean");
        Assert.AreEqual("-10.0°C", mean.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.None, mean.RecordStatus);
        Assert.AreEqual("11th lowest of 27", mean.RankText);
        Assert.AreEqual("5.0°C", mean.RecordHigh!.Value);
        Assert.AreEqual("-20.0°C", mean.RecordLow!.Value);
    }

    [TestMethod]
    public async Task CompletenessThresholdStripsExpandedRecords()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == "period").Metrics.Single();

        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.IsFalse(total.HasRecords);
        Assert.IsNull(total.RecordHigh);
        Assert.IsNull(total.RecordLow);
        Assert.IsNull(total.RankText);
        Assert.AreEqual(RecentObservationRecordStatus.None, total.RecordStatus);
        Assert.IsNotNull(total.CurrentValue);
    }

    private static RecentObservationsService CreateTemperatureServiceWithExtremes(
        Func<DateOnly, double> recentMax,
        Func<DateOnly, double> recentMin,
        List<DataRecord> historicalMax,
        List<DataRecord> historicalMin)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.TempMax, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), recentMax),
            });
        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.TempMin, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), recentMin),
            });
        dataService
            .Setup(x => x.GetClimateRecords(
                LocationId,
                DataType.TempMax,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMax, DataAdjustment.Unadjusted, historicalMax));
        dataService
            .Setup(x => x.GetClimateRecords(
                LocationId,
                DataType.TempMin,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMin, DataAdjustment.Unadjusted, historicalMin));

        return CreateRecentObservationsService(dataService);
    }

    private static List<DataRecord> CreateHistoricalDailyValues(
        DateOnly templateStart,
        DateOnly templateEnd,
        Func<int, DateOnly, double> getValue,
        int startYear = 2000,
        int endYear = 2025)
    {
        var records = new List<DataRecord>();
        for (var year = startYear; year <= endYear; year++)
        {
            for (var day = templateStart.Day; day <= templateEnd.Day; day++)
            {
                var date = new DateOnly(year, templateStart.Month, day);
                records.Add(new DataRecord(date, getValue(year, date)));
            }
        }

        return records;
    }

    private static async Task<List<RecentObservationTileViewModel>> GetGeneratedTiles(RecentObservationsService service)
    {
        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: RecentObservationPeriodSelection.MaximumPreviousMonthCount,
            previousSeasonCount: RecentObservationPeriodSelection.MaximumPreviousSeasonCount);

        return result.Tiles;
    }

    private static void AssertMetricGroupLabel(
        RecentObservationsTabResult result,
        RecentObservationPeriodKind periodKind,
        int? periodOffset,
        string expectedLabel)
    {
        var tile = result.Tiles.Single(x => x.PeriodKind == periodKind && (!periodOffset.HasValue || x.PeriodOffset == periodOffset));
        var periodGroup = tile.MetricGroups.Single(x => x.Key == "period");

        Assert.AreEqual(expectedLabel, tile.MetricGroupLabel, $"Unexpected tile metric group label for {tile.PeriodTitle}.");
        Assert.AreEqual(expectedLabel, periodGroup.Title, $"Unexpected period metric group title for {tile.PeriodTitle}.");
    }

    private static IEnumerable<int> GetAvailableOffsets(
        IEnumerable<RecentObservationTileViewModel> tiles,
        RecentObservationPeriodKind periodKind)
    {
        return tiles
            .Where(tile => tile.PeriodKind == periodKind && tile.PeriodOffset.HasValue)
            .Select(tile => tile.PeriodOffset!.Value)
            .Order();
    }

    private static RecentObservationTileViewModel GetTile(
        IEnumerable<RecentObservationTileViewModel> tiles,
        RecentObservationPeriodKind periodKind,
        int periodOffset)
    {
        return tiles.Single(tile => tile.PeriodKind == periodKind && tile.PeriodOffset == periodOffset);
    }

    private static IEnumerable<RecentObservationTileViewModel> CreateOrderedDynamicTiles(RecentObservationPeriodKind periodKind, int count)
    {
        for (var offset = 1; offset <= count; offset++)
        {
            var title = periodKind == RecentObservationPeriodKind.Daily
                ? offset switch
                {
                    1 => "Today",
                    2 => "Yesterday",
                    3 => "12 June",
                    4 => "11 June",
                    _ => $"{15 - offset} June",
                }
                : $"{periodKind} {offset}";

            yield return CreateTile(periodKind, offset, title);
        }
    }

    private static RecentObservationTileViewModel CreateTile(RecentObservationPeriodKind periodKind, int? periodOffset, string periodTitle)
    {
        var startDate = periodOffset.HasValue
            ? new DateOnly(2026, 6, Math.Max(1, 15 - periodOffset.Value))
            : new DateOnly(2026, 6, 14);

        return new RecentObservationTileViewModel
        {
            PeriodKind = periodKind,
            PeriodOffset = periodOffset,
            PeriodStartDate = startDate,
            PeriodEndDate = startDate,
            PeriodTitle = periodTitle,
        };
    }

    private static RecentObservationsService CreateService(
        DateOnly? recentStartDate = null,
        DateOnly? recentEndDate = null,
        List<DataRecord>? historicalRecords = null,
        Func<DateOnly, bool>? includeRecentRecord = null,
        DateOnly? today = null)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.Precipitation, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(
                    recentStartDate ?? new DateOnly(2025, 7, 1),
                    recentEndDate ?? new DateOnly(2026, 6, 14),
                    _ => 1d,
                    includeRecentRecord),
            });

        if (historicalRecords is not null)
        {
            dataService
                .Setup(x => x.GetClimateRecords(
                    LocationId,
                    DataType.Precipitation,
                    null,
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    false,
                    It.IsAny<int?>()))
                .ReturnsAsync(CreateClimateRecordsResponse(DataType.Precipitation, null, historicalRecords));
        }

        return CreateRecentObservationsService(dataService, today);
    }

    private static RecentObservationsService CreateTemperatureService(
        List<DataRecord> historicalRecords,
        Func<DateOnly, bool>? includeMaxRecord = null,
        Func<DateOnly, bool>? includeMinRecord = null)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.TempMax, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), _ => 20d, includeMaxRecord),
            });
        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.TempMin, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), _ => 10d, includeMinRecord),
            });
        dataService
            .Setup(x => x.GetClimateRecords(
                LocationId,
                DataType.TempMean,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMean, DataAdjustment.Unadjusted, historicalRecords));

        return CreateRecentObservationsService(dataService);
    }

    private static void SetupEmptyClimateRecords(Mock<IDataService> dataService)
    {
        dataService
            .Setup(x => x.GetClimateRecords(
                It.IsAny<Guid>(),
                It.IsAny<DataType>(),
                It.IsAny<DataAdjustment?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>()))
            .ReturnsAsync(new ClimateRecordsResponse());
    }

    private static RecentObservationsService CreateRecentObservationsService(Mock<IDataService> dataService, DateOnly? today = null)
    {
        var currentDate = today ?? new DateOnly(2026, 6, 14);

        return new RecentObservationsService(
            dataService.Object,
            new FixedTimeProvider(new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day, 12, 0, 0, TimeSpan.Zero)));
    }

    private static ClimateRecordsResponse CreateClimateRecordsResponse(DataType dataType, DataAdjustment? dataAdjustment, List<DataRecord> records)
    {
        return new ClimateRecordsResponse
        {
            DataType = dataType,
            DataAdjustment = dataAdjustment,
            DataResolution = DataResolution.Daily,
            Records = records,
            StartYear = records.Min(x => (int)x.Year),
            EndYear = records.Max(x => (int)x.Year),
            TotalCount = records.Count,
        };
    }

    private static List<DataRecord> CreateDailyRecords(DateOnly startDate, DateOnly endDate)
    {
        return CreateDailyRecords(startDate, endDate, _ => 1d);
    }

    private static List<DataRecord> CreateDailyRecords(
        DateOnly startDate,
        DateOnly endDate,
        Func<DateOnly, double> getValue,
        Func<DateOnly, bool>? includeRecord = null)
    {
        var records = new List<DataRecord>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (includeRecord is not null && !includeRecord(date))
            {
                continue;
            }

            records.Add(new DataRecord(date, getValue(date)));
        }

        return records;
    }

    private static List<DataRecord> CreateHistoricalSameDateRecords(DateOnly templateDate, int startYear, int endYear, double valueOffset)
    {
        var records = new List<DataRecord>();
        for (var year = startYear; year <= endYear; year++)
        {
            var date = new DateOnly(year, templateDate.Month, templateDate.Day);
            records.Add(new DataRecord(date, valueOffset + year - startYear + 1));
        }

        return records;
    }

    private static List<DataRecord> CreateHistoricalRangeRecords(
        DateOnly templateStart,
        DateOnly templateEnd,
        int startYear = 2000,
        int endYear = 2025,
        double valueOffset = 0d)
    {
        var records = new List<DataRecord>();
        for (var year = startYear; year <= endYear; year++)
        {
            var startDate = new DateOnly(year, templateStart.Month, templateStart.Day);
            var endDate = new DateOnly(year, templateEnd.Month, templateEnd.Day);
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                records.Add(new DataRecord(date, valueOffset + year - startYear + date.Day));
            }
        }

        return records;
    }

    private static Location CreateSouthernHemisphereLocation()
    {
        return CreateLocation(-35.3d);
    }

    private static Location CreateLocation(double latitude)
    {
        return new Location
        {
            Id = LocationId,
            Name = latitude < 0d ? "Canberra" : "New York",
            CountryCode = latitude < 0d ? "AU" : "US",
            Coordinates = new Coordinates(latitude, latitude < 0d ? 149.1d : -74.0d),
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
